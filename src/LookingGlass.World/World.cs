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

    private Dictionary<string, IEntityAvatar> m_avatarDictionary;

    private List<IAgent> m_agentList;

    // list of the region information build for the simulator
    List<RegionContextBase> m_regionList;

    #region Events
    # pragma warning disable 0067   // disable unused event warning
    // when the underlying simulator is changing.
    public event WorldRegionNewCallback OnWorldRegionNew;

    // when the underlying simulator is changing.
    public event WorldRegionUpdatedCallback OnWorldRegionUpdated;
    
    // when the underlying simulator is changing.
    public event WorldRegionRemovedCallback OnWorldRegionRemoved;

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

    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        m_instance = this;      // there is only one world
        m_avatarDictionary = new Dictionary<string, IEntityAvatar>();
        m_regionList = new List<RegionContextBase>();
        m_agentList = new List<IAgent>();
        ModuleParams.AddDefaultParameter(m_moduleName + ".Communication", "Comm", "Communication to connect to");
    }

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
        RegionContextBase foundRegion = null;
        lock (m_regionList) {
            foundRegion = GetRegion(rcontext.Name);
            if (foundRegion == null) {
                // we don't know about this region. Add it and connect to events
                m_regionList.Add(rcontext);
                if (Region_OnNewEntityCallback == null) {
                    Region_OnNewEntityCallback = new RegionEntityNewCallback(Region_OnNewEntity);
                }
                rcontext.OnEntityNew += Region_OnNewEntityCallback;
                if (Region_OnRemovedEntityCallback == null) {
                    Region_OnRemovedEntityCallback = new RegionEntityRemovedCallback(Region_OnNewEntity);
                }
                rcontext.OnEntityRemoved += Region_OnRemovedEntityCallback;
                if (Region_OnRegionUpdatedCallback == null) {
                    Region_OnRegionUpdatedCallback = new RegionRegionUpdatedCallback(Region_OnRegionUpdated);
                }
                rcontext.OnRegionUpdated += Region_OnRegionUpdatedCallback;
            }
        }
        if (foundRegion == null) {
            if (OnWorldRegionNew != null) OnWorldRegionNew(rcontext);
        }
    }

    #region REGION EVENT PROCESSING
    private RegionEntityNewCallback Region_OnNewEntityCallback = null;
    private void Region_OnNewEntity(IEntity ent) {
        if (OnWorldEntityNew != null) OnWorldEntityNew(ent);
        return;
    }

    private RegionEntityRemovedCallback Region_OnRemovedEntityCallback = null;
    private void Region_OnRemovedEntity(IEntity ent) {
        if (OnWorldEntityRemoved != null) OnWorldEntityRemoved(ent);
        return;
    }

    private RegionRegionUpdatedCallback Region_OnRegionUpdatedCallback = null;
    private void Region_OnRegionUpdated(RegionContextBase rcontext, UpdateCodes what) {
        if (OnWorldRegionUpdated != null) OnWorldRegionUpdated(rcontext, what);
        return;
    }
    #endregion REGION EVENT PROCESSING

    public RegionContextBase GetRegion(EntityName name) {
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

    public void RemoveRegion(RegionContextBase rcontext) {
        RegionContextBase foundRegion = null;
        lock (m_regionList) {
            foundRegion = GetRegion(rcontext.Name);
            if (foundRegion != null) {
                // we know about this region so remove it and disconnect from events
                m_regionList.Remove(foundRegion);
                if (Region_OnNewEntityCallback != null) {
                    rcontext.OnEntityNew -= Region_OnNewEntityCallback;
                }
                if (Region_OnRemovedEntityCallback != null) {
                    rcontext.OnEntityRemoved -= Region_OnRemovedEntityCallback;
                }
                if (Region_OnRegionUpdatedCallback != null) {
                    rcontext.OnRegionUpdated -= Region_OnRegionUpdatedCallback;
                }
                if (OnWorldRegionRemoved != null) OnWorldRegionRemoved(rcontext);
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "RemoveRegion: asked to remove region we don't have. Name={0}", rcontext.Name);
            }
        }
    }

    #endregion Region Management

    public bool TryGetEntity(EntityName entName, out IEntity ent) {
        IEntity ret = null;
        lock (m_regionList) {
            foreach (RegionContextBase rcb in m_regionList) {
                rcb.TryGetEntity(entName, out ret);
                if (ret != null) break;
            }
        }
        ent = ret;
        return (ret != null);
    }

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
