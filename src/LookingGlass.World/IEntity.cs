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
using LookingGlass.Framework;
using OMV = OpenMetaverse;

namespace LookingGlass.World {

public interface IEntity : IRegistryCore, IDisposable {
    ulong LGID { get; }
    EntityName Name { get; set; }
    World WorldContext { get; }
    RegionContextBase RegionContext { get; }
    AssetContextBase AssetContext { get; }

    // Returns the entity which implements IEntityCollection which contains this entity
    IEntity ContainingEntity { get; set;  }
    // do what is necessary to set ContainingEntity to null (remove from parent if necessary)
    void DisconnectFromContainer();

    void AddEntityToContainer( IEntity ent);
    void RemoveEntityFromContainer( IEntity ent);

    OMV.Quaternion Heading { get; set; }
    OMV.Vector3 LocalPosition { get; set; }     // position relative to parent (if any)
    OMV.Vector3 RegionPosition { get; }         // position relative to RegionContext
    OMV.Vector3d GlobalPosition { get; }

    // code to check to see if this thing has changed from before
    int LastEntityHashCode { get; set; }
    // Notify the object that some of it state changed
    void Update(UpdateCodes what);

    /// <summary>
    /// An entity is decorated with additional Objects by other subsystems
    /// that either build information about or references to an entity.
    /// These additional objects are kept in a small array of objects for
    /// speed. The index into the array is an integer for the subsystem.
    /// There are predefined codes for the Viewer and Render but other
    /// systems can create a new subsystem index.
    /// </summary>
    Object Addition(int i);
    Object Addition(string s);
    void SetAddition(int i, Object obj);
}
}