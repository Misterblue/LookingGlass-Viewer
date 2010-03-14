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

class RenderPrim {

    bool m_shouldForceMeshRebuild;
    bool m_shouldPrebuildMesh;
    bool m_shouldShareMeshes;

    RendererOgre m_renderer;

    static Dictionary<ulong, EntityName> prebuiltMeshes = new Dictionary<ulong, EntityName>();

    public RenderPrim(RendererOgre contextRenderer) {
        m_renderer = contextRenderer;
        m_shouldForceMeshRebuild = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".Ogre.ForceMeshRebuild");
        m_shouldPrebuildMesh = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".Ogre.PrebuildMesh");
        m_shouldShareMeshes = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".ShouldShareMeshes");
    }

    public bool Create(IEntity ent, ref RenderableInfo ri, ref bool m_hasMesh, float priority, int retries) {
        string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(ent.Name);

        lock (ent) {
            if (RendererOgre.GetSceneNodeName(ent) == null) {
                try {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Adding SceneNode to new entity " + ent.Name);
                    if (ri == null) {
                        IWorldRenderConv conver;
                        if (ent.TryGet<IWorldRenderConv>(out conver)) {
                            ri = conver.RenderingInfo(priority, m_renderer.m_sceneMgr, ent, retries);
                            if (ri == null) {
                                // The rendering info couldn't be built now. This is usually because
                                // the parent of this object is not available so we don't know where to put it
                                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL,
                                    "Delaying rendering. RenderingInfo not built for {0}", ent.Name.Name);
                                return false;
                            }
                        }
                        else {
                            m_renderer.m_log.Log(LogLevel.DBADERROR, "DoRenderLater: NO WORLDRENDERCONV FOR PRIM {0}", 
                                            ent.Name.Name);
                            // this probably creates infinite retries but other things are clearly broken
                            return false;
                        }
                    }

                    // Check to see if something of this mesh shape already exists. Use it if so.
                    EntityName entMeshName = (EntityName)ri.basicObject;
                    if (m_shouldShareMeshes && (ri.shapeHash != RenderableInfo.NO_HASH_SHARE)) {
                        lock (prebuiltMeshes) {
                            if (prebuiltMeshes.ContainsKey(ri.shapeHash)) {
                                entMeshName = prebuiltMeshes[ri.shapeHash];
                                m_hasMesh = true;   // presume someone else created it
                                // m_log.Log(LogLevel.DRENDERDETAIL, "DorRenderLater: using prebuilt {0}", entMeshName);
                                // m_statShareInstances.Event();
                            }
                            else {
                                // this is a new mesh. Remember that it has been built
                                prebuiltMeshes.Add(ri.shapeHash, entMeshName);
                            }
                        }
                    }

                    // Find a handle to the parent for this node
                    string parentSceneNodeName = null;
                    if (ri.parentEntity != null) {
                        // this entity has a parent entity. create scene node off his
                        IEntity parentEnt = ri.parentEntity;
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(parentEnt.Name);
                    }
                    else {
                        // if no parent, add it at the top level of the region
                        parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(ent.RegionContext.Name);
                    }

                    // create the mesh we know we need
                    if ((m_shouldPrebuildMesh || m_shouldForceMeshRebuild) && !m_hasMesh) {
                        // way kludgy... but we see if the cached mesh file exists and, if so, we know it exists
                        if (!m_shouldForceMeshRebuild && ent.AssetContext.CheckIfCached(ent, entMeshName)) {
                            // if we just want the mesh built, if the file exists that's enough prebuilding
                            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.DorRenderLater: mesh file exists: {0}", 
                                                ent.Name.CacheFilename);
                            m_hasMesh = true;
                        }
                        if (!m_hasMesh) {
                            // TODO: figure out how to do this without queuing -- do it now
                            //2 RequestMesh(ent.Name, entMeshName.Name);
                            //1 Object[] meshLaterParams = { entMeshName.Name, entMeshName };
                            //1 bool worked = RequestMeshLater(qInstance, meshLaterParams);
                            //1 // if we can't get the mesh now, we'll have to wait until all the pieces are here
                            //1 if (!worked) return false;
                            IWorldRenderConv conver;
                            ent.TryGet<IWorldRenderConv>(out conver);
                            if (!conver.CreateMeshResource(priority, ent, entMeshName.Name, entMeshName)) {
                                // we need to wait until some resource exists before we can complete this creation
                                return false; // note that m_hasMesh is still false so we'll do this routine again
                            }
                            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.DorRenderLater: prebuild/forced mesh build for {0}",
                                                    entMeshName.Name);
                            // The mesh was crreated into the queued item so if a 'false' is returned
                            // later, we don't create the mesh again.
                            m_hasMesh = true;
                        }
                    }

                    // Create the scene node for this entity
                    // and add the definition for the object on to the scene node
                    // This will cause the load function to be called and create all
                    //   the callbacks that will actually create the object
                    // m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: mesh={0}, prio={1}", entMeshName.Name, priority);
                    if (!m_renderer.m_sceneMgr.CreateMeshSceneNodeBF(priority,
                                    entitySceneNodeName,
                                    parentSceneNodeName,
                                    entMeshName.Name,
                                    false, true,
                                    ri.position.X, ri.position.Y, ri.position.Z,
                                    ri.scale.X, ri.scale.Y, ri.scale.Z,
                                    ri.rotation.W, ri.rotation.X, ri.rotation.Y, ri.rotation.Z)) {
                        // m_log.Log(LogLevel.DRENDERDETAIL, "Delaying rendering. {0} waiting for parent {1}",
                        //     ent.Name.Name, (parentSceneNodeName == null ? "NULL" : parentSceneNodeName));
                        return false;   // if I must have parent, requeue if no parent
                    }

                    // Add the name of the created scene node name so we know it's created and
                    // we can find it later.
                    ent.SetAddition(RendererOgre.AddSceneNodeName, entitySceneNodeName);

                }
                catch (Exception e) {
                    m_renderer.m_log.Log(LogLevel.DBADERROR, "Render: Failed conversion of {0}: {1}", ent.Name.Name, e.ToString());
                }
            }
            else {
                // the entity already has a scene node. We're just forcing the rebuild of the prim
                if (ri == null) {
                    IWorldRenderConv conver;
                    ent.TryGet<IWorldRenderConv>(out conver);
                    ri = conver.RenderingInfo(priority, m_renderer.m_sceneMgr, ent, retries);
                    if (ri == null) {
                        // The rendering info couldn't be built now. This is usually because
                        // the parent of this object is not available so we don't know where to put it
                        m_renderer.m_log.Log(LogLevel.DRENDERDETAIL,
                            "Delaying rendering with scene node RenderingInfo not built for {0}", ent.Name.Name);
                        return false;
                    }
                }
                // Find a handle to the parent for this node
                string parentSceneNodeName = null;
                if (ri.parentEntity != null) {
                    // this entity has a parent entity. create scene node off his
                    IEntity parentEnt = ri.parentEntity;
                    parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(parentEnt.Name);
                }
                else {
                    parentSceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(ent.RegionContext.Name);
                }

                EntityName entMeshName = (EntityName)ri.basicObject;
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: entity has scenenode. Rebuilding mesh: {0}", entMeshName);
                m_renderer.RequestMesh(ent.Name, entMeshName.Name);
            }
            return true;
        }
    }
}
}
