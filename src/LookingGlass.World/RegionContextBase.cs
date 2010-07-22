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
    public event RegionRegionUpdatedCallback OnRegionUpdated;

    # pragma warning restore 0067
    #endregion

    protected WorldGroupCode m_worldGroup;
    public WorldGroupCode WorldGroup { get { return m_worldGroup; } }

    private RegionStateChangedCallback m_regionStateChangedCallback;
    protected RegionState m_regionState;
    public RegionState State {
        get { return m_regionState;  }
    }

    public RegionContextBase(RegionContextBase rcontext, AssetContextBase acontext) 
                : base(rcontext, acontext) {
        m_regionState = new RegionState();
        m_regionStateChangedCallback = new RegionStateChangedCallback(State_OnChange);
        State.OnStateChanged += m_regionStateChangedCallback;
        m_entityCollection = new EntityCollection(this.Name.Name);
        this.RegisterInterface<IEntityCollection>(m_entityCollection);
        this.RegisterInterface<IRegionContext>(this);
    }

    private void State_OnChange(RegionStateCode newState) {
        if (OnRegionStateChange != null) OnRegionStateChange(this, newState);
    }

    protected OMV.Vector3 m_size = new OMV.Vector3(256f, 256f, 8000f);
    public OMV.Vector3 Size { get { return m_size; } }

    // the world coordinate of the region's {0,0,0}
    protected OMV.Vector3d m_worldBase = new OMV.Vector3d(0d, 0d, 0d);
    public OMV.Vector3d WorldBase { get { return m_worldBase; } }

    // given an address relative to this region, return a global, world address
    public OMV.Vector3d CalculateGlobalPosition(OMV.Vector3 pos) {
        return m_worldBase + new OMV.Vector3d(pos.X, pos.Y, pos.Z);
    }
    public OMV.Vector3d CalculateGlobalPosition(float x, float y, float z) {
        return m_worldBase + new OMV.Vector3d(x, y, z);
    }

    // information on terrain for this region
    protected TerrainInfoBase m_terrainInfo = null;
    public TerrainInfoBase TerrainInfo { get { return m_terrainInfo; } }

    // try and get an entity from the entity collection in this region
    public virtual bool TryGetEntity(EntityName entName, out IEntity foundEnt) {
        bool ret = false;
        foundEnt = null;
        IEntityCollection coll;
        if (this.TryGet<IEntityCollection>(out coll)) {
            IEntity ent;
            if (coll.TryGetEntity(entName, out ent)) {
                foundEnt = ent;
                ret = true;
            }
        }
        return ret;
    }

    public override void Update(UpdateCodes what) {
        base.Update(what);      // this sends an EntityUpdate for the region
        if (OnRegionUpdated != null) OnRegionUpdated(this, what);
    }

    public override void Dispose() {
        m_terrainInfo = null; // let the garbage collector work
        if (m_regionState != null && m_regionStateChangedCallback != null) {
            State.OnStateChanged -= m_regionStateChangedCallback;
        }
        return;
    }
}
}
