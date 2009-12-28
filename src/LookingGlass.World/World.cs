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

    private IAgent m_agent = null;

    // list of the region information build for the simulator
    List<RegionContextBase> m_regionList;

    // list of parameters and information on the grids
    Grids m_grids;

    #region Events
    # pragma warning disable 0067   // disable unused event warning
    // A new region has been added to the world
    public event WorldRegionNewCallback OnWorldRegionNew;
    // A known region has changed it's state (terrain, location, ...)
    public event WorldRegionUpdatedCallback OnWorldRegionUpdated;
    // a region is removed from the world
    public event WorldRegionRemovedCallback OnWorldRegionRemoved;

    // when new items are added to the world
    public event WorldEntityNewCallback OnWorldEntityNew;
    // when an entity is updated
    public event WorldEntityUpdateCallback OnWorldEntityUpdate;
    // when an object is killed
    public event WorldEntityRemovedCallback OnWorldEntityRemoved;

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
        m_regionList = new List<RegionContextBase>();
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
        m_log.Log(LogLevel.DWORLD, "Simulator connected " + rcontext.Name);
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

                if (Region_OnUpdateEntityCallback == null) {
                    Region_OnUpdateEntityCallback = new RegionEntityUpdateCallback(Region_OnUpdateEntity);
                }
                rcontext.OnEntityUpdate += Region_OnUpdateEntityCallback;

                if (Region_OnRemovedEntityCallback == null) {
                    Region_OnRemovedEntityCallback = new RegionEntityRemovedCallback(Region_OnRemovedEntity);
                }
                rcontext.OnEntityRemoved += Region_OnRemovedEntityCallback;

                if (Region_OnRegionUpdatedCallback == null) {
                    Region_OnRegionUpdatedCallback = new RegionRegionUpdatedCallback(Region_OnRegionUpdated);
                }
                rcontext.OnRegionUpdated += Region_OnRegionUpdatedCallback;
            }
        }
        // tell the world there is a new region (do it outside the lock)
        if (foundRegion == null) {
            if (OnWorldRegionNew != null) OnWorldRegionNew(rcontext);
        }
    }

    #region REGION EVENT PROCESSING
    private RegionEntityNewCallback Region_OnNewEntityCallback = null;
    private void Region_OnNewEntity(IEntity ent) {
        m_log.Log(LogLevel.DWORLDDETAIL, "Region_OnNewEntity: {0}", ent.Name.Name);
        if (OnWorldEntityNew != null) OnWorldEntityNew(ent);
        return;
    }

    private RegionEntityUpdateCallback Region_OnUpdateEntityCallback = null;
    private void Region_OnUpdateEntity(IEntity ent, UpdateCodes what) {
        if (OnWorldEntityUpdate != null) OnWorldEntityUpdate(ent, what);
        return;
    }

    private RegionEntityRemovedCallback Region_OnRemovedEntityCallback = null;
    private void Region_OnRemovedEntity(IEntity ent) {
        m_log.Log(LogLevel.DWORLDDETAIL, "Region_OnRemovedEntity: {0}", ent.Name.Name);
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

    public void RemoveRegion(RegionContextBase rcontext) {
        RegionContextBase foundRegion = null;
        lock (m_regionList) {
            foundRegion = GetRegion(rcontext.Name);
            if (foundRegion != null) {
                // we know about this region so remove it and disconnect from events
                m_regionList.Remove(foundRegion);
                m_log.Log(LogLevel.DWORLD, "Removing region " + foundRegion.Name);
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

    /// <summary>
    /// A global call to find an entity. We ask all the regions if they have it.
    /// This is only here because the renderer looses the context for an entity
    /// when control passes into the renderer and then back. The renderer only
    /// has the name of the entity.
    /// </summary>
    /// <param name="entName">the name of the entity to look for</param>
    /// <param name="ent">place to store the reference to the found entity</param>
    /// <returns>'true' if entity found</returns>
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

    #region AGENT MANAGEMENT
    public IAgent Agent { get { return m_agent; } }

    public void AddAgent(IAgent agnt) {
        m_log.Log(LogLevel.DWORLD, "AddAgent: ");
        m_agent = agnt;
        if (OnAgentNew != null) OnAgentNew(agnt);
    }

    public void UpdateAgent(UpdateCodes what) {
        m_log.Log(LogLevel.DWORLDDETAIL, "UpdateAgent: ");
        if (OnAgentUpdate != null) OnAgentUpdate(m_agent, what);
    }

    public void RemoveAgent() {
        m_log.Log(LogLevel.DWORLD, "RemoveAgent: ");
        if (m_agent != null) {
            if (OnAgentRemoved != null) OnAgentRemoved(m_agent);
            m_agent = null;
        }
    }

    void UpdateAgentCamera(IAgent agnt, OMV.Vector3 position, OMV.Quaternion direction) {
        return;
    }
    #endregion AGENT MANAGEMENT
    #endregion IWorld methods

    #region GRID MANAGEMENT
    public Grids Grids {
        get {
            if (m_grids == null) {
                m_grids = new Grids();
            } 
            return m_grids;
        }
    }
    #endregion GRID MANAGEMENT


}
}
