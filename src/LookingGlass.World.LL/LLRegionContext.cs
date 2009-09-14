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
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
public sealed class LLRegionContext : RegionContextBase {

    private OMV.Simulator m_simulator;
    public OMV.Simulator Simulator { get { return m_simulator; } }

    private Dictionary<uint, int> m_recentLocalIDRequests = null;

    public LLRegionContext(RegionContextBase rcontext, AssetContextBase acontext, 
                        LLTerrainInfo tinfo, OMV.Simulator sim) 
                : base(rcontext, acontext) {
        m_terrainInfo = tinfo;
        m_size = new OMV.Vector3(256f, 256f, 8000f);
        uint x, y;
        OMV.Utils.LongToUInts(sim.Handle, out x, out y);
        m_worldBase = new OMV.Vector3d((double)x, (double)y, 0d);
        m_simulator = sim;
        // this should be more general as "GRID/SIM"
        m_name = new EntityName(sim.Name);
        m_recentLocalIDRequests = new Dictionary<uint, int>();
    }

    /// <summary>
    /// Called to request a particular local ID should be sent to us. Very LLLP dependent.
    /// This is rare enough  that we don't bother locking.
    /// </summary>
    /// <param name="localID"></param>
    public void RequestLocalID(uint localID) {
        int now = System.Environment.TickCount;
        if (m_recentLocalIDRequests.ContainsKey(localID)) {
            // we've asked for this localID recently. See how recent.
            if (m_recentLocalIDRequests[localID] < (now - 20 * 1000)) {
                // it was a while ago. Time to ask again
                m_recentLocalIDRequests.Remove(localID);
            }
        }
        if (!m_recentLocalIDRequests.ContainsKey(localID)) {
            LogManager.Log.Log(LogLevel.DCOMMDETAIL, "LLRegionContext.RequestLocalID: asking for {0}/{1}", this.Name, localID);
            m_recentLocalIDRequests.Add(localID, now);
            m_comm.Objects.RequestObject(this.Simulator, localID);
        }
    }

    public override void Dispose() {
        throw new NotImplementedException();
    }

    private OMV.GridClient m_comm;
    public OMV.GridClient Comm { 
        get { return m_comm; }
        set { m_comm = value; }
    }

}
}
