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
using OMV = OpenMetaverse;

namespace LookingGlass.World {
public abstract class RegionContextBase : EntityBase, IRegionContext, IDisposable {

    protected WorldGroupCode m_worldGroup;
    public WorldGroupCode WorldGroup { get { return m_worldGroup; } }

    public RegionContextBase(RegionContextBase rcontext, AssetContextBase acontext) 
                : base(rcontext, acontext) {
    }

    protected OMV.Vector3 m_size = new OMV.Vector3(256f, 256f, 8000f);
    public OMV.Vector3 Size { get { return m_size; } }

    // the world coordinate of the region's {0,0,0}
    protected OMV.Vector3d m_worldBase = new OMV.Vector3d(0d, 0d, 0d);
    public OMV.Vector3d WorldBase { get { return m_worldBase; } }

    // information on terrain for this region
    protected TerrainInfoBase m_terrainInfo = null;
    public TerrainInfoBase TerrainInfo { get { return m_terrainInfo; } }

    public bool TryGetEntityLocalID(uint entName, out IEntity ent) {
        // someday, entities will be managed by regions. For the moment they are in the world
        return World.Instance.TryGetEntityLocalID(this, entName, out ent);
    }

    public bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, WorldCreateEntityCallback creater) {
        // someday, entities will be managed by regions. For the moment they are in the world
        return World.Instance.TryGetCreateEntityLocalID(this, localID, out ent, creater);
    }

    public bool TryGetCreateAvatar(EntityName ename, out IEntityAvatar ent, WorldCreateAvatarCallback creater) {
        // someday, entities will be managed by regions. For the moment they are in the world
        return World.Instance.TryGetCreateAvatar(this, ename, out ent, creater);
    }

    public override void Dispose() {
        m_terrainInfo = null; // let the garbage collector work
        return;
    }
}
}
