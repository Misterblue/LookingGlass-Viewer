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
using LookingGlass.Framework.Logging;
using OMV = OpenMetaverse;

namespace LookingGlass.World {
public abstract class RegionContextBase : EntityBase, IRegionContext, IDisposable {
    protected ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    #region Events
    # pragma warning disable 0067   // disable unused event warning
    // when the underlying simulator is changing.
    public event RegionRegionStateChangeCallback OnRegionStateChange;

    // when some state of  the region has changed
    public event RegionRegionUpdatedCallback OnRegionUpdated;

    // when new items are added to the world
    public event RegionEntityNewCallback OnEntityNew;

    // when an entity is updated
    public event RegionEntityUpdateCallback OnEntityUpdate;

    // when an object is killed
    public event RegionEntityRemovedCallback OnEntityRemoved;

    # pragma warning restore 0067
    #endregion

    private OMV.DoubleDictionary<string, ulong, IEntity> m_entityDictionary;
    private Dictionary<EntityName, IEntityAvatar> m_avatarDictionary;

    protected WorldGroupCode m_worldGroup;
    public WorldGroupCode WorldGroup { get { return m_worldGroup; } }

    private RegionStateChangedCallback m_regionStateChangedCallback;
    protected RegionState m_regionState;
    public RegionState State {
        get { return m_regionState;  }
    }

    public RegionContextBase(RegionContextBase rcontext, AssetContextBase acontext) 
                : base(rcontext, acontext) {
        m_entityDictionary = new OMV.DoubleDictionary<string, ulong, IEntity>();
        m_avatarDictionary = new Dictionary<EntityName, IEntityAvatar>();
        m_regionState = new RegionState();
        m_regionStateChangedCallback = new RegionStateChangedCallback(State_OnChange);
        State.OnStateChanged += m_regionStateChangedCallback;
    }

    private void State_OnChange(RegionStateCode newState) {
        if (OnRegionStateChange != null) OnRegionStateChange(this, newState);
    }

    protected OMV.Vector3 m_size = new OMV.Vector3(256f, 256f, 8000f);
    public OMV.Vector3 Size { get { return m_size; } }

    // the world coordinate of the region's {0,0,0}
    protected OMV.Vector3d m_worldBase = new OMV.Vector3d(0d, 0d, 0d);
    public OMV.Vector3d WorldBase { get { return m_worldBase; } }

    // information on terrain for this region
    protected TerrainInfoBase m_terrainInfo = null;
    public TerrainInfoBase TerrainInfo { get { return m_terrainInfo; } }

    public override void Changed(UpdateCodes what) {
        base.Changed(what);
        if (OnRegionUpdated != null) OnRegionUpdated(this, what);
    }

    #region ENTITY MANAGEMENT
    public void AddEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "AddEntity: " + entity.Name);
        if (TrackEntity(entity)) {
            // tell the viewer about this prim and let the renderer convert it
            //    into the format needed for display
            if (OnEntityNew != null) OnEntityNew(entity);
        }
    }

    public void UpdateEntity(IEntity entity, UpdateCodes detail) {
        m_log.Log(LogLevel.DWORLDDETAIL, "UpdateEntity: " + entity.Name);
        if (OnEntityUpdate != null) OnEntityUpdate(entity, detail);
    }

    public void RemoveEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "RemoveEntity: " + entity.Name);
        if (OnEntityRemoved != null) OnEntityRemoved(entity);
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
        // TODO: add some checking for rcontext since the localIDs are scoped by 'simulator'
        // we are relying on a low collision rate for localIDs
        // A linear search of the list takes way too long for the number of objects arriving
        return TryGetEntity((ulong)localID, out ent);
    }

    /// <summary>
    /// </summary>
    /// <param name="localID"></param>
    /// <param name="ent"></param>
    /// <param name="createIt"></param>
    /// <returns>true if we created a new entry</returns>
    public bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, RegionCreateEntityCallback createIt) {
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
    #endregion ENTITY MANAGEMENT

    #region AVATAR MANAGEMENT
    /// <summary>
    /// Get the avatar entity or call the callback to create a new avatar that we can return
    /// </summary>
    /// <param name="ename">Name of the avatar entity we want</param>
    /// <param name="ent">returned avatar entity</param>
    /// <param name="createIt">delegate to call to create the avatar if not found</param>
    /// <returns>'true' if a new avatar was created</returns>
    public bool TryGetCreateAvatar(EntityName ename, out IEntityAvatar ent, WorldCreateAvatarCallback createIt) {
        IEntityAvatar av = null; ;
        try {
            lock (m_avatarDictionary) {
                if (!m_avatarDictionary.TryGetValue(ename, out av)) {
                    av = createIt();
                    m_avatarDictionary.Add(av.Name, av);
                }
                ent = av;
            }
            return true;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "TryGetCreateAvatar: Failed to create avatar: {0}", e.ToString());
        }
        ent = null;
        return false;
    }

    /// <summary>
    /// Remove the specified avatar
    /// </summary>
    /// <param name="av"></param>
    /// <returns>'true' if we actually deleted an avatar</returns>
    public bool RemoveAvatar(IEntityAvatar av) {
        IEntityAvatar av2 = null;
        try {
            lock (m_avatarDictionary) {
                if (m_avatarDictionary.TryGetValue(av.Name, out av2)) {
                    m_avatarDictionary.Remove(av2.Name);
                }
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "RemoveAvatar: Failed removing avatar: {0}", e.ToString());
        }
        return (av2 != null);
    }

    #endregion AVATAR MANAGEMENT

    public override void Dispose() {
        m_terrainInfo = null; // let the garbage collector work
        if (m_regionState != null && m_regionStateChangedCallback != null) {
            State.OnStateChanged -= m_regionStateChangedCallback;
        }

        // TODO: do something about the entity list
        m_entityDictionary.ForEach(delegate(IEntity ent) {
            ent.Dispose();
        });
        m_entityDictionary.Clear(); // release any entities we might have

        return;
    }
}
}
