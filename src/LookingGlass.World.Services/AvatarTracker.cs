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
using System.Net;
using System.Text;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Rest;
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.World.Services {

public class AvatarTracker : IAvatarTrackerService, IModule {

    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    protected RestManager m_restManager;
    protected World m_world;
    protected RestHandler m_restHandler;

    protected Dictionary<string, IEntityAvatar> m_avatars;
    protected IEntityAvatar m_agentAV = null;

    public AvatarTracker() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }
    
    #region IModule
    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, "AvatarTracker.OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        m_avatars = new Dictionary<string, IEntityAvatar>();

        ModuleParams.AddDefaultParameter(m_moduleName + ".World.Name", "World",
                    "Name of world to connect to");
        ModuleParams.AddDefaultParameter(m_moduleName + ".RestManager.Name", "RestManager",
                    "Name of REST interface manager to connect to");
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, "AvatarTracker.AfterAllModulesLoaded()");
        // Find the rest manager and setup to get web requests
        String restManagerName = ModuleParams.ParamString(m_moduleName + ".RestManager.Name");
        m_restManager = (RestManager)LGB.ModManager.Module(restManagerName);
        m_restHandler = new RestHandler("/avatars", GetHandler, PostHandler);

        // Find the world and connect to same to hear about all the avatars
        String worldName = ModuleParams.ParamString(m_moduleName + ".World.Name");
        m_world = (World)LGB.ModManager.Module(worldName);
        m_world.OnAgentNew += new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved += new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew += new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityRemoved += new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        // when told to stop, we forget everything
        m_avatars.Clear();
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        m_world.OnAgentNew -= new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved -= new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew -= new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityRemoved -= new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        return false;
    }

    #endregion IModule

    void World_OnAgentNew(IAgent agnt) {
        // someday remember the main agent for highlighting
        return;
    }

    void World_OnAgentRemoved(IAgent agnt) {
        return;
    }

    void World_OnWorldEntityNew(IEntity ent) {
        IEntityAvatar av;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            // this new entity is an avatar. If we don't know it already, remember same
            lock (m_avatars) {
                if (!m_avatars.ContainsKey(av.Name.Name)) {
                    m_avatars.Add(av.Name.Name, av);
                }
            }
        }
        return;
    }

    void World_OnWorldEntityRemoved(IEntity ent) {
        IEntityAvatar av;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            // this entity is an avatar. If we're tracking it, stop that
            lock (m_avatars) {
                if (m_avatars.ContainsKey(av.Name.Name)) {
                    m_avatars.Remove(av.Name.Name);
                }
            }
        }
        return;
    }


    private OMVSD.OSD GetHandler(Uri uri, String after) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        lock (m_avatars) {
            foreach (KeyValuePair<string, IEntityAvatar> kvp in m_avatars) {
                OMVSD.OSDMap oneAV = new OMVSD.OSDMap();
                try {
                    oneAV.Add("Name", new OMVSD.OSDString(kvp.Value.DisplayName));
                    oneAV.Add("Region", new OMVSD.OSDString(kvp.Value.RegionContext.Name.Name));
                    oneAV.Add("X", new OMVSD.OSDString(kvp.Value.RelativePosition.X.ToString("###0.###")));
                    oneAV.Add("Y", new OMVSD.OSDString(kvp.Value.RelativePosition.Y.ToString("###0.###")));
                    oneAV.Add("Z", new OMVSD.OSDString(kvp.Value.RelativePosition.Z.ToString("###0.###")));
                    float dist = 0f;
                    if (m_agentAV != null) {
                        dist = OMV.Vector3.Distance(m_agentAV.RelativePosition, kvp.Value.RelativePosition);
                    }
                    oneAV.Add("Distance", new OMVSD.OSDReal(dist));
                    oneAV.Add("Flags", new OMVSD.OSDString(kvp.Value.ActivityFlags));
                }
                catch (Exception e) {
                    LogManager.Log.Log(LogLevel.DBADERROR, "AvatarTracker.GetHandler: exception building response: {0}", e);
                }
                ret.Add(kvp.Value.Name.Name.Replace('/', '-'), oneAV);
            }
        }
        return ret;
    }

    private OMVSD.OSD PostHandler(Uri uri, String after, OMVSD.OSD body) {
        return new OMVSD.OSDMap();
    }


}
}
