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
using System.IO;
using System.Text;
using LookingGlass.World;

namespace LookingGlass.Renderer {
    /// <summary>
    /// Interface to the persistant storage system which holds the local
    /// copies of rendered objects and textures. Asset storage systems
    /// can be local and remote. The local cache will keep cross session
    /// copies of assets while remote stores will hold larger collections
    /// of assets.
    /// 
    /// Anyone using the persistant store will reference multiple stores
    /// until the asset is found.
    /// </summary>
    public interface IPersistantStore {
        // return 'true' if we have the entity and are returning a stream to its bits
        bool TryGetEntity(IEntity context, EntityName entName, out Stream bits);

        // return 'true' if this is our storable local cache
        bool isStoreable();

        // store a stream of bits as this entity
        void StoreEntity(IEntity context, EntityName entName, Stream bits);

        /// <summary>
        /// Return 'true' if the entity exists in the cache. This is only good
        /// for checking existance in the local cache. Otherwise the result is
        /// undefined.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entName"></param>
        /// <returns>'true' if the entity is in the local cache</returns>
        bool ExistsInCache(IEntity context, EntityName entName);


    }
}
