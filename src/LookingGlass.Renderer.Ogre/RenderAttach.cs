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
using LookingGlass.Renderer;
using LookingGlass.World;

namespace LookingGlass.Renderer.Ogr {
class RenderAttach : IRenderEntity {

    RendererOgre m_renderer;
    IEntity m_ent;

    public RenderAttach(RendererOgre contextRenderer, IEntity contextEntity) {
        m_renderer = contextRenderer;
        m_ent = contextEntity;
    }

    public bool Create(ref RenderableInfo ri, ref bool m_hasMesh, float priority, int retries) {
        lock (m_ent) {
            if (RendererOgre.GetSceneNodeName(m_ent) == null) {
                IAttachment atch;
                if (m_ent.TryGet<IAttachment>(out atch)) {
                    // an attachment is almost a prim, use prim code to get details
                    if (ri == null) {
                        IWorldRenderConv conver;
                        if (m_ent.TryGet<IWorldRenderConv>(out conver)) {
                            ri = conver.RenderingInfo(priority, m_renderer.m_sceneMgr, m_ent, retries);
                            if (ri == null) {
                                // The rendering info couldn't be built now. This is usually because
                                // the parent of this object is not available so we don't know where to put it
                                // m_renderer.m_log.Log(LogLevel.DRENDERDETAIL,
                                //     "Delaying rendering. RenderingInfo not built for {0}", m_ent.Name.Name);
                                return false;
                            }
                        }
                        else {
                            m_renderer.m_log.Log(LogLevel.DBADERROR, "DoRenderLater: NO WORLDRENDERCONV FOR PRIM {0}", 
                                            m_ent.Name.Name);
                            // this probably creates infinite retries but other things are clearly broken
                            return false;
                        }
                    }

                    string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
                    EntityName entMeshName = (EntityName)ri.basicObject;

                    // resolve the name of the parent scene node
                    string parentSceneNodeName = null;
                    if (ri.parentEntity != null) {
                        // this entity has a parent entity. create scene node off his
                        IEntity parentEnt = ri.parentEntity;
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(parentEnt.Name);
                    }
                    else {
                        // if no parent, add it at the top level of the region
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.RegionContext.Name);
                    }

                    if (!m_renderer.m_sceneMgr.CreateMeshSceneNodeBF(priority,
                                    entitySceneNodeName,
                                    parentSceneNodeName,
                                    entMeshName.Name,
                                    false, true,
                                    ri.position.X, ri.position.Y, ri.position.Z,
                                    ri.scale.X, ri.scale.Y, ri.scale.Z,
                                    ri.rotation.W, ri.rotation.X, ri.rotation.Y, ri.rotation.Z)) {
                        // m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering. {0} waiting for parent {1}",
                        //     m_ent.Name.Name, (parentSceneNodeName == null ? "NULL" : parentSceneNodeName));
                        return false;   // if I must have parent, requeue if no parent
                    }

                    // Add the name of the created scene node name so we know it's created and
                    // we can find it later.
                    m_ent.SetAddition(RendererOgre.AddSceneNodeName, entitySceneNodeName);
                }
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
                m_ent.RegionPosition.X, m_ent.RegionPosition.Y, m_ent.RegionPosition.Z, 0.25f,
                false, 1f, 1f, 1f, 1f,  // don't pass scale yet
                ((what & UpdateCodes.Rotation) != 0),
                m_ent.Heading.W, m_ent.Heading.X, m_ent.Heading.Y, m_ent.Heading.Z, 0.25f);
        }
    }
}
}
