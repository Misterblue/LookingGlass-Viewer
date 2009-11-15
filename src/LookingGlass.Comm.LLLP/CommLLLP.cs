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
public class CommLLLP : IModule, LookingGlass.Comm.ICommProvider  {
    protected ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // ICommProvider.Name
    public string Name { get { return this.ModuleName; } }

    // ICommProvider.CommStatistics
    protected ParameterSet m_commStatistics;
    public ParameterSet CommStatistics() { return m_commStatistics; }
    protected RestHandler m_commStatsHandler;
    private int m_statNetDisconnected;
    private int m_statNetQueueRunning;
    private int m_statNetLoginProgress;
    private int m_statNetSimChanged;
    private int m_statNetSimConnected;
    private int m_statObjAttachmentUpdate;
    private int m_statObjAvatarUpdate;
    private int m_statObjKillObject;
    private int m_statObjObjectProperties;
    private int m_statObjObjectPropertiesUpdate;
    private int m_statObjObjectUpdate;
    private int m_statObjTerseUpdate;

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
    protected IAgent MainAgent {
        get {
            if (m_myAgent == null) {
                m_myAgent = new LLAgent(m_client);
            }
            return m_myAgent;
        }
        set { m_myAgent = value; }
    }

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

    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public virtual void OnLoad(string name, LookingGlassBase lgbase) {
        OnLoad2(name, lgbase, true);
    }

    // Internal OnLoad that can be used by derived classes
    protected void OnLoad2(string name, LookingGlassBase lgbase, bool shouldInit) {
        m_moduleName = name;
        m_lgb = lgbase;
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.CacheDir", 
                    Utilities.GetDefaultApplicationStorageDir(null),
                    "Filesystem location to build the texture cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.OMVResources",
                    "./LookingGlassResources/openmetaverse_data",
                    "Directory for resources used by libopenmetaverse (mostly for appearance)");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.NoTextureFilename", 
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "./LookingGlassResources/NoTexture.png"),
                    "Filename of texture to display when we can't get the real texture");
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.ConvertPNG", "true",
                    "whether to convert incoming JPEG2000 files to PNG files in the cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Texture.MaxRequests", 
                    "4",
                    "Maximum number of outstanding textures requests");
        ModuleParams.AddDefaultParameter(ModuleName + ".Settings.MultipleSims", 
                    "false",
                    "Whether to enable multiple sims");

        // This is not the right place for this but there is no World.LL module
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMoveAvatar", 
                    "true",
                    "Whether to move avatar when user types (otherwise wait for server round trip)");
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMove.RotFudge", 
                    "5.0",
                    "Degrees to rotate avatar when user turns (float)");
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMove.FlyFudge", 
                    "2.5",
                    "Meters to move avatar when moves forward when flying (float)");
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMove.RunFudge", 
                    "1.5",
                    "Meters to move avatar when moves forward when running (float)");
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMove.MoveFudge", 
                    "0.4",
                    "Meters to move avatar when moves forward when walking (float)");

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
            m_client.Settings.PARCEL_TRACKING = true;
            m_client.Settings.ALWAYS_REQUEST_PARCEL_ACL = false;
            m_client.Settings.ALWAYS_REQUEST_PARCEL_DWELL = false;
            m_client.Settings.USE_INTERPOLATION_TIMER = false;  // don't need the library helping
            m_client.Settings.SEND_AGENT_UPDATES = true;
            m_client.Self.Movement.AutoResetControls = true;   // I will do the key repeat operations
            m_client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = false;
            m_client.Settings.USE_ASSET_CACHE = false;
            m_client.Settings.PIPELINE_REQUEST_TIMEOUT = 120 * 1000;
            m_client.Settings.ASSET_CACHE_DIR = ModuleParams.ParamString(ModuleName + ".Assets.CacheDir");
            OMV.Settings.RESOURCE_DIR = ModuleParams.ParamString(ModuleName + ".Assets.OMVResources");
            // Crank up the throttle on texture downloads
            m_client.Throttle.Total = 2000000.0f;
            m_client.Throttle.Texture = 2446000.0f;
            m_client.Throttle.Asset = 2446000.0f;
            m_client.Settings.THROTTLE_OUTGOING_PACKETS = false;

            m_client.Network.LoginProgress += Network_LoginProgress;
            m_client.Network.Disconnected += Network_Disconnected;
            m_client.Network.SimConnected += Network_SimConnected;
            m_client.Network.SimChanged += Network_SimChanged;
            m_client.Network.EventQueueRunning += Network_EventQueueRunning;

        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "EXCEPTION BUILDING GRIDCLIENT: " + e.ToString());
        }

        // fake like this is the initial teleport
        m_SwitchingSims = true;
    }

    // IModule.Start()
    public virtual void Start() {
        Start2(true);
    }

    // internal Start to be used by derived classes
    protected void Start2(bool shouldKeepLoggedin) {
        m_loaded = true;
        if (shouldKeepLoggedin) {
            if (m_LoginThread == null) {
                m_LoginThread = new Thread(KeepLoggedIn);
                m_LoginThread.Name = "Communication Login";
                m_log.Log(LogLevel.DCOMM, "Starting keep logged in thread");
                m_LoginThread.Start();
            }
        }
    }

    // IModule.Stop()
    // If the base system says to stop, we make sure we're disconnected
    public virtual void Stop() {
        m_log.Log(LogLevel.DCOMM, "Stopping. Attempting to disconnect");
        Disconnect();
    }

    // IModule.PrepareForUnload()
    public virtual bool PrepareForUnload() {
        m_log.Log(LogLevel.DCOMM, "communication unload. We'll never login again");
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
                    m_log.Log(LogLevel.DCOMM, "KeepLoggedIn: Starting logout process");
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
        m_log.Log(LogLevel.DCOMM, "Starting login of {0} {1}", m_loginFirst, m_loginLast);
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
                        m_log.Log(LogLevel.DCOMM, "StartLogin: prev location of {0}", parts[0]);
                        loginSetting = parts[0];
                    }
                    else {
                        // put the user in the center of teh specified sim
                        loginSetting = OMV.NetworkManager.StartLocation(parts[0], 128, 128, 40);
                        m_log.Log(LogLevel.DCOMM, "StartLogin: user spec middle of {0} -> {1}", parts[0], loginSetting);
                    }
                }
                else if (parts.Length == 4) {
                    int posX = int.Parse(parts[1]);
                    int posY = int.Parse(parts[2]);
                    int posZ = int.Parse(parts[3]);
                    loginSetting = OMV.NetworkManager.StartLocation(parts[0], posX, posY, posZ);
                    m_log.Log(LogLevel.DCOMM, "StartLogin: user spec start at {0}/{1}/{2}/Z -> {3}",
                        parts[0], posX, posY, loginSetting);
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


    // ===========================================================
    public virtual void Network_LoginProgress(Object sender, OMV.LoginProgressEventArgs args) {
        this.m_statNetLoginProgress++;
        if (args.Status == OMV.LoginStatus.Success) {
            m_log.Log(LogLevel.DCOMM, "Successful login: {0}", args.Message);
            m_isConnected = true;
            m_isLoggedIn = true;
            m_isLoggingIn = false;
            m_loginMsg = args.Message;
            Comm_OnLoggedIn();
        }
        else if (args.Status == OMV.LoginStatus.Failed) {
            m_log.Log(LogLevel.DCOMM, "Login failed: {0}", args.Message);
            m_isLoggingIn = false;
            m_shouldBeLoggedIn = false;
            m_loginMsg = args.Message;
        }
    }

    public virtual void Network_Disconnected(Object sender, OMV.DisconnectedEventArgs args) {
        this.m_statNetDisconnected++;
        m_log.Log(LogLevel.DCOMM, "Disconnected");
        m_isConnected = false;
        //x BeginInvoke(
        //x     (MethodInvoker)delegate() {
        //x         cmdTeleport.Enabled = false;
        //x         DoLogout();
        //x });
    }

    public virtual void Network_EventQueueRunning(Object sender, OMV.EventQueueRunningEventArgs args) {
        this.m_statNetQueueRunning++;
        m_log.Log(LogLevel.DCOMM, "Event queue running on {0}", args.Simulator.Name);
        if (args.Simulator == m_client.Network.CurrentSim) {
            m_SwitchingSims = false;
        }
        // Now seems like a good time to start requesting parcel information
        m_client.Parcels.RequestAllSimParcels(m_client.Network.CurrentSim, false, 100);
    }


    public bool AfterAllModulesLoaded() {
        // make my connections for the communication events
        OMV.GridClient gc = m_client;
        // gc.Network.OnSimConnected += new OMV.NetworkManager.SimConnectedCallback(Network_OnSimConnected);
        // gc.Network.OnCurrentSimChanged += new OMV.NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
        // gc.Objects.OnNewPrim += new OMV.ObjectManager.NewPrimCallback(Objects_OnNewPrim);
        // gc.Objects.OnNewAttachment += new OMV.ObjectManager.NewAttachmentCallback(Objects_OnNewAttachment);
        // gc.Objects.OnObjectUpdated += new OMV.ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
        // NewAttachmentCallback
        // gc.Objects.OnNewAvatar += new OMV.ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
        // AvatarSitChangedCallback
        // ObjectPropertiesCallback
        // gc.Objects.OnObjectKilled += new OMV.ObjectManager.KillObjectCallback(Objects_OnObjectKilled);

        gc.Objects.ObjectPropertiesUpdated += Objects_ObjectPropertiesUpdated;
        gc.Objects.ObjectUpdate += Objects_ObjectUpdate;
        gc.Objects.ObjectProperties += Objects_ObjectProperties;
        gc.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
        gc.Objects.AvatarUpdate += Objects_AvatarUpdate;
        gc.Objects.KillObject += Objects_KillObject;
        gc.Settings.STORE_LAND_PATCHES = true;
        gc.Terrain.OnLandPatch += new OMV.TerrainManager.LandPatchCallback(Terrain_OnLandPatch);

        m_commStatistics.Add("WaitingTilOnline", 
            delegate(string xx) { return new OMVSD.OSDString(m_waitTilOnline.Count.ToString()); },
            "Number of event waiting until sim is online");
        m_commStatistics.Add("Network_Disconnected", 
            delegate(string xx) { return new OMVSD.OSDString(m_statNetDisconnected.ToString()); },
            "Number of 'network disconnected' messages");
        m_commStatistics.Add("Network_EventQueueRunning", 
            delegate(string xx) { return new OMVSD.OSDString(m_statNetQueueRunning.ToString()); },
            "Number of 'event queue running' messages");
        m_commStatistics.Add("Network_LoginProgress", 
            delegate(string xx) { return new OMVSD.OSDString(m_statNetLoginProgress.ToString()); },
            "Number of 'login progress' messages");
        m_commStatistics.Add("Network_SimChanged", 
            delegate(string xx) { return new OMVSD.OSDString(m_statNetSimChanged.ToString()); },
            "Number of 'sim changed' messages");
        m_commStatistics.Add("Network_SimConnected", 
            delegate(string xx) { return new OMVSD.OSDString(m_statNetSimConnected.ToString()); },
            "Number of 'sim connected' messages");
        m_commStatistics.Add("Objects_AttachmentUpdate", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjAttachmentUpdate.ToString()); },
            "Number of 'attachment update' messages");
        m_commStatistics.Add("Objects_AvatarUpdate", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjAvatarUpdate.ToString()); },
            "Number of 'avatar update' messages");
        m_commStatistics.Add("Objects_KillObject", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjKillObject.ToString()); },
            "Number of 'kill object' messages");
        m_commStatistics.Add("Objects_ObjectProperties", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjObjectProperties.ToString()); },
            "Number of 'object properties' messages");
        m_commStatistics.Add("Objects_ObjectPropertiesUpdate", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjObjectPropertiesUpdate.ToString()); },
            "Number of 'object properties update' messages");
        m_commStatistics.Add("Objects_ObjectUpdate", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjObjectUpdate.ToString()); },
            "Number of 'object update' messages");
        m_commStatistics.Add("Objects_TerseObjectUpdate", 
            delegate(string xx) { return new OMVSD.OSDString(m_statObjTerseUpdate.ToString()); },
            "Number of 'terse object update' messages");

        m_commStatsHandler = new RestHandler("/stats/" + m_moduleName + "/stats", m_commStatistics);

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
    public virtual void Network_SimConnected(Object sender, OMV.SimConnectedEventArgs args) {
        this.m_statNetSimConnected++;
        m_isConnected = true;   // good enough reason to think we're connected
        this.m_statNetSimConnected++;
        m_log.Log(LogLevel.DWORLD, "Network_SimConnected: Simulator connected {0}", args.Simulator.Name);

        LLRegionContext regionContext = FindRegion(args.Simulator);
        if (regionContext == null) {
            m_log.Log(LogLevel.DWORLD, "Network_SimConnected: NO REGION CONTEXT FOR {0}", args.Simulator.Name);
            return;
        }

        // a kludge to handle race conditions. We lock the region state while we empty queues
        regionContext.State.State = RegionStateCode.Online;
        // regionContext.State.IfOnline(delegate() {
            // this region is online and here. This can start a lot of IO

            // if we'd queued up actions, do them now that it's online
            DoAnyWaitingEvents(regionContext);
        // });

        // tell the world there is a new region
        World.World.Instance.AddRegion(regionContext);

        // this is needed to make the avatar appear
        // TODO: figure out if the linking between agent and appearance is right
        // m_client.Appearance.SetPreviousAppearance(true);
        m_client.Self.Movement.UpdateFromHeading(0.0, true);
    }

    // ===============================================================
    public virtual void Network_SimChanged(Object sender, OMV.SimChangedEventArgs args) {
        // disable teleports until we have a good connection to the simulator (event queue working)
        this.m_statNetSimChanged++;
        if (!m_client.Network.CurrentSim.Caps.IsEventQueueRunning) {
            m_SwitchingSims = true;
        }
        if (args.PreviousSimulator != null) {      // there is no prev sim the first time
            m_log.Log(LogLevel.DWORLD, "Simulator changed from {0}", args.PreviousSimulator.Name);
            LLRegionContext regionContext = FindRegion(args.PreviousSimulator);
            if (regionContext == null) return;
            // TODO: what to do with this operation?
        }
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
    public void Objects_ObjectUpdate(Object sender, OMV.PrimEventArgs args) {
        if (args.IsAttachment) {
            Objects_AttachmentUpdate(sender, args);
            return;
        }
        LLRegionContext rcontext = FindRegion(args.Simulator);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnObjectUpdated, sender, args);
            }) ) return;
        this.m_statObjObjectUpdate++;
        IEntity updatedEntity = null;
        // a full update says everything changed
        UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                    | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
        lock (m_opLock) {
            m_log.Log(LogLevel.DUPDATEDETAIL, "Object update: id={0}, p={1}, r={2}", 
                args.Prim.LocalID, args.Prim.Position.ToString(), args.Prim.Rotation.ToString());
            // assume somethings changed no matter what
            try {
                if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out updatedEntity, delegate() {
                            IEntity newEnt = new LLEntityPhysical(rcontext.AssetContext,
                                            rcontext, args.Simulator.Handle, args.Prim.LocalID, args.Prim);
                            updateFlags |= UpdateCodes.New;
                            return newEnt;
                        }) ) {
                    // new prim created
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FAILED CREATION OF NEW PRIM: " + e.ToString());
            }
        }
        // special update for the agent so it knows there is new info from the network
        // The real logic to push the update through happens in the IEntityAvatar.Update()
        if (updatedEntity != null) {
            int thisHashCode = args.Prim.GetHashCode();
            if (thisHashCode != updatedEntity.LastEntityHashCode) {
                updateFlags |= UpdateCodes.FullUpdate;
                updatedEntity.LastEntityHashCode = thisHashCode;
            }
            if (updatedEntity == this.MainAgent.AssociatedAvatar) {
                this.MainAgent.DataUpdate(updateFlags);
            }
            updatedEntity.Update(updateFlags);
        }

        return;
    }
    // ===============================================================
    public void Objects_AttachmentUpdate(Object sender, OMV.PrimEventArgs args) {
        LLRegionContext rcontext = FindRegion(args.Simulator);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnAttachmentUpdate, sender, args);
            }) ) return;
        this.m_statObjAttachmentUpdate++;
        m_log.Log(LogLevel.DUPDATEDETAIL, "OnNewAttachment: id={0}, lid={1}", args.Prim.ID.ToString(), args.Prim.LocalID);
        try {
            IEntity ent;
            if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out ent, delegate() {
                        IEntity newEnt = new LLEntityPhysical(rcontext.AssetContext,
                                        rcontext, args.Simulator.Handle, args.Prim.LocalID, args.Prim);
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
    private void Objects_TerseObjectUpdate(Object sender, OMV.TerseObjectUpdateEventArgs args) {
        LLRegionContext rcontext = FindRegion(args.Simulator);
        OMV.ObjectMovementUpdate update = args.Update;
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.TerseObjectUpdate, sender, args);
            }) ) return;
        this.m_statObjTerseUpdate++;
        IEntity updatedEntity = null;
        UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                    | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
        lock (m_opLock) {
            m_log.Log(LogLevel.DUPDATEDETAIL, "Object update: id={0}, p={1}, r={2}", 
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
                m_log.Log(LogLevel.DUPDATE, "OnObjectUpdated: can't find local ID {0}. NOT UPDATING", update.LocalID);
            }
        }
        if (updatedEntity != null) {
            if (updatedEntity == this.MainAgent.AssociatedAvatar) {
                // special update for the agent so it knows there is new info from the network
                // The real logic to push the update through happens in the IEntityAvatar.Update()
                this.MainAgent.DataUpdate(updateFlags);
            }
            updatedEntity.Update(updateFlags);
        }

        return;
    }
    // ===============================================================
    private void Objects_ObjectProperties(Object sender, OMV.ObjectPropertiesEventArgs args) {
        m_log.Log(LogLevel.DUPDATEDETAIL, "Objects_ObjectProperties:");
        this.m_statObjObjectProperties++;
    }
    // ===============================================================
    private void Objects_ObjectPropertiesUpdated(Object sender, OMV.ObjectPropertiesUpdatedEventArgs args) {
        m_log.Log(LogLevel.DUPDATEDETAIL, "Objects_ObjectPropertiesUpdated:");
        this.m_statObjObjectPropertiesUpdate++;
    }
    // ===============================================================
    public void Objects_AvatarUpdate(Object sender, OMV.AvatarUpdateEventArgs args) {
        LLRegionContext rcontext = FindRegion(args.Simulator);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.OnAvatarUpdate, sender, args);
            }) ) return;
        this.m_statObjAvatarUpdate++;
        m_log.Log(LogLevel.DUPDATEDETAIL, "Objects_AvatarUpdate: cntl={0}, parent={1}, p={2}, r={3}", 
                    args.Avatar.ControlFlags.ToString("x"), args.Avatar.ParentID, 
                    args.Avatar.Position, args.Avatar.Rotation);
        IEntity updatedEntity = null;
        UpdateCodes updateFlags = UpdateCodes.Acceleration | UpdateCodes.AngularVelocity
                    | UpdateCodes.Position | UpdateCodes.Rotation | UpdateCodes.Velocity;
        lock (m_opLock) {
            // This is an avatar, assume somethings changed no matter what
            updateFlags |= UpdateCodes.CollisionPlane;

            EntityName avatarEntityName = LLEntityAvatar.AvatarEntityNameFromID(rcontext.AssetContext, args.Avatar.ID);
            if (rcontext.TryGetCreateEntity(avatarEntityName, out updatedEntity, delegate() {
                            m_log.Log(LogLevel.DUPDATEDETAIL, "AvatarUpdate: creating avatar {0} {1} ({2})",
                                args.Avatar.FirstName, args.Avatar.LastName, args.Avatar.ID);
                            IEntityAvatar newEnt = new LLEntityAvatar(rcontext.AssetContext,
                                            rcontext, args.Simulator.Handle, args.Avatar);
                            updateFlags |= UpdateCodes.New;
                            return (IEntity)newEnt;
                        }) ) {
                updatedEntity.RelativePosition = args.Avatar.Position;
                updatedEntity.Heading = args.Avatar.Rotation;
                // We check here if this avatar goes with the agent in the world
                // If this av is with the agent, make the connection
                if (args.Avatar.LocalID == m_client.Self.LocalID) {
                    m_log.Log(LogLevel.DUPDATEDETAIL, "AvatarUpdate: associating agent with new avatar");
                    this.MainAgent.AssociatedAvatar = (IEntityAvatar)updatedEntity;
                }
            }
        }
        if (args.Avatar.LocalID == m_client.Self.LocalID) {
            // an extra special update for the agent so it knows things have changed
            this.MainAgent.DataUpdate(updateFlags);
        }

        // tell the entity it changed. Since this is an avatar entity it will update the agent if necessary.
        if (updatedEntity != null) updatedEntity.Update(updateFlags);
        return;
    }

    // ===============================================================
    public virtual void Objects_KillObject(Object sender, OMV.KillObjectEventArgs args) {
        LLRegionContext rcontext = FindRegion(args.Simulator);
        if (rcontext == null) return;
        if (rcontext.State.IfNotOnline(delegate() {
                QueueTilOnline(rcontext, CommActionCode.KillObject, sender, args);
            }) ) return;
        m_statObjKillObject++;
        m_log.Log(LogLevel.DWORLDDETAIL, "Object killed:");
        try {
            IEntity removedEntity;
            if (rcontext.TryGetEntityLocalID(args.ObjectLocalID, out removedEntity)) {
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
    /// Called when we just log in. We create our agent and put it into the world
    /// </summary>
    public virtual void Comm_OnLoggedIn() {
        m_log.Log(LogLevel.DWORLD, "Comm_OnLoggedIn:");
        World.World.Instance.AddAgent(this.MainAgent);
        // I work by taking LLLP messages and updating the agent
        // The agent will be updated in the world (usually by the viewer)
        // Create the two way communication linkage
        this.MainAgent.OnAgentUpdated += new AgentUpdatedCallback(Comm_OnAgentUpdated);
    }

    // ===============================================================
    public virtual void Comm_OnLoggedOut() {
        m_log.Log(LogLevel.DWORLD, "Comm_OnLoggedOut:");
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
                    // ret.Name = new EntityNameLL(LoggedInGridName + "/Region/" + sim.ToString().Trim());
                    ret.Name = new EntityNameLL(LoggedInGridName + "/Region/" + sim.Name.Trim());
                    ret.RegionContext = ret;    // since we don't know ourself before
                    ret.Comm = m_client;
                    ret.TerrainInfo.RegionContext = ret;
                    m_regionList.Add(ret);
                    m_log.Log(LogLevel.DWORLD, "Creating region context for " + ret.Name);
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
        OnObjectUpdated,
        TerseObjectUpdate,
        OnAttachmentUpdate,
        KillObject,
        OnAvatarUpdate
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

    private bool DoCommAction(DoLaterBase qInstance, Object oparms) {
        ParamBlock parms = (ParamBlock)oparms;
        m_log.Log(LogLevel.DCOMMDETAIL, "DoCommAction: executing queued action {0}", parms.cac);
        RegionAction(parms.cac, parms.p1, parms.p2, parms.p3, parms.p4);
        return true;
    }

    private void DoAnyWaitingEvents(RegionContextBase rcontext) {
        OnDemandWorkQueue q = null;
        lock (m_waitTilOnline) {
            m_log.Log(LogLevel.DCOMM, "DoAnyWaitingEvents: unqueuing {0} waiting events for {1}", 
                        m_waitTilOnline.Count, rcontext.Name);
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
            case CommActionCode.OnObjectUpdated:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnObjectUpdated");
                // Objects_OnObjectUpdated((OMV.Simulator)p1, (OMV.ObjectUpdate)p2, (ulong)p3, (ushort)p4);
                Objects_ObjectUpdate(p1, (OMV.PrimEventArgs)p2);
                break;
            case CommActionCode.TerseObjectUpdate:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: TerseObjectUpdate");
                Objects_TerseObjectUpdate(p1, (OMV.TerseObjectUpdateEventArgs)p2);
                break;
            case CommActionCode.OnAttachmentUpdate:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: OnAttachmentUpdated");
                Objects_AttachmentUpdate(p1, (OMV.PrimEventArgs)p2);
                break;
            case CommActionCode.KillObject:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: KillObject");
                Objects_KillObject(p1, (OMV.KillObjectEventArgs)p2);
                break;
            case CommActionCode.OnAvatarUpdate:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: AvatarUpdate");
                Objects_AvatarUpdate(p1, (OMV.AvatarUpdateEventArgs)p2);
                break;
            default:
                break;
        }
    }
    #endregion DELAYED REGION MANAGEMENT



}
}
