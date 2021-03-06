﻿/* Copyright 2010 (c) Robert Adams
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
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;

namespace LookingGlass.Renderer.OGL {

    public struct FaceData
    {
        public float[] Vertices;
        public ushort[] Indices;
        public float[] Normals;
        public float[] TexCoords;
        public int TexturePointer;
        public System.Drawing.Image Texture;
        // TODO: Normals
    }

    public sealed class TextureInfo
    {
        /// <summary>OpenGL Texture ID</summary>
        public int ID;
        /// <summary>True if this texture has an alpha component</summary>
        public bool Alpha;

        public TextureInfo(int id, bool alpha)
        {
            ID = id;
            Alpha = alpha;
        }
    }

    public struct HeightmapLookupValue : IComparable<HeightmapLookupValue>
    {
        public ushort Index;
        public float Value;

        public HeightmapLookupValue(ushort index, float value) {
            Index = index;
            Value = value;
        }

        public int CompareTo(HeightmapLookupValue val) {
            return Value.CompareTo(val.Value);
        }
    }

    /// <summary>
    /// Rendering information for OpenGL in the region. This is attached the
    /// rcontext as an interface. This holds all the per region information needed
    /// to render the region.
    /// </summary>
    public sealed class RegionRenderInfo {
        public RegionRenderInfo() {
            this.renderFoliageList = new Dictionary<uint, OMV.Primitive>();
            this.renderPrimList = new Dictionary<uint, RenderablePrim>();
            this.renderAvatarList = new Dictionary<ulong, RenderableAvatar>();
            this.animations = new List<AnimatBase>();
            this.oceanHeight = 0f;
            this.terrainWidth = this.terrainLength = -1;
            this.refreshTerrain = true;     // force initial build
        }
        public Dictionary<uint, OMV.Primitive> renderFoliageList;
        public Dictionary<uint, RenderablePrim> renderPrimList;
        public Dictionary<ulong, RenderableAvatar> renderAvatarList;

        public List<AnimatBase> animations;

        public bool refreshTerrain;
        public float[] terrainVertices;
        public float[] terrainTexCoord;
        public float[] terrainNormal;
        public UInt16[] terrainIndices;
        public float terrainWidth;
        public float terrainLength;
        public float oceanHeight;
    }

    /// <summary>
    /// Description of all the information for OpenGL to render the prim.
    /// Kept in a list in the RegionRenderInfo for the region.
    /// </summary>
    public sealed class RenderablePrim {
        public OMV.Primitive Prim;          // the prim underlying this
        public OMVR.FacetedMesh Mesh;       // meshed prim
        public RegionContextBase rcontext;  // used for positioning in displayed world
        public AssetContextBase acontext;   // used for finding textures for Prim
        public bool isVisible;              // prim is visible from the current camera location

        private OMV.Vector3 localPosition;
        public OMV.Vector3 Position {
            get { return localPosition; }
            set { localPosition = value; }
        }
        private OMV.Quaternion localRotation;
        public OMV.Quaternion Rotation {
            get { return localRotation; }
            set { localRotation = value; }
        }

        public readonly static RenderablePrim Empty = new RenderablePrim();
    }

    public sealed class RenderableAvatar {
        public IEntityAvatar avatar;
    }

    public static class Math3D
    {
        // Column-major:
        // |  0  4  8 12 |
        // |  1  5  9 13 |
        // |  2  6 10 14 |
        // |  3  7 11 15 |

        public static float[] CreateTranslationMatrix(OMV.Vector3 v)
        {
            float[] mat = new float[16];

            mat[12] = v.X;
            mat[13] = v.Y;
            mat[14] = v.Z;
            mat[0] = mat[5] = mat[10] = mat[15] = 1;

            return mat;
        }

        public static float[] CreateRotationMatrix(OMV.Quaternion q)
        {
            float[] mat = new float[16];

            // Transpose the quaternion (don't ask me why)
            q.X = q.X * -1f;
            q.Y = q.Y * -1f;
            q.Z = q.Z * -1f;

            float x2 = q.X + q.X;
            float y2 = q.Y + q.Y;
            float z2 = q.Z + q.Z;
            float xx = q.X * x2;
            float xy = q.X * y2;
            float xz = q.X * z2;
            float yy = q.Y * y2;
            float yz = q.Y * z2;
            float zz = q.Z * z2;
            float wx = q.W * x2;
            float wy = q.W * y2;
            float wz = q.W * z2;

            mat[0] = 1.0f - (yy + zz);
            mat[1] = xy - wz;
            mat[2] = xz + wy;
            mat[3] = 0.0f;

            mat[4] = xy + wz;
            mat[5] = 1.0f - (xx + zz);
            mat[6] = yz - wx;
            mat[7] = 0.0f;

            mat[8] = xz - wy;
            mat[9] = yz + wx;
            mat[10] = 1.0f - (xx + yy);
            mat[11] = 0.0f;

            mat[12] = 0.0f;
            mat[13] = 0.0f;
            mat[14] = 0.0f;
            mat[15] = 1.0f;

            return mat;
        }

        public static float[] CreateScaleMatrix(OMV.Vector3 v)
        {
            float[] mat = new float[16];

            mat[0] = v.X;
            mat[5] = v.Y;
            mat[10] = v.Z;
            mat[15] = 1;

            return mat;
        }
    }
}
