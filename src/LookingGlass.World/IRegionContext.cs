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
    /// <summary>
    /// Region context has to do with the space an entity is in. Regions
    /// can overlap. The region defines the space (XYZ) any terrain (heightmap)
    /// and is the basic interface between mapping local coordinates
    /// into the displayed view.
    /// </summary>
 

public delegate void RegionRegionConnectedCallback(RegionContextBase rcontext);
public delegate void RegionRegionUpdatedCallback(RegionContextBase rcontext, UpdateCodes what);
public delegate void RegionRegionDisconnectedCallback(RegionContextBase rcontext);
public delegate void RegionEntityNewCallback(IEntity ent);
public delegate void RegionEntityUpdateCallback(IEntity ent, UpdateCodes what);
public delegate void RegionEntityRemovedCallback(IEntity ent);
public delegate void RegionTerrainUpdateCallback(RegionContextBase rcontext);

// used in TryGetCreateentity calls to create the entity if needed
public delegate IEntity RegionCreateEntityCallback();

public interface IRegionContext {

    #region Events
    // when a new simulator is initialized
    event RegionRegionConnectedCallback OnRegionConnected;
    // when the underlying simulator is changing.
    event RegionRegionUpdatedCallback OnRegionUpdated;
    // when a new simulator is not active an probably going away
    event RegionRegionDisconnectedCallback OnRegionDisconnected;
    // when new items are added to the world
    event RegionEntityNewCallback OnEntityNew;
    // when a prim is updated
    event RegionEntityUpdateCallback OnEntityUpdate;
    // when an object is killed
    event RegionEntityRemovedCallback OnEntityRemoved;
    // when the terrain information is changed
    event RegionTerrainUpdateCallback OnTerrainUpdated;

    #endregion Events

    // get the name of the region
    EntityName Name { get; }

    // get the type of the region
    WorldGroupCode WorldGroup { get; }

    // the size of the region (bounding box)
    OMV.Vector3 Size { get; }

    // the world coordinate of the region's {0,0,0}
    OMV.Vector3d WorldBase { get; }

    // information on terrain for this region
    TerrainInfoBase TerrainInfo { get; }

    // ENTITY MANAGEMENT
    void AddEntity(IEntity entity);
    void UpdateEntity(IEntity entity, UpdateCodes detail);
    void RemoveEntity(IEntity entity);

    bool TryGetEntity(ulong lgid, out IEntity ent);
    bool TryGetEntity(string entName, out IEntity ent);
    bool TryGetEntity(EntityName entName, out IEntity ent);
    IEntity FindEntity(Predicate<IEntity> pred);

    // In  transition requests for getting region entities based on implementation
    // specific info. In this case the LLLP localID. This is part of the conversion
    // of entites being in the world to the entities being in regions.
    bool TryGetEntityLocalID(uint entName, out IEntity ent);
    bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, RegionCreateEntityCallback creater);

}
}
