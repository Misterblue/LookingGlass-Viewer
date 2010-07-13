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
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Renderer;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;

namespace LookingGlass.Renderer.Ogr {

public class RendererOgreLL : IWorldRenderConv {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    private float m_sceneMagnification = 1.0f;
    bool m_buildMaterialsAtMeshCreationTime = false;
    bool m_buildMaterialsAtRenderInfoTime = true;

    // we've added things to the interface for scuplties. Need to push back into OMV someday.
    // private OMVR.IRendering m_meshMaker = null;
    private Mesher.MeshmerizerR m_meshMaker = null;
    // set to true if the generation of the mesh includes the scale factors
    private bool m_useRendererMeshScaling;
    private bool m_useRendererTextureScaling;

    private string m_defaultAvatarMesh;

    static RendererOgreLL m_instance = null;
    public static RendererOgreLL Instance {
        get {
            if (m_instance == null) {
                m_instance = new RendererOgreLL();
            }
            return m_instance;
        }
    }

    public RendererOgreLL() {
        if (m_meshMaker == null) {
            Renderer.Mesher.MeshmerizerR amesher = new Renderer.Mesher.MeshmerizerR();
            // There is two ways to do scaling: in the mesh or in Ogre. We choose the latter here
            // so we can create shared vertices for the standard shapes (the cubes  that are everywhere)
            // this causes the mesherizer to not scale the node coordinates by the prim scaling factor
            // Update: scaling with Ogre has proved problematic: the scaling effects the mesh and
            // the position coordinates around the object. This is a problem for child nodes.
            // It also effects the texture mapping so texture scaling factors would have to be
            // scaled by the scale of teh face that they appear on. Ugh.
            // For the moment, turned off while I figure that stuff out.
            // m_useRendererMeshScaling = true; // use Ogre to scale the mesh
            m_useRendererMeshScaling = false; // scale the mesh in the meshmerizer
            amesher.ShouldScaleMesh = !m_useRendererMeshScaling;
            m_useRendererTextureScaling = false; // use software texture face scaling
            // m_useRendererTextureScaling = true; // use Ogre texture scaling rather than computing it
            m_meshMaker = amesher;
        }

        // magnification of passed World coordinates into Ogre coordinates
        // NOTE: scene magnification is depricated and probably doesn't work. Leave at 1.0.
        m_sceneMagnification = LookingGlassBase.Instance.AppParams.ParamFloat("Renderer.Ogre.LL.SceneMagnification");
        // true if to creat materials while we are creating the mesh
        m_buildMaterialsAtMeshCreationTime = LookingGlassBase.Instance.AppParams.ParamBool("Renderer.Ogre.LL.EarlyMaterialCreate");
        // true if to create materials while building renderinfo structure
        m_buildMaterialsAtRenderInfoTime = LookingGlassBase.Instance.AppParams.ParamBool("Renderer.Ogre.LL.RenderInfoMaterialCreate");
        // resource name of the mesh to use for an avatar
        m_defaultAvatarMesh = LookingGlassBase.Instance.AppParams.ParamString("Renderer.Ogre.LL.DefaultAvatarMesh");
    }

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
    /// <param name="sceneMgr"></param>
    /// <param name="ent"></param>
    /// <param name="callCount">zero if do nothing, otherwise the number of times that
    /// this RenderingInfo has been asked for</param>
    /// <returns>rendering info or null if we cannot collect all data</returns>
    public RenderableInfo RenderingInfo(float priority, Object sceneMgr, IEntity ent, int callCount) {
        LLEntityBase llent;
        LLRegionContext rcontext;
        OMV.Primitive prim;
        // true if we should do the scaling with the rendering parameters
        bool shouldHaveRendererScale = m_useRendererMeshScaling;

        try {
            llent = (LLEntityBase)ent;
            rcontext = (LLRegionContext)llent.RegionContext;
            prim = llent.Prim;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderingInfoLL: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        RenderableInfo ri = new RenderableInfo();
        if (prim == null) 
            throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null");

        EntityName newMeshName = EntityNameOgre.ConvertToOgreMeshName(ent.Name);
        ri.basicObject = newMeshName;   // pass the name of the mesh that should be created

        // if a standard type (done by Ogre), let the rendering system do the scaling
        int meshType = 0;
        int meshFaces = 0;
        if (CheckStandardMeshType(prim, out meshType, out meshFaces)) {
            // if a standard mesh type, use Ogre scaling so we can reuse base shapes
            shouldHaveRendererScale = true;
        }

        // if the prim has a parent, we must hang this scene node off the parent's scene node
        if (prim.ParentID != 0) {
            if (ent.ContainingEntity == null) {
                // NOTE: in theory, the parent container has been resolved before we get here
                // but it is a legacy feature that the comm system does not hold entities
                // that don't have their parent so it's possible to get here and find the
                // parent entity does not exist. If this is the case, we return 'null' saying
                // we cannot yet build this entity.
                IEntity parentEntity = null;
                rcontext.TryGetEntityLocalID(prim.ParentID, out parentEntity);
                if (parentEntity != null) {
                    ent.ContainingEntity = parentEntity;
                    parentEntity.AddEntityToContainer(ent);
                }
                else {
                    // we can't find the parent. Can't build render info.
                    // if we've been waiting for that parent, ask for same
                    if ((callCount != 0) && ((callCount % 3) == 0)) {
                        rcontext.RequestLocalID(prim.ParentID);
                    }
                    return null;
                }
            }
            ri.parentEntity = ent.ContainingEntity;
        }
        
        ri.rotation = prim.Rotation;
        ri.position = prim.Position;

        // If the mesh was scaled just pass the renderer a scale of one
        // otherwise, if the mesh was not scaled, have the renderer do the scaling
        // This specifies what we want the renderer to do
        if (shouldHaveRendererScale) {
            ri.scale = prim.Scale * m_sceneMagnification;
        }
        else {
            ri.scale = new OMV.Vector3(m_sceneMagnification, m_sceneMagnification, m_sceneMagnification);
        }

        // Compute a unique hash code for this shape.
        ri.shapeHash = GetMeshKey(prim, prim.Scale, 0);

        // while we're in the neighborhood, we can create the materials
        if (m_buildMaterialsAtRenderInfoTime) {
            CreateMaterialResource7X(priority, ent, prim, 7);
        }
        return ri;
    }

    /// <summary>
    /// Create a mesh in the renderer.
    /// </summary>
    /// <param name="sMgr">the scene manager receiving  the mesh</param>
    /// <param name="ent">The entity the mesh is coming from</param>
    /// <param name="meshName">The name the mesh should take</param>
    public bool CreateMeshResource(float priority, IEntity ent, string meshName, EntityName contextEntity) {
        LLEntityBase llent;
        OMV.Primitive prim;

        try {
            // this will change when we are checking for avatars and other types
            llent = (LLEntityBase)ent;
            prim = llent.Prim;
            if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null");
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMeshResource: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        int meshType = 0;
        int meshFaces = 0;
        // TODO: This is the start of come code to specialize the creation of standard
        // meshes. Since a large number of prims are cubes, they can share the face vertices
        // and thus reduce the total number of Ogre's vertices stored.
        // At the moment, CheckStandardMeshType returns false so we don't do anything special yet
        lock (ent) {
            if (CheckStandardMeshType(prim, out meshType, out meshFaces)) {
                m_log.Log(LogLevel.DBADERROR, "CreateMeshResource: not implemented Standard Type");
                /*
                // while we're in the neighborhood, we can create the materials
                if (m_buildMaterialsAtMeshCreationTime) {
                    for (int j = 0; j < meshFaces; j++) {
                        CreateMaterialResource2(ent, prim, EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, j), j);
                    }
                }

                Ogr.CreateStandardMeshResource(meshName, meshType);
                 */
            }
            else {
                OMVR.FacetedMesh mesh;
                try {

                    if (prim.Sculpt != null) {
                        // looks like it's a sculpty. Do it that way
                        EntityNameLL textureEnt = EntityNameLL.ConvertTextureWorldIDToEntityName(ent.AssetContext, prim.Sculpt.SculptTexture);
                        System.Drawing.Bitmap textureBitmap = ent.AssetContext.GetTexture(textureEnt);
                        if (textureBitmap == null) {
                            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMeshResource: waiting for texture for sculpty {0}", ent.Name.Name);
                            // Don't have the texture now so ask for the texture to be loaded.
                            // Note that we ignore the callback and let the work queue requeing get us back here
                            ent.AssetContext.DoTextureLoad(textureEnt, AssetContextBase.AssetType.SculptieTexture, 
                                delegate(string name, bool trans) { return; });
                            // This will cause the work queue to requeue the mesh creation and call us
                            //   back later to retry creating the mesh
                            return false;
                        }
                        m_log.Log(LogLevel.DRENDERDETAIL, "CreateMeshResource: mesherizing scuplty {0}", ent.Name.Name);
                        // mesh = m_meshMaker.GenerateSculptMesh(textureBitmap, prim, OMVR.DetailLevel.Highest);
                        mesh = m_meshMaker.GenerateSculptMesh(textureBitmap, prim, OMVR.DetailLevel.Medium);
                        // mesh = m_meshMaker.GenerateSculptMesh(textureBitmap, prim, OMVR.DetailLevel.Low);
                        if (mesh.Faces.Count > 10) {
                            m_log.Log(LogLevel.DBADERROR, "CreateMeshResource: mesh has {0} faces!!!!", mesh.Faces.Count);
                        }
                        textureBitmap.Dispose();
                    }
                    else {
                        // we really should use Low for boxes, med for most things and high for megaprim curves
                        // OMVR.DetailLevel meshDetail = OMVR.DetailLevel.High;
                        OMVR.DetailLevel meshDetail = OMVR.DetailLevel.Medium;
                        if (prim.Type == OMV.PrimType.Box) {
                            meshDetail = OMVR.DetailLevel.Low;
                            // m_log.Log(LogLevel.DRENDERDETAIL, "CreateMeshResource: Low detail for {0}", ent.Name.Name);
                        }
                        mesh = m_meshMaker.GenerateFacetedMesh(prim, meshDetail);
                        if (mesh.Faces.Count > 10) {
                            m_log.Log(LogLevel.DBADERROR, "CreateMeshResource: mesh has {0} faces!!!!", mesh.Faces.Count);
                        }
                    }
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DRENDERDETAIL, "CreateMeshResource: failed mesh generate for {0}: {1}", 
                        ent.Name.Name, e.ToString());
                    throw e;
                }

                // we have the face data. We package this up into a few big arrays to pass them
                //   to the real renderer.

                // we pass two one-dimensional arrays of floating point numbers over to the
                // unmanaged code. The first array contains:
                //   faceCounts[0] = total number of int's in this array (for alloc and freeing in Ogre)
                //   faceCounts[1] = number of faces
                //   faceCounts[2] = offset in second array for beginning of vertex info for face 1
                //   faceCounts[3] = number of vertices for face 1
                //   faceCounts[4] = stride for vertex info for face 1 (= 8)
                //   faceCounts[5] = offset in second array for beginning of indices info for face 1
                //   faceCounts[6] = number of indices for face 1
                //   faceCounts[7] = stride for indices (= 3)
                //   faceCounts[8] = offset in second array for beginning of vertex info for face 2
                //   faceCounts[9] = number of vertices for face 2
                //   faceCounts[10] = stride for vertex info for face 2 (= 8)
                //   etc
                // The second array starts with the vertex color:
                //   v.R, v.G, v.B, v.A
                // which is followed by the vertex info in the order:
                //   v.X, v.Y, v.Z, u.X, u.Y, n.X, n.Y, n.Z
                // this is repeated for each vertex
                // This is followed by the list of indices listed as i.X, i.Y, i.Z
                
                const int faceCountsStride = 6;
                const int verticesStride = 8;
                const int indicesStride = 3;
                const int vertexColorStride = 4;
                // calculate how many floating point numbers we're pushing over
                int[] faceCounts = new int[mesh.Faces.Count * faceCountsStride + 2];
                faceCounts[0] = faceCounts.Length;
                faceCounts[1] = mesh.Faces.Count;
                int totalVertices = 0;
                for (int j = 0; j < mesh.Faces.Count; j++) {
                    OMVR.Face face = mesh.Faces[j];
                    int faceBase = j * faceCountsStride + 2;
                    // m_log.Log(LogLevel.DRENDERDETAIL, "Mesh F" + j.ToString() + ":"
                    //     + " vcnt=" + face.Vertices.Count.ToString()
                    //     + " icnt=" + face.Indices.Count.ToString());
                    faceCounts[faceBase + 0] = totalVertices;
                    faceCounts[faceBase + 1] = face.Vertices.Count;
                    faceCounts[faceBase + 2] = verticesStride;
                    totalVertices += vertexColorStride + face.Vertices.Count * verticesStride;
                    faceCounts[faceBase + 3] = totalVertices;
                    faceCounts[faceBase + 4] = face.Indices.Count;
                    faceCounts[faceBase + 5] = indicesStride;
                    totalVertices += face.Indices.Count;
                }

                float[] faceVertices = new float[totalVertices+2];
                faceVertices[0] = faceVertices.Length;
                int vertI = 1;
                for (int j = 0; j < mesh.Faces.Count; j++) {
                    OMVR.Face face = mesh.Faces[j];

                    // Texture transform for this face
                    OMV.Primitive.TextureEntryFace teFace = face.TextureFace;
                    try {
                        if ((teFace != null) && !m_useRendererTextureScaling) {
                            m_meshMaker.TransformTexCoords(face.Vertices, face.Center, teFace);
                        }
                    }
                    catch {
                        m_log.Log(LogLevel.DBADERROR, "RenderOgreLL.CreateMeshResource:"
                            + " more faces in mesh than in prim:"
                            + " ent=" + ent.Name
                            + ", face=" + j.ToString()
                        );
                    }

                    // Vertices color for this face
                    OMV.Primitive.TextureEntryFace tef = prim.Textures.GetFace((uint)j);
                    if (tef != null) {
                        faceVertices[vertI + 0] = tef.RGBA.R;
                        faceVertices[vertI + 1] = tef.RGBA.G;
                        faceVertices[vertI + 2] = tef.RGBA.B;
                        faceVertices[vertI + 3] = tef.RGBA.A;
                    }
                    else {
                        faceVertices[vertI + 0] = 1f;
                        faceVertices[vertI + 1] = 1f;
                        faceVertices[vertI + 2] = 1f;
                        faceVertices[vertI + 3] = 1f;
                    }
                    vertI += vertexColorStride;
                    // Vertices for this face
                    for (int k = 0; k < face.Vertices.Count; k++) {
                        OMVR.Vertex thisVert = face.Vertices[k];
                        // m_log.Log(LogLevel.DRENDERDETAIL, "CreateMesh: vertices: p={0}, t={1}, n={2}",
                        //     thisVert.Position.ToString(), thisVert.TexCoord.ToString(), thisVert.Normal.ToString());
                        faceVertices[vertI + 0] = thisVert.Position.X;
                        faceVertices[vertI + 1] = thisVert.Position.Y;
                        faceVertices[vertI + 2] = thisVert.Position.Z;
                        faceVertices[vertI + 3] = thisVert.TexCoord.X;
                        faceVertices[vertI + 4] = thisVert.TexCoord.Y;
                        faceVertices[vertI + 5] = thisVert.Normal.X;
                        faceVertices[vertI + 6] = thisVert.Normal.Y;
                        faceVertices[vertI + 7] = thisVert.Normal.Z;
                        vertI += verticesStride;
                    }
                    for (int k = 0; k < face.Indices.Count; k += 3) {
                        faceVertices[vertI + 0] = face.Indices[k + 0];
                        faceVertices[vertI + 1] = face.Indices[k + 1];
                        faceVertices[vertI + 2] = face.Indices[k + 2];
                        vertI += indicesStride;
                    }
                }

                // while we're in the neighborhood, we can create the materials
                if (m_buildMaterialsAtMeshCreationTime) {
                    CreateMaterialResource7X(priority, ent, prim, mesh.Faces.Count);
                }

                // We were passed a 'context' entity. Create a scene node name to pass to
                // Ogre. If the scene node is not found, nothing bad happens.
                string contextSceneNode = EntityNameOgre.ConvertToOgreSceneNodeName(contextEntity);

                m_log.Log(LogLevel.DRENDERDETAIL, 
                    "RenderOgreLL: {0}, f={1}, fcs={2}, fs={3}",
                    ent.Name, mesh.Faces.Count, faceCounts.Length, faceVertices.Length
                    );
                // Now create the mesh
                Ogr.CreateMeshResourceBF(priority, meshName, contextSceneNode, faceCounts, faceVertices);
            }
        }
        return true;
    }

    /// <summary>
    /// Create a mesh for the avatar. We either use the specified avatar mesh or create the
    /// mesh from the LAD definition file.
    /// </summary>
    public bool CreateAvatarMeshResource(float priority, IEntity ent, string meshName, EntityName contextEntity) {
        if (m_defaultAvatarMesh != null && m_defaultAvatarMesh.Length > 0) {
            return CreateAvatarMeshFromDefault(priority, ent, meshName, contextEntity);
        }

        string meshInfoDir = LookingGlassBase.Instance.AppParams.ParamString("Renderer.Avatar.Mesh.InfoDir");
        string meshInfoDefn = LookingGlassBase.Instance.AppParams.ParamString("Renderer.Avatar.Mesh.Description");
        string meshDescriptionDir = LookingGlassBase.Instance.AppParams.ParamString("Renderer.Avatar.Mesh.DescriptionDir");

        Dictionary<string, OMVR.LindenMesh> meshTypes = new Dictionary<string, OMVR.LindenMesh>();


        XmlDocument lad = new XmlDocument();
        lad.Load(Path.Combine(meshInfoDir, meshInfoDefn));

        XmlNodeList meshes = lad.GetElementsByTagName("mesh");
        foreach (XmlNode meshNode in meshes) {
            string type = meshNode.Attributes.GetNamedItem("type").Value;
            int lod = Int32.Parse(meshNode.Attributes.GetNamedItem("lod").Value);
            string fileName = meshNode.Attributes.GetNamedItem("file_name").Value;
            //string minPixelWidth = meshNode.Attributes.GetNamedItem("min_pixel_width").Value;

            // for the moment, ignore the skirt
            if (type == "skirtMesh") continue;

            if (lod == 0) {
                // only collect the meshes with the highest resolution
                try {
                    fileName = Path.Combine(meshDescriptionDir, fileName);
                    if (!meshTypes.ContainsKey(type)) {
                        OMVR.LindenMesh lmesh = new OMVR.LindenMesh(type);
                        lmesh.LoadMesh(fileName);
                        meshTypes.Add(type, lmesh);
                    }
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "Failure reading avatar defn file {0}: {1}", fileName, e);
                }
            }
        }
        // meshTypes now contains the pieces of the avatar.

        string[] meshOrder = {
                    "headMesh", "upperBodyMesh", "lowerBodyMesh",
                    "eyeBallLeftMesh", "hairMesh", "skirtMesh", 
                    "eyelashMesh", "eyeBallRightMesh"
        };

        // Assemble into one mesh for passing to lower system
        // TODO: get and pass the skeleton information
        if (!CreateAvatarTextures(ent, false)) {
            // something about the textures can't be built yet. Try again later.
            return false;
        }

        const int faceCountsStride = 6;
        const int verticesStride = 8;
        const int indicesStride = 3;
        const int vertexColorStride = 4;
        // calculate how many floating point numbers we're pushing over
        int[] faceCounts = new int[meshTypes.Count * faceCountsStride + 2];
        faceCounts[0] = faceCounts.Length;
        faceCounts[1] = meshTypes.Count;
        int totalVertices = 0;

        try {
            for (int jj=0; jj < meshTypes.Count; jj++) {
                if (!meshTypes.ContainsKey(meshOrder[jj])) continue;
                OMVR.LindenMesh lmesh = meshTypes[meshOrder[jj]];
                int faceBase = jj * faceCountsStride + 2;
                faceCounts[faceBase + 0] = totalVertices;
                faceCounts[faceBase + 1] = lmesh.NumVertices;
                faceCounts[faceBase + 2] = verticesStride;
                totalVertices += vertexColorStride + lmesh.NumVertices * verticesStride;
                faceCounts[faceBase + 3] = totalVertices;
                faceCounts[faceBase + 4] = lmesh.NumFaces * indicesStride;
                faceCounts[faceBase + 5] = indicesStride;
                totalVertices += lmesh.NumFaces * indicesStride;
            }

            float[] faceVertices = new float[totalVertices + 2];
            faceVertices[0] = faceVertices.Length;
            int vertI = 1;
            for (int jj=0; jj < meshTypes.Count; jj++) {
                if (!meshTypes.ContainsKey(meshOrder[jj])) continue;
                OMVR.LindenMesh lmesh = meshTypes[meshOrder[jj]];
                faceVertices[vertI + 0] = 0.6f;
                faceVertices[vertI + 1] = 0.6f;
                faceVertices[vertI + 2] = 0.6f;
                faceVertices[vertI + 3] = 0.5f;
                vertI += vertexColorStride;

                for (int k = 0; k < lmesh.NumVertices; k++) {
                    OMVR.LindenMesh.Vertex thisVert = lmesh.Vertices[k];
                    // m_log.Log(LogLevel.DRENDERDETAIL, "CreateMesh: vertices: p={0}, t={1}, n={2}",
                    //     thisVert.Position.ToString(), thisVert.TexCoord.ToString(), thisVert.Normal.ToString());
                    faceVertices[vertI + 0] = thisVert.Coord.X;
                    faceVertices[vertI + 1] = thisVert.Coord.Y;
                    faceVertices[vertI + 2] = thisVert.Coord.Z;
                    faceVertices[vertI + 3] = thisVert.TexCoord.X;
                    faceVertices[vertI + 4] = thisVert.TexCoord.Y;
                    faceVertices[vertI + 5] = thisVert.Normal.X;
                    faceVertices[vertI + 6] = thisVert.Normal.Y;
                    faceVertices[vertI + 7] = thisVert.Normal.Z;
                    vertI += verticesStride;
                }
                for (int k = 0; k < lmesh.NumFaces; k++) {
                    faceVertices[vertI + 0] = lmesh.Faces[k].Indices[0];
                    faceVertices[vertI + 1] = lmesh.Faces[k].Indices[1];
                    faceVertices[vertI + 2] = lmesh.Faces[k].Indices[2];
                    vertI += indicesStride;
                }
            }
            m_log.Log(LogLevel.DRENDERDETAIL, 
                "RenderOgreLL.CreateAvatarMeshResource: {0}, fcs={1}, fs={2}, vi={3}",
                ent.Name, faceCounts.Length, faceVertices.Length, vertI
                );

            // We were passed a 'context' entity. Create a scene node name to pass to
            // Ogre. If the scene node is not found, nothing bad happens.
            string contextSceneNode = EntityNameOgre.ConvertToOgreSceneNodeName(contextEntity);

            // Now create the mesh
            Ogr.CreateMeshResourceBF(priority, meshName, contextSceneNode, faceCounts, faceVertices);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failure building avatar mesh: {0}", e);
        }

        return true;
    }

    /// <summary>
    /// Create the textures for the avatar. If this is an LLEntityAvatar, extract the
    /// texture info from the avatar description and blindly create textures for the
    /// parts of the body in the order of the faces that were generated above.
    /// </summary>
    /// <param name="ent">The entity of the avatar being decorated</param>
    /// <param name="forceUpdateFlag">'true' if to force a redraw. If doing the initial
    /// creation, a redraw update is not necessary.</param>
    public bool CreateAvatarTextures(IEntity ent, bool forceUpdateFlag) {
        IEntityAvatar ientav;
        LLEntityAvatar entav;
        if (ent.TryGet<IEntityAvatar>(out ientav)) {
            if (ientav is LLEntityAvatar) {
                entav = (LLEntityAvatar)ientav;
                OMV.Avatar av = entav.Avatar;
                if (av != null && av.Textures != null) {
                    OMV.Primitive.TextureEntry texEnt = av.Textures;
                    OMV.Primitive.TextureEntryFace[] texFaces = texEnt.FaceTextures;

                    const int genCount = 7;
                    float[] textureParams = new float[1 + ((int)Ogr.CreateMaterialParam.maxParam) * genCount];
                    string[] materialNames = new string[genCount];
                    string[] textureOgreNames = new string[genCount];
                    textureParams[0] = (float)Ogr.CreateMaterialParam.maxParam;

                    int[] texIndexes = {
                        (int)OMV.AvatarTextureIndex.HeadBaked,
                        (int)OMV.AvatarTextureIndex.UpperBaked,
                        (int)OMV.AvatarTextureIndex.LowerBaked,
                        (int)OMV.AvatarTextureIndex.EyesBaked,
                        (int)OMV.AvatarTextureIndex.HairBaked,
                        // (int)OMV.AvatarTextureIndex.SkirtBaked
                    };

                    int pBase = 1;
                    int jj = 0;
                    string textureOgreName;
                    foreach (int baker in texIndexes) {
                        CreateMaterialParameters(texFaces[baker],
                            ent, null, pBase, ref textureParams, jj, out textureOgreName);
                        materialNames[jj] = EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, jj);
                        textureOgreNames[jj] = textureOgreName;
                        // m_log.Log(LogLevel.DRENDERDETAIL, "CreateAvatarTextures: mat={0}, tex={1}",
                        //             materialNames[jj], textureOgreName);
                        
                        /*
                        // The textures for the baked avatar textures are processed specially
                        // Here we request they be loaded (if not already available) so we can specify their type
                        EntityNameOgre textureEnt = EntityNameOgre.ConvertOgreResourceToEntityName(textureOgreName);
                        System.Drawing.Bitmap textureBitmap = ent.AssetContext.GetTexture(textureEnt);
                        if (textureBitmap == null) {
                            // texture is not immediately available. Ask for it in a special way
                            ent.AssetContext.DoTextureLoad(textureEnt, AssetContextBase.AssetType.BakedTexture,
                                delegate(string name, bool trans) { return; });
                        }
                         */

                        pBase += (int)textureParams[0];
                        jj++;
                    }

                    m_log.Log(LogLevel.DRENDERDETAIL, "CreateAvatarTextures: materials for {0}", ent.Name);
                    Ogr.CreateMaterialResource7BF(0f, materialNames[0],
                        materialNames[0], materialNames[1], materialNames[2], materialNames[3],
                        materialNames[4], materialNames[5], materialNames[6],
                        textureOgreNames[0], textureOgreNames[1], textureOgreNames[2], textureOgreNames[3],
                        textureOgreNames[4], textureOgreNames[5], textureOgreNames[6],
                        textureParams
                    );
                }
                else {
                    // the avatar is not initialized yet. Try again later.
                    return false;
                }
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "CreateAvatarTexture: REQUEST BUT NOT LLAVATAR");
            }
        }
        else {
            string modNames = "";
            foreach (string mod in ent.ModuleInterfaceTypeNames()) modNames += " " + mod;
            m_log.Log(LogLevel.DBADERROR, "CreateAvatarTexture: REQUEST FOR TEXTURES FOR NON LL ENTITY. Mod={0}", modNames);
        }
        return true;
    }

    public bool CreateAvatarMeshFromDefault(float priority, IEntity ent, string meshName, EntityName contextEntity) {
        m_log.Log(LogLevel.DBADERROR, "CreateAvatarMeshFromDefault: NOT IMPLEMENTED!!");
        return true;
    }

    // Examine the prim and see if it's a standard shape that we can pass to Ogre to implement
    // in a standard way. This is most useful for cubes which don't change and are just 
    // scaled along their dimensions.
    private bool CheckStandardMeshType(OMV.Primitive prim, out int meshType, out int meshFaces) {
        meshType = 0;
        meshFaces = 0;
        return false;
    }

    public void CreateMaterialResource(float priority, IEntity ent, string materialName) {
        LLEntityPhysical llent;
        OMV.Primitive prim;

        try {
            llent = (LLEntityPhysical)ent;
            prim = llent.Prim;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null 2");

        int faceNum = EntityNameOgre.GetFaceFromOgreMaterialNameX(materialName);
        if (faceNum < 0) {
            // no face was found in the material name
            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource: no face number for " + materialName);
            return;
        }
        CreateMaterialResource2(priority, ent, prim, materialName, faceNum);
    }

    /// <summary>
    /// Create a material resource in Ogre. This is the new way done by passing an
    /// array of parameters. Bool values are 0f for false and true otherwise.
    /// The offsets in the passed parameter array is defined with the interface in
    /// LookingGlass.Renderer.Ogre.Ogr.
    /// </summary>
    /// <param name="ent">the entity of the underlying prim</param>
    /// <param name="prim">the OMV.Primitive that is getting the material</param>
    /// <param name="materialName">the name to give the new material</param>
    /// <param name="faceNum">the index of the primitive face getting the material</param>
    private void CreateMaterialResource2(float priority, IEntity ent, OMV.Primitive prim, 
                            string materialName, int faceNum) {
        float[] textureParams = new float[(int)Ogr.CreateMaterialParam.maxParam];
        string textureOgreResourceName = "";
        CreateMaterialParameters(ent, prim, 0, ref textureParams, faceNum, out textureOgreResourceName);
        m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource2: m=" + materialName + ",o=" + textureOgreResourceName);
        Ogr.CreateMaterialResource2BF(priority, materialName, textureOgreResourceName, textureParams);
    }

    private void CreateMaterialParameters(IEntity ent, OMV.Primitive prim, int pBase, ref float[] textureParams,
                    int faceNum, out String texName) {
        OMV.Primitive.TextureEntryFace textureFace = prim.Textures.GetFace((uint)faceNum);
        CreateMaterialParameters(textureFace, ent, prim, pBase, ref textureParams, faceNum, out texName);
        return;
    }

    private void CreateMaterialParameters(OMV.Primitive.TextureEntryFace textureFace, 
                    IEntity ent, OMV.Primitive prim, int pBase, ref float[] textureParams, 
                    int faceNum, out String texName) {
        OMV.UUID textureID = OMV.Primitive.TextureEntry.WHITE_TEXTURE;
        if (textureFace != null) {
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorR] = textureFace.RGBA.R;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorG] = textureFace.RGBA.G;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorB] = textureFace.RGBA.B;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorA] = textureFace.RGBA.A;
            if (m_useRendererTextureScaling) {
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleU] = 1f / textureFace.RepeatU;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleV] = 1f / textureFace.RepeatV;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollU] = textureFace.OffsetU;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollV] = -textureFace.OffsetV;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.rotate] = textureFace.Rotation;
            }
            else {
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleU] = 1.0f;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleV] = 1.0f;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollU] = 1.0f;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollV] = 1.0f;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.rotate] = 0.0f;
            }
            textureParams[pBase + (int)Ogr.CreateMaterialParam.glow] = textureFace.Glow;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.bump] = (float)textureFace.Bump;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.shiny] = (float)textureFace.Shiny;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.fullBright] = textureFace.Fullbright ? 1f : 0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.mappingType] = (float)textureFace.TexMapType;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.mediaFlags] = textureFace.MediaFlags ? 1f : 0f;

            textureParams[pBase + (int)Ogr.CreateMaterialParam.animationFlag] = 0f;
            if (prim != null && (prim.TextureAnim.Face == faceNum || (int)prim.TextureAnim.Face == 255)
                // && ((prim.TextureAnim.Flags & OpenMetaverse.Primitive.TextureAnimMode.ANIM_ON) != 0)) {
                        && ((prim.TextureAnim.Flags != 0))) {
                m_log.Log(LogLevel.DRENDERDETAIL, "Adding animation for material texture");
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animationFlag] = (float)prim.TextureAnim.Flags;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animSizeX] = (float)prim.TextureAnim.SizeX;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animSizeY] = (float)prim.TextureAnim.SizeY;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animStart] = prim.TextureAnim.Start;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animRate] = prim.TextureAnim.Rate;
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animLength] = prim.TextureAnim.Length;
            }
            else {
                textureParams[pBase + (int)Ogr.CreateMaterialParam.animationFlag] = 0f;
            }

            // since we can't calculate whether material is transparent or not (actually
            //   we don't have that information at this instant), assume color transparent
            if (textureFace.RGBA.A == 1.0) {
                // The vertex color doesn't have alpha. Assume the texture that goes on here does.
                // We do this because we don't have the texture at the moment
                textureParams[pBase + (int)Ogr.CreateMaterialParam.textureHasTransparent] = 2f;
            }
            else {
                // Pass the overall transparancy
                textureParams[pBase + (int)Ogr.CreateMaterialParam.textureHasTransparent] = textureFace.RGBA.A;
            }
            textureID = textureFace.TextureID;
        }
        else {
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorR] = 0.4f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorG] = 0.4f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorB] = 0.4f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.colorA] = 0.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleU] = 1.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.scaleV] = 1.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollU] = 1.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.scrollV] = 1.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.rotate] = 0.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.glow] = 0.0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.bump] = (float)OMV.Bumpiness.None;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.shiny] = (float)OMV.Shininess.None;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.fullBright] = 0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.mappingType] = 0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.mediaFlags] = 0f;
            textureParams[pBase + (int)Ogr.CreateMaterialParam.textureHasTransparent] = 0f;
        }
        EntityName textureEntityName = new EntityName(ent, textureID.ToString());
        string textureOgreResourceName = "";
        if (textureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
            textureOgreResourceName = EntityNameOgre.ConvertToOgreNameX(textureEntityName, null);
        }
        texName = textureOgreResourceName;
    }

    public void RebuildEntityMaterials(float priority, IEntity ent) {
        LLEntityBase llent;
        LLRegionContext rcontext;
        OMV.Primitive prim;

        try {
            llent = (LLEntityBase)ent;
            rcontext = (LLRegionContext)llent.RegionContext;
            prim = llent.Prim;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderingInfoLL: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null 3");
        CreateMaterialResource7X(priority, llent, prim, 6);
    }

    /// <summary>
    /// Create the primary six materials for the prim
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="prim"></param>
    private void CreateMaterialResource6(float priority, IEntity ent, OMV.Primitive prim, int faces) {
        // we create the usual ones. extra faces will be asked for on demand
        for (int j = 0; j <= faces; j++) {
            CreateMaterialResource2(priority, ent, prim, EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, j), j);
        }
    }

    /// <summary>
    /// Create seven of the basic materials for this prim. This is passed to Ogre in one big lump
    /// to make things go a lot quicker.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="prim"></param>
    private void CreateMaterialResource7X(float prio, IEntity ent, OMV.Primitive prim, int faces) {
        // we create the usual ones. extra faces will be asked for on demand
        const int genCount = 7;
        float[] textureParams = new float[1 + ((int)Ogr.CreateMaterialParam.maxParam) * genCount];
        string[] materialNames = new string[genCount];
        string[] textureOgreNames = new string[genCount];

        textureParams[0] = (float)Ogr.CreateMaterialParam.maxParam;
        int pBase = 1;
        string textureOgreName;
        for (int j = 0; j < genCount; j++) {
            if (j >= faces) {
                // if no face here, say there is no face here
                materialNames[j] = null;
            }
            else {
                CreateMaterialParameters(ent, prim, pBase, ref textureParams, j, out textureOgreName);
                materialNames[j] = EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, j);
                textureOgreNames[j] = textureOgreName;
                pBase += (int)textureParams[0];
            }
        }
        m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource7X: materials for {0}", ent.Name);
        Ogr.CreateMaterialResource7BF(prio, materialNames[0],
            materialNames[0], materialNames[1], materialNames[2], materialNames[3], 
            materialNames[4], materialNames[5], materialNames[6],
            textureOgreNames[0], textureOgreNames[1], textureOgreNames[2], textureOgreNames[3], 
            textureOgreNames[4], textureOgreNames[5], textureOgreNames[6],
            textureParams
        );
    }

    // Called to animate something in the renderer.
    // The only animation so far is the rotation animation.
    // Return 'true' if animation set. false if not set and try again later.
    public bool UpdateAnimation(float prio, IEntity ent, string sceneNodeName, IAnimation anim) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Update animation for {0}: {1} at {2}", ent.Name, 
                            anim.StaticRotationAxis, anim.StaticRotationRotPerSec);
        if (anim.DoStaticRotation) {
            Ogr.UpdateAnimationBF(prio, sceneNodeName, 
                    anim.StaticRotationAxis.X, 
                    anim.StaticRotationAxis.Y,
                    anim.StaticRotationAxis.Z, 
                    anim.StaticRotationRotPerSec);
        }
        return true;
    }

    // Routine from OpenSim which creates a hash for the prim shape
    // TODO: Should this hash key include material information since Ogre applies
    // the material as part of the mesh?
    private ulong GetMeshKey(OMV.Primitive prim, OMV.Vector3 size, float lod)
    {
        // ulong hash = (ulong)prim.GetHashCode();
        ulong hash = 5381;

        hash = djb2(hash, (byte)prim.PrimData.PathCurve);
        hash = djb2(hash, prim.PrimData.ProfileHollow);
        hash = djb2(hash, (byte)prim.PrimData.ProfileHole);
        hash = djb2(hash, prim.PrimData.PathBegin);
        hash = djb2(hash, prim.PrimData.PathEnd);
        hash = djb2(hash, prim.PrimData.PathScaleX);
        hash = djb2(hash, prim.PrimData.PathScaleY);
        hash = djb2(hash, prim.PrimData.PathShearX);
        hash = djb2(hash, prim.PrimData.PathShearY);
        hash = djb2(hash, (byte)prim.PrimData.PathTwist);
        hash = djb2(hash, (byte)prim.PrimData.PathTwistBegin);
        hash = djb2(hash, (byte)prim.PrimData.PathRadiusOffset);
        hash = djb2(hash, (byte)prim.PrimData.PathTaperX);
        hash = djb2(hash, (byte)prim.PrimData.PathTaperY);
        hash = djb2(hash, prim.PrimData.PathRevolutions);
        hash = djb2(hash, (byte)prim.PrimData.PathSkew);
        hash = djb2(hash, prim.PrimData.ProfileBegin);
        hash = djb2(hash, prim.PrimData.ProfileEnd);
        hash = djb2(hash, prim.PrimData.ProfileHollow);

        // TODO: Separate scale out from the primitive shape data (after
        // scaling is supported at the physics engine level)
        // byte[] scaleBytes = size.GetBytes();
        // for (int i = 0; i < scaleBytes.Length; i++)
        //     hash = djb2(hash, scaleBytes[i]);
        hash = djb2(hash, size.X);
        hash = djb2(hash, size.Y);
        hash = djb2(hash, size.Z);

        // Include LOD in hash, accounting for endianness
        byte[] lodBytes = new byte[4];
        Buffer.BlockCopy(BitConverter.GetBytes(lod), 0, lodBytes, 0, 4);
        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(lodBytes, 0, 4);
        }
        for (int i = 0; i < lodBytes.Length; i++)
            hash = djb2(hash, lodBytes[i]);

        // include sculpt UUID
        if (prim.Sculpt != null) {
            byte[] scaleBytes = prim.Sculpt.SculptTexture.GetBytes();
            for (int i = 0; i < scaleBytes.Length; i++)
                hash = djb2(hash, scaleBytes[i]);
        }

        // since these are displayed meshes, we need to include the material
        // information
        if (prim.Textures != null) {
            for (uint ii = 0; ii < 7; ii++) {
                OMV.Primitive.TextureEntryFace texFace = prim.Textures.GetFace(ii);
                if (texFace != null) {
                    hash = djb2(hash, texFace.RGBA.R);
                    hash = djb2(hash, texFace.RGBA.G);
                    hash = djb2(hash, texFace.RGBA.B);
                    hash = djb2(hash, texFace.RGBA.A);
                    hash = djb2(hash, texFace.RepeatU);
                    hash = djb2(hash, texFace.RepeatV);
                    hash = djb2(hash, texFace.OffsetU);
                    hash = djb2(hash, texFace.OffsetV);
                    hash = djb2(hash, texFace.Rotation);
                    hash = djb2(hash, texFace.Glow);
                    hash = djb2(hash, (byte)texFace.Bump);
                    hash = djb2(hash, (byte)texFace.Shiny);
                    hash = djb2(hash, texFace.Fullbright ? 1.0f : 0.5f);
                    hash = djb2(hash, texFace.Glow);
                    byte[] texIDBytes = texFace.TextureID.GetBytes();
                    for (int jj = 0; jj < texIDBytes.Length; jj++) {
                        hash = djb2(hash, texIDBytes[jj]);
                    }
                }
            }
        }

        return hash;
    }

    private ulong djb2(ulong hash, byte c)
    {
        return ((hash << 5) + hash) + (ulong)c;
    }

    private ulong djb2(ulong hash, ushort c)
    {
        hash = ((hash << 5) + hash) + (ulong)((byte)c);
        return ((hash << 5) + hash) + (ulong)(c >> 8);
    }

    private ulong djb2(ulong hash, float c)
    {
        byte[] asBytes = BitConverter.GetBytes(c);
        hash = ((hash << 5) + hash) + (ulong)asBytes[0];
        hash = ((hash << 5) + hash) + (ulong)asBytes[1];
        hash = ((hash << 5) + hash) + (ulong)asBytes[2];
        return ((hash << 5) + hash) + (ulong)asBytes[3];
    }

    private void CalcAttachmentPoint(LLAttachment atch, out OMV.Vector3 pos, out OMV.Quaternion rot) {
        OMV.Vector3 posRet = OMV.Vector3.Zero;
        OMV.Quaternion rotRet = OMV.Quaternion.Identity;

        switch (atch.AttachmentPoint) {
            case OMV.AttachmentPoint.Default:
            case OMV.AttachmentPoint.Chest:
            case OMV.AttachmentPoint.Skull:
            case OMV.AttachmentPoint.LeftShoulder:
            case OMV.AttachmentPoint.RightShoulder:
            case OMV.AttachmentPoint.LeftHand:
            case OMV.AttachmentPoint.RightHand:
            case OMV.AttachmentPoint.LeftFoot:
            case OMV.AttachmentPoint.RightFoot:
            case OMV.AttachmentPoint.Spine:
            case OMV.AttachmentPoint.Pelvis:
            case OMV.AttachmentPoint.Mouth:
            case OMV.AttachmentPoint.Chin:
            case OMV.AttachmentPoint.LeftEar:
            case OMV.AttachmentPoint.RightEar:
            case OMV.AttachmentPoint.LeftEyeball:
            case OMV.AttachmentPoint.RightEyeball:
            case OMV.AttachmentPoint.Nose:
            case OMV.AttachmentPoint.RightUpperArm:
            case OMV.AttachmentPoint.RightForearm:
            case OMV.AttachmentPoint.LeftUpperArm:
            case OMV.AttachmentPoint.LeftForearm:
            case OMV.AttachmentPoint.RightHip:
            case OMV.AttachmentPoint.RightUpperLeg:
            case OMV.AttachmentPoint.RightLowerLeg:
            case OMV.AttachmentPoint.LeftHip:
            case OMV.AttachmentPoint.LeftUpperLeg:
            case OMV.AttachmentPoint.LeftLowerLeg:
            case OMV.AttachmentPoint.Stomach:
            case OMV.AttachmentPoint.LeftPec:
            case OMV.AttachmentPoint.RightPec:
            case OMV.AttachmentPoint.HUDCenter2:
            case OMV.AttachmentPoint.HUDTopRight:
            case OMV.AttachmentPoint.HUDTop:
            case OMV.AttachmentPoint.HUDTopLeft:
            case OMV.AttachmentPoint.HUDCenter:
            case OMV.AttachmentPoint.HUDBottomLeft:
            case OMV.AttachmentPoint.HUDBottom:
            case OMV.AttachmentPoint.HUDBottomRight:
                break;
            default:
                break;
        }
        pos = posRet;
        rot = rotRet;
    }

}
}
