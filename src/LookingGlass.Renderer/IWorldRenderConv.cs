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
    /// Collect rendering info. The information collected for rendering has a pre
    /// phase (this call), a doit phase and then a post phase (usually on demand
    /// requests).
    /// If we can't collect all the information return null. For LLLP, the one thing
    /// we might not have is the parent entity since child prims are rendered relative
    /// to the parent.
    /// This will be called multiple times trying to get the information for the 
    /// renderable. The callCount is the number of times we have asked. The caller
    /// can pass zero and know nothing will happen. Values more than zero can cause
    /// this routine to try and do some implementation specific thing to fix the
    /// problem. For LLLP, this is usually asking for the parent to be loaded.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="sceneMgr"></param>
    /// <param name="ent"></param>
    /// <param name="callCount">zero if do nothing, otherwise the number of times that
    /// this RenderingInfo has been asked for</param>
    /// <returns>rendering info or null if we cannot collect all data</returns>
    RenderableInfo RenderingInfo(float priority, Object sceneMgr, IEntity ent, int callCount);

    /// <summary>
    /// If doing mesh creation post processing, this causes the mesh resource to
    /// be created from the passed, world specific entity information.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="ent"></param>
    /// <returns>false if we need to wait for resources before completing mesh creation</returns>
    bool CreateMeshResource(float priority, IEntity ent, string meshName, EntityName contextEntityName);

    /// <summary>
    /// If doing material creation post processing, this causes the mesh resource to
    /// be created from the passed, world specific entity information.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="ent"></param>
    void CreateMaterialResource(float priority, IEntity ent, string materialName);

    /// <summary>
    /// Given an entity, recreate all the materials for this entity. Used when object
    /// initially created and when materials change
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="ent"></param>
    void RebuildEntityMaterials(float priority, IEntity ent);

    /// <summary>
    /// Given a new region context and a scene, convert the world specific region
    /// info into renderer coordinates.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="sceneMgr"></param>
    /// <param name="rcontext"></param>
    void MapRegionIntoView(float priority, Object sceneMgr, IRegionContext rcontext);

    /// <summary>
    /// Given an animation. Update the view of the entity with that animation. If teh
    /// entity is an avatar, the action will be different than if the entity is just
    /// a thing.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="ent"></param>
    /// <param name="sceneNodeName"></param>
    /// <param name="anim"></param>
    bool UpdateAnimation(float priority, IEntity ent, string sceneNodeName, IAnimation anim);
}
}
