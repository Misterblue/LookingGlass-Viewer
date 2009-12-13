/* Copyright (c) 2008 Robert Adams
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
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Rest;
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.View {
    /// <summary>
    /// Watch the comings and goings of entities in the world and create collections
    /// of entities (regions, avatars, ...) for display. The interface to this is
    /// through the HTTP interface.
    /// </summary>
public class EntityTracker : IEntityTrackerProvider, IModule {

protected RestHandler m_avatarRestHandler;
protected RestHandler m_regionRestHandler;

protected IWorld m_world;

#region IMODULE
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public EntityTracker() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, "EntityTracker.OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;
        ModuleParams.AddDefaultParameter(ModuleName + ".Avatar.Enable", "true",
                    "Whether to make avatar information available");
        ModuleParams.AddDefaultParameter(ModuleName + ".Regions.Enable", "true",
                    "Whether to make region information available");
        ModuleParams.AddDefaultParameter(ModuleName + ".World.Name", "World",
                    "Name of world module to track entities in");
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, "EntityTracker.AfterAllModulesLoaded()");
        // connect to the world and listen for entity events
        m_world = (IWorld)LGB.ModManager.Module(ModuleParams.ParamString(ModuleName + ".World.Name"));
        m_world.OnAgentNew += new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved += new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew += new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityRemoved += new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        m_world.OnWorldEntityUpdate += new WorldEntityUpdateCallback(World_OnWorldEntityUpdate);
        if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
            m_world.OnWorldRegionNew += new WorldRegionNewCallback(World_OneWorldRegionNew);
            m_world.OnWorldRegionRemoved += new WorldRegionRemovedCallback(World_OneWorldRegionRemoved);
            m_world.OnWorldRegionUpdated += new WorldRegionUpdatedCallback(World_OneWorldRegionUpdated);
        }

        if (ModuleParams.ParamBool(ModuleName + ".Avatars.Enable")) {
            m_avatarRestHandler = new RestHandler("/Tracker/Avatars", new AvatarInformation(this));
        }
        if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
            m_regionRestHandler = new RestHandler("/Tracker/Regions", new RegionInformation(this));
        }
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        m_world.OnAgentNew -= new WorldAgentNewCallback(World_OnAgentNew);
        m_world.OnAgentRemoved -= new WorldAgentRemovedCallback(World_OnAgentRemoved);
        m_world.OnWorldEntityNew -= new WorldEntityNewCallback(World_OnWorldEntityNew);
        m_world.OnWorldEntityRemoved -= new WorldEntityRemovedCallback(World_OnWorldEntityRemoved);
        m_world.OnWorldEntityUpdate -= new WorldEntityUpdateCallback(World_OnWorldEntityUpdate);
        if (ModuleParams.ParamBool(ModuleName + ".Regions.Enable")) {
            m_world.OnWorldRegionNew -= new WorldRegionNewCallback(World_OneWorldRegionNew);
            m_world.OnWorldRegionRemoved -= new WorldRegionRemovedCallback(World_OneWorldRegionRemoved);
            m_world.OnWorldRegionUpdated -= new WorldRegionUpdatedCallback(World_OneWorldRegionUpdated);
        }
        return false;
    }
#endregion IMODULE

#region EVENT PROCESSING
    void World_OnAgentNew(IAgent agnt) {
    }
    void World_OnAgentRemoved(IAgent agnt) {
    }
    void World_OnWorldEntityNew(IEntity ent) {
    }
    void World_OnWorldEntityRemoved(IEntity ent) {
    }
    void World_OnWorldEntityUpdate(IEntity ent, UpdateCodes what) {
    }
    void World_OneWorldRegionNew(RegionContextBase rcontext) {
    }
    void World_OneWorldRegionRemoved(RegionContextBase rcontext) {
    }
    void World_OneWorldRegionUpdated(RegionContextBase rcontext, UpdateCodes what) {
    }
#endregion EVENT PROCESSING

#region RESPONSE DATA CONTSRUCTION
    private class AvatarInformation : IDisplayable {
        EntityTracker m_tracker;
        public AvatarInformation(EntityTracker entTrack) {
            m_tracker = entTrack;
        }
        /// <summary>
        /// Returns avatar information as:
        /// {"First Last": {"first": "f", "last": "l", "distance": "N", "activity": "TFWAB"},
        ///  "First Last": { ... }
        ///  ...
        ///  }
        /// </summary>
        /// <returns></returns>
        public OMVSD.OSDMap GetDisplayable() {
            return new OMVSD.OSDMap();
        }
    }

    private class RegionInformation : IDisplayable {
        EntityTracker m_tracker;
        public RegionInformation(EntityTracker entTrack) {
            m_tracker = entTrack;
        }
        public OMVSD.OSDMap GetDisplayable() {
            return new OMVSD.OSDMap();
        }
    }
#endregion RESPONSE DATA CONSTRUCTION

}
}
