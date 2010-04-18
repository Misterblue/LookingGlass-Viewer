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
using System.Net;
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
using LookingGlass.World.OS;
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
    protected List<ParamBlock> m_waitTilOnline;

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
        m_waitTilOnline = new List<ParamBlock>();
        m_commStatistics = new ParameterSet();

        m_loginGrid = "Unknown";
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
                World.World.Instance.Grids.ForEach(delegate(OMVSD.OSDMap gg) { gridNames.Add(gg["Name"]); });
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
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.ConvertPNG", "false",
                    "whether to convert incoming JPEG2000 files to PNG files in the cache");
        ModuleParams.AddDefaultParameter(ModuleName + ".Texture.MaxRequests", 
                    "4",
                    "Maximum number of outstanding textures requests");
        ModuleParams.AddDefaultParameter(ModuleName + ".Settings.MultipleSims",
                    "false",
                    "Wether to connect to multiple sims");
        ModuleParams.AddDefaultParameter(ModuleName + ".Settings.MovementUpdateInterval", 
                    "100",
                    "Milliseconds between movement messages sent to server");

        // This is not the right place for this but there is no World.LL module
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMoveAvatar", 
                    "true",
                    "Whether to move avatar when user types (otherwise wait for server round trip)");
        ModuleParams.AddDefaultParameter("World.LL.Agent.PreMove.RotFudge", 
                    "3.0",
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
            m_client.Self.Movement.AutoResetControls = false;
            m_client.Self.Movement.UpdateInterval = ModuleParams.ParamInt(ModuleName + ".Settings.MovementUpdateInterval");
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
        Thread.Sleep(3000); // let the logout and disconnect happen
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
        m_log.Log(LogLevel.DCOMMDETAIL, "Disconnect request -- logout and disconnect");
        m_shouldBeLoggedIn = false;
        return true;
    }

    public virtual bool DoTeleport(string dest) {
        bool ret = true;
        string sim = "";
        float x = 128;
        float y = 128;
        float z = 40;
        dest = dest.Trim();
        string[] tokens = dest.Split(new char[] { '/' });
        if (tokens.Length == 4) {
            sim = tokens[0];
            if (!float.TryParse(tokens[1], out x) ||
                            !float.TryParse(tokens[2], out y) ||
                            !float.TryParse(tokens[3], out z)) {
                m_log.Log(LogLevel.DBADERROR, "Could not parse teleport destination '{0}'", dest);
                ret = false;
            }
        }
        else if (tokens.Length == 1) {
            sim = tokens[0];
            x = 128;
            y = 128;
            z = 40;
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "Did not recognize format of teleport destination: '{0}'", dest);
            ret = false;
        }
        if (ret) {
            if (m_client.Self.Teleport(sim, new OMV.Vector3(x, y, z))) {
                m_log.Log(LogLevel.DBADERROR, "Teleport successful to '{0}'", dest);
                ret = true;
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "Teleport to '{0}' failed", dest);
                ret = false;
            }
        }
        return ret;
    }

    /// <summary>
    /// Using its own thread, this sits around checking to see if we're logged in
    /// and, if not, starting the login dialog so we can get logged in.
    /// </summary>
    protected void KeepLoggedIn() {
        while (m_loaded) {
            if (LGB.KeepRunning && m_shouldBeLoggedIn && !IsLoggedIn) {
                // we should be logged in and we are not
                if (!m_isLoggingIn) {
                    StartLogin();
                }
            }
            if (!LGB.KeepRunning && !IsLoggedIn && IsConnected) {
                // if we're not supposed to be running, disconnect everything
                m_log.Log(LogLevel.DCOMM, "KeepLoggedIn: Shutting down the network");
                m_client.Network.Shutdown(OpenMetaverse.NetworkManager.DisconnectType.ClientInitiated);
                m_isConnected = false;
            }
            if (!LGB.KeepRunning || (!m_shouldBeLoggedIn && IsLoggedIn)) {
                // we shouldn't be logged in but it looks like we are
                m_log.Log(LogLevel.DCOMM, "KeepLoggedIn: Shouldn't be logged in");
                if (!m_isLoggingIn && !m_isLoggingOut) {
                    // not in logging transistion. start the logout process
                    m_log.Log(LogLevel.DCOMM, "KeepLoggedIn: Starting logout process");
                    m_client.Network.Logout();
                    m_isLoggingIn = false;
                    m_isLoggingOut = true;
                    m_isLoggedIn = false;
                    m_shouldBeLoggedIn = false;
                }
            }
            // update our login parameters for the UI

            Thread.Sleep(1*1000);
        }
        m_log.Log(LogLevel.DCOMM, "KeepLoggingIn: exiting keep loggin in thread");
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
                // User specified a sim. In the form of "simname/x/y/z" where the locations
                // are optional.
                char sep = '/';
                string[] parts = System.Uri.UnescapeDataString(m_loginSim).ToLower().Split(sep);
                if (parts.Length > 0) {
                    // since the name comes in through the web page, spaces get turned into pluses
                    parts[0] = parts[0].Replace('+', ' ');
                }
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

        World.World.Instance.Grids.SetCurrentGrid(m_loginGrid);
        loginParams.URI = World.World.Instance.Grids.GridLoginURI(World.Grids.Current);
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

        gc.Objects.ObjectPropertiesUpdated += Objects_ObjectPropertiesUpdated;
        gc.Objects.ObjectUpdate += Objects_ObjectUpdate;
        gc.Objects.ObjectProperties += Objects_ObjectProperties;
        gc.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
        gc.Objects.AvatarUpdate += Objects_AvatarUpdate;
        gc.Objects.KillObject += Objects_KillObject;
        gc.Avatars.AvatarAppearance += Avatars_AvatarAppearance;
        gc.Settings.STORE_LAND_PATCHES = true;
        gc.Terrain.LandPatchReceived += Terrain_LandPatchReceived;

        #region COMM REST STATS
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
        #endregion COMM REST STATS

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

        // tell the world there is a new region
        World.World.Instance.AddRegion(regionContext);

        // regionContext.State.IfOnline(delegate() {
            // this region is online and here. This can start a lot of IO

            // if we'd queued up actions, do them now that it's online
            DoAnyWaitingEvents(args.Simulator);
        // });

        // this is needed to make the avatar appear
        // TODO: figure out if the linking between agent and appearance is right
        // m_client.Appearance.SetPreviousAppearance(true);
        m_client.Appearance.RequestSetAppearance(true);
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
    public virtual void Terrain_LandPatchReceived(object sender, OMV.LandPatchReceivedEventArgs args) {
        // m_log.Log(LogLevel.DWORLDDETAIL, "Land patch for {0}: {1}, {2}, {3}", 
        //             args.Simulator.Name, args.X, args.Y, args.PatchSize);
        LLRegionContext regionContext = FindRegion(args.Simulator);
        if (regionContext == null) return;
        // update the region's view of the terrain
        regionContext.TerrainInfo.UpdatePatch(regionContext, args.X, args.Y, args.HeightMap);
        // tell the world the earth is moving
        if (QueueTilOnline(args.Simulator, CommActionCode.RegionStateChange, regionContext, World.UpdateCodes.Terrain)) {
            return;
        }
        regionContext.Update(World.UpdateCodes.Terrain);
    }

    // ===============================================================
    public void Objects_ObjectUpdate(Object sender, OMV.PrimEventArgs args) {
        if (args.IsAttachment) {
            Objects_AttachmentUpdate(sender, args);
            return;
        }
        if (QueueTilOnline(args.Simulator, CommActionCode.OnObjectUpdated, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
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
                // The way this is supposed to work is that entities have parents and parents have collections
                // of entities as children. Occasionally we learn about children before we hear about
                // the parent. This code just punts that problem. There is code over in Renderer.Ogre
                // to hold the child until a parent is found. What should really happen is comm should
                // hold the child until a parent is found. This would make parent/child relatioships 
                // first class relations and hide any comm implmentation dependencies from the rest
                // of the system.
                if (args.Prim.ParentID != 0 && updatedEntity.ContainingEntity == null) {
                    IEntity parentEntity = null;
                    rcontext.TryGetEntityLocalID(args.Prim.ParentID, out parentEntity);
                    if (parentEntity != null) {
                        updatedEntity.ContainingEntity = parentEntity;
                        parentEntity.AddEntityToContainer(updatedEntity);
                    }
                    else {
                        m_log.Log(LogLevel.DUPDATEDETAIL, "Can't assign parent. Entity not found. ent={0}", updatedEntity.Name);
                    }
                }
                // DEBUGGGING. REMOVE WHEN YOU KNOW WHAT THE NAME VALUES ARE
                if (args.Prim.NameValues != null) {
                    foreach (OMV.NameValue nv in args.Prim.NameValues) {
                        m_log.Log(LogLevel.DRENDERDETAIL, "NAMEVALUE: {0}={1} on {2}", nv.Name, nv.Value, updatedEntity.Name);
                    }
                }
                // if  there is an angular velocity and this is not an avatar, pass the information
                // along as an animation (llTargetOmega)
                // we convert the information into a standard form
                IEntityAvatar av;
                if (!updatedEntity.TryGet<IEntityAvatar>(out av)) {
                    if (args.Prim.AngularVelocity != OMV.Vector3.Zero) {
                        IAnimation anim;
                        float rotPerSec = args.Prim.AngularVelocity.Length() / Constants.TWOPI;
                        OMV.Vector3 axis = args.Prim.AngularVelocity;
                        axis.Normalize();
                        if (!updatedEntity.TryGet<IAnimation>(out anim)) {
                            anim = new LLAnimation();
                            updatedEntity.RegisterInterface<IAnimation>(anim);
                            m_log.Log(LogLevel.DUPDATEDETAIL, "Created prim animation on {0}", updatedEntity.Name);
                        }
                        if (rotPerSec != anim.StaticRotationRotPerSec || axis != anim.StaticRotationAxis) {
                            anim.AngularVelocity = args.Prim.AngularVelocity;   // legacy. Remove when other part plumbed
                            anim.StaticRotationAxis = axis;
                            anim.StaticRotationRotPerSec = rotPerSec;
                            anim.DoStaticRotation = true;
                            updateFlags |= UpdateCodes.Animation;
                            m_log.Log(LogLevel.DUPDATEDETAIL, "Updating prim animation on {0}", updatedEntity.Name);
                        }
                    }
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FAILED CREATION OF NEW PRIM: " + e.ToString());
            }
        }
        // special update for the agent so it knows there is new info from the network
        // The real logic to push the update through happens in the IEntityAvatar.Update()
        if (updatedEntity != null) {
            if (updatedEntity == this.MainAgent.AssociatedAvatar) {
                this.MainAgent.DataUpdate(updateFlags);
            }
            updatedEntity.Update(updateFlags);
        }

        return;
    }
    // ===============================================================
    public void Objects_AttachmentUpdate(Object sender, OMV.PrimEventArgs args) {
        if (QueueTilOnline(args.Simulator, CommActionCode.OnAttachmentUpdate, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
        this.m_statObjAttachmentUpdate++;
        m_log.Log(LogLevel.DUPDATEDETAIL, "OnNewAttachment: id={0}, lid={1}", args.Prim.ID.ToString(), args.Prim.LocalID);
        try {
            UpdateCodes updateFlags = UpdateCodes.FullUpdate;
            IEntity ent;
            if (rcontext.TryGetCreateEntityLocalID(args.Prim.LocalID, out ent, delegate() {
                        IEntity newEnt = new LLEntityPhysical(rcontext.AssetContext,
                                        rcontext, args.Simulator.Handle, args.Prim.LocalID, args.Prim);
                        updateFlags |= UpdateCodes.New;
                        LLAttachment att = new LLAttachment();
                        newEnt.RegisterInterface<LLAttachment>(att);
                        string attachmentID = null;
                        if (args.Prim.NameValues != null) {
                            foreach (OMV.NameValue nv in args.Prim.NameValues) {
                                m_log.Log(LogLevel.DCOMMDETAIL, "AttachmentUpdate: ent={0}, {1}->{2}", newEnt.Name, nv.Name, nv.Value);
                                if (nv.Name == "AttachItemID") {
                                    attachmentID = nv.Value.ToString();
                                    break;
                                }
                            }
                        }
                        att.AttachmentID = attachmentID;
                        att.AttachmentPoint = args.Prim.PrimData.AttachmentPoint;
                        return newEnt;
                    }) ) {
                // if new or not, assume everything about this entity has changed
                IEntityCollection coll;
                if (rcontext.TryGet<IEntityCollection>(out coll)) {
                    coll.UpdateEntity(ent, updateFlags);
                }
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
        if (QueueTilOnline(args.Simulator, CommActionCode.TerseObjectUpdate, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
        OMV.ObjectMovementUpdate update = args.Update;
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
        if (QueueTilOnline(args.Simulator, CommActionCode.OnAvatarUpdate, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
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
            IEntityCollection coll;
            rcontext.TryGet<IEntityCollection>(out coll);
            if (coll.TryGetCreateEntity(avatarEntityName, out updatedEntity, delegate() {
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
        if (QueueTilOnline(args.Simulator, CommActionCode.KillObject, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
        m_statObjKillObject++;
        m_log.Log(LogLevel.DWORLDDETAIL, "Object killed:");
        try {
            IEntity removedEntity;
            if (rcontext.TryGetEntityLocalID(args.ObjectLocalID, out removedEntity)) {
                // we need a handle to the objectID
                IEntityCollection coll;
                if (rcontext.TryGet<IEntityCollection>(out coll)) {
                    coll.RemoveEntity(removedEntity);
                }
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "FAILED DELETION OF OBJECT: " + e.ToString());
        }
        return;
    }

    // ===============================================================
    public virtual void Avatars_AvatarAppearance(Object sender, OMV.AvatarAppearanceEventArgs args) {
        if (QueueTilOnline(args.Simulator, CommActionCode.OnAvatarAppearance, sender, args)) return;
        LLRegionContext rcontext = FindRegion(args.Simulator);
        m_log.Log(LogLevel.DCOMMDETAIL, "AvatarAppearance: id={0}", args.AvatarID.ToString());
        // the appearance information is stored in the avatar info in libomv
        // We just kick the system to look at it
        lock (m_opLock) {
            EntityName avatarEntityName = LLEntityAvatar.AvatarEntityNameFromID(rcontext.AssetContext, args.AvatarID);
            IEntity ent;
            if (rcontext.TryGetEntity(avatarEntityName, out ent)) {
                ent.Update(UpdateCodes.Appearance);
            }
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
                    // we are connected but doen't have a regionContext for this simulator. Build one.
                    LLTerrainInfo llterr = new LLTerrainInfo(null, SelectAssetContextForGrid(sim));
                    llterr.WaterHeight = sim.WaterHeight;
                    // TODO: copy terrain texture IDs

                    ret = new LLRegionContext(null, SelectAssetContextForGrid(sim), llterr, sim);
                    // ret.Name = new EntityNameLL(LoggedInGridName + "/Region/" + sim.Name.Trim());
                    ret.Name = new EntityNameLL(LoggedInGridName + "/" + sim.Name.Trim());
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

    // Use a uniqe test to select a region
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

    /// <summary>
    /// Given a simulator, figure out the asset context for same. This very LL operation
    /// checks the parameters to see if a uniqe context was specified. If not, just use
    /// the connection to the simulator.
    /// </summary>
    /// <param name="sim"></param>
    /// <returns>the AssetContextBase for this simulator</returns>
    private AssetContextBase SelectAssetContextForGrid(OMV.Simulator sim) {
        AssetContextBase ret = null;
        // If user specifies an URL to get textures from, get that asset fetcher
        string otherAssets = World.World.Instance.Grids.GridParameter(World.Grids.Current, "OS.AssetServer.V1");
        if (otherAssets != null && otherAssets.Length != 0) {
            m_log.Log(LogLevel.DCOMM, "CommLLLP: creating OSAssetContextV1 for {0}", m_loginGrid);
            ret = new OSAssetContextV1(LoggedInGridName);
            ret.InitializeContext(this, 
                ModuleParams.ParamString(ModuleName + ".Assets.CacheDir"),
                ModuleParams.ParamInt(ModuleName + ".Texture.MaxRequests"));
        }

        // If the simulator has the texture capability, use that
        Uri textureUri = sim.Caps.CapabilityURI("GetTexture");
        if (ret == null && textureUri != null) {
            m_log.Log(LogLevel.DCOMM, "CommLLLP: creating OSAssetContextCap for {0}", m_loginGrid);
            ret = new OSAssetContextCap(LoggedInGridName, textureUri);
            ret.InitializeContext(this, 
                ModuleParams.ParamString(ModuleName + ".Assets.CacheDir"),
                ModuleParams.ParamInt(ModuleName + ".Texture.MaxRequests"));
        }

        // default to legacy UDP texture fetch
        if (ret == null) {
            // Create the asset contect for this communication instance
            // this should happen after connected. reconnection is a problem.
            m_log.Log(LogLevel.DCOMM, "CommLLLP: creating default asset context for grid {0}", m_loginGrid);
            ret = new LLAssetContext(LoggedInGridName);
            ret.InitializeContext(this,
                ModuleParams.ParamString(ModuleName + ".Assets.CacheDir"),
                ModuleParams.ParamInt(ModuleName + ".Texture.MaxRequests"));
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
        OnAvatarUpdate,
        OnAvatarAppearance
    }

    protected struct ParamBlock {
        public OMV.Simulator sim;
        public CommActionCode cac;
        public Object p1; public Object p2; public Object p3; public Object p4;
        public ParamBlock(OMV.Simulator psim, CommActionCode pcac, Object pp1, Object pp2, Object pp3, Object pp4) {
            sim = psim;  cac = pcac; p1 = pp1; p2 = pp2; p3 = pp3; p4 = pp4;
        }
    }
    private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, Object p1) {
        return QueueTilOnline(sim, cac, p1, null, null, null);
    }

    private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, Object p1, Object p2) {
        return QueueTilOnline(sim, cac, p1, p2, null, null);
    }

    private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, Object p1, Object p2, Object p3) {
        return QueueTilOnline(sim, cac, p1, p2, p3, null);
    }

    /// <summary>
    ///  Check to see if this action can happen now or has to be queued for later.
    /// </summary>
    /// <param name="rcontext"></param>
    /// <param name="cac"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="p4"></param>
    /// <returns>true if the action was queued, false if the action should be done</returns>
    private bool QueueTilOnline(OMV.Simulator sim, CommActionCode cac, Object p1, Object p2, Object p3, Object p4) {
        bool ret = false;
        lock (m_waitTilOnline) {
            RegionContextBase rcontext = FindRegion(sim);
            if (rcontext != null && rcontext.State.isOnline) {
                // not queuing until later
                ret = false;
            }
            else {
                ParamBlock pb = new ParamBlock(sim, cac, p1, p2, p3, p4);
                m_waitTilOnline.Add(pb);
                // return that we queued the action
                ret = true;
            }
        }
        return ret;
    }

    private void DoAnyWaitingEvents(OMV.Simulator sim) {
        m_log.Log(LogLevel.DCOMMDETAIL, "DoAnyWaitingEvents: examining {0} queued events", m_waitTilOnline.Count);
        List<ParamBlock> m_queuedActions = new List<ParamBlock>();
        lock (m_waitTilOnline) {
            // get out all of teh actions saved for this sim
            foreach (ParamBlock pb in m_waitTilOnline) {
                if (pb.sim == sim) {
                    m_queuedActions.Add(pb);
                }
            }
            // remove the entries for the sim
            foreach (ParamBlock pb in m_queuedActions) {
                m_waitTilOnline.Remove(pb);
            }
        }
        // process each of the actions. If they should stay queued, they will get requeued
        m_log.Log(LogLevel.DCOMMDETAIL, "DoAnyWaitingEvents: processing {0} queued events", m_queuedActions.Count);
        foreach (ParamBlock pb in m_queuedActions) {
            RegionAction(pb.cac, pb.p1, pb.p2, pb.p3, pb.p4);
        }
    }

    public void RegionAction(CommActionCode cac, Object p1, Object p2, Object p3, Object p4) {
        switch (cac) {
            case CommActionCode.RegionStateChange:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: RegionStateChange");
                // NOTE that this goes straight to the status update routine
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
            case CommActionCode.OnAvatarAppearance:
                m_log.Log(LogLevel.DCOMMDETAIL, "RegionAction: AvatarAppearance");
                Avatars_AvatarAppearance(p1, (OMV.AvatarAppearanceEventArgs)p2);
                break;
            default:
                break;
        }
    }
    #endregion DELAYED REGION MANAGEMENT



}
}
