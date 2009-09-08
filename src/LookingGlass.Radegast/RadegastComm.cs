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
using System.Collections.Generic;
using System.Text;
using LookingGlass.Comm;
using LookingGlass.Comm.LLLP;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using Radegast;

namespace LookingGlass.Radegast {
    /// <summary>
    /// When LookingGlass is loaded as part of Radegast, all of the communication is done
    /// via that program. This module overloads the LLLP communication module and replaces
    /// most of the login code with Radegast specific stuff. Also the initialization of
    /// the GridClient is left to Radegast although we do tweek things.
    /// </summary>
public class RadegastComm : CommLLLP {

    private RadegastInstance m_radInstance;

    public RadegastComm() {
        InitVariables();
    }

    #region IModule methods

    public override void OnLoad(string name, LookingGlassBase lgbase) {
        // set up initial variables but don't initialize comm
        OnLoad2(name, lgbase, false);

        try {
            // find the Radegast client
            m_radInstance = (RadegastInstance)m_lgb.OtherManager;
            m_client = m_radInstance.Client;

            m_client.Settings.ENABLE_CAPS = true;
            // m_client.Settings.MULTIPLE_SIMS = ModuleParams.ParamBool(ModuleName + ".Settings.MultipleSims");
            m_client.Settings.ALWAYS_DECODE_OBJECTS = true;
            m_client.Settings.ALWAYS_REQUEST_OBJECTS = true;
            // m_client.Settings.OBJECT_TRACKING = false; // We use our own object tracking system
            m_client.Settings.AVATAR_TRACKING = true; //but we want to use the libsl avatar system
            // m_client.Settings.SEND_AGENT_APPEARANCE = false;    // for the moment, don't do appearance
            // m_client.Settings.PARCEL_TRACKING = false;
            // m_client.Settings.USE_INTERPOLATION_TIMER = false;  // don't need the library helping
            m_client.Settings.SEND_AGENT_UPDATES = true;
            // m_client.Self.Movement.AutoResetControls = false;
            // m_client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;
            // m_client.Settings.USE_ASSET_CACHE = false;
            m_client.Settings.PIPELINE_REQUEST_TIMEOUT = 120 * 1000;
            m_client.Settings.ASSET_CACHE_DIR = ModuleParams.ParamString(ModuleName + ".Assets.CacheDir");
            m_client.Settings.ALWAYS_REQUEST_PARCEL_ACL = false;
            m_client.Settings.ALWAYS_REQUEST_PARCEL_DWELL = false;
            // m_client.Settings.Apply();
            // Crank up the throttle on texture downloads
            m_client.Throttle.Texture = 446000.0f;

            m_client.Network.OnLogin += new OMV.NetworkManager.LoginCallback(Network_OnLogin);
            m_client.Network.OnDisconnected += new OMV.NetworkManager.DisconnectedCallback(Network_OnDisconnected);
            m_client.Network.OnCurrentSimChanged += new OMV.NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            m_client.Network.OnEventQueueRunning += new OMV.NetworkManager.EventQueueRunningCallback(Network_OnEventQueueRunning);
        }
        catch (Exception e) {
        }

        // fake like this is the initial teleport
        m_SwitchingSims = true;
    }

    /*
     * Don't need to override this since it's all ok
    public override bool AfterAllModulesLoaded() {
        // make my connections for the communication events
        OMV.GridClient gc = m_client;
        gc.Network.OnSimConnected += new OMV.NetworkManager.SimConnectedCallback(Network_OnSimConnected);
        gc.Network.OnCurrentSimChanged += new OMV.NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
        gc.Objects.OnNewPrim += new OMV.ObjectManager.NewPrimCallback(Objects_OnNewPrim);
        gc.Objects.OnObjectUpdated += new OMV.ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
        // NewAttachmentCallback
        gc.Objects.OnNewAvatar += new OMV.ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
        // AvatarSitChangedCallback
        // ObjectPropertiesCallback
        gc.Objects.OnObjectKilled += new OMV.ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
        gc.Settings.STORE_LAND_PATCHES = true;
        gc.Terrain.OnLandPatch += new OMV.TerrainManager.LandPatchCallback(Terrain_OnLandPatch);
        gc.Parcels.OnSimParcelsDownloaded += new OMV.ParcelManager.SimParcelsDownloaded(Parcels_OnSimParcelsDownloaded);

        return true;
    }
     */


    protected override void Network_OnLogin(OMV.LoginStatus login, string message) {
        switch (m_radInstance.Netcom.LoginOptions.Grid) {
            case global::Radegast.Netcom.LoginGrid.MainGrid:
                m_loginGrid = "SecondLife";
                break;
            case global::Radegast.Netcom.LoginGrid.BetaGrid:
                m_loginGrid = "SecondLifeBeta";
                break;
            case global::Radegast.Netcom.LoginGrid.Custom:
                Uri loginUri = new Uri(m_radInstance.Netcom.LoginOptions.GridCustomLoginUri);
                // extract the login host name as the grid name
                m_loginGrid = loginUri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
                break;
        }
        base.Network_OnLogin(login, message);
    }

    public override void Start() {
        // do the base's sstart but don't turn on the auto login control stuff
        base.Start2(false);
    }

    // If the base system says to stop, we make sure we're disconnected
    public override void Stop() {
        base.Stop();
    }

    public override bool PrepareForUnload() {
        base.PrepareForUnload();
        return true;
    }

    #endregion IModule methods

    #region ICommProvider methods
    public override bool Connect(ParameterSet parms) {
        return false;
    }

    public override bool Disconnect() {
        return false;
    }

    #endregion ICommProvider methods
}
}
