/* Copyright 2008 (c) Robert Adams
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
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LookingGlass;
using LookingGlass.Comm;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.Rest;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Comm.LLLP {
/// <summary>
/// Communication handler for Linden Lab Legacy Protocol
/// </summary>
public class CommLLLP : ModuleBase, LookingGlass.Comm.ICommProvider  {
    protected ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // ICommProvider.Name
    public string Name { get { return this.ModuleName; } }

    // ICommProvider.CommStatistics
    protected ParameterSet m_commStatistics;
    public ParameterSet CommStatistics() { return m_commStatistics; }
    protected RestHandler m_commStatsHandler;

    // ICommProvider.GridClient
    protected OMV.GridClient m_client;
    public OMV.GridClient GridClient { get { return m_client;} }

    // list of the region information build for the simulator
    protected List<LLRegionContext> m_regionList;

    // while we wait for a region to be online, we queue requests here
    protected Dictionary<RegionContextBase, OnDemandWorkQueue> m_waitTilOnline;
    protected bool m_shouldWaitTilOnline = true;

    protected LLAssetContext m_defaultAssetContext = null;
    protected LLAssetContext DefaultAssetContext {
        get {
            if (m_defaultAssetContext == null) {
                // Create the asset contect for this communication instance
                // this should happen after connected. reconnection is a problem.
                m_log.Log(LogLevel.DBADERROR, "CommLLLP: creating default asset context for grid {0}", m_loginGrid);
                m_defaultAssetContext = new LLAssetContext(LoggedInGridName);
                m_defaultAssetContext.InitializeContext(this,
                    ModuleParams.ParamString(ModuleName + ".Assets.CacheDir"),
                    ModuleParams.ParamInt(ModuleName + ".Texture.MaxRequests"));
            }
            return m_defaultAssetContext;
        }
        set { m_defaultAssetContext = value; }
    }

    // There are some messages that come in that are rare but could use some locking.
    // The main paths of prims and updates is pretty solid and multi-threaded but
    // others, like avatar control, can use a little locking.
    private Object m_opLock = new Object();

    /// <summary>
    /// Flag saying we're switching simulator connections. This would suppress things like teleport
    /// and certain status indications.
    /// </summary>
    public bool SwitchingSims { get { return m_SwitchingSims; } }
    protected bool m_SwitchingSims;       // true when we're setting up the connection to a different sim

    protected ParameterSet m_connectionParams = null;
    public ParameterSet ConnectionParams {
        get { return m_connectionParams; }
    }

    // The whole module is loaded or unloaded. This controls the whole trying to login loop.
    // m_shouldBeLoggedIn says whether we think we should be logged in. If true then the
    // first, last, ... parameters have the info to use logging in.
    // The logging in and out flags are true when we're doing that. Use to make sure
    // we don't try logging in or out again.
    // The module flag 'm_connected' is set true when logged in and connected.
    protected Thread m_LoginThread = null;
    protected bool m_loaded = false;  // if comm is loaded and should be trying to connect
    protected bool m_shouldBeLoggedIn;    // true if we should be logged in
    protected bool m_isLoggingIn;         // true if we are in the process of loggin in
    protected bool m_isLoggingOut;        // true if we are in the process of logging out
    protected string m_loginFirst, m_loginLast, m_loginPassword, m_loginGrid, m_loginSim;
    // m_loginGrid has the displayable name. LoggedInGridName has cannoicalized name for app use.
    protected string LoggedInGridName { get { return m_loginGrid.Replace(".", "_").ToLower(); } }
    protected string m_loginMsg = "";
    public const string FIELDFIRST = "first";
    public const string FIELDLAST = "last";
    public const string FIELDPASS = "password";
    public const string FIELDGRID = "grid";
    public const string FIELDSIM = "sim";
    public const string FIELDMSG = "msg";
    public const string FIELDCURRENTSIM = "currentsim"; // the current simulator
    public const string FIELDCURRENTGRID = "currentgrid"; // the current simulator
    public const string FIELDLOGINSTATE = "loginstate"; // the current login state
    public const string FIELDPOSSIBLEGRIDS = "possiblegrids"; // the list of possible grids
    public const string FIELDPOSITIONX = "positionx"; // the client relative location
    public const string FIELDPOSITIONY = "positiony";
    public const string FIELDPOSITIONZ = "positionz";


    protected GridLists m_gridLists = null;

    protected IAgent m_myAgent = null;

    public CommLLLP() {
        InitVariables();
        InitLoginParameters();
    }

    protected void InitVariables() {
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        m_isConnected = false;
        m_isLoggedIn = false;
        m_isLoggingIn = false;
        m_isLoggingOut = false;
        m_connectionParams = new ParameterSet();
        m_regionList = new List<LLRegionContext>();
        m_waitTilOnline = new Dictionary<RegionContextBase,OnDemandWorkQueue>();
        m_commStatistics = new ParameterSet();

        m_loginGrid = "Unknown";
        m_gridLists = new GridLists();
    }

    protected void InitLoginParameters() {
        m_connectionParams.Add(FIELDFIRST, "");
        m_connectionParams.Add(FIELDLAST, "");
        m_connectionParams.Add(FIELDPASS, "");
        m_connectionParams.Add(FIELDGRID, "");
        m_connectionParams.Add(FIELDSIM, "");
        m_connectionParams.Add(FIELDMSG, delegate(string k) {
            return new OMVSD.OSDString(m_loginMsg);
        });
        // some of the values are calculated when they are fetched. Provide delgates
        m_connectionParams.Add(FIELDCURRENTSIM, RuntimeValueFetch);
        m_connectionParams.Add(FIELDCURRENTGRID, RuntimeValueFetch);
        m_connectionParams.Add(FIELDPOSSIBLEGRIDS, delegate(string k) {
            try {
                OMVSD.OSDArray gridNames = new OMVSD.OSDArray();
                m_gridLists.ForEach(delegate(OMVSD.OSDMap gg) { gridNames.Add(gg["Name"]); });
                return gridNames;
            }
            catch {
            }
            return new OMVSD.OSDArray();
        });
        m_connectionParams.Add(FIELDLOGINSTATE, delegate(string k) {
            if (m_isLoggedIn) {
                return new OMVSD.OSDString("login");
            }
            if (m_isLoggingIn) {
                return new OMVSD.OSDString("loggingin");
            }
            if (m_isLoggingOut) {
                return new OMVSD.OSDString("loggingout");
            }
            return new OMVSD.OSDString("logout");
        });
        m_connectionParams.Add(FIELDPOSITIONX, RuntimeValueFetch);
        m_connectionParams.Add(FIELDPOSITIONY, RuntimeValueFetch);
        m_connectionParams.Add(FIELDPOSITIONZ, RuntimeValueFetch);
    }

    /// <summary>
    /// The statistics ParameterSet has some delegated values that are only valid
    /// when logging in, etc. When the values are asked for, this routine is called
    /// delegate which calculates the current values of the statistic.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected OMVSD.OSD RuntimeValueFetch(string key) {
        OMVSD.OSD ret = null;
        try {
            if ((m_client != null) && (IsConnected && m_isLoggedIn)) {
                switch (key) {
                    case FIELDCURRENTSIM:
                        ret = new OMVSD.OSDString(m_client.Network.CurrentSim.Name);
                        break;
                    case FIELDCURRENTGRID:
                        ret = new OMVSD.OSDString(m_loginGrid);
                        break;
                    case FIELDPOSITIONX:
                        ret = new OMVSD.OSDString(m_client.Self.SimPosition.X.ToString());
                        break;
                    case FIELDPOSITIONY:
                        ret = new OMVSD.OSDString(m_client.Self.SimPosition.Y.ToString());
                        break;
                    case FIELDPOSITIONZ:
                        ret = new OMVSD.OSDString(m_client.Self.SimPosition.Z.ToString());
                        break;
                }
                if (ret != null) return ret;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DCOMM, "RuntimeValueFetch: failure getting {0}: {1}", key, e.ToString());
        }
        return new OMVSD.OSDString("");
    }

    #region IModule methods

    // IModule.OnLoad
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        OnLoad2(name, lgbase, true);
    }

    // Internal OnLoad that can be used by derived classes
    protected void OnLoad2(string name, LookingGlassBase lgbase, bool shouldInit) {
        base.OnLoad(name, lgbase);
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.CacheDir", 
                    Utilities.GetDefaultApplicationStorageDir(null),
                    "Filesystem location to build the texture cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.OMVResources",
                    "../LookingGlassResources/openmetaverse_data",
                    "Directory for resources used by libopenmetaverse (mostly for appearance)");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.NoTextureFilename", 
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../LookingGlassResources/NoTexture.png"),
                    "Filesystem location to build the texture cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.ConvertPNG", "true",
                    "whether to convert incoming JPEG2000 files to PNG files in the cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Texture.MaxRequests", 
                    "6",
                    "Maximum number of outstanding textures requests");
        ModuleParams.AddDefaultParameter(ModuleName + ".Settings.MultipleSims", 
                    "false",
                    "Whether to enable multiple sims");

        if (shouldInit) InitConnectionFramework();
    }

    protected void InitConnectionFramework() {
        // Initialize the SL client
        try {
            m_client = new OMV.GridClient();
            m_client.Settings.ENABLE_CAPS = true;
            m_client.Settings.ENABLE_SIMSTATS = true;
            m_client.Settings.MULTIPLE_SIMS = ModuleParams.ParamBool(ModuleName + ".Settings.MultipleSims");
            m_client.Settings.ALWAYS_DECODE_OBJECTS = true;
            m_client.Settings.ALWAYS_REQUEST_OBJECTS = true;
            m_client.Settings.OBJECT_TRACKING = true; // We use our own object tracking system
            m_client.Settings.AVATAR_TRACKING = true; //but we want to use the libsl avatar system
            m_client.Settings.SEND_AGENT_APPEARANCE = true;    // for the moment, don't do appearance
            m_client.Settings.SEND_AGENT_THROTTLE = true;    // tell them how fast we want it when connected
            m_client.Settings.PARCEL_TRACKING = false;
            m_client.Settings.ALWAYS_REQUEST_PARCEL_ACL = false;
            m_client.Settings.ALWAYS_REQUEST_PARCEL_DWELL = false;
            m_client.Settings.USE_INTERPOLATION_TIMER = false;  // don't need the library helping
            m_client.Settings.SEND_AGENT_UPDATES = true;
            m_client.Self.Movement.AutoResetControls = false;   // Do the key up and down to turn on and off
            m_client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;
            m_client.Settings.USE_ASSET_CACHE = false;
            m_client.Settings.PIPELINE_REQUEST_TIMEOUT = 120 * 1000;
            m_client.Settings.ASSET_CACHE_DIR = ModuleParams.ParamString(ModuleName + ".Assets.CacheDir");
            OMV.Settings.RESOURCE_DIR = ModuleParams.ParamString(ModuleName + ".Assets.OMVResources");
            // Crank up the throttle on texture downloads
            m_client.Throttle.Total = 2000000.0f;
            m_client.Throttle.Texture = 2446000.0f;
            m_client.Throttle.Asset = 2446000.0f;
            m_client.Settings.THROTTLE_OUTGOING_PACKETS = true;

            m_client.Network.OnLogin += new OMV.NetworkManager.LoginCallback(Network_OnLogin);
            m_client.Network.OnDisconnected += new OMV.NetworkManager.DisconnectedCallback(Network_OnDisconnected);
            m_client.Network.OnCurrentSimChanged += new OMV.NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            m_client.Network.OnEventQueueRunning += new OMV.NetworkManager.EventQueueRunningCallback(Network_OnEventQueueRunning);

        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "EXCEPTION BUILDING GRIDCLIENT: " + e.ToString());
        }

        // fake like this is the initial teleport
        m_SwitchingSims = true;
    }

    // IModule.Start()
    public override void Start() {
        Start2(true);
    }

    // internal Start to be used by derived classes
    protected void Start2(bool shouldKeepLoggedin) {
        base.Start();
        m_loaded = true;
        if (shouldKeepLoggedin) {
            if (m_LoginThread == null) {
                m_LoginThread = new Thread(KeepLoggedIn);
                m_LoginThread.Name = "Communication Login";
                m_log.Log(LogLevel.DCOMMDETAIL, "Starting keep logged in thread");
                m_LoginThread.Start();
            }
        }
    }

    // IModule.Stop()
    // If the base system says to stop, we make sure we're disconnected
    public override void Stop() {
        base.Stop();
        m_log.Log(LogLevel.DCOMMDETAIL, "Stopping. Attempting to disconnect");
        Disconnect();
    }

    // IModule.PrepareForUnload()
    public override bool PrepareForUnload() {
        base.PrepareForUnload();
        m_log.Log(LogLevel.DCOMMDETAIL, "communication unload. We'll never login again");
        if (m_commStatsHandler != null) {
            m_commStatsHandler.Dispose();   // get rid of the handlers we created
            m_commStatsHandler = null;
        }
        m_loaded = false; ;
        return true;
    }

    // ICommProvider.Connect()
    public virtual bool Connect(ParameterSet parms) {
        // Are we already logged in?
        if (m_isLoggedIn || m_isLoggingIn) {
            return false;
        }

        m_loginFirst = parms.ParamString(FIELDFIRST);
        m_loginLast = parms.ParamString(FIELDLAST);
        m_loginPassword = parms.ParamString(FIELDPASS);
        m_loginGrid = parms.ParamString(FIELDGRID);
        m_loginSim = parms.ParamString(FIELDSIM);

        // put it in the connection parameters so it shows up in status
        m_connectionParams.UpdateSilent(FIELDFIRST, m_loginFirst);
        m_connectionParams.UpdateSilent(FIELDLAST, m_loginLast);
        m_connectionParams.UpdateSilent(FIELDGRID, m_loginGrid);
        m_connectionParams.UpdateSilent(FIELDSIM, m_loginSim);
        
        // push some back to the user params so it can be persisted for next session
        ModuleParams.AddUserParameter("User.FirstName", m_loginFirst, null);
        ModuleParams.AddUserParameter("User.LastName", m_loginLast, null);
        ModuleParams.AddUserParameter("User.Grid", m_loginGrid, null);
        ModuleParams.AddUserParameter("User.Sim", m_loginGrid, null);

        m_shouldBeLoggedIn = true;
        return true;
    }

    // ICommProvider.Disconnect()
    public virtual bool Disconnect() {
        m_shouldBeLoggedIn = false;
        m_log.Log(LogLevel.DCOMMDETAIL, "Should not be logged in");
        return true;
    }

    /// <summary>
    /// Using its own thread, this sits around checking to see if we're logged in
    /// and, if not, starting the login dialog so we can get logged in.
    /// </summary>
    protected void KeepLoggedIn() {
        while (m_loaded) {
            if (m_shouldBeLoggedIn && !IsLoggedIn) {
                // we should be logged in and we are not
                if (!m_isLoggingIn) {
                    StartLogin();
                }
            }
            if (!LGB.KeepRunning || (!m_shouldBeLoggedIn && IsLoggedIn)) {
                // we shouldn't be logged in but it looks like we are
                if (!m_isLoggingIn && !m_isLoggingOut) {
                    // not in logging transistion. start the logout process
                    m_log.Log(LogLevel.DCOMMDETAIL, "KeepLoggedIn: Starting logout process");
                    m_client.Network.Logout();
                    m_isLoggingIn = false;
                    m_isLoggingOut = true;
                }
            }
            // update our login parameters for the UI

            Thread.Sleep(1*1000);
        }
    }

    public void StartLogin() {
        m_log.Log(LogLevel.DCOMMDETAIL, "Starting login of {0} {1}", m_loginFirst, m_loginLast);
        m_isLoggingIn = true;
        OMV.LoginParams loginParams = this.GridClient.Network.DefaultLoginParams(
            m_loginFirst,
            m_loginLast,
            m_loginPassword,
            LookingGlassBase.ApplicationName, 
            LookingGlassBase.ApplicationVersion);

        // Select sim in the grid
        // the format that we must pass is "uri:sim&x&y&z" or the strings "home" or "last"
        // The user inputs either "home", "last", "sim" or "sim/x/y/z"
        string loginSetting = null;
        if ((m_loginSim != null) && (m_loginSim.Length > 0)) {
            try {
                char sep = '/';
                string[] parts = System.Uri.UnescapeDataString(m_loginSim).ToLower().Split(sep);
                if (parts.Length == 1) {
                    // just specifying last or home or just a simulator
                    if (parts[0] == "last" || parts[0] == "home") {
                        m_log.Log(LogLevel.DCOMMDETAIL, "StartLogin: prev location of {0}", parts[0]);
                        loginSetting = parts[0];
                    }
                    else {
                        // put the user in the center of teh specified sim
                        m_log.Log(LogLevel.DCOMMDETAIL, "StartLogin: user spec middle of {0}", parts[0]);
                        loginSetting = OMV.NetworkManager.StartLocation(parts[0], 128, 128, 40);
                    }
                }
                else if (parts.Length == 4) {
                    int posX = int.Parse(parts[1]);
                    int posY = int.Parse(parts[2]);
                    int posZ = int.Parse(parts[3]);
                    m_log.Log(LogLevel.DCOMMDETAIL, "StartLogin: user spec start at {0}/{1}/{2}/{3}",
                        parts[0], posX, posY, posZ);
                    loginSetting = OMV.NetworkManager.StartLocation(parts[0], posX, posY, posZ);
                }
            }
            catch {
                loginSetting = null;
            }
        }
        loginParams.Start = (loginSetting == null) ? "last" : loginSetting;

        loginParams.URI = m_gridLists.GridLoginURI(m_loginGrid);
        if (loginParams.URI == null) {
            m_log.Log(LogLevel.DBADERROR, "COULD NOT FIND URL OF GRID. Grid=" + m_loginGrid);
            m_loginMsg = "Unknown Grid name";
            m_isLoggingIn = false;
            m_shouldBeLoggedIn = false;
        }
        else {
            try {
                this.GridClient.Network.Login(loginParams);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "BeginLogin exception: " + e.ToString());
                m_isLoggingIn = false;
                m_shouldBeLoggedIn = false;
            }
        }
        return;
    }


    public virtual void Network_OnLogin(OMV.LoginStatus login, string message) {
        if (login == OMV.LoginStatus.Success) {
            m_log.Log(LogLevel.DCOMM, "Successful login: {0}", message);
            m_isConnected = true;
            m_isLoggedIn = true;
            m_isLoggingIn = false;
            m_loginMsg = message;
            Comm_OnLoggedIn();
        }
        else if (login == OMV.LoginStatus.Failed) {
            m_log.Log(LogLevel.DCOMM, "Login failed: {0}", message);
            m_isLoggingIn = false;
            m_shouldBeLoggedIn = false;
            m_loginMsg = message;
        }
    }

    public virtual void Network_OnDisconnected(OMV.NetworkManager.DisconnectType reason, string message) {
        m_log.Log(LogLevel.DCOMMDETAIL, "Disconnected");
        m_isConnected = false;
        //x BeginInvoke(
        //x     (MethodInvoker)delegate() {
        //x         cmdTeleport.Enabled = false;
        //x         DoLogout();
        //x });
    }

    public virtual void Network_OnEventQueueRunning(OMV.Simulator simulator) {
        m_log.Log(LogLevel.DCOMMDETAIL, "Event queue running on {0}", simulator.Name);
        if (simulator == m_client.Network.CurrentSim) {
            m_SwitchingSims = false;
        }
        // Now seems like a good time to start requesting parcel information
        m_client.Parcels.RequestAllSimParcels(m_client.Network.CurrentSim, false, 100);
    }


    public override bool AfterAllModulesLoaded() {
        // make my connections for the communication events
        OMV.GridClient gc = m_client;
        gc.Network.OnSimConnected += new OMV.NetworkManager.SimConnectedCallback(Network_OnSimConnected);
        gc.Network.OnCurrentSimChanged += new OMV.NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
        gc.Objects.OnNewPrim += new OMV.ObjectManager.NewPrimCallback(Objects_OnNewPrim);
        gc.Objects.OnNewAttachment += new OMV.ObjectManager.NewAttachmentCallback(Objects_OnNewAttachment);
        gc.Objects.OnObjectUpdated += new OMV.ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
        // NewAttachmentCallback
        gc.Objects.OnNewAvatar += new OMV.ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
        // AvatarSitChangedCallback
        // ObjectPropertiesCallback
        gc.Objects.OnObjectKilled += new OMV.ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
        gc.Settings.STORE_LAND_PATCHES = true;
        gc.Terrain.OnLandPatch += new OMV.TerrainManager.LandPatchCallback(Terrain_OnLandPatch);
        gc.Parcels.OnSimParcelsDownloaded += new OMV.ParcelManager.SimParcelsDownloaded(Parcels_OnSimParcelsDownloaded);

        m_commStatsHandler = new RestHandler("/stats/" + m_moduleName + "/stats", m_commStatistics);
        m_commStatistics.Add("WaitingTilOnline", delegate(string xx) { 
            return new OMVSD.OSDString(m_waitTilOnline.Count.ToString()); });

        return true;
    }
    #endregion IModule methods

    #region ICommProvider
    protected bool m_isConnected;
    public bool IsConnected { get { return m_isConnected; } }

    protected bool m_isLoggedIn;
    public bool IsLoggedIn { get { return m_isLoggedIn; } }

    #endregion ICommProvider
    // ===============================================================
    public virtual void Network_OnSimConnected(OMV.Simulator sim) {
        m_isConnected = true;   // good enough reason to think we're connected
        m_log.Log(LogLevel.DWORLDDETAIL, "Simulator connected {0}", sim.Name);
        LLRegionContext regionContext = FindRegion(sim);
        if (regionContext == null) return;
        World.World.Instance.AddRegion(regionContext);

        // this region is online and here. This can start a lot of IO
        regionContext.State.State = RegionStateCode.Online;

        // if we'd queued up actions, do them now that it's online
        DoAnyWaitingEvents(regionContext);

        // this is needed to make the avatar appear
        // TODO: figure out if the linking between agent and appearance is right
        // m_client.Appearance.SetPreviousAppearance(true);
        m_client.Self.Movement.UpdateFromHeading(0.0, true);
    }

    // ===============================================================
    public virtual void Network_OnCurrentSimChanged(OMV.Simulator prevSim) {
        // disable teleports until we have a good connection to the simulator (event queue working)
        if (!m_client.Network.CurrentSim.Caps.IsEventQueueRunning) {
            m_SwitchingSims = true;
        }
        if (prevSim != null) {      // there is no prev sim the first time
            m_log.Log(LogLevel.DWORLDDETAIL, "Simulator changed from {0}", prevSim.Name);
            LLRegionContext regionContext = FindRegion(prevSim);
            if (regionContext == null) return;
            // TODO: what to do with this operation?
        }
    }

    // ===============================================================
    public virtual void Parcels_OnSimParcelsDownloaded(OMV.Simulator simulator, OMV.InternalDictionary<int, OMV.Parcel> simParcels, int[,] parcelMap) {
        m_log.Log(LogLevel.DWORLDDETAIL, "Sim parcels downloaded");
        //x TotalPrims = 0;
        //x simParcels.ForEach(
        //x     delegate(Parcel parcel) {
        //x         TotalPrims += parcel.TotalPrims;
        //x     });
    }

    // ===============================================================
    public virtual void Terrain_OnLandPatch(OMV.Simulator sim, int x, int y, int width, float[] data) {
        // m_log.Log(LogLevel.DWORLDDETAIL, "Land patch for {0}: {1}, {2}, {3}", 
        //             sim.Name, x.ToString(), y.ToString(), width.ToString());
        LLRegionContext regionContext = FindRegion(sim);
        if (regionContext == null) return;
        // update the region's view of the terrain
        regionContext.TerrainInfo.UpdatePatch(regionContext, x, y, data);
        // tell the world the earth is moving
        if (regionContext.State.IfNotOnline(delegate() {
                QueueTilOnline(regionContext, CommActionCode.RegionStateChange, regionContext, World.UpdateCodes.Terrain);
            }) ) return;
        regionContext.Update(World.UpdateCodes.Terrain);
    }

    // ===============================================================
    public virtual void Objects_OnNewPrim(OMV.Simulator sim, OMV.Primitive prim, ulong regionHandle, ushort timeDilation) {
        LLRegionContext rcontext = FindRegion(sim);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnNewPrim, sim, prim, regionHandle, timeDilation);
            }) ) return;
        IEntity ent = null; ;
        lock (m_opLock) {
            m_log.Log(LogLevel.DCOMMDETAIL, "OnNewPrim: id={0}, lid={1}", prim.ID.ToString(), prim.LocalID);
            try {
                if (rcontext.TryGetCreateEntityLocalID(prim.LocalID, out ent, delegate() {
                            IEntity newEnt = new LLEntityPhysical(rcontext.AssetContext,
                                            rcontext, regionHandle, prim.LocalID, prim);
                            return newEnt;
                        }) ) {
                    // new prim created
                }
                else {
                    // if not created, make an update happen
                    if (ent != null) ent.Update(UpdateCodes.FullUpdate);
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FAILED CREATION OF NEW PRIM: " + e.ToString());
            }
        }
        return;
    }

    // ===============================================================
    public virtual void Objects_OnNewAttachment(OMV.Simulator sim, OMV.Primitive prim, ulong regionHandle, ushort timeDilation) {
        LLRegionContext rcontext = FindRegion(sim);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnNewPrim, sim, prim, regionHandle, timeDilation);
            }) ) return;
        m_log.Log(LogLevel.DCOMMDETAIL, "OnNewAttachment: id={0}, lid={1}", prim.ID.ToString(), prim.LocalID);
        try {
            IEntity ent;
            if (rcontext.TryGetCreateEntityLocalID(prim.LocalID, out ent, delegate() {
                        IEntity newEnt = new LLEntityPhysical(rcontext.AssetContext,
                                        rcontext, regionHandle, prim.LocalID, prim);
                        return newEnt;
                    }) ) {
                // if new or not, assume everything about this entity has changed
                rcontext.UpdateEntity(ent, UpdateCodes.FullUpdate | UpdateCodes.New);
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "FAILED CREATION OF NEW ATTACHMENT");
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "FAILED CREATION OF NEW ATTACHMENT: " + e.ToString());
        }
        return;
    }

    // ===============================================================
    public virtual void Objects_OnObjectUpdated(OMV.Simulator sim, OMV.ObjectUpdate update, ulong regionHandle, ushort timeDilation) {
        LLRegionContext rcontext = FindRegion(sim);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnObjectUpdated, sim, update, regionHandle, timeDilation);
            }) ) return;
        IEntity updatedEntity = null;
        UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                    | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
        lock (m_opLock) {
            m_log.Log(LogLevel.DCOMMDETAIL, "Object update: id={0}, p={1}, r={2}", 
                update.LocalID, update.Position.ToString(), update.Rotation.ToString());
            // assume somethings changed no matter what
            if (update.Avatar) updateFlags |= UpdateCodes.CollisionPlane;
            if (update.Textures != null) updateFlags |= UpdateCodes.Textures;

            if (rcontext.TryGetEntityLocalID(update.LocalID, out updatedEntity)) {
                if ((updateFlags & UpdateCodes.Position) != 0) {
                    updatedEntity.RelativePosition = update.Position;
                }
                if ((updateFlags & UpdateCodes.Rotation) != 0) {
                    updatedEntity.Heading = update.Rotation;
                }
            }
            else {
                m_log.Log(LogLevel.DCOMM, "OnObjectUpdated: can't find local ID {0}. NOT UPDATING", update.LocalID);
            }
        }
        // special update for the agent so it knows there is new info from the network
        // The real logic to push the update through happens in the IEntityAvatar.Update()
        if (updatedEntity != null) {
            if (m_myAgent != null && updatedEntity == m_myAgent.AssociatedAvatar) {
                m_myAgent.DataUpdate(updateFlags);
            }
            updatedEntity.Update(updateFlags);
        }

        return;
    }

    // ===============================================================
    public virtual void Objects_OnObjectKilled(OMV.Simulator sim, uint objectID) {
        LLRegionContext rcontext = FindRegion(sim);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnObjectKilled, sim, objectID);
            }) ) return;
        m_log.Log(LogLevel.DWORLDDETAIL, "Object killed:");
        try {
            IEntity removedEntity;
            if (rcontext.TryGetEntityLocalID(objectID, out removedEntity)) {
                // we need a handle to the objectID
                rcontext.RemoveEntity(removedEntity);
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "FAILED DELETION OF OBJECT: " + e.ToString());
        }
        return;
    }

    // ===============================================================
    /// <summary>
    /// This routine is called when there is a new avatar and whenever the avatar is
    /// updated. Should be called "OnNewOrUpdatedAvatar". 
    /// </summary>
    /// <param name="sim"></param>
    /// <param name="av"></param>
    /// <param name="regionHandle"></param>
    /// <param name="timeDilation"></param>
    public virtual void Objects_OnNewAvatar(OMV.Simulator sim, OMV.Avatar av, ulong regionHandle, ushort timeDilation) {
        LLRegionContext rcontext = FindRegion(sim);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnNewAvatar, sim, av, regionHandle, timeDilation);
            }) ) return;
        IEntity updatedEntity = null;
        UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                    | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
        lock (m_opLock) {
            m_log.Log(LogLevel.DCOMMDETAIL, "Objects_OnNewAvatar:");
            m_log.Log(LogLevel.DCOMMDETAIL, "cntl={0}, parent={1}, p={2}, r={3}", 
                    av.ControlFlags.ToString("x"), av.ParentID, av.Position.ToString(), av.Rotation.ToString());


            // assume somethings changed no matter what
            updateFlags |= UpdateCodes.CollisionPlane;

            EntityName avatarEntityName = LLEntityAvatar.AvatarEntityNameFromID(rcontext.AssetContext, av.ID);
            if (rcontext.TryGetCreateEntity(avatarEntityName, out updatedEntity, delegate() {
                            m_log.Log(LogLevel.DCOMMDETAIL, "OnNewAvatar: creating avatar {0} {1} ({2})",
                                av.FirstName, av.LastName, av.ID.ToString());
                            IEntityAvatar newEnt = new LLEntityAvatar(rcontext.AssetContext,
                                            rcontext, regionHandle, av);
                            return (IEntity)newEnt;
                        }) ) {
                updatedEntity.RelativePosition = av.Position;
                updatedEntity.Heading = av.Rotation;
                updateFlags |= UpdateCodes.New;     // a new avatar
            }
        }

        // we can check here if this avatar goes with the agent in the world
        // If this av is with the agent, make the connection
        if (av.LocalID == m_client.Self.LocalID) {
            m_log.Log(LogLevel.DCOMMDETAIL, "OnNewAvatar: associating agent with new avatar");
            m_myAgent.AssociatedAvatar = (IEntityAvatar)updatedEntity;
            // an extra update for the agent so it knows things have changed
            m_myAgent.DataUpdate(updateFlags);
        }

        // tell the entity it changed. Since this is an avatar entity it will update the agent if necessary.
        if (updatedEntity != null) updatedEntity.Update(updateFlags);
        return;
    }

    // ===============================================================
    /// <summary>
    /// Called when we just log in. We create our agent and put it into the world
    /// </summary>
    public virtual void Comm_OnLoggedIn() {
        m_log.Log(LogLevel.DWORLDDETAIL, "Comm_OnLoggedIn:");
        if (m_myAgent != null) {
            m_log.Log(LogLevel.DWORLDDETAIL, "Comm_OnLoggedIn: Removing agent that is already here");
            // there shouldn't be on already there... odd but remove it
            World.World.Instance.RemoveAgent();
            m_myAgent = null;
        }
        m_myAgent = new LLAgent(m_client);
        World.World.Instance.AddAgent(m_myAgent);
        // I work by taking LLLP messages and updating the agent
        // The agent will be updated in the world (usually by the viewer)
        // Create the two way communication linkage
        m_myAgent.OnAgentUpdated += new AgentUpdatedCallback(Comm_OnAgentUpdated);
    }

    // ===============================================================
    public virtual void Comm_OnLoggedOut() {
        m_log.Log(LogLevel.DWORLDDETAIL, "Comm_OnLoggedOut:");
    }

    // ===============================================================
    public virtual void Comm_OnAgentUpdated(IAgent agnt, UpdateCodes what) {
        m_log.Log(LogLevel.DWORLDDETAIL, "Comm_OnAgentUpdated:");

    }

    // ===============================================================
    // given a simulator. Find the region info that we store the stuff in
    // Note that, if we are not connected, we just return null thus showing our unhappiness.
    public virtual LLRegionContext FindRegion(OMV.Simulator sim) {
        LLRegionContext ret = null;
        if (IsConnected) {
            lock (m_regionList) {
                foreach (LLRegionContext reg in m_regionList) {
                    if (reg.Simulator.ID == sim.ID) {
                        ret = reg;
                        break;
                    }
                }
                if (ret == null) {
                    LLTerrainInfo llterr = new LLTerrainInfo(null, DefaultAssetContext);
                    llterr.WaterHeight = sim.WaterHeight;
                    // TODO: copy terrain texture IDs

                    ret = new LLRegionContext(null, DefaultAssetContext, llterr, sim);
                    ret.Name = new EntityNameLL(LoggedInGridName + "/Region/" + sim.ToString().Trim());
                    ret.RegionContext = ret;    // since we don't know ourself before
                    ret.Comm = m_client;
                    ret.TerrainInfo.RegionContext = ret;
                    m_regionList.Add(ret);
                    m_log.Log(LogLevel.DWORLDDETAIL, "Creating region context for " + ret.Name);
                }
            }
        }
        return ret;
    }

    public LLRegionContext FindRegion(Predicate<LLRegionContext> pred) {
        LLRegionContext ret = null;
        lock (m_regionList) {
            foreach (LLRegionContext rcb in m_regionList) {
                if (pred(rcb)) {
                    ret = rcb;
                    break;
                }
            }
        }
        return ret;
    }

    #region DELAYED REGION MANAGEMENT
    // We get events before the sim comes online. This is a way to queue up those
    // events until we're online.
    public enum CommActionCode {
        RegionStateChange,
        OnNewPrim,
        OnObjectUpdated,
        OnObjectKilled,
        OnNewAvatar
    }

    struct ParamBlock {
        public CommActionCode cac;
        public Object p1; public Object p2; public Object p3; public Object p4;
        public ParamBlock(CommActionCode pcac, Object pp1, Object pp2, Object pp3, Object pp4) {
            cac = pcac;  p1 = pp1; p2 = pp2; p3 = pp3; p4 = pp4;
        }
    }
    private void QueueTilOnline(RegionContextBase rcontext, CommActionCode cac, Object p1) {
        QueueTilOnline(rcontext, cac, p1, null, null, null);
    }

    private void QueueTilOnline(RegionContextBase rcontext, CommActionCode cac, Object p1, Object p2) {
        QueueTilOnline(rcontext, cac, p1, p2, null, null);
    }

    private void QueueTilOnline(RegionContextBase rcontext, CommActionCode cac, Object p1, Object p2, Object p3) {
        QueueTilOnline(rcontext, cac, p1, p2, p3, null);
    }

    private void QueueTilOnline(RegionContextBase rcontext, CommActionCode cac, Object p1, Object p2, Object p3, Object p4) {
        lock (m_waitTilOnline) {
            if (m_shouldWaitTilOnline) {
                if (!m_waitTilOnline.ContainsKey(rcontext)) {
                    m_log.Log(LogLevel.DCOMMDETAIL, "QueueTilOnline: creating queue for {0}", rcontext.Name);
                    m_waitTilOnline.Add(rcontext, new OnDemandWorkQueue("QueueTilOnline:" + rcontext.Name));
                }
                m_log.Log(LogLevel.DCOMMDETAIL, "QueueTilOnline: queuing action {0} for {1}", cac, rcontext.Name);
                m_waitTilOnline[rcontext].DoLater(DoCommAction, new ParamBlock(cac, p1, p2, p3, p4));
            }
        }
    }

    private bool DoCommAction(Object oparms) {
        ParamBlock parms = (ParamBlock)oparms;
        m_log.Log(LogLevel.DCOMMDETAIL, "DoCommAction: executing queued action {0}", parms.cac);
        RegionAction(parms.cac, parms.p1, parms.p2, parms.p3, parms.p4);
        return true;
    }

    private void DoAnyWaitingEvents(RegionContextBase rcontext) {
        OnDemandWorkQueue q = null;
        lock (m_waitTilOnline) {
            m_log.Log(LogLevel.DCOMMDETAIL, "DoAnyWaitingEvents: unqueuing waiting events for {0}", rcontext.Name);
            if (m_waitTilOnline.ContainsKey(rcontext)) {
                q = m_waitTilOnline[rcontext];
                m_waitTilOnline.Remove(rcontext);
            }
        }
        if (q != null) {
            while (q.CurrentQueued > 0) {
                q.ProcessQueue(1000);
            }
        }
    }

    public void RegionAction(CommActionCode cac, Object p1, Object p2, Object p3, Object p4) {
        switch (cac) {
            case CommActionCode.RegionStateChange:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: RegionStateChange");
                ((RegionContextBase)p1).Update((World.UpdateCodes)p2);
                break;
            case CommActionCode.OnNewPrim:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnNewPrim");
                Objects_OnNewPrim((OMV.Simulator)p1, (OMV.Primitive)p2, (ulong)p3, (ushort)p4);
                break;
            case CommActionCode.OnObjectUpdated:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnObjectUpdated");
                Objects_OnObjectUpdated((OMV.Simulator)p1, (OMV.ObjectUpdate)p2, (ulong)p3, (ushort)p4);
                break;
            case CommActionCode.OnObjectKilled:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnObjectKilled");
                Objects_OnObjectKilled((OMV.Simulator)p1, (uint)p2);
                break;
            case CommActionCode.OnNewAvatar:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnNewAvatar");
                Objects_OnNewAvatar((OMV.Simulator)p1, (OMV.Avatar)p2, (ulong)p3, (ushort)p4);
                break;
            default:
                break;
        }
    }
    #endregion DELAYED REGION MANAGEMENT



}
}
