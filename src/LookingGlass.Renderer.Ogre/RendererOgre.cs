﻿/* Copyright (c) Robert Adams
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of the copyright holder may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Forms;     // used for the Keys class
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Statistics;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.Renderer;
using LookingGlass.Rest;
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Renderer.Ogr {

public class RendererOgre : ModuleBase, IRenderProvider {
    public ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
    private ILog m_logOgre = LogManager.GetLogger("RendererCpp");

    // we decorate IEntities with SceneNodes. This is our slot in the IEntity addition table
    // SceneNode of the entity itself
    public static int AddSceneNodeName;
    public static string AddSceneNodeNameName = "OgreSceneNodeName";
    public static string GetSceneNodeName(IEntity ent) {
        return (string)ent.Addition(RendererOgre.AddSceneNodeName);
    }
    // SceneNode of the region if this is a IRegionContext
    public static int AddRegionSceneNode;
    public static string AddRegionSceneNodeName = "OgreRegionSceneNode";
    public static OgreSceneNode GetRegionSceneNode(IEntity ent) {
        return (OgreSceneNode)ent.Addition(RendererOgre.AddRegionSceneNode);
    }

    protected IUserInterfaceProvider m_userInterface = null;

    public OgreSceneMgr m_sceneMgr;

    protected Thread m_rendererThread = null;

    // If true, requesting a mesh causes the mesh to be rebuilt and written out
    // This makes sure cached copy is the same as server but is also slow
    protected bool m_shouldForceMeshRebuild = false;
    // True if meshes with the same characteristics should be shared
    protected bool m_shouldShareMeshes = true;
    // True if to make sure the mesh exists before creating it's scene node
    protected bool m_shouldPrebuildMesh = false;

    // this shouldn't be here... this is a feature of the LL renderer
    protected float m_sceneMagnification;
    public float SceneMagnification { get { return m_sceneMagnification; } }

    // we remember where the camera last was so we can do some interest management
    private OMV.Vector3d m_lastCameraPosition;
    private OMV.Quaternion m_lastCameraOrientation;

    protected BasicWorkQueue m_workQueueRender = new BasicWorkQueue("OgreRendererRender");
    protected BasicWorkQueue m_workQueueReqMesh = new BasicWorkQueue("OgreRendererRequestMesh");
    protected BasicWorkQueue m_workQueueReqMaterial = new BasicWorkQueue("OgreRendererRequestMaterial");
    protected BasicWorkQueue m_workQueueReqTexture = new BasicWorkQueue("OgreRendererRequestTexture");
    protected OnDemandWorkQueue m_betweenFramesQueue = new OnDemandWorkQueue("OgreBetweenFrames");
    private static int m_betweenFrameTotalCost = 300;

    // private Thread m_rendererThread = null;
    private bool m_shouldRenderOnMainThread = false;

    private RestHandler m_restHandler;
    private StatisticManager m_stats;
    private ICounter m_statMaterialsRequested;
    private ICounter m_statMeshesRequested;
    private ICounter m_statTexturesRequested;
    private ICounter m_statSharableTotal;
    private ICounter m_statShareInstances;

    private RestHandler m_ogreStatsHandler;
    private ParameterSet m_ogreStats;
    private int[] m_ogreStatsPinned;
    private GCHandle m_ogreStatsHandle;

    // ==========================================================================
    public RendererOgre() {
    }

    #region IModule
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.InputSystem.Name", 
                    "OgreUI",
                    "Module to handle user IO on the rendering screen");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Name", "LookingGlass",
                    "Name of the Ogre resources to load");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SkyboxName", "LookingGlass/CloudyNoonSkyBox",
                    "Name of the skybox resource to use");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.ShadowTechnique", "none",
                    "Shadow technique: none, texture-additive, texture-modulative, stencil-modulative, stencil-additive");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.ShadowFarDistance", "100",
                    "Integer units of distance within which to do shadows (mul by magnification)");
        // cp.AddParameter(m_moduleName + ".Ogre.Renderer", "Direct3D9 Rendering Subsystem",
        //             "Name of the rendering subsystem to use");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Renderer", "OpenGL Rendering Subsystem",
                    "Name of the rendering subsystem to use");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.VideoMode", "800 x 600@ 32-bit colour",
                    "Initial window size");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.FramePerSecMax", "30",
                    "Maximum number of frames to display per second");
        ModuleParams.AddDefaultParameter(m_moduleName + ".ShouldRenderOnMainThread", "false",
                    "True if ogre rendering otherwise someone has to call RenderOneFrame");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.PluginFilename", "plugins.cfg",
                    "File that lists Ogre plugins to load");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.ResourcesFilename", "resources.cfg",
                    "File that lists the Ogre resources to load");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultNumMipmaps", "4",
                    "Default number of mip maps created for a texture (usually 6)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.CacheDir", Utilities.GetDefaultApplicationStorageDir(null),
                    "Directory to store cached meshs, textures, etc");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.PreLoadedDir", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./LookingGlassResources/Preloaded/"),
                    "Directory to for preloaded textures, etc");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTerrainMaterial",
                    "LookingGlass/DefaultTerrainMaterial",
                    "Material applied to terrain");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Ocean.Processor",
                    "none",
                    "The processing routine to create the ocean. Either 'none' or 'hydrax'");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.OceanMaterialName",
                    "LookingGlass/Ocean",
                    "The ogre name of the ocean texture");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultMeshFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./LookingGlassResources/LoadingShape.mesh"),
                    "Filename of the default shape found in the cache dir");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTextureFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./LookingGlassResources/LoadingTexture.png"),
                    "Filename of the default texture found in the cache dir");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTextureResourceName", 
                    "LoadingTexture.png",
                    "Resource name of  the default texture");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.WhiteTextureResourceName", 
                    "Preload/" + OMV.Primitive.TextureEntry.WHITE_TEXTURE.ToString().Substring(0,1) 
                            + "/" + OMV.Primitive.TextureEntry.WHITE_TEXTURE.ToString(),
                    "Resource name of a white texture used as default base color");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.SceneMagnification", "1",
                    "Magnification of LL coordinates into Ogre space");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.RenderInfoMaterialCreate", "true",
                    "Create materials while gathering mesh generation info (earlier than mesh creation)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.EarlyMaterialCreate", "false",
                    "Create materials while creating mesh rather than waiting");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.DefaultAvatarMesh", 
                    "Preload/00000000-0000-2222-3333-112200000003",
                    "Entity name of mesh to use for avatars");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.WorkMilliSecondsMax", "300",
                    "Cost of queued C++ work items to do between each frame");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.Total", "200",
                    "The total cost of C# operations to do between each frame");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SerializeMaterials", "false",
                    "Write out materials to files (replace with DB someday)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SerializeMeshes", "true",
                    "Write out meshes to files");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.ForceMeshRebuild", "false",
                    "True if to force the generation a mesh when first rendered (don't rely on cache)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.PrebuildMesh", "true",
                    "True if to make sure the mesh exists before creating the scene node");
        ModuleParams.AddDefaultParameter(m_moduleName + ".ShouldShareMeshes", "true",
                    "True if to share meshes with similar characteristics");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.UseShaders", "true",
                    "Whether to use the new technique of using GPU shaders");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.CollectOgreStats", "true",
                    "Whether to collect detailed Ogre stats and make available to web");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Sky", "Default",
                    "Name of the key system to use");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SkyX.LightingHDR", "true",
                    "Use high resolution lighting shaders");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Ambient.Scene", "<0.4,0.4,0.4>",
                    "color value for scene initial ambient lighting");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Ambient.Material", "<0.4,0.4,0.4>",
                    "color value for material ambient lighting");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Sun.Color", "<1.0,1.0,1.0>",
                    "Color of light from the sun at noon");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Moon.Color", "<0.5,0.5,0.6>",
                    "Color of light from the moon");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Processor", "FrustrumDistance",
                    "Name of the culling plugin to use ('FrustrumDistance', 'VariableFrustDist', 'none')");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Cull.Frustrum", "false",
                    "whether to cull (unload) objects if not visible in camera frustrum");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Cull.Distance", "true",
                    "whether to cull (unload) objects depending on distance from camera");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Cull.Meshes", "true",
                    "unload culled object meshes");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Cull.Textures", "true",
                    "unload culled textures");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.MaxDistance", "200",
                    "the maximum distance to see any entites (far clip)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.MinDistance", "30",
                    "below this distance, everything is visible");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.OnlyLargeAfter", "120",
                    "After this distance, only large things are visible");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Large", "8",
                    "How big is considered 'large' for 'OnlyLargeAfter' calculation");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.MeshesReloadedPerFrame", "80",
                    "When reloading newly visible meshes, how many to load per frame");

        // some counters and intervals to see how long things take
        m_stats = new StatisticManager(m_moduleName);
        m_statMaterialsRequested = m_stats.GetCounter("MaterialsRequested");
        m_statMeshesRequested = m_stats.GetCounter("MeshesRequested");
        m_statTexturesRequested = m_stats.GetCounter("TexturesRequested");
        m_statSharableTotal = m_stats.GetCounterValue("TotalMeshes", delegate() { return (long)prebuiltMeshes.Count; });
        m_statShareInstances = m_stats.GetCounter("TotalSharedInstances");

        // renderer keeps rendering specific data in an entity's addition/subsystem slots
        AddSceneNodeName = EntityBase.AddAdditionSubsystem(RendererOgre.AddSceneNodeNameName);
        AddRegionSceneNode = EntityBase.AddAdditionSubsystem(RendererOgre.AddRegionSceneNodeName);
    }

    // these exist to keep one managed pointer to the delegates so they don't get garbage collected
    private Ogr.DebugLogCallback debugLogCallbackHandle;
    private Ogr.FetchParameterCallback fetchParameterCallbackHandle;
    private Ogr.CheckKeepRunningCallback checkKeepRunningCallbackHandle;
    private Ogr.UserIOCallback userIOCallbackHandle;
    private Ogr.RequestResourceCallback requestResourceCallbackHandle;
    private Ogr.BetweenFramesCallback betweenFramesCallbackHandle;
    // ==========================================================================
    override public bool AfterAllModulesLoaded() {
        // allow others to get our statistics
        m_restHandler = new RestHandler("/stats/" + m_moduleName + "/detailStats", m_stats);

        #region OGRE STATS
        // Setup the shared piece of memory that Ogre can place statistics in
        m_ogreStatsPinned = new int[Ogr.StatSize];
        if (ModuleParams.ParamBool("Renderer.Ogre.CollectOgreStats")) {
            m_ogreStatsHandle = GCHandle.Alloc(m_ogreStatsPinned, GCHandleType.Pinned);
            Ogr.SetStatsBlock(m_ogreStatsHandle.AddrOfPinnedObject());
            // m_ogreStatsPinned = (int[])Marshal.AllocHGlobal(Ogr.StatSize * 4);
            // Ogr.SetStatsBlock(m_ogreStatsPinned);
        }

        // Create a ParameterSet that can be read externally via REST/JSON
        m_ogreStats = new ParameterSet();
        // add an initial parameter that calculates frames per sec
        m_ogreStats.Add("FramesPerSecond",
            delegate(string xx) {
                // Ogre passed the number *1000 so  there can be some decimal points
                float fps = (float)m_ogreStatsPinned[Ogr.StatFramesPerSec] / 1000f;
                return new OMVSD.OSDString(fps.ToString());
            }, "Frames per second"
        );
        m_ogreStats.Add("LastFrameMS", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatLastFrameMs].ToString()); },
                "Milliseconds used rendering last frame");
        m_ogreStats.Add("TotalFrames", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatTotalFrames].ToString()); },
                "Number of frames rendered");
        m_ogreStats.Add("VisibleToVisible", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatVisibleToVisible].ToString()); },
                "Meshes at were visible that are still visible in last frame");
        m_ogreStats.Add("InvisibleToVisible", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatInvisibleToVisible].ToString()); },
                "Meshes that were invisible that are now visible in last frame");
        m_ogreStats.Add("VisibleToInvisible", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatVisibleToInvisible].ToString()); },
                "Meshes that were visible that are now invisible in last frame");
        m_ogreStats.Add("InvisibleToInvisible", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatInvisibleToInvisible].ToString()); },
                "Meshes that were invisible that are still invisible in last frame");
        m_ogreStats.Add("CullMeshesLoaded", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatCullMeshesLoaded].ToString()); },
                "Total meshes loaded due to unculling");
        m_ogreStats.Add("CullTexturesLoaded", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatCullTexturesLoaded].ToString()); },
                "Total textures loaded due to unculling");
        m_ogreStats.Add("CullMeshesUnloaded", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatCullMeshesUnloaded].ToString()); },
                "Total meshes unloaded due to culling");
        m_ogreStats.Add("CullTexturesUnloaded", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatCullTexturesUnloaded].ToString()); },
                "Total textures unloaded due to culling");
        m_ogreStats.Add("CullMeshesQueuedToLoad", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatCullMeshesQueuedToLoad].ToString()); },
                "Meshes currently queued to load due to unculling");
        // between frame work
        m_ogreStats.Add("BetweenFrameworkItems", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameWorkItems].ToString()); },
                "Number of between frame work items waiting");
        m_ogreStats.Add("BetweenFrameworkDiscardedDups", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameDiscardedDups].ToString()); },
                "Between frame work requests which duplicated existing requests");
        m_ogreStats.Add("TotalBetweenFrameRefreshResource", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameRefreshResource].ToString()); },
                "Number of 'refresh resource' work items queued");
        m_ogreStats.Add("TotalBetweenFrameRemoveSceneNode", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameRemoveSceneNode].ToString()); },
                "Number of 'remove scene node' work items queued");
        m_ogreStats.Add("TotalBetweenFrameCreateMaterialResource", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameCreateMaterialResource].ToString()); },
                "Number of 'create material resource' work items queued");
        m_ogreStats.Add("TotalBetweenFrameCreateMeshResource", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameCreateMeshResource].ToString()); },
                "Number of 'create mesh resource' work items queued");
        m_ogreStats.Add("TotalBetweenFrameCreateMeshScenenode", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameCreateMeshSceneNode].ToString()); },
                "Number of 'create mesh scene node' work items queued");
        m_ogreStats.Add("TotalBetweenFrameAddLoadedMesh", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameAddLoadedMesh].ToString()); },
                "Number of 'add loaded mesh' work items queued");
        m_ogreStats.Add("TotalBetweenframeupdatescenenode", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameUpdateSceneNode].ToString()); },
                "Number of 'update scene node' work items queued");
        m_ogreStats.Add("TotalBetweenFrameUnknownProcess", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameUnknownProcess].ToString()); },
                "Number of work items with unknow process codes");
        m_ogreStats.Add("TotalBetweenFrameTotalProcessed", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatBetweenFrameTotalProcessed].ToString()); },
                "Total number of work items actually processed");
        // material processing queues
        m_ogreStats.Add("MaterialUpdatesRemaining", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatMaterialUpdatesRemaining].ToString()); },
                "Number of material updates waiting");
        // mesh processing queues
        m_ogreStats.Add("MeshTrackerLoadQueued", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatMeshTrackerLoadQueued].ToString()); },
                "Number of mesh loads queued");
        m_ogreStats.Add("MeshTrackerUnloadQueued", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatMeshTrackerUnloadQueued].ToString()); },
                "Number of mesh unloads queued");
        m_ogreStats.Add("MeshTrackerSerializedQueued", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatMeshTrackerSerializedQueued].ToString()); },
                "Number of mesh serializations queued");
        m_ogreStats.Add("MeshTrackerTotalQueued", delegate(string xx) {
                return new OMVSD.OSDString(m_ogreStatsPinned[Ogr.StatMeshTrackerTotalQueued].ToString()); },
                "Total mesh tracker requests queued");

        // make the values accessable from outside
        m_ogreStatsHandler = new RestHandler("/stats/" + m_moduleName + "/ogreStats", m_ogreStats);
        #endregion OGRE STATS

        // Load the input system we're supposed to be using
        // The input system is a module tha we get given the name of. Go find it and link it in.
        String uiModule = ModuleParams.ParamString(m_moduleName + ".Ogre.InputSystem.Name");
        if (uiModule != null && uiModule.Length > 0) {
            try {
                m_log.Log(LogLevel.DRENDER, "Loading UI processor '{0}'", uiModule);
                m_userInterface = (IUserInterfaceProvider)LGB.ModManager.Module(uiModule);
                if (m_userInterface == null) {
                    m_log.Log(LogLevel.DBADERROR, "FATAL: Could not find user interface class {0}", uiModule);
                    return false;
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FATAL: Could not load user interface class {0}: {1}", uiModule, e.ToString());
                return false;
            }
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "Using null user interfare");
            m_userInterface = new UserInterfaceNull();
        }

        // if we are doing detail logging, enable logging by  the LookingGlassOgre code
        if (m_log.WouldLog(LogLevel.DOGREDETAIL)) {
            m_log.Log(LogLevel.DRENDER, "Logging detail high enough to enable unmanaged code log messages");
            debugLogCallbackHandle = new Ogr.DebugLogCallback(OgrLogger);
            Ogr.SetDebugLogCallback(debugLogCallbackHandle);
        }
        // push the callback pointers into the LookingGlassOgre code
        fetchParameterCallbackHandle = new Ogr.FetchParameterCallback(GetAParameter);
        Ogr.SetFetchParameterCallback(fetchParameterCallbackHandle);
        checkKeepRunningCallbackHandle = new Ogr.CheckKeepRunningCallback(CheckKeepRunning);
        Ogr.SetCheckKeepRunningCallback(checkKeepRunningCallbackHandle);

        // link the input devices to and turn on low level IO reception
        if (m_userInterface.NeedsRendererLinkage()) {
            userIOCallbackHandle = new Ogr.UserIOCallback(ReceiveUserIOConv);
            Ogr.SetUserIOCallback(userIOCallbackHandle);
        }

        // handles so unmanaged code can call back to managed code
        requestResourceCallbackHandle = new Ogr.RequestResourceCallback(RequestResource);
        Ogr.SetRequestResourceCallback(requestResourceCallbackHandle);
        betweenFramesCallbackHandle = new Ogr.BetweenFramesCallback(ProcessBetweenFrames);
        Ogr.SetBetweenFramesCallback(betweenFramesCallbackHandle);

        m_sceneMagnification = float.Parse(ModuleParams.ParamString(m_moduleName + ".Ogre.LL.SceneMagnification"));

        m_shouldForceMeshRebuild = ModuleParams.ParamBool(m_moduleName + ".Ogre.ForceMeshRebuild");
        m_shouldPrebuildMesh = ModuleParams.ParamBool(m_moduleName + ".Ogre.PrebuildMesh");
        m_shouldRenderOnMainThread = ModuleParams.ParamBool(m_moduleName + ".ShouldRenderOnMainThread");
        m_shouldShareMeshes = ModuleParams.ParamBool(m_moduleName + ".ShouldShareMeshes");

        // pick up a bunch of parameterized values
        m_betweenFrameTotalCost = ModuleParams.ParamInt(m_moduleName + ".Ogre.BetweenFrame.Costs.Total");

        // start up the Ogre renderer
        try {
            Ogr.InitializeOgre();
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "EXCEPTION INITIALIZING OGRE: {0}", e.ToString());
            return false;
        }
        m_sceneMgr = new OgreSceneMgr(Ogr.GetSceneMgr());

        // if we get here, rendering is set up and running
        return true;
    }

    // do some type conversion of the stuff coming in from the unmanaged code
    private void ReceiveUserIOConv(int type, int param1, float param2, float param3) {
        m_userInterface.ReceiveUserIO((ReceiveUserIOInputEventTypeCode)type, param1, param2, param3);
        return;
    }

    // routine called from unmanaged code to log a message
    private void OgrLogger(string msg) {
        m_logOgre.Log(LogLevel.DOGREDETAIL, msg);
    }
    
    // Called from unmanaged code to get the state of the KeepRunning flag
    // Not used much since the between frame callback also returns the KeepRunning flag
    private bool CheckKeepRunning() {
        return LGB.KeepRunning;
    }

    // Called from unmanaged code to get the value of a parameter
    // Fixed so this doesn't throw but returns a null value. Calling code has to check return value.
    private string GetAParameter(string parm) {
        string ret = null;
        paramErrorType oldtype = ModuleParams.ParamErrorMethod;
        ModuleParams.ParamErrorMethod = paramErrorType.eException;
        try {
            ret = ModuleParams.ParamString(parm);
        }
        catch {
            m_log.Log(LogLevel.DBADERROR, "GetAParameter: returning default value for parameter {0}", parm);
            ret = "";
        }
        ModuleParams.ParamErrorMethod = oldtype;
        return ret;
    }

    // ==========================================================================
    // IModule.Start()
    override public void Start() {
        // NOTE: this doesn't work any more as Main uses a forms window
        if (m_shouldRenderOnMainThread) {
            m_log.Log(LogLevel.DRENDER, "Start: requesting main thread");
            LGB.GetMainThread(RendererThread);
        }
        return;
    }

    // ==========================================================================
    // IModule.Stop()
    override public void Stop() {
        if (m_userInterface != null) {
            m_userInterface.Dispose();
            m_userInterface = null;
        }
        return;
    }

    // ==========================================================================
    // IModule.PrepareForUnload()
    override public bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    #region IRenderProvider
    public IUserInterfaceProvider UserInterface {
        get { return m_userInterface; }
    }

    // Given the main thread. Run and then say we're all done.
    public bool RendererThread() {
        m_log.Log(LogLevel.DRENDER, "RendererThread: have main thread");
        /*
        // Try creating a thread just for the renderer
        // create a thread for the renderer
        // NOTE: this does not work as the display thread needs to be the main window thread
        m_rendererThread = new Thread(RunRenderer);
        m_rendererThread.Name = "Renderer";
        m_rendererThread.Start();
        // tell the caller I'm not using the main thread
        return false;
         */

        // Let the Ogre code decide if it needs the main thread
        return Ogr.RenderingThread();

        /*
        // HISTORICAL NOTE: origionally this was done here in managed space
        //  but all the between frame work (except terrain) was moved into unmanaged code
        //  This code should be deleted someday.
        // code  to keep the rendering thread mostly in managed space
        // periodically call to draw a frame
        int maxFramePerSec = ModuleParams.ParamInt(m_moduleName + ".Ogre.FramePerSecMax");
        int msPerFrame = 1000 / maxFramePerSec;
        int frameStart, frameEnd, frameDuration, frameLeft;
        while (LGB.KeepRunning) {
            frameStart = System.Environment.TickCount;
            if (!Ogr.RenderOneFrame(true, 100)) {
                LGB.KeepRunning = false;
            }
            while (true) {
                frameEnd = System.Environment.TickCount;
                frameDuration = frameEnd - frameStart;
                frameLeft = msPerFrame - frameDuration;
                if (frameLeft < 10) break;
                if (IsProcessBetweenFramesWork()) {
                    ProcessBetweenFrames(m_betweenFrameMinTotalCost);
                }
                else {
                    Thread.Sleep(frameLeft);
                    break;
                }
            }
        }
        return false;
         */
    }

    public bool RenderOneFrame(bool pump, int len) {
        return Ogr.RenderOneFrame(pump, len);
    }

    // pass the thread into the renderer
    private void RunRenderer() {
        Ogr.RenderingThread();
        return;
    }

    // ==========================================================================
    // IRenderProvider.Render()
    public void Render(IEntity ent) {
        // do we have a format converted for this entity?
        IWorldRenderConv conver;
        if (!ent.TryGet<IWorldRenderConv>(out conver)) {
            // for the moment, we only know about LL stuff
            // TODO: Figure out how to make this dynamic, extendable and runtime
            ent.RegisterInterface<IWorldRenderConv>(RendererOgreLL.Instance);
        }
        // Depending on type, add a creation/management interface
        RenderPrim rprim;
        if (!ent.TryGet<RenderPrim>(out rprim)) {
            ent.RegisterInterface<RenderPrim>(new RenderPrim(this));
        }
        // We don't create the entity here because an update immediately follows the 'new'
        //    call. That update will create the entity with the new values.
        // DoRenderQueued(ent);
        return;
    }

    // wrapper routine for the queuing if rendering work for this entity
    private void DoRenderQueued(IEntity ent) {
        // NOTE: we pass extra parameters that are later modified by DoRenderLater to remember
        // state (RenderableInfo and if mesh has already been rebuilt)
        Object[] renderParameters = { ent, null, false };
        m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderQueued: ent={0}", ent.Name);
        m_workQueueRender.DoLater(CalculateInterestOrder(ent), DoRenderLater, renderParameters);
    }

    // collection of meshes that have already been built
    private Dictionary<ulong, EntityName> prebuiltMeshes = new Dictionary<ulong, EntityName>();
    private bool DoRenderLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        IEntity m_ent = (IEntity)loadParams[0];
        RenderableInfo m_ri = (RenderableInfo)loadParams[1];
        bool m_hasMesh = (bool)loadParams[2];
        string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
        m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: ent={0}", m_ent.Name);

        RenderPrim rprim;
        if (m_ent.TryGet<RenderPrim>(out rprim)) {
            if (!rprim.Create(m_ent, ref m_ri, ref m_hasMesh, qInstance.priority, qInstance.timesRequeued)) {
                // Didn't want to create for some reason.
                // Remember progress flags and return 'false' so we get retried
                loadParams[1] = m_ri;
                loadParams[2] = m_hasMesh;
                return false;
            }
        }

        /*
        lock (m_ent) {
            if (RendererOgre.GetSceneNodeName(m_ent) == null) {
                try {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Adding SceneNode to new entity " + m_ent.Name);
                    if (m_ri == null) {
                        IWorldRenderConv conver;
                        if (m_ent.TryGet<IWorldRenderConv>(out conver)) {
                            m_ri = conver.RenderingInfo(qInstance.priority, m_sceneMgr, m_ent, qInstance.timesRequeued);
                            if (m_ri == null) {
                                // The rendering info couldn't be built now. This is usually because
                                // the parent of this object is not available so we don't know where to put it
                                m_log.Log(LogLevel.DRENDERDETAIL,
                                    "Delaying rendering {0}/{1}. RenderingInfo not built for {2}",
                                    qInstance.sequence, qInstance.timesRequeued, m_ent.Name.Name);
                                return false;
                            }
                            else {
                                // save the value in the parameter block if we get called again (from a 'return false' below)
                                loadParams[1] = (Object)m_ri;
                            }
                        }
                        else {
                            m_log.Log(LogLevel.DBADERROR, "DoRenderLater: NO WORLDRENDERCONV FOR PRIM {0}", m_ent.Name);
                            // this probably creates infinite retries but other things are clearly broken
                            return false;
                        }
                    }

                    // Check to see if something of this mesh shape already exists. Use it if so.
                    EntityName entMeshName = (EntityName)m_ri.basicObject;
                    if (m_shouldShareMeshes && (m_ri.shapeHash != RenderableInfo.NO_HASH_SHARE)) {
                        lock (prebuiltMeshes) {
                            if (prebuiltMeshes.ContainsKey(m_ri.shapeHash)) {
                                entMeshName = prebuiltMeshes[m_ri.shapeHash];
                                // m_log.Log(LogLevel.DRENDERDETAIL, "DorRenderLater: using prebuilt {0}", entMeshName);
                                m_statShareInstances.Event();
                            }
                            else {
                                // this is a new mesh. Remember that it has been built
                                prebuiltMeshes.Add(m_ri.shapeHash, entMeshName);
                            }
                        }
                    }

                    // Find a handle to the parent for this node
                    string parentSceneNodeName = null;
                    if (m_ri.parentEntity != null) {
                        // this entity has a parent entity. create scene node off his
                        IEntity parentEnt = m_ri.parentEntity;
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(parentEnt.Name);
                    }
                    else {
                        // if no parent, add it at the top level of the region
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.RegionContext.Name);
                    }

                    // create the mesh we know we need
                    if ((m_shouldPrebuildMesh || m_shouldForceMeshRebuild) && !m_hasMesh) {
                        // way kludgy... but we see if the cached mesh file exists and, if so, we know it exists
                        if (!m_shouldForceMeshRebuild && m_ent.AssetContext.CheckIfCached(m_ent, entMeshName)) {
                            // if we just want the mesh built, if the file exists that's enough prebuilding
                            m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.DorRenderLater: mesh file exists: {0}", m_ent.Name.CacheFilename);
                            m_hasMesh = true;
                        }
                        if (!m_hasMesh) {
                            // TODO: figure out how to do this without queuing -- do it now
                            //2 RequestMesh(m_ent.Name, entMeshName.Name);
                            //1 Object[] meshLaterParams = { entMeshName.Name, entMeshName };
                            //1 bool worked = RequestMeshLater(qInstance, meshLaterParams);
                            //1 // if we can't get the mesh now, we'll have to wait until all the pieces are here
                            //1 if (!worked) return false;
                            IWorldRenderConv conver;
                            m_ent.TryGet<IWorldRenderConv>(out conver);
                            if (!conver.CreateMeshResource(qInstance.priority, m_ent, entMeshName.Name, entMeshName)) {
                                // we need to wait until some resource exists before we can complete this creation
                                return false; // note that m_hasMesh is still false so we'll do this routine again
                            }
                            m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.DorRenderLater: prebuild/forced mesh build for {0}",
                                entMeshName.Name);
                            m_hasMesh = true;
                        }
                        // Push that the mesh was crreated into the queued item so if a 'false' is returned
                        // later, we don't create the mesh again.
                        loadParams[2] = m_hasMesh;
                    }

                    // Create the scene node for this entity
                    // and add the definition for the object on to the scene node
                    // This will cause the load function to be called and create all
                    //   the callbacks that will actually create the object
                    // m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: mesh={0}, prio={1}", entMeshName.Name, qInstance.priority);
                    if (!m_sceneMgr.CreateMeshSceneNodeBF(qInstance.priority,
                                    entitySceneNodeName,
                                    parentSceneNodeName,
                                    entMeshName.Name,
                                    false, true,
                                    m_ri.position.X, m_ri.position.Y, m_ri.position.Z,
                                    m_ri.scale.X, m_ri.scale.Y, m_ri.scale.Z,
                                    m_ri.rotation.W, m_ri.rotation.X, m_ri.rotation.Y, m_ri.rotation.Z)) {
                        // m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering {0}/{1}. {2} waiting for parent {3}",
                        //     qInstance.sequence, qInstance.timesRequeued, m_ent.Name.Name,
                        //     (parentSceneNodeName == null ? "NULL" : parentSceneNodeName));
                        return false;   // if I must have parent, requeue if no parent
                    }

                    // Add the name of the created scene node name so we know it's created and
                    // we can find it later.
                    m_ent.SetAddition(RendererOgre.AddSceneNodeName, entitySceneNodeName);

                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "Render: Failed conversion of {0}: {1}", m_ent.Name.Name, e.ToString());
                }
            }
            else {
                // the entity already has a scene node. We're just forcing the rebuild of the prim
                if (m_ri == null) {
                    IWorldRenderConv conver;
                    m_ent.TryGet<IWorldRenderConv>(out conver);
                    m_ri = conver.RenderingInfo(qInstance.priority, m_sceneMgr, m_ent, qInstance.timesRequeued);
                    if (m_ri == null) {
                        // The rendering info couldn't be built now. This is usually because
                        // the parent of this object is not available so we don't know where to put it
                        m_log.Log(LogLevel.DRENDERDETAIL,
                            "Delaying rendering with scene node {0}/{1}. RenderingInfo not built for {2}",
                            qInstance.sequence, qInstance.timesRequeued, m_ent.Name.Name);
                        return false;
                    }
                    else {
                        // save the value in the parameter block if we get called again ('return false' below)
                        loadParams[1] = (Object)m_ri;
                    }
                }
                // Find a handle to the parent for this node
                string parentSceneNodeName = null;
                if (m_ri.parentEntity != null) {
                    // this entity has a parent entity. create scene node off his
                    IEntity parentEnt = m_ri.parentEntity;
                    parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(parentEnt.Name);
                }
                else {
                    parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.RegionContext.Name);
                }

                EntityName entMeshName = (EntityName)m_ri.basicObject;
                m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: entity has scenenode. Rebuilding mesh: {0}", entMeshName);
                RequestMesh(m_ent.Name, entMeshName.Name);
            }
        }
         */
        // System.GC.Collect(); // only for debugging
        return true;
    }

    /// <summary>
    /// How interesting is this entity. Returns a floating point number that ranges from
    /// zero (VERY interested) to N (less interested). The rough calculation is the distance
    /// from the agent but it need not be linear (things behind can be 'farther away'.
    /// </summary>
    /// <param name="ent">Entity we're checking interest in</param>
    /// <returns>Zero to N with larger meaning less interested</returns>
    private float CalculateInterestOrder(IEntity ent) {
        float ret = 100f;
        if (m_lastCameraPosition != null && m_lastCameraOrientation != null) {
            double dist = OMV.Vector3d.Distance(ent.GlobalPosition, m_lastCameraPosition);
            // if (entity is behind the camera) dist += 200;
            OMV.Vector3 dir = OMV.Vector3.UnitX * m_lastCameraOrientation;
            if (dir.X < 0) dist += 200;
            if (dist < 0) dist = -dist;
            if (dist > 1000.0) dist = 1000.0;
            // m_log.Log(LogLevel.DRENDERDETAIL, "CalcualteInterestOrder: ent={0}, cam={1}, d={2}", 
            //             ent.GlobalPosition, m_lastCameraPosition, dist);
            ret = (float)dist;
        }
        return ret;
    }

    // ==========================================================================
    /// <summary>
    /// An entity has been updated. Make the call into the renderer to move or rotate the entity.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="what"></param>
    public void RenderUpdate(IEntity ent, UpdateCodes what) {
        float priority = CalculateInterestOrder(ent);
        bool fullUpdate = false;    // true if a full update was done on this entity
        if ((what & UpdateCodes.New) != 0) {
            // new entity. Gets the full treatment
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: New entity: {0}", ent.Name.Name);
            DoRenderQueued(ent);
            fullUpdate = true;
        }
        // some things we don't do if it's a new entry since building the new entry will do these already
        if ((what & UpdateCodes.New) == 0) {
            // don't do these checks if the entity is new
            if ((what & UpdateCodes.ParentID) != 0) {
                // prim was detached or attached. Rerender if not the first update
                m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: parentID changed");
                if (!fullUpdate) DoRenderQueued(ent);
                fullUpdate = true;
            }
            if ((what & UpdateCodes.Material) != 0) {
                // the materials have changed on this entity. Cause materials to be recalcuated
                m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: Material changed");
            }
            // if (((what & (UpdateCodes.PrimFlags | UpdateCodes.PrimData)) != 0))) {
            if ((what & (UpdateCodes.PrimFlags | UpdateCodes.PrimData)) != 0) {
                // the prim parameters were changed. Re-render if this is not the new creation request
                m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: prim data changed");
                if (!fullUpdate) DoRenderQueued(ent);
                fullUpdate = true;
            }
            if ((what & UpdateCodes.Textures) != 0) {
                // texure on the prim were updated. Refresh them if not the initial creation update
                m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: textures changed");
                // to get the textures to refresh, we must force the situation
                IWorldRenderConv conver;
                if (ent.TryGet<IWorldRenderConv>(out conver)) {
                    conver.RebuildEntityMaterials(priority, ent);
                    Ogr.RefreshResourceBF(priority, Ogr.ResourceTypeMesh, EntityNameOgre.ConvertToOgreMeshName(ent.Name).Name);
                }
            }
        }
        if ((what & UpdateCodes.Animation) != 0) {
            // the prim has changed its rotation animation
            IAnimation anim;
            if (ent.TryGet<IAnimation>(out anim)) {
                IWorldRenderConv conver;
                if (ent.TryGet<IWorldRenderConv>(out conver)) {
                    m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: update animation");
                    // since the entity might not have been rendererd yet, we need to queue this operstion
                    Object[] parms = { 0f, ent, conver, anim };
                    m_workQueueRender.DoLater(CalculateInterestOrder(ent), DoUpdateAnimationLater, parms);
                    
                }
            }
        }
        if ((what & UpdateCodes.Text) != 0) {
            // text associated with the prim changed
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: text changed");
        }
        if ((what & UpdateCodes.Particles) != 0) {
            // particles associated with the prim changed
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: particles changed");
        }
        if (!fullUpdate && (what & (UpdateCodes.Scale | UpdateCodes.Position | UpdateCodes.Rotation)) != 0) {
            // world position has changed. Tell Ogre they have changed
            string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(ent.Name);
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: Updating position/rotation for {0}", entitySceneNodeName);
            Ogr.UpdateSceneNodeBF(priority, entitySceneNodeName,
                ((what & UpdateCodes.Position) != 0),
                ent.RelativePosition.X, ent.RelativePosition.Y, ent.RelativePosition.Z,
                false, 1f, 1f, 1f,  // don't pass scale yet
                ((what & UpdateCodes.Rotation) != 0),
                ent.Heading.W, ent.Heading.X, ent.Heading.Y, ent.Heading.Z);
        }
        return;
    }

    /// <summary>
    /// Current animations are for TargetOmega which causes rotation in the client.
    /// </summary>
    /// <param name="qInstance"></param>
    /// <param name="parms"></param>
    /// <returns>true if we can do this update now. False if to retry</returns>
    private bool DoUpdateAnimationLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        float prio = (float)loadParams[0];
        IEntity m_ent = (IEntity)loadParams[1];
        IWorldRenderConv m_conver = (IWorldRenderConv)loadParams[2];
        IAnimation m_anim = (IAnimation)loadParams[3];

        string sceneNodeName = RendererOgre.GetSceneNodeName(m_ent);
        if (sceneNodeName == null) {
            // prim does not yet have a scene node. Try again later.
            return false;
        }
        return m_conver.UpdateAnimation(0, m_ent, sceneNodeName, m_anim);
    }

    // ==========================================================================
    public void UnRender(IEntity ent) {
        lock (ent) {
            // if a parent, the children go too
            IEntityCollection coll = null;
            if (ent.TryGet<IEntityCollection>(out coll)) {
                coll.ForEach(delegate(IEntity entt) { this.UnRender(entt); });
            }
            if (RendererOgre.GetSceneNodeName(ent) != null) {
                string sNodeName = RendererOgre.GetSceneNodeName(ent);
                Ogr.RemoveSceneNodeBF(0, sNodeName);
            }
        }
        return;
    }

    // ==========================================================================
    /// <summary>
    /// Update the camera. The coordinate system from the EntityCamera is LL's
    /// (Z up). We have to convert the rotation and position to Ogre coords
    /// (Y up).
    /// </summary>
    /// <param name="cam"></param>
    // private bool haveAttachedCamera = false;
    public void UpdateCamera(CameraControl cam) {
        /* Historical Note: This is part of code to attach the camera to the avatar. When this was written
         * the avatar code was not in place so the actual scene node to attach the camera too is problematic.
         * Fix this when there is an avatar.
        if (!haveAttachedCamera && cam.AssociatedAgent != null && cam.AssociatedAgent.AssociatedAvatar != null) {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: Attaching camera with {0}", cam.AssociatedAgent.AssociatedAvatar.Name);
            haveAttachedCamera = Ogr.AttachCamera(cam.AssociatedAgent.AssociatedAvatar.Name.Name, 1.0f, 0.0f, 1.0f, 0f, 0f, 0f, 1f);
        }
         */
        
        // OMV.Quaternion orient = new OMV.Quaternion(OMV.Vector3.UnitX, -Constants.PI / 2)
                    // * new OMV.Quaternion(OMV.Vector3.UnitZ, -Constants.PI / 2)
                    // * cam.Direction;
        // we need to rotate the camera 90 to make it work out in Ogre. Not sure why.
        // OMV.Quaternion orient = cam.Heading * OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 2);
        OMV.Quaternion orient = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 2) * cam.Heading;
        // OMV.Quaternion orient = cam.Heading;
        orient.Normalize();
        m_lastCameraPosition = cam.GlobalPosition;
        m_lastCameraOrientation = orient;
        // note the conversion from LL coordinates (Z up) to Ogre coordinates (Y up)
        OMV.Vector3d pos = cam.GlobalPosition * m_sceneMagnification;
        Ogr.UpdateCameraBF(pos.X, pos.Z, -pos.Y, 
            orient.W, orient.X, orient.Z, -orient.Y,
            1.0f, (float)cam.Far*m_sceneMagnification, 1.0f);

        m_log.Log(LogLevel.DRENDERDETAIL, "UpdateCamera: Camera to p={0}, r={1}", pos, orient);
        return;
    }

    // ==========================================================================
    public void UpdateEnvironmentalLights(EntityLight sunLight, EntityLight moonLight) {
        return;
    }
    
    // ==========================================================================
    // Given the current input pointer, return a point in the world
    public OMV.Vector3d SelectPoint() {
        return new OMV.Vector3d(0, 0, 0);
    }
    
    // ==========================================================================
    public void MapRegionIntoView(RegionContextBase rcontext) {
        Ogr.AddRegionBF(0f, EntityNameOgre.ConvertToOgreSceneNodeName(rcontext.Name),
            // rcontext.GlobalPosition.X, rcontext.GlobalPosition.Z, -rcontext.GlobalPosition.Y,
            rcontext.GlobalPosition.X, (double)0, -rcontext.GlobalPosition.Y,
            rcontext.Size.X, rcontext.Size.Y,
            rcontext.TerrainInfo.WaterHeight);
        return;
    }

    // ==========================================================================
    public void SetFocusRegion(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DRENDERDETAIL, "SetFocusRegion: setting focus region {0}", rcontext.Name);
        Ogr.SetFocusRegionBF(EntityNameOgre.ConvertToOgreSceneNodeName(rcontext.Name));
    }

    // ==========================================================================
    public RenderableInfo RenderingInfo(IEntity ent) {
        return null;
    }

    // ==========================================================================
    public void UpdateTerrain(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DRENDERDETAIL, "RenderOgre: UpdateTerrain: for region {0}", rcontext.Name.Name);
        try {
            float[,] hm = rcontext.TerrainInfo.HeightMap;
            int hmWidth = rcontext.TerrainInfo.HeightMapWidth;
            int hmLength = rcontext.TerrainInfo.HeightMapLength;

            int loc = 0;
            float[] passingHM = new float[hmWidth * hmLength];
            for (int xx = 0; xx < hmWidth; xx++) {
                for (int yy = 0; yy < hmLength; yy++) {
                    passingHM[loc++] = hm[xx, yy];
                }
            }

            Ogr.UpdateTerrainBF(0, EntityNameOgre.ConvertToOgreSceneNodeName(rcontext.Name),
                        hmWidth, hmLength, passingHM);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "UpdateTerrainLater: mesh creation failure: " + e.ToString());
        }

        return;
    }
    #endregion IRenderProvider

    // ==========================================================================
    // Called from the Ogre side asking for a resource to be created. Depending on the
    // type of resource requested, we load the resource and inform Ogre it's there.
    // This call is from the unmanaged code so most of the work is queued.
    private void RequestResource(string resourceContext, string resourceName, int resourceType) {
        switch (resourceType) {
            case Ogr.ResourceTypeMesh:
                m_statMeshesRequested.Event();
                // if we are forcing mesh creation this should 1) never happen and 2) been take care of elsewhere
                if (!m_shouldForceMeshRebuild) {
                    RequestMesh(new EntityName(resourceContext), resourceName);
                }
                break;
            case Ogr.ResourceTypeMaterial:
                m_statMaterialsRequested.Event();
                RequestMaterial(resourceContext, resourceName);
                // RequestMaterialX(resourceContext, resourceName);
                break;
            case Ogr.ResourceTypeTexture:
                m_statTexturesRequested.Event();
                RequestTexture(resourceContext, resourceName);
                break;
        }
        return;
    }

    public void RequestMesh(EntityName contextEntity, string meshName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for mesh " + meshName);
        Object[] meshLaterParams = { meshName, contextEntity };
        m_workQueueReqMesh.DoLater(RequestMeshLater, (object)meshLaterParams);
        return;
    }

    // Called on workQueue to call into gather parameters and create the mesh resource
    private bool RequestMeshLater(DoLaterBase qInstance, Object parm) {
        Object[] lparams = (Object[])parm;
        string m_meshName = (string)lparams[0];
        EntityName m_contextEntityName = (EntityName)lparams[1];
        try {
            // type information is at the end of the name. remove if there
            EntityName eName = EntityNameOgre.ConvertOgreResourceToEntityName(m_meshName);
            // the terrible kludge here is that the name of the mesh is the name of
            //   the entity. We reach back into the worlds and find the underlying
            //   entity then we can construct the mesh.
            IEntity ent;
            if (!World.World.Instance.TryGetEntity(eName, out ent)) {
                m_log.Log(LogLevel.DBADERROR, "RendererOgre.RequestMeshLater: could not find entity " + eName);
                return true;
            }
            // Create mesh resource. In this case its most likely a .mesh file in the cache
            // The actual mesh creation is queued and done later between frames
            float priority = CalculateInterestOrder(ent);
            IWorldRenderConv conver;
            ent.TryGet<IWorldRenderConv>(out conver);
            if (!conver.CreateMeshResource(priority, ent, m_meshName, m_contextEntityName)) {
                // we need to wait until some resource exists before we can complete this creation
                return false;
            }

            // tell Ogre to refresh (reload) the resource
            m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.RequestMeshLater: refresh for {0}. prio={1}", 
                    m_meshName, priority);
            Ogr.RefreshResourceBF(priority, Ogr.ResourceTypeMesh, m_meshName);
        }
        catch {
            // an oddity but not fatal
        }
        return true;
    }
    
    /// <summary>
    /// Request from the C++ world to create a specific material resource. We queue
    /// the request on a work queue so the renderer can get back to work.
    /// The material information is gathered and then sent back to the renderer.
    /// </summary>
    /// <param name="contextEntity"></param>
    /// <param name="matName"></param>
    private Dictionary<string, int> m_rememberMats = new Dictionary<string, int>();
    private int m_matRememberTime = 20000;
    private void RequestMaterial(string contextEntity, string matName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for material " + matName);
        EntityNameOgre entName = EntityNameOgre.ConvertOgreResourceToEntityName(contextEntity);
        // remember all the materials we created. This could be a problem when regions are unloaded
        if (m_rememberMats.ContainsKey(entName.Name)) {
            if (m_rememberMats[entName.Name] > Utilities.TickCount()) {
                return;
            }
            m_rememberMats[entName.Name] = Utilities.TickCount() + m_matRememberTime;
        }
        else {
            m_rememberMats.Add(entName.Name, Utilities.TickCount() + m_matRememberTime);
        }
        Object[] materialParameters = { entName, matName };
        m_workQueueReqMaterial.DoLater(RequestMaterialLater, materialParameters);
        return;
    }

    // Called on workqueue thread to create material in Ogre
    private bool RequestMaterialLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        EntityNameOgre m_entName = (EntityNameOgre)loadParams[0];
        string m_matName = (string)loadParams[1];
        try {
            IEntity ent;
            if (World.World.Instance.TryGetEntity(m_entName, out ent)) {
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, "RequestMaterialLater.DoIt(): converting {0}", m_entName);
                IWorldRenderConv conver;
                if (!ent.TryGet<IWorldRenderConv>(out conver)) {
                    // the rendering context is not set up. Odd but not fatal
                    // try again later
                    return false;
                }
                else {
                    // Create the material resource and then make the rendering redisplay
                    conver.CreateMaterialResource(qInstance.priority, ent, m_matName);
                    Ogr.RefreshResourceBF(qInstance.priority, Ogr.ResourceTypeMaterial, m_matName);
                }
            }
            else {
                // we couldn't find the entity for the material. not good
                m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: could not find entity for material {0}", m_matName);
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: exception realizing material: {0}", e.ToString());
        }
        return true;
    }

    // ==========================================================================
    /// <summary>
    /// Textures work by the renderer finding it doesn't have the texture. It 
    /// uses a default  texture but then tells us to get the real one. We request
    /// it from the asset servers who put a texture file into the Ogre cache.
    /// Once the texture is there, we tell Ogre to update the texture which
    /// will cause it to appear on the screen.
    /// </summary>
    /// <param name="contextEntity"></param>
    /// <param name="txtName"></param>
    private void RequestTexture(string contextEntity, string txtName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for texture " + txtName);
        EntityNameOgre entName = EntityNameOgre.ConvertOgreResourceToEntityName(txtName);
        // get this work off the thread from the renderer
        // m_workQueue.DoLater(new RequestTextureLater(this, entName));
        m_workQueueReqTexture.DoLater(RequestTextureLater, entName);
    }

    private bool RequestTextureLater(DoLaterBase qInstance, Object parm) {
        EntityNameOgre m_entName = (EntityNameOgre)parm;
        // note the super kludge since we don't know the real asset context
        // This information is hopefully coded into the entity name
        // The callback can (and will) be called multiple times as the texture gets better resolution
        AssetContextBase.RequestTextureLoad(m_entName, AssetContextBase.AssetType.Texture, TextureLoadedCallback);
        return true;
    }

    // the texture is loaded so get to the right time to tell the renderer
    private void TextureLoadedCallback(string textureEntityName, bool hasTransparancy) {
        LogManager.Log.Log(LogLevel.DRENDERDETAIL, "TextureLoadedCallback: Load complete. Name: {0}", textureEntityName);
        EntityNameOgre entName = new EntityNameOgre(textureEntityName);
        string ogreResourceName = entName.OgreResourceName;
        if (hasTransparancy) {
            Ogr.RefreshResourceBF(100f, Ogr.ResourceTypeTransparentTexture, ogreResourceName);
        }
        else {
            Ogr.RefreshResourceBF(100f, Ogr.ResourceTypeTexture, ogreResourceName);
        }
        return;
    }

    // ==========================================================================
    /// <summary>
    /// If there is work queued to happen between frames. Do some of the work now.
    /// </summary>
    private bool ProcessBetweenFrames() {
        return ProcessBetweenFrames(m_betweenFrameTotalCost);
    }

    private bool ProcessBetweenFrames(int cost) {
        // m_log.Log(LogLevel.DRENDERDETAIL, "Process between frames");
        if (m_betweenFramesQueue != null) {
            m_betweenFramesQueue.ProcessQueue(cost);
        }
        return LGB.KeepRunning;
    }

    private bool IsProcessBetweenFramesWork() {
        return ( m_betweenFramesQueue.CurrentQueued > 0 );
    }
}
}
