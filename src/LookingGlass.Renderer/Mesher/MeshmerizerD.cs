/* Copyright (c) 2008 Robert Adams
 * Portions of code Copyright (c) Contributors, http://opensimulator.org/
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
    /// Meshing code based on the code from OpenSimulator (20081210).
    /// It is broken in that extrusions always scale to nothing.
    /// </summary>
public class MeshmerizerD : OMVR.IRendering {
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
    /// <returns>The generated mesh</returns// >
    public OMVR.FacetedMesh GenerateFacetedMesh(OMV.Primitive prim, OMVR.DetailLevel lod) {
        PrimMesher.PrimMesh primMesh;
        PrimMesher.SculptMesh sculptMesh;
        List<PrimMesher.ViewerFace> viewerFaces;
        int numViewerFaces = 0;
        string primName = "localID-" + prim.LocalID.ToString();

        // scalings that are in the Meshermizer from OpenSim
        float scale1 = 0.01f;
        float scale2 = 2.0e-5f;
        float scale3 = 1.8f;
        float scale4 = 3.2f;

        if (prim.Sculpt.Type != OMV.SculptType.None) {
            Image idata = null;
            try {
                // TODO: there needs to be a way to sleep while we wait for the texture
                //RA ManagedImage managedImage;  // we never use this
                //RA OpenJPEG.DecodeToImage(primShape.SculptData, out managedImage, out idata);
            }
            catch (Exception) {
                System.Console.WriteLine("[PHYSICS]: Unable to generate a Sculpty physics proxy.  Sculpty texture decode failed!");
                return null;
            }

            PrimMesher.SculptMesh.SculptType sculptType;
            switch (prim.Sculpt.Type) {
                case OMV.SculptType.Cylinder:
                    sculptType = PrimMesher.SculptMesh.SculptType.cylinder;
                    break;
                case OMV.SculptType.Plane:
                    sculptType = PrimMesher.SculptMesh.SculptType.plane;
                    break;
                case OMV.SculptType.Torus:
                    sculptType = PrimMesher.SculptMesh.SculptType.torus;
                    break;
                case OMV.SculptType.Sphere:
                default:
                    sculptType = PrimMesher.SculptMesh.SculptType.sphere;
                    break;
            }
            sculptMesh = new PrimMesher.SculptMesh((Bitmap)idata, sculptType, (int)lod, true);

            //RA idata.Dispose();

            // sculptMesh.DumpRaw(baseDir, primName, "primMesh");

            sculptMesh.Scale(prim.Scale.X, prim.Scale.Y, prim.Scale.Z);

            viewerFaces = sculptMesh.viewerFaces;
            numViewerFaces = 1;
        }

        else {
            float pathShearX = prim.PrimData.PathShearX < 128 ? (float)prim.PrimData.PathShearX * scale1 : (float)(prim.PrimData.PathShearX - 256) * scale1;
            float pathShearY = prim.PrimData.PathShearY < 128 ? (float)prim.PrimData.PathShearY * scale1 : (float)(prim.PrimData.PathShearY - 256) * scale1;
            float pathBegin = (float)prim.PrimData.PathBegin * scale2;
            float pathEnd = 1.0f - (float)prim.PrimData.PathEnd * scale2;
            float pathScaleX = (float)(prim.PrimData.PathScaleX - 100) * scale1;
            float pathScaleY = (float)(prim.PrimData.PathScaleY - 100) * scale1;

            float profileBegin = (float)prim.PrimData.ProfileBegin * scale2;
            float profileEnd = 1.0f - (float)prim.PrimData.ProfileEnd * scale2;
            float profileHollow = (float)prim.PrimData.ProfileHollow * scale2;

            int sides = 4;
            if (prim.PrimData.ProfileCurve == OMV.ProfileCurve.EqualTriangle)
                sides = 3;
            else if (prim.PrimData.ProfileCurve == OMV.ProfileCurve.Circle)
                sides = 24;
            else if (prim.PrimData.ProfileCurve == OMV.ProfileCurve.HalfCircle) {
                // half circle, prim is a sphere
                sides = 24;

                //RA profileBegin = 0.5f * profileBegin + 0.5f;
                //RA profileEnd = 0.5f * profileEnd + 0.5f;
            }

            int hollowSides = sides;
            if (prim.PrimData.ProfileHole == OMV.HoleType.Circle)
                hollowSides = 24;
            else if (prim.PrimData.ProfileHole == OMV.HoleType.Square)
                hollowSides = 4;
            else if (prim.PrimData.ProfileHole == OMV.HoleType.Triangle)
                hollowSides = 3;

            // Globals.Log.Log(LookingGlass.Logging.LogLevel.DRENDERDETAIL, "");

            primMesh = new PrimMesher.PrimMesh(sides, profileBegin, profileEnd, profileHollow, hollowSides);
            primMesh.viewerMode = true;

            primMesh.topShearX = pathShearX;
            primMesh.topShearY = pathShearY;
            primMesh.pathCutBegin = pathBegin;
            primMesh.pathCutEnd = pathEnd;

            if (prim.PrimData.PathCurve == OMV.PathCurve.Line) {
                primMesh.twistBegin = (int)(prim.PrimData.PathTwistBegin * scale3);
                primMesh.twistEnd = (int)(prim.PrimData.PathTwist * scale3);
                primMesh.taperX = pathScaleX;
                primMesh.taperY = pathScaleY;

                if (profileBegin < 0.0f || profileBegin >= profileEnd || profileEnd > 1.0f) {
                    LogManager.Log.Log(LogLevel.DBADERROR, 
                                "*** CORRUPT PRIM!! ***" + primName);
                    if (profileBegin < 0.0f) profileBegin = 0.0f;
                    if (profileEnd > 1.0f) profileEnd = 1.0f;
                }
#if SPAM
            Console.WriteLine("****** PrimMesh Parameters (Linear) ******\n" + primMesh.ParamsToDisplayString());
#endif
                try {
                    primMesh.ExtrudeLinear();
                }
                catch (Exception ex) {
                    LogManager.Log.Log(LogLevel.DBADERROR,
                            "Extrusion failure: exception: " + ex.ToString());
                    return null;
                }
            }
            else {
                //RA primMesh.holeSizeX = (200 - prim.PrimData.PathScaleX) * scale1;
                //RA primMesh.holeSizeY = (200 - prim.PrimData.PathScaleY) * scale1;
                primMesh.holeSizeX = prim.PrimData.PathScaleX * scale1;
                primMesh.holeSizeY = prim.PrimData.PathScaleY * scale1;
                primMesh.radius = scale1 * prim.PrimData.PathRadiusOffset;
                //RA primMesh.revolutions = 1.0f + 0.015f * prim.PrimData.PathRevolutions;
                primMesh.revolutions = prim.PrimData.PathRevolutions;
                primMesh.skew = scale1 * prim.PrimData.PathSkew;
                primMesh.twistBegin = (int)(prim.PrimData.PathTwistBegin * scale4);
                primMesh.twistEnd = (int)(prim.PrimData.PathTwist * scale4);
                primMesh.taperX = prim.PrimData.PathTaperX * scale1;
                primMesh.taperY = prim.PrimData.PathTaperY * scale1;

                if (profileBegin < 0.0f || profileBegin >= profileEnd || profileEnd > 1.0f) {
                    LogManager.Log.Log(LogLevel.DBADERROR,
                            "*** CORRUPT PRIM!! ***" + primName);
                    if (profileBegin < 0.0f) profileBegin = 0.0f;
                    if (profileEnd > 1.0f) profileEnd = 1.0f;
                }
#if SPAM
            Console.WriteLine("****** PrimMesh Parameters (Circular) ******\n" + primMesh.ParamsToDisplayString());
#endif
                try {
                    primMesh.ExtrudeCircular();
                }
                catch (Exception ex) {
                    LogManager.Log.Log(LogLevel.DBADERROR,
                            "Extrusion failure: prim=" + primName + " e: " + ex.ToString());
                    return null;
                }
            }

            // primMesh.DumpRaw(baseDir, primName, "primMesh");

            primMesh.Scale(prim.Scale.X, prim.Scale.Y, prim.Scale.Z);

            viewerFaces = primMesh.viewerFaces;
            numViewerFaces = primMesh.numPrimFaces;
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

        for (int ii=0; ii<numViewerFaces; ii++) {
            OMVR.Face oface = new OMVR.Face();
            oface.Vertices = new List<OMVR.Vertex>();
            oface.Indices = new List<ushort>();
            int faceVertices = 0;
            foreach (PrimMesher.ViewerFace vface in viewerFaces) {
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
        return;
    }
}
}
