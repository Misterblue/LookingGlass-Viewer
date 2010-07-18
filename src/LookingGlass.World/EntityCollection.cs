/* Copyright (c) 2009 Robert Adams
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
using LookingGlass.Framework.WorkQueue;
using OMV = OpenMetaverse;

namespace LookingGlass.World {
public class EntityCollection : IEntityCollection {
    protected ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    public event EntityNewCallback OnEntityNew;
    public event EntityUpdateCallback OnEntityUpdate;
    public event EntityRemovedCallback OnEntityRemoved;

    static BasicWorkQueue m_workQueueEvent = new BasicWorkQueue("EntityCollectionEvent");

    protected OMV.DoubleDictionary<string, ulong, IEntity> m_entityDictionary;

    public EntityCollection() {
        m_entityDictionary = new OMV.DoubleDictionary<string, ulong, IEntity>();
    }

    public int Count {
        get { return m_entityDictionary.Count; }
    }

    public void AddEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "AddEntity: n={0}, lid={1}", entity.Name, entity.LGID);
        if (TrackEntity(entity)) {
            // tell the viewer about this prim and let the renderer convert it
            //    into the format needed for display
            // if (OnEntityNew != null) OnEntityNew(entity);
            // disconnect this work from the caller -- use another thread
            m_workQueueEvent.DoLater(DoEventLater, entity);
        }
    }

    private bool DoEventLater(DoLaterBase qInstance, object parm) {
        EntityNewCallback enc = OnEntityNew;
        if (enc != null) {
            enc((IEntity)parm);
        }
        return true;
    }

    public void UpdateEntity(IEntity entity, UpdateCodes detail) {
        m_log.Log(LogLevel.DUPDATEDETAIL, "UpdateEntity: " + entity.Name);
        // if (OnEntityUpdate != null) OnEntityUpdate(entity, detail);
        object[] parms = { entity, detail };
        m_workQueueEvent.DoLater(DoUpdateLater, parms);
    }

    private bool DoUpdateLater(DoLaterBase qInstance, object parm) {
        object[] parms = (object[])parm;
        IEntity ent = (IEntity)parms[0];
        UpdateCodes detail = (UpdateCodes)parms[1];
        EntityUpdateCallback euc = OnEntityUpdate;
        if (euc != null) {
            euc(ent, detail);
        }
        return true;
    }

    public void RemoveEntity(IEntity entity) {
        m_log.Log(LogLevel.DWORLDDETAIL, "RemoveEntity: " + entity.Name);
        if (OnEntityRemoved != null) OnEntityRemoved(entity);
        lock (this) {
            m_entityDictionary.Remove(entity.Name.Name);
        }
    }

    private void SelectEntity(IEntity ent) {
    }

    private bool TrackEntity(IEntity ent) {
        try {
            lock (this) {
                if (m_entityDictionary.ContainsKey(ent.Name.Name)) {
                    m_log.Log(LogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
                }
                else {
                    m_entityDictionary.Add(ent.Name.Name, ent.LGID, ent);
                    return true;
                }
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

    /// <summary>
    /// </summary>
    /// <param name="localID"></param>
    /// <param name="ent"></param>
    /// <param name="createIt"></param>
    /// <returns>true if we created a new entry</returns>
    public bool TryGetCreateEntity(EntityName entName, out IEntity ent, RegionCreateEntityCallback createIt) {
        try {
            lock (this) {
                if (!TryGetEntity(entName, out ent)) {
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

    public void ForEach(Action<IEntity> act) {
        lock (this) {
            m_entityDictionary.ForEach(act);
        }
    }

    public void Dispose() {
        // TODO: do something about the entity list
        m_entityDictionary.ForEach(delegate(IEntity ent) {
            ent.Dispose();
        });
        m_entityDictionary.Clear(); // release any entities we might have

    }
}
}
