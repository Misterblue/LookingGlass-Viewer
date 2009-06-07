﻿/* Copyright (c) 2008 Robert Adams
 * Portions of code (c) 2008 Teravis (TODO: look up proper copyright)
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
 * DISCLAIMED. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY
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
using System.Text;
using LookingGlass.Framework.Logging;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;

namespace LookingGlass.Renderer.Mesher {
    /// <summary>
    /// Meshing code based on the Idealist Viewer (20081213).
    /// </summary>
public class MeshmerizerG : OMVR.IRendering {
    // If this is set to 'true' the returned mesh will be scaled by the prim's scaling
    // parameters. Otherwise the mesh is a unit mesh and needs scaling elsewhere.
    private bool m_shouldScale = true;
    public bool ShouldScaleMesh { get { return m_shouldScale; } set { m_shouldScale = value; } }

    /// <summary>
    /// Generates a basic mesh structure from a primitive
    /// </summary>
    /// <param name="prim">Primitive to generate the mesh from</param>
    /// <param name="lod">Level of detail to generate the mesh at</param>
    /// <returns>The generated mesh</returns>
    public OMVR.SimpleMesh GenerateSimpleMesh(OMV.Primitive prim, OMVR.DetailLevel lod) {
        return null;
    }

    /// <summary>
    /// Generates a a series of faces, each face containing a mesh and
    /// metadata
    /// </summary>
    /// <param name="prim">Primitive to generate the mesh from</param>
    /// <param name="lod">Level of detail to generate the mesh at</param>
    /// <param name="shouldScale">'true' if the mesh should be scaled by the prim's scale parameters</param>
    /// <returns>The generated mesh</returns// >
    public OMVR.FacetedMesh GenerateFacetedMesh(OMV.Primitive prim, OMVR.DetailLevel lod) {
        OMV.Primitive.ConstructionData primData = prim.PrimData;
        int sides = 4;
        int hollowsides = 4;

        float profileBegin = primData.ProfileBegin;
        float profileEnd = primData.ProfileEnd;
        bool isSphere = false;

        if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.Circle) {
            switch (lod) {
                case OMVR.DetailLevel.Low:
                    sides = 6;
                    break;
                case OMVR.DetailLevel.Medium:
                    sides = 12;
                    break;
                default:
                    sides = 24;
                    break;
            }
        }
        else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.EqualTriangle)
            sides = 3;
        else if ((OMV.ProfileCurve)(primData.profileCurve & 0x07) == OMV.ProfileCurve.HalfCircle) { 
            // half circle, prim is a sphere
            isSphere = true;
            switch (lod) {
                case OMVR.DetailLevel.Low:
                    sides = 6;
                    break;
                case OMVR.DetailLevel.Medium:
                    sides = 12;
                    break;
                default:
                    sides = 24;
                    break;
            }
            profileBegin = 0.5f * profileBegin + 0.5f;
            profileEnd = 0.5f * profileEnd + 0.5f;
        }

        if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Same)
            hollowsides = sides;
        else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Circle) {
            switch (lod) {
                case OMVR.DetailLevel.Low:
                    hollowsides = 6;
                    break;
                case OMVR.DetailLevel.Medium:
                    hollowsides = 12;
                    break;
                default:
                    hollowsides = 24;
                    break;
            }
        }
        else if ((OMV.HoleType)primData.ProfileHole == OMV.HoleType.Triangle)
            hollowsides = 3;

        PrimMesher.PrimMesh newPrim = new PrimMesher.PrimMesh(sides, profileBegin, profileEnd, (float)primData.ProfileHollow, hollowsides);
        newPrim.viewerMode = true;
        newPrim.holeSizeX = primData.PathScaleX;
        newPrim.holeSizeY = primData.PathScaleY;
        newPrim.pathCutBegin = primData.PathBegin;
        newPrim.pathCutEnd = primData.PathEnd;
        newPrim.topShearX = primData.PathShearX;
        newPrim.topShearY = primData.PathShearY;
        newPrim.radius = primData.PathRadiusOffset;
        newPrim.revolutions = primData.PathRevolutions;
        newPrim.skew = primData.PathSkew;
        switch (lod) {
            case OMVR.DetailLevel.Low:
                newPrim.stepsPerRevolution = 6;
                break;
            case OMVR.DetailLevel.Medium:
                newPrim.stepsPerRevolution = 12;
                break;
            default:
                newPrim.stepsPerRevolution = 24;
                break;
        }

        if (primData.PathCurve == OMV.PathCurve.Line) {
            newPrim.taperX = 1.0f - primData.PathScaleX;
            newPrim.taperY = 1.0f - primData.PathScaleY;
            newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
            newPrim.twistEnd = (int)(180 * primData.PathTwist);
            newPrim.ExtrudeLinear();
        }
        else {
            newPrim.taperX = primData.PathTaperX;
            newPrim.taperY = primData.PathTaperY;
            newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
            newPrim.twistEnd = (int)(360 * primData.PathTwist);
            newPrim.ExtrudeCircular();
        }

        int numViewerFaces = newPrim.viewerFaces.Count;
        int numPrimFaces = newPrim.numPrimFaces;

        for (uint i = 0; i < numViewerFaces; i++) {
            PrimMesher.ViewerFace vf = newPrim.viewerFaces[(int)i];

            if (isSphere) {
                vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
            }
        }
        if (m_shouldScale) {
            newPrim.Scale(prim.Scale.X, prim.Scale.Y, prim.Scale.Z);
        }

        // copy the vertex information into OMVR.IRendering structures
        OMVR.FacetedMesh omvrmesh = new OMVR.FacetedMesh();
        omvrmesh.Faces = new List<OMVR.Face>();
        omvrmesh.Prim = prim;
        omvrmesh.Profile = new OMVR.Profile();
        omvrmesh.Profile.Faces = new List<OMVR.ProfileFace>();
        omvrmesh.Profile.Positions = new List<OMV.Vector3>();
        omvrmesh.Path = new OMVR.Path();
        omvrmesh.Path.Points = new List<OMVR.PathPoint>();

        for (int ii=0; ii<numPrimFaces; ii++) {
            OMVR.Face oface = new OMVR.Face();
            oface.Vertices = new List<OMVR.Vertex>();
            oface.Indices = new List<ushort>();
            int faceVertices = 0;
            foreach (PrimMesher.ViewerFace vface in newPrim.viewerFaces) {
                if (vface.primFaceNumber == ii) {
                    OMVR.Vertex vert = new OMVR.Vertex();
                    vert.Position = new OMV.Vector3(vface.v1.X, vface.v1.Y, vface.v1.Z);
                    vert.TexCoord = new OMV.Vector2(vface.uv1.U, vface.uv1.V);
                    vert.Normal = new OMV.Vector3(vface.n1.X, vface.n1.Y, vface.n1.Z);
                    oface.Vertices.Add(vert);

                    vert = new OMVR.Vertex();
                    vert.Position = new OMV.Vector3(vface.v2.X, vface.v2.Y, vface.v2.Z);
                    vert.TexCoord = new OMV.Vector2(vface.uv2.U, vface.uv2.V);
                    vert.Normal = new OMV.Vector3(vface.n2.X, vface.n2.Y, vface.n2.Z);
                    oface.Vertices.Add(vert);

                    vert = new OMVR.Vertex();
                    vert.Position = new OMV.Vector3(vface.v3.X, vface.v3.Y, vface.v3.Z);
                    vert.TexCoord = new OMV.Vector2(vface.uv3.U, vface.uv3.V);
                    vert.Normal = new OMV.Vector3(vface.n3.X, vface.n3.Y, vface.n3.Z);
                    oface.Vertices.Add(vert);

                    oface.Indices.Add((ushort)(faceVertices*3+0));
                    oface.Indices.Add((ushort)(faceVertices*3+1));
                    oface.Indices.Add((ushort)(faceVertices*3+2));
                    faceVertices++;
                }
            }
            if (faceVertices > 0) {
                oface.TextureFace = prim.Textures.FaceTextures[ii];
                if (oface.TextureFace == null) {
                    oface.TextureFace = prim.Textures.DefaultTexture;
                }
                oface.ID = ii;
                omvrmesh.Faces.Add(oface);
            }
        }

        return omvrmesh;
    }

    /// <summary>
    /// Apply texture coordinate modifications from a
    /// <seealso cref="TextureEntryFace"/> to a list of vertices
    /// </summary>
    /// <param name="vertices">Vertex list to modify texture coordinates for</param>
    /// <param name="center">Center-point of the face</param>
    /// <param name="teFace">Face texture parameters</param>
    public void TransformTexCoords(List<OMVR.Vertex> vertices, OMV.Vector3 center, OMV.Primitive.TextureEntryFace teFace) {
        // compute trig stuff up front
        float cosineAngle = (float)Math.Cos(teFace.Rotation);
        float sinAngle = (float)Math.Sin(teFace.Rotation);

        // need a check for plainer vs default
        // just do default for now (I don't know what planar is)
        /* LogManager.Log.Log(LogLevel.DRENDERDETAIL,
            "TransformTex: c=" + vertices.Count.ToString()
            + ", rU=" + teFace.RepeatU
            + ", rV=" + teFace.RepeatV
            + ", oU=" + teFace.OffsetU
            + ", oV=" + teFace.OffsetV
            + ", cos=" + cosineAngle.ToString()
            + ", sin=" + sinAngle.ToString()
            ); */
        for (int ii=0; ii<vertices.Count; ii++ ) {
            OMVR.Vertex vert = vertices[ii];
            // tex coord comes to us as a number between zero and one
            // transform about the center of the texture
            float tX = vert.TexCoord.X - 0.5f;
            float tY = vert.TexCoord.Y - 0.5f;
            // rotate, scale, offset
            vert.TexCoord.X = (tX * cosineAngle + tY * sinAngle) * teFace.RepeatU + teFace.OffsetU + 0.5f; ;
            vert.TexCoord.Y = (-tX * sinAngle + tY * cosineAngle) * teFace.RepeatV + teFace.OffsetV + 0.5f;
        }
        return;
    }
}
}
