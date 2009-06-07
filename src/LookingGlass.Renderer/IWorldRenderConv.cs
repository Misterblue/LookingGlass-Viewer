/* Copyright (c) Robert Adams
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
using LookingGlass.World;

namespace LookingGlass.Renderer {
/// <summary>
/// There is a cross product of potential world format types and render
/// display formats. This class contains routines for the conversion
/// of some world type (LL prim, for instance) to renderer display
/// formats (Mogre mesh).
/// </summary>
public interface IWorldRenderConv {
    
    /// <summary>
    /// Generate the rendering info for an entity. The entity could have
    /// already had information added to it or the rendering information could
    /// already be converted and in cache.
    /// </summary>
    /// <param name="sceneMgr"></param>
    /// <param name="ent"></param>
    /// <returns></returns>
    RenderableInfo RenderingInfo(Object sceneMgr, IEntity ent);

    /// <summary>
    /// If doing mesh creation post processing, this causes the mesh resource to
    /// be created from the passed, world specific entity information.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>false if we need to wait for resources before completing mesh creation</returns>
    bool CreateMeshResource(Object sceneMgr, IEntity ent, string meshName);

    /// <summary>
    /// If doing material creation post processing, this causes the mesh resource to
    /// be created from the passed, world specific entity information.
    /// </summary>
    /// <param name="ent"></param>
    void CreateMaterialResource(Object sceneMgr, IEntity ent, string materialName);

    /// <summary>
    /// Given a new region context and a scene, convert the world specific region
    /// info into renderer coordinates.
    /// </summary>
    /// <param name="sceneMgr"></param>
    /// <param name="rcontext"></param>
    void MapRegionIntoView(Object sceneMgr, IRegionContext rcontext);
}
}
