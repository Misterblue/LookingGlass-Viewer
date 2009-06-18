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

    private float m_sceneMagnification = 10.0f;
    bool m_buildMaterialsAtMeshCreationTime = true;

    private OMVR.IRendering m_meshMaker = null;
    // some of the mesh conversion routines include the scale calculation
    private bool m_usePrimScaleFactor = false;

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
            Renderer.Mesher.MeshmerizerG amesher = new Renderer.Mesher.MeshmerizerG();
            // There is two ways to do scaling: in the mesh or in Ogre. We choose the latter here
            // so we can create shared vertices for the standard shapes (the cubes  that are everywhere)
            // this causes the mesherizer to not scale the node coordinates by the prim scaling factor
            // Update: scaling with Ogre has proved problematic: the scaling effects the mesh and
            // the position coordinates around the object. This is a problem for child nodes.
            // It also effects the texture mapping so texture scaling factors would have to be
            // scaled by the scale of teh face that they appear on. Ugh.
            // For the moment, turned off while I figure that stuff out.
            // amesher.ShouldScaleMesh = false;
            // m_usePrimScaleFactor = true; // use Ogre scaling rather than mesh scaling
            amesher.ShouldScaleMesh = true;
            m_usePrimScaleFactor = false; // use Ogre scaling rather than mesh scaling
            m_meshMaker = amesher;
        }

        // magnification of passed World coordinates into Ogre coordinates
        m_sceneMagnification = float.Parse(Globals.Configuration.ParamString("Renderer.Ogre.LL.SceneMagnification"));
        // true if to creat materials while we are creating the mesh
        m_buildMaterialsAtMeshCreationTime = Globals.Configuration.ParamBool("Renderer.Ogre.LL.EarlyMaterialCreate");
    }

    public RenderableInfo RenderingInfo(Object sceneMgr, IEntity ent) {
        LLEntityPhysical llent;
        LLRegionContext rcontext;
        OMV.Primitive prim;
        string newMeshName = EntityNameOgre.ConvertToOgreNameX(ent.Name, ".mesh");
        // true if we should do the scaling with the rendering parameters
        bool shouldScale = m_usePrimScaleFactor;

        try {
            llent = (LLEntityPhysical)ent;
            rcontext = (LLRegionContext)llent.RegionContext;
            prim = llent.Prim;
            if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null");
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderingInfoLL: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        RenderableInfo ri = new RenderableInfo();
        ri.basicObject = newMeshName;   // pass the name of the mesh that should be created
        
        // if a standard type (done by Ogre), let the rendering system do the scaling
        int meshType = 0;
        int meshFaces = 0;
        if (CheckStandardMeshType(prim, out meshType, out meshFaces)) {
            // if a standard mesh type, use Ogre scaling so we can reuse base shapes
            shouldScale = true;
        }

        // if the prim has a parent, we must hang this scene node off the parent's scene node
        ri.parentID = prim.ParentID;
        ri.rotation = prim.Rotation;
        // some of the mesh creators include the scale calculations
        ri.position = prim.Position;
        if (shouldScale) {
            ri.scale = prim.Scale * m_sceneMagnification;
        }
        else {
            ri.scale = new OMV.Vector3(m_sceneMagnification, m_sceneMagnification, m_sceneMagnification);
        }

        // The region has a root node. Dig through the LL specific data structures to find it
        if (ent is LLEntityBase) {
            ri.RegionRoot = ent.RegionContext.Addition(RendererOgre.AddRegionSceneNode);
        }

        return ri;
    }

    /// <summary>
    /// Create a mesh in the renderer.
    /// </summary>
    /// <param name="sMgr">the scene manager receiving  the mesh</param>
    /// <param name="ent">The entity the mesh is coming from</param>
    /// <param name="meshName">The name the mesh should take</param>
    public bool CreateMeshResource(Object sMgr, IEntity ent, string meshName) {
        OgreSceneMgr m_sceneMgr = (OgreSceneMgr)sMgr;
        LLEntityPhysical llent;
        OMV.Primitive prim;

        try {
            llent = (LLEntityPhysical)ent;
            prim = llent.Prim;
            if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null");
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "RenderingInfoLL: conversion of pointers failed: " + e.ToString());
            throw e;
        }

        int meshType = 0;
        int meshFaces = 0;
        if (CheckStandardMeshType(prim, out meshType, out meshFaces)) {
            m_log.Log(LogLevel.DBADERROR, "RenderingInfoLL: not implemented Standard Type");
            // while we're in the neighborhood, we can create the materials
            if (m_buildMaterialsAtMeshCreationTime) {
                for (int j = 0; j < meshFaces; j++) {
                    CreateMaterialResource2(m_sceneMgr, ent, prim, EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, j), j);
                }
            }

            // Ogr.CreateStandardMeshResource(meshName, meshType);
        }
        else {
            OMVR.FacetedMesh mesh;
            try {
                mesh = m_meshMaker.GenerateFacetedMesh(prim, OMVR.DetailLevel.High);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DRENDERDETAIL, "RenderingInfoLL: failed generating mesh: " + e.ToString());
                // would happen for types not working out
                throw e;
            }

            // we have the face data. We package this up into a few big arrays to pass them
            //   to the real renderer.

            // we pass two one-dimensional arrays of floating point numbers over to the
            // unmanaged code. The first array contains:
            //   faceCounts[0] = number of faces
            //   faceCounts[1] = offset in second array for beginning of vertex info for face 1
            //   faceCounts[2] = number of vertices for face 1
            //   faceCounts[3] = stride for vertex info for face 1 (= 5)
            //   faceCounts[4] = offset in second array for beginning of indices info for face 1
            //   faceCounts[5] = number of indices for face 1
            //   faceCounts[6] = stride for indices (= 3)
            //   faceCounts[7] = offset in second array for beginning of vertex info for face 2
            //   faceCounts[8] = number of vertices for face 2
            //   faceCounts[9] = stride for vertex info for face 2 (= 9)
            //   etc
            // The second array contains the vertex info in the order:
            //   v.X, v.Y, v.Z, t.X, t.Y
            // this is repeated for each vertex
            // This is followed by the list of indices listed as i.X, i.Y, i.Z

            const int faceCountsStride = 6;
            const int verticesStride = 5;
            const int indicesStride = 3;
            // calculate how many floating point numbers we're pushing over
            int[] faceCounts = new int[mesh.Faces.Count * faceCountsStride + 1];
            faceCounts[0] = mesh.Faces.Count;
            int totalVertices = 0;
            for (int j = 0; j < mesh.Faces.Count; j++) {
                OMVR.Face face = mesh.Faces[j];
                int faceBase = j * faceCountsStride + 1;
                // m_log.Log(LogLevel.DRENDERDETAIL, "Mesh F" + j.ToString() + ":"
                //     + " vcnt=" + face.Vertices.Count.ToString()
                //     + " icnt=" + face.Indices.Count.ToString());
                faceCounts[faceBase + 0] = totalVertices;
                faceCounts[faceBase + 1] = face.Vertices.Count;
                faceCounts[faceBase + 2] = verticesStride;
                totalVertices += face.Vertices.Count * verticesStride;
                faceCounts[faceBase + 3] = totalVertices;
                faceCounts[faceBase + 4] = face.Indices.Count;
                faceCounts[faceBase + 5] = indicesStride;
                totalVertices += face.Indices.Count;
            }

            float[] faceVertices = new float[totalVertices];
            int vertI = 0;
            for (int j = 0; j < mesh.Faces.Count; j++) {
                OMVR.Face face = mesh.Faces[j];

                // Texture transform for this face
                OMV.Primitive.TextureEntryFace teFace = null;
                try {
                    teFace = prim.Textures.GetFace((uint)j);
                    m_meshMaker.TransformTexCoords(face.Vertices, face.Center, teFace);
                }
                catch {
                    m_log.Log(LogLevel.DBADERROR, "RenderOgreLL.CreateMeshResource:"
                        + " more faces in mesh than in prim:"
                        + " ent=" + ent.Name
                        + ", face=" + j.ToString()
                    );
                }

                // Vertices for this face
                for (int k = 0; k < face.Vertices.Count; k++) {
                    OMVR.Vertex thisVert = face.Vertices[k];
                    /* m_log.Log(LogLevel.DRENDERDETAIL, "CreateMesh:"
                        + " px=" + thisVert.Position.X.ToString()
                        + ", py=" + thisVert.Position.Y.ToString()
                        + ", pz=" + thisVert.Position.Z.ToString()
                        + ", tx=" + thisVert.TexCoord.X.ToString()
                        + ", ty=" + thisVert.TexCoord.Y.ToString()
                        ); */
                    faceVertices[vertI + 0] = thisVert.Position.X;
                    faceVertices[vertI + 1] = thisVert.Position.Y;
                    faceVertices[vertI + 2] = thisVert.Position.Z;
                    faceVertices[vertI + 3] = thisVert.TexCoord.X;
                    faceVertices[vertI + 4] = thisVert.TexCoord.Y;
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
                for (int j = 0; j < mesh.Faces.Count; j++) {
                    CreateMaterialResource2(m_sceneMgr, ent, prim, EntityNameOgre.ConvertToOgreMaterialNameX(ent.Name, j), j);
                }
            }

            m_log.Log(LogLevel.DRENDERDETAIL, "RenderOgreLL: "
                + ent.Name
                + " f=" + mesh.Faces.Count.ToString()
                + " fcs=" + faceCounts.Length
                + " fs=" + faceVertices.Length
                + " vi=" + vertI
                );
            // Now create the mesh
            Ogr.CreateMeshResource(meshName, faceCounts, faceVertices);
        }
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

    public void CreateMaterialResource(Object sMgr, IEntity ent, string materialName) {
        OgreSceneMgr m_sceneMgr = (OgreSceneMgr)sMgr;
        LLEntityPhysical llent;
        OMV.Primitive prim;

        try {
            llent = (LLEntityPhysical)ent;
            prim = llent.Prim;
            if (prim == null) throw new LookingGlassException("ASSERT: RenderOgreLL: prim is null");
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource: conversion of pointers failed: " + e.ToString());
            throw e;
        }
        int faceNum = EntityNameOgre.GetFaceFromOgreMaterialNameX(materialName);
        if (faceNum < 0) {
            // no face was found in the material name
            m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource: no face number for " + materialName);
            return;
        }
        CreateMaterialResource2(m_sceneMgr, ent, prim, materialName, faceNum);
    }

    private void CreateMaterialResource(OgreSceneMgr m_sceneMgr, IEntity ent, OMV.Primitive prim, string materialName, int faceNum) {
        OMV.Primitive.TextureEntryFace textureFace = prim.Textures.GetFace((uint)faceNum);
        OMV.Color4 textureParamColor = new OMV.Color4(0.1f, 0.1f, 1f, 0f);
        // float textureParamRepeatU = 1.0f;
        // float textureParamRepeatV = 1.0f;
        // float textureParamOffsetU = 1.0f;
        // float textureParamOffsetV = 1.0f;
        // float textureParamRotation = 0f;
        float textureParamGlow = 0f;
        OMV.Bumpiness textureParamBump = OMV.Bumpiness.None;
        OMV.Shininess textureParamShiny = OMV.Shininess.None;
        bool textureParamFullBright = false;
        // bool textureParamMediaFlags;
        // OMV.MappingType textureParamMappingType;
        OMV.UUID textureID = OMV.Primitive.TextureEntry.WHITE_TEXTURE;
        if (textureFace != null) {
            textureParamColor = textureFace.RGBA;
            // textureParamRepeatU = textureFace.RepeatU;
            // textureParamRepeatV = textureFace.RepeatV;
            // textureParamOffsetU = textureFace.OffsetU;
            // textureParamOffsetV = textureFace.OffsetV;
            // textureParamRotation = textureFace.Rotation;
            textureParamGlow = textureFace.Glow;
            textureParamBump = textureFace.Bump;
            textureParamShiny = textureFace.Shiny;
            textureParamFullBright = textureFace.Fullbright;
            // textureParamMediaFlags = textureFace.MediaFlags;
            // textureParamMappingType = textureFace.TexMapType;
            textureID = textureFace.TextureID;
        }
        EntityNameOgre textureEntityName = new EntityNameOgre(ent, textureID.ToString());
        string textureOgreResourceName = "";
        if (textureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
            // what if the ogre resource has filetypes on it? Where does that info come from?
            // textureOgreResourceName = EntityNameOgre.ConvertToOgreName(textureEntityName.Name, ".jp2");
            textureOgreResourceName = textureEntityName.OgreResourceName;
        }
        m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource: m=" + materialName + ",o=" + textureOgreResourceName);
        Ogr.CreateMaterialResource(materialName, textureOgreResourceName,
                textureParamColor.R, textureParamColor.G, textureParamColor.B, textureParamColor.A,
                textureParamGlow, textureParamFullBright, (int)textureParamShiny, (int)textureParamBump);
    }

    /// <summary>
    /// Create a material resource in Ogre. This is the new way done by passing an
    /// array of parameters. Bool values are 0f for false and true otherwise.
    /// The offsets in the passed parameter array is defined with the interface in
    /// LookingGlass.Renderer.Ogre.Ogr.
    /// </summary>
    /// <param name="m_sceneMgr">The scene manager</param>
    /// <param name="ent">the entity of the underlying prim</param>
    /// <param name="prim">the OMV.Primitive that is getting the material</param>
    /// <param name="materialName">the name to give the new material</param>
    /// <param name="faceNum">the index of the primitive face getting the material</param>
    private void CreateMaterialResource2(OgreSceneMgr m_sceneMgr, IEntity ent, OMV.Primitive prim, string materialName, int faceNum) {
        OMV.Primitive.TextureEntryFace textureFace = prim.Textures.GetFace((uint)faceNum);
        float[] textureParams = new float[(int)Ogr.CreateMaterialParam.maxParam];
        OMV.UUID textureID = OMV.Primitive.TextureEntry.WHITE_TEXTURE;
        if (textureFace != null) {
            textureParams[(int)Ogr.CreateMaterialParam.colorR] = textureFace.RGBA.R;
            textureParams[(int)Ogr.CreateMaterialParam.colorG] = textureFace.RGBA.G;
            textureParams[(int)Ogr.CreateMaterialParam.colorB] = textureFace.RGBA.B;
            textureParams[(int)Ogr.CreateMaterialParam.colorA] = textureFace.RGBA.A;
            textureParams[(int)Ogr.CreateMaterialParam.scaleU] = 1f/textureFace.RepeatU;
            textureParams[(int)Ogr.CreateMaterialParam.scaleV] = 1f/textureFace.RepeatV;
            textureParams[(int)Ogr.CreateMaterialParam.scrollU] = textureFace.OffsetU;
            textureParams[(int)Ogr.CreateMaterialParam.scrollV] = textureFace.OffsetV;
            textureParams[(int)Ogr.CreateMaterialParam.rotate] = textureFace.Rotation;
            textureParams[(int)Ogr.CreateMaterialParam.glow] = textureFace.Glow;
            textureParams[(int)Ogr.CreateMaterialParam.bump] = (float)textureFace.Bump;
            textureParams[(int)Ogr.CreateMaterialParam.shiny] = (float)textureFace.Shiny;
            textureParams[(int)Ogr.CreateMaterialParam.fullBright] = textureFace.Fullbright ? 1f : 0f;
            textureParams[(int)Ogr.CreateMaterialParam.mappingType] = (float)textureFace.TexMapType;
            textureParams[(int)Ogr.CreateMaterialParam.mediaFlags] = textureFace.MediaFlags ? 1f : 0f;
            textureParams[(int)Ogr.CreateMaterialParam.textureHasTransparent] = 1f; // true for the moment
            textureID = textureFace.TextureID;
        }
        else {
            textureParams[(int)Ogr.CreateMaterialParam.colorR] = 0.4f;
            textureParams[(int)Ogr.CreateMaterialParam.colorG] = 0.4f;
            textureParams[(int)Ogr.CreateMaterialParam.colorB] = 0.4f;
            textureParams[(int)Ogr.CreateMaterialParam.colorA] = 0.0f;
            textureParams[(int)Ogr.CreateMaterialParam.scaleU] = 1.0f;
            textureParams[(int)Ogr.CreateMaterialParam.scaleV] = 1.0f;
            textureParams[(int)Ogr.CreateMaterialParam.scrollU] = 1.0f;
            textureParams[(int)Ogr.CreateMaterialParam.scrollV] = 1.0f;
            textureParams[(int)Ogr.CreateMaterialParam.rotate] = 0.0f;
            textureParams[(int)Ogr.CreateMaterialParam.glow] = 0.0f;
            textureParams[(int)Ogr.CreateMaterialParam.bump] = (float)OMV.Bumpiness.None;
            textureParams[(int)Ogr.CreateMaterialParam.shiny] = (float)OMV.Shininess.None;
            textureParams[(int)Ogr.CreateMaterialParam.fullBright] = 0f;
            textureParams[(int)Ogr.CreateMaterialParam.mappingType] = 0f;
            textureParams[(int)Ogr.CreateMaterialParam.mediaFlags] = 0f;
            textureParams[(int)Ogr.CreateMaterialParam.textureHasTransparent] = 0f;
        }
        EntityName textureEntityName = new EntityName(ent, textureID.ToString());
        string textureOgreResourceName = "";
        if (textureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
            textureOgreResourceName = EntityNameOgre.ConvertToOgreNameX(textureEntityName, null);
        }
        m_log.Log(LogLevel.DRENDERDETAIL, "CreateMaterialResource2: m=" + materialName + ",o=" + textureOgreResourceName);
        Ogr.CreateMaterialResource2(materialName, textureOgreResourceName, textureParams);
    }


    /// <summary>
    /// We have a new region to place in the view. Create the scene node for the 
    /// whole region.
    /// </summary>
    /// <param name="sMgr"></param>
    /// <param name="rcontext"></param>
    public void MapRegionIntoView(Object sMgr, IRegionContext rcontext) {
        OgreSceneMgr m_sceneMgr = (OgreSceneMgr)sMgr;
        if (rcontext is LLRegionContext) {
            // a SL compatible region
            LLRegionContext llrcontext = (LLRegionContext)rcontext;
            // if we don't have a region scene node create one
            if (llrcontext.Addition(RendererOgre.AddRegionSceneNode) == null) {
                // this funny rotation of the region's scenenode causes the region
                // to be twisted from LL coordinates (Z up) to Ogre coords (Y up)
                // Anything added under this node will not need to be converted.
                OMV.Quaternion orient = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitX, -Globals.PI / 2);

                m_log.Log(LogLevel.DRENDERDETAIL, "MapRegionIntoView: Region at {0}, {1}, {2}",
                        (float)rcontext.WorldBase.X * m_sceneMagnification,
                        (float)rcontext.WorldBase.Z * m_sceneMagnification,
                        -(float)rcontext.WorldBase.Y * m_sceneMagnification
                        );

                OgreSceneNode node = m_sceneMgr.CreateSceneNode("RegionSceneNode/" + rcontext.Name,
                        null,        // because NULL, will add to root
                        false, true,
                        (float)rcontext.WorldBase.X * m_sceneMagnification,
                        (float)rcontext.WorldBase.Z * m_sceneMagnification,
                        -(float)rcontext.WorldBase.Y * m_sceneMagnification,
                        m_sceneMagnification, m_sceneMagnification, m_sceneMagnification,
                        // 1f, 1f, 1f,
                        orient.W, orient.X, orient.Y, orient.Z);

                // the region scene node is saved in the region context additions
                llrcontext.SetAddition(RendererOgre.AddRegionSceneNode, node);

                // Terrain will be added as we get the messages describing same

                // if the region has water, add that
                if (rcontext.TerrainInfo.WaterHeight != TerrainInfoBase.NOWATER) {
                    Ogr.AddOceanToRegion(m_sceneMgr.BasePtr, node.BasePtr,
                                rcontext.Size.X * m_sceneMagnification, 
                                rcontext.Size.Y * m_sceneMagnification,
                                rcontext.TerrainInfo.WaterHeight, 
                                "Water/" + rcontext.Name);
                }
            }
        }
        return;
    }
}
}
