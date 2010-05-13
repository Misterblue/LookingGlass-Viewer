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

namespace LookingGlass.World {

public delegate void EntityNewCallback(IEntity ent);
public delegate void EntityUpdateCallback(IEntity ent, UpdateCodes what);
public delegate void EntityRemovedCallback(IEntity ent);

    /// <summary>
    /// A collection of entities. Any entity that is a 'parent' of a set
    /// of other entities has one of these collections. The entities
    /// in teh collection are the 'children' of the entity. Entity collections
    /// are also kept by regions to hold all the top level entities
    /// in that region.
    /// </summary>
    public interface IEntityCollection : IDisposable {
        // when new items are added to the world
        event EntityNewCallback OnEntityNew;
        event EntityUpdateCallback OnEntityUpdate;
        event EntityRemovedCallback OnEntityRemoved;

        int Count { get; }

        void AddEntity(IEntity entity);

        void UpdateEntity(IEntity entity, UpdateCodes detail);

        void RemoveEntity(IEntity entity);

        /// <summary>
        /// Find an entity based on a LGID.
        /// </summary>
        /// <param name="lgid"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(ulong lgid, out IEntity ent);

        /// <summary>
        /// Find an entity based on its name
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(string entName, out IEntity ent);

        /// <summary>
        /// Find an entity based on its name
        /// </summary>
        /// <param name="entName"></param>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was found</returns>
        bool TryGetEntity(EntityName entName, out IEntity ent);

        /// <summary>
        /// Try to get an entity and create it if it doesn't exist.  If the
        /// entity is not found, the 'createIt' delegate is called to create
        /// the entity. Thus this routine always returns an entity.
        /// </summary>
        /// <param name="entName">name of entity to search for</param>
        /// <param name="ent">found entity</param>
        /// <param name="createIt">delegate called to create the entity if it doesn't exist</param>
        /// <returns>true if we created a new entry</returns>
        bool TryGetCreateEntity(EntityName entName, out IEntity ent, RegionCreateEntityCallback createIt);

        IEntity FindEntity(Predicate<IEntity> pred);

        void ForEach(Action<IEntity> act);
    }
}
