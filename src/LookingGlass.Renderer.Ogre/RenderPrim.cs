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

class RenderPrim : IRenderEntity {

    // If true, requesting a mesh causes the mesh to be rebuilt and written out
    // This makes sure cached copy is the same as server but is also slow
    bool m_shouldForceMeshRebuild;
    // True if to make sure the mesh exists before creating it's scene node
    bool m_shouldPrebuildMesh;
    // True if meshes with the same characteristics should be shared
    bool m_shouldShareMeshes;

    RendererOgre m_renderer;
    IEntity m_ent;

    public static Dictionary<ulong, EntityName> prebuiltMeshes = new Dictionary<ulong, EntityName>();

    public RenderPrim(RendererOgre contextRenderer, IEntity referenceEntity) {
        m_renderer = contextRenderer;
        m_ent = referenceEntity;
        m_shouldForceMeshRebuild = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".Ogre.ForceMeshRebuild");
        m_shouldPrebuildMesh = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".Ogre.PrebuildMesh");
        m_shouldShareMeshes = m_renderer.ModuleParams.ParamBool(m_renderer.ModuleName + ".ShouldShareMeshes");
    }

    public bool Create(ref RenderableInfo ri, ref bool m_hasMesh, float priority, int retries) {
        string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);

        lock (m_ent) {
            // create the rendering info if not created yet
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

            // Check to see if something of this mesh shape already exists. Use it if so.
            EntityName entMeshName = (EntityName)ri.basicObject;
            if (m_shouldShareMeshes && (ri.shapeHash != RenderableInfo.NO_HASH_SHARE)) {
                lock (prebuiltMeshes) {
                    if (prebuiltMeshes.TryGetValue(ri.shapeHash, out entMeshName)) {
                        m_hasMesh = true;   // presume someone else created it
                        // m_log.Log(LogLevel.DRENDERDETAIL, "DorRenderLater: using prebuilt {0}", entMeshName);
                        // m_statShareInstances.Event();
                    }
                    else {
                        // re-get mesh name since TryGet defaults it if not found
                        entMeshName = (EntityName)ri.basicObject;
                        // this is a new mesh. Remember that it has been built
                        prebuiltMeshes.Add(ri.shapeHash, entMeshName);
                    }
                }
            }

            // see if this scene node has already been created
            if (RendererOgre.GetSceneNodeName(m_ent) == null) {
                try {
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Adding SceneNode to new entity " + m_ent.Name);

                    // Find a handle to the parent for this node
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

                    // Create the mesh if prebilding. The mesh can be either built later on demand or now if
                    // we're forcing the build.
                    if ((m_shouldPrebuildMesh || m_shouldForceMeshRebuild) && !m_hasMesh) {
                        // way kludgy... but we see if the cached mesh file exists and, if so, we don't need to rebuild
                        if (!m_shouldForceMeshRebuild && m_ent.AssetContext.CheckIfCached(entMeshName)) {
                            // if we just want the mesh built, if the file exists that's enough prebuilding
                            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RendererOgre.DorRenderLater: mesh file exists: {0}", 
                                                m_ent.Name.CacheFilename);
                            m_hasMesh = true;
                        }
                        if (!m_hasMesh) {
                            // TODO: figure out how to do this without queuing -- do it now
                            //2 RequestMesh(m_ent.Name, entMeshName.Name);
                            //1 Object[] meshLaterParams = { entMeshName.Name, entMeshName };
                            //1 bool worked = RequestMeshLater(qInstance, meshLaterParams);
                            //1 // if we can't get the mesh now, we'll have to wait until all the pieces are here
                            //1 if (!worked) return false;
                            IWorldRenderConv conver;
                            m_ent.TryGet<IWorldRenderConv>(out conver);
                            if (!conver.CreateMeshResource(priority, m_ent, entMeshName.Name, entMeshName)) {
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
                    m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim: ent={0}, mesh={1}", m_ent.Name, entMeshName.Name);
                    if (!m_renderer.m_sceneMgr.CreateMeshSceneNodeBF(priority,
                                    parentSceneNodeName,
                                    m_ent,
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
                catch (Exception e) {
                    m_renderer.m_log.Log(LogLevel.DBADERROR, "Render: Failed conversion of {0}: {1}", m_ent.Name.Name, e.ToString());
                }
            }
            else {
                // the entity already has a scene node. We're just forcing the rebuild of the prim
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "DoRenderLater: entity has scenenode. Rebuilding mesh: {0}", entMeshName);
                m_renderer.RequestMesh(m_ent.Name, entMeshName.Name);
            }
            return true;
        }
    }

    /// <summary>
    /// Update the state of the prim.
    /// </summary>
    /// <param name="what">UpdateCodes for what is being updated</param>
    /// <param name="fullUpdate">'true' if a full update (rebuild) has already been done
    ///    (usually a new prim). This means this routine is just decorating the prim and doesn't
    ///    need to rebuild the whole prim if that is necessary.</param>
    public void Update(UpdateCodes what) {
        float priority = m_renderer.CalculateInterestOrder(m_ent);
        bool fullUpdate = false;    // true if a full update was done on this entity
        lock (m_ent) {
            if ((what & UpdateCodes.New) != 0) {
                // new entity. Gets the full treatment
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: New entity: {0}", m_ent.Name.Name);
                m_renderer.DoRenderQueued(m_ent);
                fullUpdate = true;
            }
            if ((what & UpdateCodes.New) == 0) {
                // if not a new update, see what in particular is changing for this prim
                if ((what & UpdateCodes.ParentID) != 0) {
                    // prim was detached or attached. Rerender if not the first update
                    m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: parentID changed");
                    if (!fullUpdate) m_renderer.DoRenderQueued(m_ent);
                    fullUpdate = true;
                }
                if ((what & UpdateCodes.Material) != 0) {
                    // the materials have changed on this entity. Cause materials to be recalcuated
                    m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: Material changed");
                }
                if ((what & (UpdateCodes.PrimFlags | UpdateCodes.PrimData)) != 0) {
                    // the prim parameters were changed. Re-render if this is not the new creation request
                    m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: prim data changed");
                    if (!fullUpdate) m_renderer.DoRenderQueued(m_ent);
                    fullUpdate = true;
                }
                if ((what & UpdateCodes.Textures) != 0) {
                    // texure on the prim were updated. Refresh them if not the initial creation update
                    m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: textures changed");
                    // to get the textures to refresh, we must force the situation
                    IWorldRenderConv conver;
                    if (m_ent.TryGet<IWorldRenderConv>(out conver)) {
                        conver.RebuildEntityMaterials(priority, m_ent);
                        Ogr.RefreshResourceBF(priority, Ogr.ResourceTypeMesh,
                                    EntityNameOgre.ConvertToOgreMeshName(m_ent.Name).Name);
                    }
                }
            }
            if ((what & UpdateCodes.Animation) != 0) {
                // the prim has changed its rotation animation
                IAnimation anim;
                if (m_ent.TryGet<IAnimation>(out anim)) {
                    IWorldRenderConv conver;
                    // Hopefully the interface with the details is attached
                    if (m_ent.TryGet<IWorldRenderConv>(out conver)) {
                        m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: update animation");
                        // since the entity might not have been rendererd yet, we need to queue this operstion
                        Object[] parms = { 0f, m_ent, conver, anim };
                        m_renderer.m_workQueueRender.DoLater(priority, DoUpdateAnimationLater, parms);

                    }
                    else {
                        m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: animation bit without animation details");
                    }
                }
            }
            if ((what & UpdateCodes.Text) != 0) {
                // text associated with the prim changed
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: text changed");
            }
            if ((what & UpdateCodes.Particles) != 0) {
                // particles associated with the prim changed
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: particles changed");
            }
            if (!fullUpdate && (what & (UpdateCodes.Scale | UpdateCodes.Position | UpdateCodes.Rotation)) != 0) {
                // world position has changed. Tell Ogre they have changed
                string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
                m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderPrim.Update: Updating position/rotation for {0}", entitySceneNodeName);
                Ogr.UpdateSceneNodeBF(priority, entitySceneNodeName,
                    ((what & UpdateCodes.Position) != 0),
                    m_ent.RegionPosition.X, m_ent.RegionPosition.Y, m_ent.RegionPosition.Z, 2f,
                    false, 1f, 1f, 1f, 2f, // don't pass scale yet
                    ((what & UpdateCodes.Rotation) != 0),
                    m_ent.Heading.W, m_ent.Heading.X, m_ent.Heading.Y, m_ent.Heading.Z, 2f);
            }
        }
    }

    /// <summary>
    /// Current animations are for TargetOmega which causes rotation in the client.
    /// </summary>
    /// <param name="qInstance"></param>
    /// <param name="parms"></param>
    /// <returns>true if we can do this update now. False if to retry</returns>
    private bool DoUpdateAnimationLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        float prio = (float)loadParams[0];
        IEntity m_ent = (IEntity)loadParams[1];
        IWorldRenderConv m_conver = (IWorldRenderConv)loadParams[2];
        IAnimation m_anim = (IAnimation)loadParams[3];

        string sceneNodeName = RendererOgre.GetSceneNodeName(m_ent);
        if (sceneNodeName == null) {
            // prim does not yet have a scene node. Try again later.
            return false;
        }
        return m_conver.UpdateAnimation(0, m_ent, sceneNodeName, m_anim);
    }
}
}
