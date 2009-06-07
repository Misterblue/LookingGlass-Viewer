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

namespace LookingGlass.World {
public class TerrainInfoBase : EntityBase, ITerrainInfo {
    protected float[,] m_heightMap;
    public float[,] HeightMap { get { return m_heightMap; } }

    protected int m_heightMapWidth; // X dimension
    public int HeightMapWidth { get { return m_heightMapWidth; } }

    protected int m_heightMapLength; // Y dimension
    public int HeightMapLength { get { return m_heightMapLength; } }

    protected float m_maximumHeight;
    public float MaximumHeight { get { return m_maximumHeight; } }

    protected float m_minimumHeight;
    public float MinimumHeight { get { return m_minimumHeight; } }

    protected int m_terrainPatchStride = 16;
    public int TerrainPatchStride { get { return m_terrainPatchStride; } }

    // X dimension (E/W)
    protected int m_terrainPatchWidth = 256;
    public int TerrainPatchWidth { get { return m_terrainPatchWidth; } }

    // Y dimension (N/S)
    protected int m_terrainPatchLength = 256;
    public int TerrainPatchLength { get { return m_terrainPatchLength; } }

    // height of the water
    public const float NOWATER = -113537;   // here because it can't go in the interface (stupid C#)
    protected float m_waterHeight = NOWATER;
    public float WaterHeight { get { return m_waterHeight; } set { m_waterHeight = value; } }

    // the patch is presumed to be Stride width and length
    public virtual void UpdatePatch(RegionContextBase reg, int x, int y, float[] data) {
        return;
    }

    public TerrainInfoBase (RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
    }

    public override void Dispose() {
        throw new NotImplementedException();
    }
}
}
