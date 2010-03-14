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
                                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL,
                                    "Delaying rendering. RenderingInfo not built for {0}", m_ent.Name.Name);
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
    }
}
}
