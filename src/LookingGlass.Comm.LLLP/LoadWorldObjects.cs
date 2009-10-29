/* Copyright 2008 (c) Robert Adams
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
using System.Threading;
using LookingGlass.Comm;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;

namespace LookingGlass.Comm.LLLP {
    /// <summary>
    /// If we get started up after OpenMetaverse as been logged in, we must
    /// suck the state out of the OpenMetaverse library and push it into
    /// our world representation.
    /// </summary>
public class LoadWorldObjects {
    static LoadWorldObjects() {
    }

    public static void Load(OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading existing context");
        List<OMV.Simulator> simsToLoad = new List<OMV.Simulator>();
        lock (netComm.Network.Simulators) {
            foreach (OMV.Simulator sim in netComm.Network.Simulators) {
                if (WeDontKnowAboutThisSimulator(sim, netComm, worldComm)) {
                    // tell the world about this simulator
                    LogManager.Log.Log(LogLevel.DCOMMDETAIL, "LoadWorldObjects: adding simulator {0}", sim.Name);
                    worldComm.Network_OnSimConnected(sim);
                    simsToLoad.Add(sim);
                }
            }
        }
        Object[] loadParams = { simsToLoad, netComm, worldComm };
        ThreadPool.QueueUserWorkItem(LoadSims, loadParams);
        // ThreadPool.UnsafeQueueUserWorkItem(LoadSims, loadParams);
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: started thread to load sim objects");
    }

    /// <summary>
    /// Routine called on a separate thread to load the avatars and objects from the simulators
    /// into LookingGlass.
    /// </summary>
    /// <param name="loadParam"></param>
    private static void LoadSims(Object loadParam) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: starting to load sim objects");
        try {
            Object[] loadParams = (Object[])loadParam;
            List<OMV.Simulator> simsToLoad = (List<OMV.Simulator>)loadParams[0];
            OMV.GridClient netComm = (OMV.GridClient)loadParams[1];
            CommLLLP worldComm = (CommLLLP)loadParams[2];

            OMV.Simulator simm = null;
            try {
                foreach (OMV.Simulator sim in simsToLoad) {
                    simm = sim;
                    LoadASim(sim, netComm, worldComm);
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, "LoadWorldObjects: exception loading {0}: {1}",
                    (simm == null ? "NULL" : simm.Name), e.ToString());
            }
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "LoadWorldObjects: exception: {0}", e.ToString());
        }
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: completed loading sim objects");
    }

    public static void LoadASim(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading avatars and objects for sim {0}", sim.Name);
        AddAvatars(sim, netComm, worldComm);
        AddObjects(sim, netComm, worldComm);
    }

    // Return 'true' if we don't have this region in our world yet
    private static bool WeDontKnowAboutThisSimulator(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LLRegionContext regn = worldComm.FindRegion(delegate(LLRegionContext rgn) {
            return rgn.Simulator.ID == sim.ID;
        });
        return (regn == null);
    }

    private static void AddAvatars(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading {0} avatars", sim.ObjectsAvatars.Count);
        List<OMV.Avatar> avatarsToNew = new List<OpenMetaverse.Avatar>();
        sim.ObjectsAvatars.ForEach(delegate(OMV.Avatar av) {
            avatarsToNew.Add(av);
        });
        // this happens outside the avatar list lock
        foreach (OMV.Avatar av in avatarsToNew) {
            worldComm.Objects_OnNewAvatar(sim, av, sim.Handle, 0);
        }
    }

    private static void AddObjects(OMV.Simulator sim, OMV.GridClient netComm, CommLLLP worldComm) {
        LogManager.Log.Log(LogLevel.DCOMM, "LoadWorldObjects: loading {0} primitives", sim.ObjectsPrimitives.Count);
        List<OMV.Primitive> primsToNew = new List<OpenMetaverse.Primitive>();
        sim.ObjectsPrimitives.ForEach(delegate(OMV.Primitive prim) {
            primsToNew.Add(prim);
        });
        foreach (OMV.Primitive prim in primsToNew) {
            worldComm.Objects_OnNewPrim(sim, prim, sim.Handle, 0);
        }
    }




}
}
