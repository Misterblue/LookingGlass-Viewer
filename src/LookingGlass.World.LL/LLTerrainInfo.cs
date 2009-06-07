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
using LookingGlass.Framework.Logging;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
public class LLTerrainInfo : TerrainInfoBase {

    protected OMV.Simulator m_simulator;
    public OMV.Simulator Simulator { get { return m_simulator; } }

    private void init() {
        m_terrainPatchStride = 16;
        m_terrainPatchLength = 256;
        m_terrainPatchWidth = 256;
        UpdateHeightMap(null);
    }

    public LLTerrainInfo(RegionContextBase rcontext, AssetContextBase acontext) 
                : base(rcontext, acontext) {
        init();
    }

    public override void UpdatePatch(RegionContextBase reg, int x, int y, float[] data) {
        // even though I am passed the data, I rely on Comm.Client to save it for me
        UpdateHeightMap(reg);
        return;
    }

    private void UpdateHeightMap(RegionContextBase reg) {
        int stride = TerrainPatchStride;
        int stride2 = stride * TerrainPatchWidth;

        lock (this) {
            float[,] newHM = new float[TerrainPatchWidth, TerrainPatchLength];
            float minHeight = 999999f;
            float maxHeight = 0f;

            if (reg == null || !(reg is LLRegionContext)) {
                // things are not set up so create a default, flat heightmap
                LogManager.Log.Log(LogLevel.DWORLDDETAIL,
                        "LLTerrainInfo: Building default zero terrain");
                CreateZeroHeight(ref newHM);
                minHeight = maxHeight = 0f;
            }
            else {
                try {
                    LLRegionContext llreg = (LLRegionContext)reg;
                    OMV.Simulator sim = llreg.Simulator;
                    OMV.TerrainPatch[] patch = llreg.Comm.Terrain.SimPatches[sim.Handle];

                    int nullPatchCount = 0;
                    for (int px = 0; px < stride; px++) {
                        for (int py = 0; py < stride; py++) {
                            OMV.TerrainPatch pat = patch[px + py * stride];
                            if (pat == null) {
                                // if no patch, it's all zeros
                                if (0.0f < minHeight) minHeight = 0.0f;
                                if (0.0f > maxHeight) maxHeight = 0.0f;
                                for (int xx = 0; xx < stride; xx++) {
                                    for (int yy = 0; yy < stride; yy++) {
                                        // newHM[(py * stride + yy), (px * stride + xx)] = 0.0f;
                                        newHM[(px * stride + xx), (py * stride + yy)] = 0.0f;
                                    }
                                }
                                nullPatchCount++;
                            }
                            else {
                                for (int xx = 0; xx < stride; xx++) {
                                    for (int yy = 0; yy < stride; yy++) {
                                        float height = pat.Data[xx + yy * stride];
                                        // newHM[(py * stride + yy), (px * stride + xx)] = height;
                                        newHM[(px * stride + xx), (py * stride + yy)] = height;
                                        if (height < minHeight) minHeight = height;
                                        if (height > maxHeight) maxHeight = height;
                                    }
                                }
                            }
                        }
                    }
                    // LogManager.Log.Log(LogLevel.DWORLDDETAIL,
                    //         "LLTerrainInfo: UpdateHeightMap: {0} null patches = {1}", sim.Name, nullPatchCount);
                }
                catch {
                    // this usually happens when first starting a region
                    LogManager.Log.Log(LogLevel.DWORLDDETAIL,
                            "LLTerrainInfo: Exception building terrain. Defaulting to zero.");
                    CreateZeroHeight(ref newHM);
                    minHeight = maxHeight = 0f;
                }
            }

            m_heightMap = newHM;
            m_heightMapWidth = TerrainPatchWidth;   // X
            m_heightMapLength = TerrainPatchLength;
            m_minimumHeight = minHeight;
            m_maximumHeight = maxHeight;
            LogManager.Log.Log(LogLevel.DWORLDDETAIL,
                    "LLTerrainInfo: New terrain:"
                    + " min=" + m_minimumHeight.ToString()
                    + " max=" + m_maximumHeight.ToString()
                    );
        }
    }

    private void CreateZeroHeight(ref float[,] newHM) {
        for (int xx = 0; xx < TerrainPatchWidth; xx++ ) {
            for (int yy = 0; yy < TerrainPatchLength; yy++) {
                newHM[xx, yy] = 0f;
            }
        }
    }
}
}
