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
using OMV = OpenMetaverse;

namespace LookingGlass.World {
/// <summary>
/// EntityBase adds the handlers for the basic entity attributes and
/// the management of the additional objects that subsystems can hang
/// on an entity.
/// </summary>
public abstract class EntityBase : IEntity {
    // Every entity has a local, session scoped ID
    protected ulong m_LGID = 0;
    public ulong LGID { 
        get {
            if (m_LGID == 0) m_LGID = NextLGID();
            return m_LGID; 
        } 
    }
    private static ulong m_LGIDIndex = 0x10000000;
    public static ulong NextLGID() { return m_LGIDIndex++; }

    // Every entity has a name
    protected EntityName m_name = null;
    public virtual EntityName Name { 
        get {
            if (m_name == null) m_name = new EntityName(m_assetContext, LGID.ToString());
            return m_name; 
        } 
        set { m_name = value; }
    }

    protected IEntity m_containingEntity;
    public virtual IEntity ContainingEntity {
        get { return m_containingEntity; }
        set { m_containingEntity = value; }
    }
    // If associated with a parent, go to the parent and remove us from
    // the parent's container.
    // Call before removing/deleting/destroying an entity.
    public virtual void DisconnectFromContainer() {
        if (m_containingEntity != null) {
            IEntityCollection coll;
            if (m_containingEntity.TryGet<IEntityCollection>(out coll)) {
                coll.RemoveEntity(this);
            }
            m_containingEntity = null;
        }
    }
    protected IEntityCollection m_entityCollection = null;
    public virtual void AddEntityToContainer(IEntity ent) {
        if (m_entityCollection == null) {
            m_entityCollection = new EntityCollection();
        }
        m_entityCollection.AddEntity(ent);
    }
    public virtual void RemoveEntityFromContainer(IEntity ent) {
        if (m_entityCollection != null) {
            m_entityCollection.RemoveEntity(ent);
            if (m_entityCollection.Count == 0) {
                m_entityCollection = null;
            }
        }
    }

    protected int m_lastEntityHashCode = 0;
    public int LastEntityHashCode { get { return m_lastEntityHashCode; } set { m_lastEntityHashCode = value; } }

    static EntityBase() {
        AdditionSubsystems = new Dictionary<string,int>();
    }

    public EntityBase(RegionContextBase rcontext, AssetContextBase acontext) {
        Additions = new Object[EntityBase.ADDITIONCLASSES];
        for (int ii = 0; ii < EntityBase.ADDITIONCLASSES; ii++) {
            Additions[ii] = null;
        }
        m_LGID = NextLGID();
        m_worldContext = (World)LookingGlassBase.Instance.ModManager.Module("World");
        m_regionContext = rcontext;
        m_assetContext = acontext;
    }


    #region IRegistryCore
    protected Dictionary<Type, object> m_moduleInterfaces = new Dictionary<Type, object>();

    /// <summary>
    /// Register an Module interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="iface"></param>
    public void RegisterInterface<T>(T iface) {
        lock (m_moduleInterfaces) {
            if (!m_moduleInterfaces.ContainsKey(typeof(T))) {
                m_moduleInterfaces.Add(typeof(T), iface);
            }
        }
    }

    public bool TryGet<T>(out T iface) {
        if (m_moduleInterfaces.ContainsKey(typeof(T))) {
            iface = (T)m_moduleInterfaces[typeof(T)];
            return true;
        }
        iface = default(T);
        return false;
    }

    public T Get<T>() {
        if (m_moduleInterfaces.ContainsKey(typeof(T))) {
            return (T)m_moduleInterfaces[typeof(T)];
        }
        return default(T);
    }

    public void StackModuleInterface<M>(M mod) {
    }

    public T[] RequestModuleInterfaces<T>() {
        return new T[] { default(T) };
    }
#endregion IRegistryCore

    public virtual void Dispose() {
        // tell all the interfaces we're done with them
        foreach (KeyValuePair<Type, object> kvp in m_moduleInterfaces) {
            try {
                IDisposable idis = (IDisposable)kvp.Value;
                idis.Dispose();
            }
            catch {
                // if it won't dispose it's not our problem
            }
        }
        m_moduleInterfaces.Clear();
    }


    #region CONTEXTS
    protected World m_worldContext;
    public World WorldContext { get { return m_worldContext; } set { m_worldContext = value; } }

    protected RegionContextBase m_regionContext;
    public RegionContextBase RegionContext { get { return m_regionContext; } set { m_regionContext = value; } }

    protected AssetContextBase m_assetContext;
    public AssetContextBase AssetContext { get { return m_assetContext; } set { m_assetContext = value; } }
    #endregion CONTEXTS

    #region LOCATION
    protected OMV.Quaternion m_heading = new OMV.Quaternion();
    virtual public OMV.Quaternion Heading {
        get {
            return m_heading;
        }
        set {
            m_heading = value;
        }
    }

    // position relative to RegionContext
    protected OMV.Vector3 m_relativePosition = new OMV.Vector3(10f, 10f, 10f);
    virtual public OMV.Vector3 RelativePosition {
        get {
                return m_relativePosition;
        }
        set {
            m_relativePosition = value;
        }
    }

    protected OMV.Vector3d m_globalPosition;
    virtual public OMV.Vector3d GlobalPosition {
        get {
            if (m_regionContext != null) {
                return new OMV.Vector3d(
                    m_regionContext.WorldBase.X + (double)RelativePosition.X,
                    m_regionContext.WorldBase.Y + (double)RelativePosition.Y,
                    m_regionContext.WorldBase.Z + (double)RelativePosition.Z);
            }
            return new OMV.Vector3d(10d, 10d, 10d);
        }
        set {
            if (RegionContext != null) {
                m_relativePosition = new OMV.Vector3(
                    (int)(value.X - m_regionContext.WorldBase.X),
                    (int)(value.Y - m_regionContext.WorldBase.Y),
                    (int)(value.Z - m_regionContext.WorldBase.Z)
                );
            }
            // if no region. fake a value
            m_relativePosition = new OMV.Vector3(
                (int)(value.X % 256d), (int)(value.X % 256d), (int)(value.Z % 256d)
                );
        }
    }
    #endregion LOCATION


    #region ADDITIONS
    const int ADDITIONCLASSES = 7;  // maximum subsystems that can be added
    public Object[] Additions;
    public static Dictionary<string,int> AdditionSubsystems;

    public Object Addition(int ii) {
        if (ii < Additions.Length) return Additions[ii];
        else return null;
    }

    public Object Addition(string ss) { 
        if (AdditionSubsystems.ContainsKey(ss)) return Additions[EntityBase.AdditionSubsystems[ss]];
        else return null;
    }
    public void SetAddition(int ii, Object obj) { Additions[ii] = obj; }

    /// <summary>
    /// Create a new subsystem index. If teh subsystem is already
    /// defined, the previously allocated index is returned.
    /// </summary>
    /// <param name="addClass">Name of the subsystem to add</param>
    /// <returns>The newly allocated index or the previously allocated
    /// index for this subsystem.</returns>
    public static int AddAdditionSubsystem(string addClass) {
        int ret = 0;
        if (AdditionSubsystems.ContainsKey(addClass)) {
            // it's already in the list, just return the old number
            ret = AdditionSubsystems[addClass];
        }
        else {
            int newIndex = 0;
            foreach (KeyValuePair<string, int>kvp in AdditionSubsystems) {
                if (kvp.Value >= newIndex) newIndex = kvp.Value+1;
            }
            AdditionSubsystems.Add(addClass, newIndex);
            ret = newIndex;
            // make sure the addition class array is big enough for the new class
            if (ADDITIONCLASSES <= newIndex) {
                // We cannot add more than the max!!
                throw new LookingGlassException("Adding more Entity object classes than allowed. Tried to add " + addClass);
                // Object[] newAdditions = new Object[Additions.Length + 4];
                // for (int ii = 0; ii < newAdditions.Length; ii++) {
                //     newAdditions[ii] = ii >= Additions.Length ? null : Additions[ii];
                // }
                // Additions = newAdditions;
            }
        }
        return ret;
    }
    #endregion ADDITIONS

    // Tell the entity that something about it changed
    virtual public void Update(UpdateCodes what) {
        if (this.RegionContext != null) {
            LogManager.Log.Log(LogLevel.DUPDATEDETAIL, "EntityBase.Update calling RegionContext.UpdateEntity. w={0}", what);
            IEntityCollection coll;
            if (this.RegionContext.TryGet<IEntityCollection>(out coll)) {
                coll.UpdateEntity(this, what);
            }
        }
        return;
    }
}
}
