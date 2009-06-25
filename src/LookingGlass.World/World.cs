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
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using OMV = OpenMetaverse;

namespace LookingGlass.World {

public sealed class World : ModuleBase, IWorld, IProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    /// <summary>
    ///  there is really only one world and this is the way to get a handle to it
    /// </summary>
    private static World m_instance = null;
    public static World Instance {
    get { return m_instance; }
    }

    private OMV.DoubleDictionary<string, ulong, IEntity> m_entityDictionary;

    private List<IAgent> m_agentList;

    // list of the region information build for the simulator
    List<RegionContextBase> m_regionList;

    #region Events
    # pragma warning disable 0067   // disable unused event warning
    // when the underlying simulator is changing.
    public event WorldRegionConnectedCallback OnWorldRegionConnected;

    // when the underlying simulator is changing.
    public event WorldRegionChangingCallback OnWorldRegionChanging;

    // when new items are added to the world
    public event WorldEntityNewCallback OnWorldEntityNew;

    // when an entity is updated
    public event WorldEntityUpdateCallback OnWorldEntityUpdate;

    // when an object is killed
    public event WorldEntityRemovedCallback OnWorldEntityRemoved;

    // when the terrain information is changed
    public event WorldTerrainUpdateCallback OnWorldTerrainUpdated;

    // When an agent is added to the world
    public event WorldAgentNewCallback OnAgentNew;

    // When an agent is added to the world
    public event WorldAgentUpdateCallback OnAgentUpdate;

    // When an agent is removed from the world
    public event WorldAgentRemovedCallback OnAgentRemoved;
    # pragma warning restore 0067
    #endregion

    /// <summary>
    /// Constructor called in instance of main and not in own thread. This is only
    /// good for setting up structures.
    /// </summary>
    public World() {
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    public override void OnLoad(string name, IAppParameters parms) {
        m_moduleName = name;
        ModuleParams = parms;
        m_instance = this;
        m_entityDictionary = new OMV.DoubleDictionary<string, ulong, IEntity>();
        m_regionList = new List<RegionContextBase>();
        m_agentList = new List<IAgent>();
        ModuleParams.AddDefaultParameter(m_moduleName + ".Communication", "Comm", "Communication to connect to");
    }

    #region IWorldProvider methods

    public bool TryGetEntity(ulong lgid, out IEntity ent) {
        return m_entityDictionary.TryGetValue(lgid, out ent);
    }

    public bool TryGetEntity(string entName, out IEntity ent) {
        return m_entityDictionary.TryGetValue(entName, out ent);
    }

    public bool TryGetEntity(EntityName entName, out IEntity ent) {
        return m_entityDictionary.TryGetValue(entName.Name, out ent);
    }

    public bool TryGetEntityLocalID(uint localID, out IEntity ent) {
        // it's a kludge, but localID is the same as global ID
        return TryGetEntity((ulong)localID, out ent);
    }

    /// <summary>
    /// </summary>
    /// <param name="localID"></param>
    /// <param name="ent"></param>
    /// <param name="createIt"></param>
    /// <returns></returns>
    public bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, WorldCreateEntityCallback createIt) {
        try {
            lock (m_entityDictionary) {
                if (!TryGetEntityLocalID(localID, out ent)) {
                    IEntity newEntity = createIt();
                    AddEntity(newEntity);
                    ent = newEntity;
                }
            }
            return true;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "TryGetCreateEntityLocalID: Failed to create entity: {0}", e.ToString());
        }
        ent = null;
        return false;
    }

    public IEntity FindEntity(Predicate<IEntity> pred) {
        return m_entityDictionary.FindValue(pred);
    }
    #endregion IWorldProvider methods

    #region IModule methods
    override public bool AfterAllModulesLoaded() {
        return true;
    }

    /// <summary>
    /// Start doing things. Since most of my actions are driven by the communication
    /// connection. There is not much to do.
    /// </summary>
    override public void Start() {
        base.Start();
        m_log.Log(LogLevel.DINIT, "entered Start()");
        return;
    }

    public override void Stop() {
        base.Stop();
    }

    #endregion IModule methods

    #region IWorld methods
    #region Region Management
    public void AddRegion(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DWORLDDETAIL, "Simulator connected " + rcontext.Name);
        if (OnWorldRegionConnected != null) OnWorldRegionConnected(rcontext);
    }

    public RegionContextBase GetRegion(string name) {
        RegionContextBase ret = null;
        lock (m_regionList) {
            foreach (RegionContextBase rcb in m_regionList) {
                if (rcb.Name.Equals(name)) {
                    ret = rcb;
                    break;
                }
            }
        }
        return ret;
    }

    public RegionContextBase FindRegion(Predicate<RegionContextBase> pred) {
        RegionContextBase ret = null;
        lock (m_regionList) {
            foreach (RegionContextBase rcb in m_regionList) {
                if (pred(rcb)) {
                    ret = rcb;
                    break;
                }
            }
        }
        return ret;
    }

    public void UpdateRegion(RegionContextBase rcontext, UpdateCodes detail) {
        switch (detail) {
            case UpdateCodes.Terrain:
                // I don't do anything with terrain. See if any viewers care.
                if (OnWorldTerrainUpdated != null) OnWorldTerrainUpdated(rcontext);
                break;
            case UpdateCodes.Position:
                m_log.Log(LogLevel.DWORLDDETAIL, "UpdateRegion: Code 'location'");
                break;
            default:
                break;
        }
        return;
    }

    public void RemoveRegion(RegionContextBase rcontext) {}

    #endregion Region Management

    #region Entity Management
    public void AddEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "AddEntity: " + entity.Name);
        if (TrackEntity(entity)) {
            // tell the viewer about this prim and let the renderer convert it
            //    into the format needed for display
            if (OnWorldEntityNew != null) OnWorldEntityNew(entity);
        }
    }

    public void UpdateEntity(IEntity entity, UpdateCodes detail) {
        m_log.Log(LogLevel.DWORLDDETAIL, "UpdateEntity: " + entity.Name);
        if (OnWorldEntityUpdate != null) OnWorldEntityUpdate(entity, detail);
    }

    public void RemoveEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "RemoveEntity: " + entity.Name);
        if (OnWorldEntityRemoved != null) OnWorldEntityRemoved(entity);
    }

    private void SelectEntity(IEntity ent) {
    }

    private bool TrackEntity(IEntity ent) {
        try {
            if (m_entityDictionary.ContainsKey(ent.Name.Name)) {
                m_log.Log(LogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
            }
            else {
                m_entityDictionary.Add(ent.Name.Name, ent.LGID, ent);
                return true;
            }
        }
        catch {
            // sometimes they send me the same entry twice
            m_log.Log(LogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
        }
        return false;
    }

    private void UnTrackEntity(IEntity ent) {
        m_entityDictionary.Remove(ent.Name.Name, ent.LGID);
    }

    private void ClearTrackedEntities() {
        m_entityDictionary.Clear();
    }
    #endregion Entity Management

    #region Agent Management
    public void AddAgent(IAgent agnt) {
        m_log.Log(LogLevel.DWORLDDETAIL, "AddAgent: ");
        lock (m_agentList) m_agentList.Add(agnt);
        if (OnAgentNew != null) OnAgentNew(agnt);
    }

    public void UpdateAgent(IAgent agnt, UpdateCodes what) {
        // m_log.Log(LogLevel.DWORLDDETAIL, "UpdateAgent: ");
        if (OnAgentUpdate != null) OnAgentUpdate(agnt, what);
    }

    public void RemoveAgent(IAgent agnt) {
        m_log.Log(LogLevel.DWORLDDETAIL, "RemoveAgent: ");
        if (OnAgentRemoved != null) OnAgentRemoved(agnt);
        lock (m_agentList) {
            if (m_agentList.Contains(agnt)) {
                m_agentList.Remove(agnt);
            }
        }
    }

    public void ForEachAgent(Action<IAgent> action) {
        lock (m_agentList) {
            foreach (IAgent aa in m_agentList) {
                action(aa);
            }
        }
    }

    public IAgent FindAgent(Predicate<IAgent> pred) {
        lock (m_agentList) {
            foreach (IAgent aa in m_agentList) {
                if (pred(aa)) return aa;
            }
        }
        return null;
    }

    void UpdateAgentCamera(IAgent agnt, OMV.Vector3 position, OMV.Quaternion direction) {
        return;
    }

    #endregion Agent Management
    #endregion IWorld methods


}
}
