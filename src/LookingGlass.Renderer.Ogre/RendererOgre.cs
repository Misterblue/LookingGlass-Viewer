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
using LookingGlass.View;
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

    protected UserInterfaceOgre m_userInterface = null;

    protected OgreSceneMgr m_sceneMgr;

    // this shouldn't be here... this is a feature of the LL renderer
    protected float m_sceneMagnification;
    public float SceneMagnification { get { return m_sceneMagnification; } }

    protected BasicWorkQueue m_workQueue = new BasicWorkQueue("OgreRendererWork");
    protected OnDemandWorkQueue m_betweenFramesQueue = new OnDemandWorkQueue("OgreBetweenFrames");
    private static int m_betweenFrameTotalCost = 500;
    private static int m_betweenFrameCreateMaterialCost = 5;
    private static int m_betweenFrameCreateSceneNodeCost = 20;
    private static int m_betweenFrameCreateMeshCost = 20;
    private static int m_betweenFrameRefreshMeshCost = 20;
    private static int m_betweenFrameMapRegionCost = 50;
    private static int m_betweenFrameUpdateTerrainCost = 50;
    private static int m_betweenFrameMapTextureCost = 10;

    private Thread m_rendererThread = null;

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
    public override void OnLoad(string name, IAppParameters parms) {
        m_moduleName = name;
        ModuleParams = parms;
        ModuleParams.AddDefaultParameter("Renderer.Ogre.Name", "LookingGlass",
                    "Name of the Ogre resources to load");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.SkyboxName", "LookingGlass/CloudyNoonSkyBox",
                    "Name of the skybox resource to use");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.ShadowTechnique", "none",
                    "Shadow technique: none, additive, modulative, stencil");
        // cp.AddParameter("Renderer.Ogre.Renderer", "Direct3D9 Rendering Subsystem",
        //             "Name of the rendering subsystem to use");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.Renderer", "OpenGL Rendering Subsystem",
                    "Name of the rendering subsystem to use");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.VideoMode", "800 x 600@ 32-bit colour",
                    "Initial window size");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.PluginFilename", "plugins.cfg",
                    "File that lists Ogre plugins to load");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.ResourcesFilename", "resources.cfg",
                    "File that lists the Ogre resources to load");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.DefaultNumMipmaps", "3",
                    "Default number of mip maps created for a texture (usually 6)");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.CacheDir", Globals.GetDefaultApplicationStorageDir(null),
                    "Directory to store cached meshs, textures, etc");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.PreLoadedDir", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/Preloaded/"),
                    "Directory to for preloaded textures, etc");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.DefaultTerrainMaterial",
                    "LookingGlass/DefaultTerrainMaterial",
                    "Material applied to terrain");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.OceanMaterialName",
                    "LookingGlass/Ocean",
                    "The ogre name of the ocean texture");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.DefaultMeshFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/LoadingShape.mesh"),
                    "Filename of the default shape found in the cache dir");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.DefaultTextureFilename", 
                    System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/LoadingTexture.png"),
                    "Filename of the default texture found in the cache dir");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.DefaultTextureResourceName", 
                    "LoadingTexture.png",
                    "Resource name of  the default texture");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.LL.SceneMagnification", "10",
                    "Magnification of LL coordinates into Ogre space");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.LL.EarlyMaterialCreate", "true",
                    "Create materials while creating mesh rather than waiting");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.SerializeMaterials", "false",
                    "Write out materials to files (replace with DB someday)");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.SerializeMeshes", "true",
                    "Write out meshes to files");
        ModuleParams.AddDefaultParameter("Renderer.Ogre.CaelumScript", "RainWindScriptTest",
                    "Write out meshes to files");

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
        m_restHandler = new RestHandler("/" + m_moduleName + "/detailStats/", m_stats);

        m_userInterface = new UserInterfaceOgre();

        if (m_log.WouldLog(LogLevel.DRENDERDETAIL)) {
            debugLogCallbackHandle = new Ogr.DebugLogCallback(OgrLogger);
            Ogr.SetDebugLogCallback(debugLogCallbackHandle);
        }
        fetchParameterCallbackHandle = new Ogr.FetchParameterCallback(ModuleParams.ParamString);
        Ogr.SetFetchParameterCallback(fetchParameterCallbackHandle);
        checkKeepRunningCallbackHandle = new Ogr.CheckKeepRunningCallback(CheckKeepRunning);
        Ogr.SetCheckKeepRunningCallback(checkKeepRunningCallbackHandle);
        userIOCallbackHandle = new Ogr.UserIOCallback(m_userInterface.ReceiveUserIO);
        Ogr.SetUserIOCallback(userIOCallbackHandle);
        requestResourceCallbackHandle = new Ogr.RequestResourceCallback(RequestResource);
        Ogr.SetRequestResourceCallback(requestResourceCallbackHandle);
        betweenFramesCallbackHandle = new Ogr.BetweenFramesCallback(ProcessBetweenFrames);
        Ogr.SetBetweenFramesCallback(betweenFramesCallbackHandle);

        m_sceneMagnification = float.Parse(Globals.Configuration.ParamString("Renderer.Ogre.LL.SceneMagnification"));

        Ogr.InitializeOgre();
        m_sceneMgr = new OgreSceneMgr(Ogr.GetSceneMgr());

        return true;
    }

    private void OgrLogger(string msg) {
        m_logOgre.Log(LogLevel.DRENDERDETAIL, msg);
    }

    private bool CheckKeepRunning() {
        return Globals.KeepRunning;
    }

    // ==========================================================================
    override public void Start() {
        // start the rendering loop
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

    // pass the main thread to the renderer. If he doesn't want it, he returns 'false'
    public bool RendererThread() {
        /*
        // create a thread for the renderer
        m_rendererThread = new Thread(RunRenderer);
        m_rendererThread.Name = "Renderer";
        m_rendererThread.Start();
        // tell the caller I'm not using the main thread
        return false;
         */
        return Ogr.RenderingThread();
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
            m_betweenFramesQueue.DoLater(new DoRender(m_sceneMgr, ent, m_log));
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
                        m_ri = RendererOgre.GetWorldRenderConv(m_ent).RenderingInfo(m_sceneMgr, m_ent);
                    }

                    // Find a handle to the parent for this node
                    OgreSceneNode parentNode = null;
                    if (m_ri.parentID != 0) {
                        // this entity has a parent entity. find him and create scene node off his
                        IEntity parentEnt;
                        if (World.World.Instance.TryGetEntityLocalID(m_ri.parentID, out parentEnt)) {
                            parentNode = RendererOgre.GetSceneNode(parentEnt);
                        }
                        if (parentNode == null) {
                            m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering {0}. {1} waiting for parent {2}",
                                this.sequence, m_ent.Name.Name, (parentEnt == null ? "NULL" : parentEnt.Name.Name));
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
        // OMV.Quaternion orient = new OMV.Quaternion(OMV.Vector3.UnitX, -Globals.PI / 2)
                    // * new OMV.Quaternion(OMV.Vector3.UnitZ, -Globals.PI / 2)
                    // * cam.Direction;
        OMV.Quaternion orient = cam.Heading;
        OMV.Vector3d pos = cam.GlobalPosition * m_sceneMagnification;
        Ogr.UpdateCamera((float)pos.X, (float)pos.Z, (float)-pos.Y, 
            orient.W, orient.X, orient.Y, orient.Z,
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
                break;
            case Ogr.ResourceTypeTexture:
                m_statTexturesRequested.Event();
                RequestTexture(resourceContext, resourceName);
                break;
        }
        return;
    }

    private Dictionary<string, int> waitingMeshes = new Dictionary<string,int>();
    private void RequestMesh(string contextEntity, string meshName) {
        lock (waitingMeshes) {
            if (waitingMeshes.ContainsKey(meshName)) {
                m_log.Log(LogLevel.DRENDERDETAIL, "Dup request for mesh " + meshName);
            }
            else {
                m_log.Log(LogLevel.DRENDERDETAIL, "Request for mesh " + meshName);
                waitingMeshes.Add(meshName, 0);
                m_betweenFramesQueue.DoLater(new RequestMeshLater(this, m_sceneMgr, meshName));
            }
        }
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
            this.cost = m_betweenFrameCreateMeshCost;
        }
        
        override public bool DoIt() {
            // type information is at the end of the name. remove if there
            string eName = EntityNameOgre.ConvertOgreResourceToEntityNameX(m_meshName);
            // the terrible kludge here is that the name of the mesh is the name of
            //   the entity. We reach back into the worlds and find the underlying
            //   entity then we can construct the mesh.
            IEntity ent;
            if (!World.World.Instance.TryGetEntity(eName, out ent)) {
                LogManager.Log.Log(LogLevel.DBADERROR, "RendererOgre.RequestMeshLater: could not find entity " + eName);
                return true;
            }
            // Create mesh resource. In this case its most likely a .mesh file in the cache
            if (!RendererOgre.GetWorldRenderConv(ent).CreateMeshResource(m_sceneMgr, ent, m_meshName)) {
                // we need to wait until some resource exists before we can complete this creation
                return false;
            }

            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.RequestMeshLater: queuing refresh for {0}, cnt={1}",
                        m_meshName, m_renderer.waitingMeshes.Count);
            m_renderer.m_betweenFramesQueue.DoLater(new RefreshMeshResourceLater(m_renderer, m_meshName));

            return true;
        }
    }

    // do the final unloading of the mesh between frames
    private sealed class RefreshMeshResourceLater : DoLaterBase {
        RendererOgre m_renderer;
        string m_meshName;
        public RefreshMeshResourceLater(RendererOgre renderer, string meshName) : base() {
            m_renderer = renderer;
            m_meshName = meshName;
            this.cost = m_betweenFrameRefreshMeshCost;
        }
        public override bool DoIt() {
            // tell Ogre to refresh (reload) the resource
            Ogr.RefreshResource(Ogr.ResourceTypeMesh, m_meshName);
            // we're no longer waiting for the mesh
            lock (m_renderer.waitingMeshes) {
                m_renderer.waitingMeshes.Remove(m_meshName);
            }
            return true;
        }
    }

    // ==========================================================================
    private struct AWaitingMaterial {
        public EntityNameOgre contextEntity;
        public string materialName;
        public AWaitingMaterial(EntityNameOgre con, string mat) {
            contextEntity = con;
            materialName = mat;
        }
    }

    // Experiement: what if we create the materials as requested
    private void RequestMaterialX(string contextEntity, string matName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for materialX " + matName);
        try{
            EntityNameOgre entName = EntityNameOgre.ConvertOgreResourceToEntityName(contextEntity);
            IEntity ent;
            if (World.World.Instance.TryGetEntity(entName, out ent)) {
                if (RendererOgre.GetWorldRenderConv(ent) == null) {
                    // the rendering context is not set up. Odd but not fatal
                    // try again later
                    m_log.Log(LogLevel.DRENDERDETAIL, "RequestMaterial. No context so queuing");
                    waitingMaterials.Enqueue(new AWaitingMaterial(entName, matName));
                }
                else {
                    // Create the material resource and then make the rendering redisplay
                    RendererOgre.GetWorldRenderConv(ent).CreateMaterialResource(m_sceneMgr, ent, matName);
                    Ogr.RefreshResource(Ogr.ResourceTypeMaterial, matName);
                }
            }
            else {
                // we couldn't find the entity for the material. not good
                m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: could not find entity for material " + matName);
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: exception realizing material: " + e.ToString());
        }
        return;
    }

    private Queue<AWaitingMaterial> waitingMaterials = new Queue<AWaitingMaterial>();
    private void RequestMaterial(string contextEntity, string matName) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Request for material2 " + matName);
        EntityNameOgre entName = EntityNameOgre.ConvertOgreResourceToEntityName(contextEntity);
        lock (waitingMaterials) {
            waitingMaterials.Enqueue(new AWaitingMaterial(entName, matName));
        }
        return;
    }

    // Called between frames to create soem of the required materials
    private void ProcessWaitingMaterials() {
        // take one tenth of the total between frame work to create materials
        int cnt = m_betweenFrameTotalCost / 10 / m_betweenFrameCreateMaterialCost;
        while ((waitingMaterials.Count > 0) && (--cnt > 0)) {
            // the manual ways it's thread safe. Throws if the queue is empty
            try {
                AWaitingMaterial wm = waitingMaterials.Dequeue();
                IEntity ent;
                if (World.World.Instance.TryGetEntity(wm.contextEntity, out ent)) {
                    // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "RequestMaterialLater.DoIt(): converting {0}", entName);
                    if (RendererOgre.GetWorldRenderConv(ent) == null) {
                        // the rendering context is not set up. Odd but not fatal
                        // try again later
                        waitingMaterials.Enqueue(wm);
                    }
                    else {
                        // Create the material resource and then make the rendering redisplay
                        int interval = m_statCreateMaterialInterval.In();
                        RendererOgre.GetWorldRenderConv(ent).CreateMaterialResource(m_sceneMgr, ent, wm.materialName);
                        m_statCreateMaterialInterval.Out(interval);

                        interval = m_statRefreshMaterialInterval.In();
                        Ogr.RefreshResource(Ogr.ResourceTypeMaterial, wm.materialName);
                        m_statRefreshMaterialInterval.Out(interval);
                    }
                }
                else {
                    // we couldn't find the entity for the material. not good
                    m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: could not find entity for material " + wm.materialName);
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "ProcessWaitingMaterials: exception realizing material: " + e.ToString());
            }
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
            AssetContextBase.RequestTextureLoad(m_entName.Name, TextureLoadedCallback);
            return true;
        }

        // the texture is loaded so get to the right time to tell the renderer
        private void TextureLoadedCallback(string textureEntityName) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, 
                    "TextureLoadedCallback {0}: Load complete. Name: {1}", this.sequence, textureEntityName);
            EntityNameOgre entName = new EntityNameOgre(textureEntityName);
            m_renderer.m_betweenFramesQueue.DoLater(new RequestTextureCompletionLater(entName));
            return;
        }

        private sealed class RequestTextureCompletionLater : DoLaterBase {
            EntityNameOgre m_entName;
            public RequestTextureCompletionLater(EntityNameOgre entName) : base() {
                m_entName = entName;
                this.cost = m_betweenFrameMapTextureCost;
            }
            override public bool DoIt() {
                string ogreResourceName = m_entName.OgreResourceName;
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, 
                    "RequestTextureCompleteLater {0}: Load complete. Refreshing texture {1}", 
                            this.sequence, ogreResourceName);
                Ogr.RefreshResource(Ogr.ResourceTypeTexture, ogreResourceName);
                return true;
            }
        }
    }

    // ==========================================================================
    /// <summary>
    /// If there is work queued to happen between frames. Do some of the work now.
    /// </summary>
    private bool ProcessBetweenFrames() {
        ProcessWaitingMaterials();
        if (m_betweenFramesQueue != null) {
            m_betweenFramesQueue.ProcessQueue(m_betweenFrameTotalCost);
        }
        return Globals.KeepRunning;
    }
}
}
