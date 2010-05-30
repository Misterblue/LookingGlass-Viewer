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
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.Renderer;
using LookingGlass.World;

namespace LookingGlass.Renderer.Ogr {
class RenderAvatar : IRenderEntity {

    RendererOgre m_renderer;
    IEntity m_ent;

    string m_defaultAvatarMesh;

    public RenderAvatar(RendererOgre contextRenderer, IEntity contextEntity) {
        m_renderer = contextRenderer;
        m_ent = contextEntity;
        m_defaultAvatarMesh = m_renderer.ModuleParams.ParamString(m_renderer.ModuleName + ".Ogre.LL.DefaultAvatarMesh");
    }

    public bool Create(ref RenderableInfo ri, ref bool m_hasMesh, float priority, int retries) {
        // collect mesh info
        // collect morph data
        // Ogr.CreateAvatarBF()

        string entitySceneNodeName;
        string parentSceneNodeName;
        EntityName entMeshName;

        if (RendererOgre.GetSceneNodeName(m_ent) == null) {
            entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
            parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.RegionContext.Name);

            IWorldRenderConv wrc;
            if (m_ent.TryGet<IWorldRenderConv>(out wrc)) {
                entMeshName = EntityNameOgre.ConvertToOgreMeshName(m_ent.Name);
                if (!wrc.CreateAvatarMeshResource(0f, m_ent, entMeshName.Name, m_ent.Name)) {
                    // something about this avatar can't be created yet. Try again later.
                    return false;
                }
            }
            else {
                entMeshName = EntityNameOgre.ConvertToOgreMeshName(new EntityName(m_defaultAvatarMesh));
            }

            IEntityAvatar av;
            if (m_ent.TryGet<IEntityAvatar>(out av)) {
                // Create the scene node for this entity
                // and add the definition for the object on to the scene node
                // This will cause the load function to be called and create all
                //   the callbacks that will actually create the object
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderAvatar.Create: mesh={0}, prio={1}", 
                            entMeshName.Name, priority);
                if (!m_renderer.m_sceneMgr.CreateMeshSceneNodeBF(priority,
                                entitySceneNodeName,
                                parentSceneNodeName,
                                entMeshName.Name,
                                false, true,
                                av.RegionPosition.X, av.RegionPosition.Y, av.RegionPosition.Z,
                                1f, 1f, 1f,
                                av.Heading.W, av.Heading.X, av.Heading.Y, av.Heading.Z)) {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Delaying avatar rendering. {0} waiting for parent {1}",
                    //     m_ent.Name.Name, (parentSceneNodeName == null ? "NULL" : parentSceneNodeName));
                    return false;   // if I must have parent, requeue if no parent
                }
                m_ent.SetAddition(RendererOgre.AddSceneNodeName, entitySceneNodeName);
            }
            else {
                // shouldn't be creating an avatar with no avatar info
            }
        }
        return true;
    }

    public void Update(UpdateCodes what) {
        float priority = m_renderer.CalculateInterestOrder(m_ent);
        bool fullUpdate = false;    // true if a full update was done on this entity
        if ((what & UpdateCodes.New) != 0) {
            // new entity. Gets the full treatment
            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: New entity: {0}", m_ent.Name.Name);
            m_renderer.DoRenderQueued(m_ent);
            fullUpdate = true;
        }
        if ((what & UpdateCodes.New) == 0) {
            // if not a new update, see what in particular is changing for this prim
            if ((what & UpdateCodes.ParentID) != 0) {
                // prim was detached or attached. Rerender if not the first update
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: parentID changed");
                if (!fullUpdate) m_renderer.DoRenderQueued(m_ent);
                fullUpdate = true;
            }
            if ((what & UpdateCodes.Material) != 0) {
                // the materials have changed on this entity. Cause materials to be recalcuated
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: Material changed");
            }
            // if (((what & (UpdateCodes.PrimFlags | UpdateCodes.PrimData)) != 0))) {
            if ((what & (UpdateCodes.PrimFlags | UpdateCodes.PrimData)) != 0) {
                // the prim parameters were changed. Re-render if this is not the new creation request
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: prim data changed");
                if (!fullUpdate) m_renderer.DoRenderQueued(m_ent);
                fullUpdate = true;
            }
            if ((what & UpdateCodes.Textures) != 0) {
                // texure on the prim were updated. Refresh them if not the initial creation update
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: textures changed");
                // to get the textures to refresh, we must force the situation
                IWorldRenderConv conver;
                if (m_ent.TryGet<IWorldRenderConv>(out conver)) {
                    conver.RebuildEntityMaterials(priority, m_ent);
                    Ogr.RefreshResourceBF(priority, Ogr.ResourceTypeMesh, 
                                EntityNameOgre.ConvertToOgreMeshName(m_ent.Name).Name);
                }
            }
        }
        if (!fullUpdate && (what & (UpdateCodes.Scale | UpdateCodes.Position | UpdateCodes.Rotation)) != 0) {
            // world position has changed. Tell Ogre they have changed
            string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: Updating position/rotation for {0}", entitySceneNodeName);
            Ogr.UpdateSceneNodeBF(priority, entitySceneNodeName,
                ((what & UpdateCodes.Position) != 0),
                m_ent.RegionPosition.X, m_ent.RegionPosition.Y, m_ent.RegionPosition.Z, 1f,
                false, 1f, 1f, 1f, 1f,  // don't pass scale yet
                ((what & UpdateCodes.Rotation) != 0),
                m_ent.Heading.W, m_ent.Heading.X, m_ent.Heading.Y, m_ent.Heading.Z, 1f);
        }
    }
}
}
