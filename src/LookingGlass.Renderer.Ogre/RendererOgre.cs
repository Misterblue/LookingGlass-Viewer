/* Copyright (c) Robert Adams
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

namespace LookingGlass.Renderer.Ogr {

public class RendererOgre : ModuleBase, IRenderProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
    private ILog m_logOgre = LogManager.GetLogger("RendererCpp");

# pragma warning disable 0067   // disable unused event warning
    public event RendererBeforeFrameCallback OnRendererBeforeFrame;
# pragma warning restore 0067

    // we decorate IEntities with SceneNodes. This is our slot in the IEntity addition table
    // SceneNode of the entity itself
    public static int AddSceneNode;
    public static string AddSceneNodeName = "OgreSceneNode";
    public static OgreSceneNode GetSceneNode(IEntity ent) {
        return (OgreSceneNode)ent.Addition(RendererOgre.AddSceneNode);
    }
    // SceneNode of the region if this is a IRegionContext
    public static int AddRegionSceneNode;
    public static string AddRegionSceneNodeName = "OgreRegionSceneNode";
    public static OgreSceneNode GetRegionSceneNode(IEntity ent) {
        return (OgreSceneNode)ent.Addition(RendererOgre.AddRegionSceneNode);
    }
    // SceneNode of  the  terrain if this is an ITerrainInfo
    public static int AddTerrainSceneNode;
    public static string AddTerrainSceneNodeName = "OgreTerrainSceneNode";
    public static OgreSceneNode GetTerrainSceneNode(IEntity ent) {
        return (OgreSceneNode)ent.Addition(RendererOgre.AddTerrainSceneNode);
    }
    // Instance if IWorldRenderConv for converting this entity to my renderer
    public static int AddWorldRenderConv;
    public static string AddWorldRenderConvName = "OgreWorldRenderConvName";
    public static IWorldRenderConv GetWorldRenderConv(IEntity ent) {
        return (IWorldRenderConv)ent.Addition(RendererOgre.AddWorldRenderConv);
    }

    protected IUserInterfaceProvider m_userInterface = null;

    protected OgreSceneMgr m_sceneMgr;

    protected Thread m_rendererThread = null;

    // this shouldn't be here... this is a feature of the LL renderer
    protected float m_sceneMagnification;
    public float SceneMagnification { get { return m_sceneMagnification; } }

    // we remember where the camera last was so we can do some interest management
    private OMV.Vector3d m_lastCameraPosition;

    protected BasicWorkQueue m_workQueue = new BasicWorkQueue("OgreRendererWork");
    protected OnDemandWorkQueue m_betweenFramesQueue = new OnDemandWorkQueue("OgreBetweenFrames");
    private static int m_betweenFrameTotalCost = 300;
    private static int m_betweenFrameMinTotalCost = 50;
    private static int m_betweenFrameCreateMaterialCost = 5;
    private static int m_betweenFrameCreateSceneNodeCost = 20;
    private static int m_betweenFrameCreateMeshCost = 20;
    private static int m_betweenFrameRefreshMeshCost = 20;
    private static int m_betweenFrameMapRegionCost = 50;
    private static int m_betweenFrameUpdateTerrainCost = 50;
    private static int m_betweenFrameMapTextureCost = 10;

    // private Thread m_rendererThread = null;

    private RestHandler m_restHandler;
    private StatisticManager m_stats;
    private IIntervalCounter m_statRefreshMaterialInterval;
    private IIntervalCounter m_statCreateMaterialInterval;
    private ICounter m_statMaterialsRequested;
    private ICounter m_statMeshesRequested;
    private ICounter m_statTexturesRequested;

    // ==========================================================================
    public RendererOgre() {
    }

    #region IModule
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
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
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.InputSystem", "LookingGlass.Renderer.Ogr.UserInterfaceOgre",
                    "Selection of user interface system.");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.FramePerSecMax", "30",
                    "Maximum number of frames to display per second");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.PluginFilename", "plugins.cfg",
                    "File that lists Ogre plugins to load");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.ResourcesFilename", "resources.cfg",
                    "File that lists the Ogre resources to load");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultNumMipmaps", "3",
                    "Default number of mip maps created for a texture (usually 6)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.CacheDir", Utilities.GetDefaultApplicationStorageDir(null),
                    "Directory to store cached meshs, textures, etc");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.PreLoadedDir", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/Preloaded/"),
                    "Directory to for preloaded textures, etc");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTerrainMaterial",
                    "LookingGlass/DefaultTerrainMaterial",
                    "Material applied to terrain");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.OceanMaterialName",
                    "LookingGlass/Ocean",
                    "The ogre name of the ocean texture");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultMeshFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/LoadingShape.mesh"),
                    "Filename of the default shape found in the cache dir");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTextureFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/LoadingTexture.png"),
                    "Filename of the default texture found in the cache dir");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.DefaultTextureResourceName", 
                    "LoadingTexture.png",
                    "Resource name of  the default texture");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.SceneMagnification", "1",
                    "Magnification of LL coordinates into Ogre space");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.RenderInfoMaterialCreate", "true",
                    "Create materials while gathering mesh generation info (earlier than mesh creation)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.LL.EarlyMaterialCreate", "false",
                    "Create materials while creating mesh rather than waiting");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.Total", "300",
                    "The total cost of operations to do between frames");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.CreateMaterial", "5",
                    "The cost of creating a material");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.CreateSceneNode", "20",
                    "The cost of creating a scene node");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.CreateMesh", "20",
                    "The cost of creating a mesh");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.RefreshMesh", "20",
                    "The cost of refreshing a mesh (scanning all entities and reloading ones using mesh)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.MapRegion", "50",
                    "The cost of mapping a region (creating the region management structures");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.UpdateTerrain", "50",
                    "The cost of updating the terrain (rebuilding terrain mesh)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.BetweenFrame.Costs.MapTexture", "10",
                    "The cost of mapping a texture (doing a texture reload)");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SerializeMaterials", "false",
                    "Write out materials to files (replace with DB someday)");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.SerializeMeshes", "true",
                    "Write out meshes to files");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Ambient", "<0.4,0.4,0.4>",
                    "color value for initial ambient lighting");

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
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.OnlyLargeAfter", "90",
                    "After this distance, only large things are visible");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Ogre.Visibility.Large", "7",
                    "How big is considered 'large' for 'OnlyLargeAfter' calculation");

        m_stats = new StatisticManager(m_moduleName);
        m_statRefreshMaterialInterval = m_stats.GetIntervalCounter("UpdateTexture");
        m_statCreateMaterialInterval = m_stats.GetIntervalCounter("CreateMaterial");
        m_statMaterialsRequested = m_stats.GetCounter("MaterialsRequested");
        m_statMeshesRequested = m_stats.GetCounter("MeshesRequested");
        m_statTexturesRequested = m_stats.GetCounter("TexturesRequested");

        // renderer keeps rendering specific data in an entity's addition/subsystem slots
        AddSceneNode = EntityBase.AddAdditionSubsystem(RendererOgre.AddSceneNodeName);
        AddRegionSceneNode = EntityBase.AddAdditionSubsystem(RendererOgre.AddRegionSceneNodeName);
        AddTerrainSceneNode = EntityBase.AddAdditionSubsystem(RendererOgre.AddTerrainSceneNodeName);
        AddWorldRenderConv = EntityBase.AddAdditionSubsystem(RendererOgre.AddWorldRenderConvName);
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

        // load the input system we're supposed to be using
        String uiClass = ModuleParams.ParamString(m_moduleName + ".Ogre.InputSystem");
        if (uiClass != null && uiClass.Length > 0) {
            try {
                m_log.Log(LogLevel.DRENDERDETAIL, "Loading UI processor {0}", uiClass);
                System.Reflection.Assembly thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                m_userInterface = (UserInterfaceOgre)thisAssembly.CreateInstance(uiClass, true);
                // m_userInterface = new UserInterfaceOgre();
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FATAL: Could not load user interface class {0}: {1}", uiClass, e.ToString());
                return false;
            }
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "Using null user interfare");
            m_userInterface = new UserInterfaceNull();
        }

        if (m_log.WouldLog(LogLevel.DRENDERDETAIL)) {
            debugLogCallbackHandle = new Ogr.DebugLogCallback(OgrLogger);
            Ogr.SetDebugLogCallback(debugLogCallbackHandle);
        }
        fetchParameterCallbackHandle = new Ogr.FetchParameterCallback(GetAParameter);
        Ogr.SetFetchParameterCallback(fetchParameterCallbackHandle);
        checkKeepRunningCallbackHandle = new Ogr.CheckKeepRunningCallback(CheckKeepRunning);
        Ogr.SetCheckKeepRunningCallback(checkKeepRunningCallbackHandle);
        if (m_userInterface.NeedsRendererLinkage()) {
            userIOCallbackHandle = new Ogr.UserIOCallback(m_userInterface.ReceiveUserIO);
            Ogr.SetUserIOCallback(userIOCallbackHandle);
        }
        requestResourceCallbackHandle = new Ogr.RequestResourceCallback(RequestResource);
        Ogr.SetRequestResourceCallback(requestResourceCallbackHandle);
        betweenFramesCallbackHandle = new Ogr.BetweenFramesCallback(ProcessBetweenFrames);
        Ogr.SetBetweenFramesCallback(betweenFramesCallbackHandle);

        m_sceneMagnification = float.Parse(ModuleParams.ParamString("Renderer.Ogre.LL.SceneMagnification"));

        m_betweenFrameTotalCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.Total");
        m_betweenFrameCreateMaterialCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.CreateMaterial");
        m_betweenFrameCreateSceneNodeCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.CreateSceneNode");
        m_betweenFrameCreateMeshCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.CreateMesh");
        m_betweenFrameRefreshMeshCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.RefreshMesh");
        m_betweenFrameMapRegionCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.MapRegion");
        m_betweenFrameUpdateTerrainCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.UpdateTerrain");
        m_betweenFrameMapTextureCost = ModuleParams.ParamInt("Renderer.Ogre.BetweenFrame.Costs.MapTexture");

        Ogr.InitializeOgre();
        m_sceneMgr = new OgreSceneMgr(Ogr.GetSceneMgr());

        return true;
    }

    private void OgrLogger(string msg) {
        m_logOgre.Log(LogLevel.DRENDERDETAIL, msg);
    }
    
    private bool CheckKeepRunning() {
        return LGB.KeepRunning;
    }

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
    override public void Start() {
        m_log.Log(LogLevel.DRENDERDETAIL, "Start: requesting main thread");
        LGB.GetMainThread(RendererThread);
        return;
    }

    // ==========================================================================
    override public void Stop() {
        return;
    }

    // ==========================================================================
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
        m_log.Log(LogLevel.DRENDERDETAIL, "RendererThread: have main thread");
        /*
        // Try creating a thread just for the renderer
        // create a thread for the renderer
        m_rendererThread = new Thread(RunRenderer);
        m_rendererThread.Name = "Renderer";
        m_rendererThread.Start();
        // tell the caller I'm not using the main thread
        return false;
         */

        // Let the Ogre code decide if it needs the main thread
        // return Ogr.RenderingThread();

        // code  to keep the rendering thread mostly in managed space
        // periodically call to draw a frame
        int maxFramePerSec = ModuleParams.ParamInt(m_moduleName + ".Ogre.FramePerSecMax");
        int msPerFrame = 1000 / maxFramePerSec;
        int frameStart, frameEnd, frameDuration, frameLeft;
        while (LGB.KeepRunning) {
            frameStart = System.Environment.TickCount;
            if (!Ogr.RenderOneFrame(true)) {
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
    }

    public bool RenderOneFrame(bool pump) {
        return Ogr.RenderOneFrame(pump);
    }

    // pass the thread into the renderer
    private void RunRenderer() {
        Ogr.RenderingThread();
        return;
    }

    // ==========================================================================
    public void Render(IEntity ent) {
        // do we have a format converted for this entity?
        if (RendererOgre.GetWorldRenderConv(ent) == null) {
            // for the moment, we only know about LL stuff
            // TODO: Figure out how to make this dynamic, extendable and runtime
            ent.SetAddition(RendererOgre.AddWorldRenderConv, RendererOgreLL.Instance);
        }
        // does the entity have a scene node assocated with it already?
        if (RendererOgre.GetSceneNode(ent) == null) {
            // race condition: two Render()s for the same entity. This check is performed
            // again in the processing routine with some locking. The check here is
            // an optimization
            DoLaterBase laterWork = new DoRender(m_sceneMgr, ent, m_log);
            laterWork.order = CalculateInterestOrder(ent);
            m_betweenFramesQueue.DoLater(laterWork);
        }
        return;
    }

    private sealed class DoRender : DoLaterBase {
        IEntity m_ent = null;
        OgreSceneMgr m_sceneMgr = null;
        ILog m_log = null;
        RenderableInfo m_ri = null;
        public DoRender(OgreSceneMgr sMgr, IEntity ent, ILog logr) : base() {
            m_ent = ent;
            m_sceneMgr = sMgr;
            m_log = logr;
            this.cost = m_betweenFrameCreateSceneNodeCost;
        }

        override public bool DoIt() {
            if (RendererOgre.GetSceneNode(m_ent) == null) {
                try {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Adding SceneNode to new entity " + ent.Name);
                    if (m_ri == null) {
                        m_ri = RendererOgre.GetWorldRenderConv(m_ent).RenderingInfo(m_sceneMgr, m_ent, this.timesRequeued);
                        if (m_ri == null) {
                            // The rendering info couldn't be built now. This is usually because
                            // the parent of this object is not available so we don't know where to put it
                            m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering {0}/{1}. RenderingInfo not built for {2}",
                                this.sequence, this.timesRequeued, m_ent.Name.Name);
                            return false;
                        }
                    }

                    // Find a handle to the parent for this node
                    OgreSceneNode parentNode = null;
                    if (m_ri.parentEntity != null) {
                        // this entity has a parent entity. create scene node off his
                        IEntity parentEnt = m_ri.parentEntity;
                        parentNode = RendererOgre.GetSceneNode(parentEnt);
                        if (parentNode == null) {
                            m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering {0}/{1}. {2} waiting for parent {3}",
                                this.sequence, this.timesRequeued, m_ent.Name.Name, 
                                (parentEnt == null ? "NULL" : parentEnt.Name.Name));
                            return false;   // if I must have parent, requeue if no parent
                        }
                    }
                    else {
                        if (m_ri.RegionRoot == null) {
                            // no root scene node for this entity so place it at the real root
                            parentNode = m_sceneMgr.RootNode();
                            // m_log.Log(LogLevel.DRENDERDETAIL, "SceneNode child of root for " + m_ent.Name);
                        }
                        else {
                            parentNode = (OgreSceneNode)m_ri.RegionRoot;
                            // m_log.Log(LogLevel.DRENDERDETAIL, "SceneNode child of region root for " + m_ent.Name);
                        }
                    }

                    // Create the scene node for this entity
                    OgreSceneNode node = m_sceneMgr.CreateSceneNode(m_ent.Name.ToString(),
                                parentNode, false, true,
                                m_ri.position.X, m_ri.position.Y, m_ri.position.Z,
                                m_ri.scale.X, m_ri.scale.Y, m_ri.scale.Z,
                                m_ri.rotation.W, m_ri.rotation.X, m_ri.rotation.Y, m_ri.rotation.Z
                    );

                    // TODO: is this the correct info to save in our slot?
                    m_ent.SetAddition(RendererOgre.AddSceneNode, node);

                    // Add the definition for the object on to the scene node
                    // This will cause the load function to be called and create all
                    //   the callbacks that will actually create the object
                    string entObjectName = (string)m_ri.basicObject;
                    // m_log.Log(LogLevel.DRENDERDETAIL, "AddEntity " + entObjectName);
                    node.AddEntity(m_sceneMgr, entObjectName);
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "Render: Failed conversion: " + e.ToString());
                }

            }
            return true;
        }

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
        if (m_lastCameraPosition != null) {
            double dist = OMV.Vector3d.Distance(ent.GlobalPosition, m_lastCameraPosition);
            if (dist < 0) dist = -dist;
            if (dist > 1000.0) dist = 1000.0;
            ret = (float)dist;
        }
        return ret;
    }

    // ==========================================================================
    public void UnRender(IEntity ent) {
        return;
    }

    // ==========================================================================
    /// <summary>
    /// Update the camera. The coordinate system from the EntityCamera is LL's
    /// (Z up). We have to convert the rotation and position to Ogre coords
    /// (Y up).
    /// </summary>
    /// <param name="cam"></param>
    public void UpdateCamera(CameraControl cam) {
        // OMV.Quaternion orient = new OMV.Quaternion(OMV.Vector3.UnitX, -Constants.PI / 2)
                    // * new OMV.Quaternion(OMV.Vector3.UnitZ, -Constants.PI / 2)
                    // * cam.Direction;
        // we need to rotate the camera 90 to make it work out in Ogre. Not sure why.
        // OMV.Quaternion orient = cam.Heading * OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 2);
        OMV.Quaternion orient = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 2) * cam.Heading;
        // OMV.Quaternion orient = cam.Heading;
        orient.Normalize();
        OMV.Vector3d pos = cam.GlobalPosition * m_sceneMagnification;
        m_lastCameraPosition = cam.GlobalPosition;
        Ogr.UpdateCamera((float)pos.X, (float)pos.Z, (float)-pos.Y, 
            orient.W, orient.X, orient.Z, -orient.Y,
            1.0f, (float)cam.Far*m_sceneMagnification, 1.0f);
        // m_log.Log(LogLevel.DRENDERDETAIL, "UpdateCamera: Camera to {0}, {1}, {2}",
        //     (float)pos.X, (float)pos.Z, (float)-pos.Y);
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
        lock (rcontext) {
            // see that the region entity has a converter between world and renderer
            // TODO: Figure out how to make this dynamic, extendable and runtime
            if (rcontext.WorldGroup == World.WorldGroupCode.LLWorld) {
                if (RendererOgre.GetWorldRenderConv(rcontext) == null) {
                    rcontext.SetAddition(RendererOgre.AddWorldRenderConv, new RendererOgreLL());
                }
            }
        }
        m_betweenFramesQueue.DoLater(new MapRegionLater(m_sceneMgr, rcontext, m_log));
        return;
    }

    private sealed class MapRegionLater : DoLaterBase {
        OgreSceneMgr m_sceneMgr;
        RegionContextBase m_rcontext;
        ILog m_log;
        public MapRegionLater(OgreSceneMgr smgr, RegionContextBase rcon, ILog logr) : base() {
            m_sceneMgr = smgr;
            m_rcontext = rcon;
            m_log = logr;
            this.cost = m_betweenFrameMapRegionCost;
        }
        override public bool DoIt() {
            RendererOgre.GetWorldRenderConv(m_rcontext).MapRegionIntoView(m_sceneMgr, m_rcontext);
            return true;
        }
    }
    
    // ==========================================================================
    public RenderableInfo RenderingInfo(IEntity ent) {
        return null;
    }

    // ==========================================================================
    public void UpdateTerrain(RegionContextBase rcontext) {
        m_betweenFramesQueue.DoLater(new UpdateTerrainLater(this, m_sceneMgr, rcontext, m_log));
        return;
    }
    private sealed class UpdateTerrainLater : DoLaterBase {
        RendererOgre m_renderer;
        OgreSceneMgr m_sceneMgr;
        RegionContextBase m_rcontext;
        ILog m_log;
        public UpdateTerrainLater(RendererOgre renderer, OgreSceneMgr smgr, RegionContextBase rcontext, ILog logr) : base() {
            m_renderer = renderer;
            m_sceneMgr = smgr;
            m_rcontext = rcontext;
            m_log = logr;
            this.cost = m_betweenFrameUpdateTerrainCost;
        }
        override public bool DoIt() {
            OgreSceneNode sn;
            // see if there is a terrain SceneNode already allocated
            if (RendererOgre.GetTerrainSceneNode(m_rcontext) != null) {
                sn = RendererOgre.GetTerrainSceneNode(m_rcontext);
            }
            else {
                OgreSceneNode regionSceneNode = RendererOgre.GetRegionSceneNode(m_rcontext);
                // no existing scene node, create one
                if (regionSceneNode == null) {
                    // we have to wait until there is a region root before placing texture
                    m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre: UpdateTerrain: waiting for region root");
                    return false;
                }
                else {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "RenderOgre: UpdateTerrain: Using world specific root node");
                    string terrainNodeName = "Terrain/" + m_rcontext.Name + "/" + OMV.UUID.Random().ToString();
                    sn = m_sceneMgr.CreateSceneNode(terrainNodeName, regionSceneNode, 
                                false, true, 
                                // the terrain is attached to the region node so it's at relative address
                                0f, 0f, 0f,
                                // scaling matches the LL to Ogre map
                                m_renderer.SceneMagnification, m_renderer.SceneMagnification, m_renderer.SceneMagnification,
                                OMV.Quaternion.Identity.W, OMV.Quaternion.Identity.X,
                                OMV.Quaternion.Identity.Y, OMV.Quaternion.Identity.Z
                    );
                    m_rcontext.SetAddition(RendererOgre.AddTerrainSceneNode, sn);
                    m_log.Log(LogLevel.DRENDERDETAIL, "Creating terrain " + sn.Name);
                }
            }

            try {
                float[,] hm = m_rcontext.TerrainInfo.HeightMap;
                int hmWidth = m_rcontext.TerrainInfo.HeightMapWidth;
                int hmLength = m_rcontext.TerrainInfo.HeightMapLength;

                int loc = 0;
                float[] passingHM = new float[hmWidth * hmLength];
                for (int xx = 0; xx < hmWidth; xx++) {
                    for (int yy = 0; yy < hmLength; yy++) {
                        passingHM[loc++] = hm[xx, yy];
                    }
                }

                Ogr.GenTerrainMesh(m_sceneMgr.BasePtr, sn.BasePtr, hmWidth, hmLength, passingHM);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "UpdateTerrainLater: mesh creation failure: " + e.ToString());
            }

            return true;
        }
    }
    #endregion IRenderProvider

    // ==========================================================================
    private void RequestResource(string resourceContext, string resourceName, int resourceType) {
        switch (resourceType) {
            case Ogr.ResourceTypeMesh:
                m_statMeshesRequested.Event();
                RequestMesh(resourceContext, resourceName);
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

    private void RequestMesh(string contextEntity, string meshName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for mesh " + meshName);
        m_workQueue.DoLater(new RequestMeshLater(this, m_sceneMgr, meshName));
        return;
    }

    private sealed class RequestMeshLater : DoLaterBase {
        RendererOgre m_renderer;
        string m_meshName;
        OgreSceneMgr m_sceneMgr;
        public RequestMeshLater(RendererOgre renderer, OgreSceneMgr sMgr, string meshName) : base() {
            m_renderer = renderer;
            m_sceneMgr = sMgr;
            m_meshName = meshName;
        }
        
        override public bool DoIt() {
            try {
                // type information is at the end of the name. remove if there
                EntityName eName = EntityNameOgre.ConvertOgreResourceToEntityName(m_meshName);
                // the terrible kludge here is that the name of the mesh is the name of
                //   the entity. We reach back into the worlds and find the underlying
                //   entity then we can construct the mesh.
                IEntity ent;
                if (!World.World.Instance.TryGetEntity(eName, out ent)) {
                    LogManager.Log.Log(LogLevel.DBADERROR, "RendererOgre.RequestMeshLater: could not find entity " + eName);
                    return true;
                }
                // Create mesh resource. In this case its most likely a .mesh file in the cache
                // The actual mesh creation is queued and done later between frames
                if (!RendererOgre.GetWorldRenderConv(ent).CreateMeshResource(m_sceneMgr, ent, m_meshName)) {
                    // we need to wait until some resource exists before we can complete this creation
                    return false;
                }

                // tell Ogre to refresh (reload) the resource
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.RequestMeshLater: refresh for {0}", m_meshName);
                Ogr.RefreshResourceBF(Ogr.ResourceTypeMesh, m_meshName);
            }
            catch {
                // an oddity but not fatal
            }
            return true;
        }
    }

    /// <summary>
    /// Request from the C++ world to create a specific material resource. We queue
    /// the request on a work queue so the renderer can get back to work.
    /// The material information is gathered and then sent back to the renderer.
    /// </summary>
    /// <param name="contextEntity"></param>
    /// <param name="matName"></param>
    private void RequestMaterial(string contextEntity, string matName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for material " + matName);
        EntityNameOgre entName = EntityNameOgre.ConvertOgreResourceToEntityName(contextEntity);
        m_workQueue.DoLater(new RequestMaterialDoLater(m_sceneMgr, entName, matName));
        return;
    }

    private class RequestMaterialDoLater : DoLaterBase {
        OgreSceneMgr m_sceneMgr;
        EntityNameOgre m_entName;
        string m_matName;
        public RequestMaterialDoLater(OgreSceneMgr smgr, EntityNameOgre entName, string matName) {
            m_sceneMgr = smgr;
            m_entName = entName;
            m_matName = matName;
        }
        public override bool DoIt() {
            try {
                IEntity ent;
                if (World.World.Instance.TryGetEntity(m_entName, out ent)) {
                    // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "RequestMaterialLater.DoIt(): converting {0}", entName);
                    if (RendererOgre.GetWorldRenderConv(ent) == null) {
                        // the rendering context is not set up. Odd but not fatal
                        // try again later
                        return false;
                    }
                    else {
                        // Create the material resource and then make the rendering redisplay
                        RendererOgre.GetWorldRenderConv(ent).CreateMaterialResource(m_sceneMgr, ent, m_matName);

                        Ogr.RefreshResourceBF(Ogr.ResourceTypeMaterial, m_matName);
                    }
                }
                else {
                    // we couldn't find the entity for the material. not good
                    LogManager.Log.Log(LogLevel.DBADERROR, 
                        "ProcessWaitingMaterials: could not find entity for material {0}", m_matName);
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, 
                    "ProcessWaitingMaterials: exception realizing material: {0}", e.ToString());
            }
            return true;
        }
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
        m_workQueue.DoLater(new RequestTextureLater(this, entName));
    }

    private sealed class RequestTextureLater: DoLaterBase {
        RendererOgre m_renderer;
        EntityNameOgre m_entName;
        public RequestTextureLater(RendererOgre renderer, EntityNameOgre entName) : base() {
            m_renderer = renderer;
            m_entName = entName;
        }
        public override bool DoIt() {
            // note the super kludge since we don't know the real asset context
            // This information is hopefully coded into the entity name
            AssetContextBase.RequestTextureLoad(m_entName, AssetContextBase.AssetType.Texture, TextureLoadedCallback);
            return true;
        }

        // the texture is loaded so get to the right time to tell the renderer
        private void TextureLoadedCallback(string textureEntityName, bool hasTransparancy) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, 
                    "TextureLoadedCallback {0}: Load complete. Name: {1}", this.sequence, textureEntityName);
            EntityNameOgre entName = new EntityNameOgre(textureEntityName);
            m_renderer.m_workQueue.DoLater(new RequestTextureCompletionLater(entName, hasTransparancy));
            return;
        }

        private sealed class RequestTextureCompletionLater : DoLaterBase {
            EntityNameOgre m_entName;
            bool m_hasTransparancy;
            public RequestTextureCompletionLater(EntityNameOgre entName, bool hasTransparancy) : base() {
                m_entName = entName;
                m_hasTransparancy = hasTransparancy;
                this.cost = m_betweenFrameMapTextureCost;
            }
            override public bool DoIt() {
                string ogreResourceName = m_entName.OgreResourceName;
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, 
                    "RequestTextureCompleteLater {0}: Load complete. Refreshing texture {1}, t={2}", 
                            this.sequence, ogreResourceName, m_hasTransparancy);
                if (m_hasTransparancy) {
                    Ogr.RefreshResourceBF(Ogr.ResourceTypeTransparentTexture, ogreResourceName);
                }
                else {
                    Ogr.RefreshResourceBF(Ogr.ResourceTypeTexture, ogreResourceName);
                }
                return true;
            }
        }
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
