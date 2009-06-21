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
using LookingGlass.Comm;
using OMV = OpenMetaverse;

namespace LookingGlass.World {

public delegate void WorldRegionConnectedCallback(RegionContextBase rcontext);
public delegate void WorldRegionChangingCallback(RegionContextBase rcontext);
public delegate void WorldEntityNewCallback(IEntity ent);
public delegate void WorldEntityUpdateCallback(IEntity ent, UpdateCodes what);
public delegate void WorldEntityRemovedCallback(IEntity ent);
public delegate void WorldTerrainUpdateCallback(RegionContextBase rcontext);
public delegate void WorldAgentNewCallback(IAgent agnt);
public delegate void WorldAgentUpdateCallback(IAgent agnt, UpdateCodes what);
public delegate void WorldAgentRemovedCallback(IAgent agnt);

public delegate IEntity WorldCreateEntityCallback();

public enum WorldGroupCode {
    LLWorld,
    OtherWorld,
}

public enum UpdateCodes : uint {
    None = 0,
    AttachmentPoint = 1 << 0,
    Material =        1 << 1,
    ClickAction =     1 << 2,
    Scale =           1 << 3,
    ParentID =        1 << 4,
    PrimFlags =       1 << 5,
    PrimData =        1 << 6,
    MediaURL =        1 << 7,
    ScratchPad =      1 << 8,
    Textures =        1 << 9,
    TextureAnim =     1 << 10,
    NameValue =       1 << 11,
    Position =        1 << 12,
    Rotation =        1 << 13,
    Velocity =        1 << 14,
    Acceleration =    1 << 15,
    AngularVelocity = 1 << 16,
    CollisionPlane =  1 << 17,
    Text =            1 << 18,
    Particles =       1 << 19,
    ExtraData =       1 << 20,
    Sound =           1 << 21,
    Joint =           1 << 22,
    Terrain =         1 << 23,
    FullUpdate = UInt32.MaxValue
}

    /// <summary>
    /// No one actually uses the IWorld interface other than World and most code
    /// references World directly since there is only one. But this defintiion
    /// exists to pull together the operations that can happen to the world.
    /// 
    /// The world is the central repository of objects that are received from
    /// the communcation stacks and that are displayed by the viewers.
    /// </summary>
public interface IWorld {

    #region Events
    // when the underlying simulator is changing.
    event WorldRegionChangingCallback OnWorldRegionChanging;
    // when a new simulator is initialized
    event WorldRegionConnectedCallback OnWorldRegionConnected;
    // when new items are added to the world
    event WorldEntityNewCallback OnWorldEntityNew;
    // when a prim is updated
    event WorldEntityUpdateCallback OnWorldEntityUpdate;
    // when an object is killed
    event WorldEntityRemovedCallback OnWorldEntityRemoved;
    // when the terrain information is changed
    event WorldTerrainUpdateCallback OnWorldTerrainUpdated;
    // when a new agent is added to the system
    event WorldAgentNewCallback OnAgentNew;
    // when an agent is updated
    event WorldAgentUpdateCallback OnAgentUpdate;
    // when an agent is removed from the world (logged out)
    event WorldAgentRemovedCallback OnAgentRemoved;

    #endregion Events

    // REGION MANAGEMENT
    void AddRegion(RegionContextBase rcontext);
    void UpdateRegion(RegionContextBase rcontext, UpdateCodes detail);
    void RemoveRegion(RegionContextBase rcontext);
    RegionContextBase GetRegion(string name);
    RegionContextBase FindRegion(Predicate<RegionContextBase> pred);

    // ENTITY MANAGEMENT
    void AddEntity(IEntity entity);
    void UpdateEntity(IEntity entity, UpdateCodes detail);
    void RemoveEntity(IEntity entity);

    bool TryGetEntity(ulong lgid, out IEntity ent);
    bool TryGetEntity(string entName, out IEntity ent);
    bool TryGetEntity(EntityName entName, out IEntity ent);
    bool TryGetEntityLocalID(uint entName, out IEntity ent);
    bool TryGetCreateEntityLocalID(uint localID, out IEntity ent, WorldCreateEntityCallback creater);
    IEntity FindEntity(Predicate<IEntity> pred);

    // AGENT MANAGEMENT
    void AddAgent(IAgent agnt);
    void UpdateAgent(IAgent agnt, UpdateCodes changed);
    void RemoveAgent(IAgent agnt);
    void ForEachAgent(Action<IAgent> action);
    IAgent FindAgent(Predicate<IAgent> pred);

}

}
