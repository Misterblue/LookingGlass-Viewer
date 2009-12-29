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
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Statistics;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.WorkQueue {
    // A static class which keeps a list of all the allocated work queues
    // and can serve up statistics about them.
    public class WorkQueueManager : IDisplayable , IInstance<WorkQueueManager> {

        private List<IWorkQueue> m_queues;

        private static WorkQueueManager m_instance = null;
        public static WorkQueueManager Instance {
            get {
                if (m_instance == null) m_instance = new WorkQueueManager();
                return m_instance;
            }
        }
        
        public WorkQueueManager() {
            m_queues = new List<IWorkQueue>();
        }

        public void Register(IWorkQueue wq) {
            Logging.LogManager.Log.Log(LogLevel.DINITDETAIL, "WorkQueueManager: registering queue {0}", wq.Name);
            lock (m_queues) m_queues.Add(wq);
        }

        public void Unregister(IWorkQueue wq) {
            lock (m_queues) m_queues.Remove(wq);
        }

        public void ForEach(Action<IWorkQueue> act) {
            lock (m_queues) {
                foreach (IWorkQueue wq in m_queues) {
                    act(wq);
                }
            }
        }

        public OMVSD.OSDMap GetDisplayable() {
            OMVSD.OSDMap aMap = new OMVSD.OSDMap();
            lock (m_queues) {
                foreach (IWorkQueue wq in m_queues) {
                    try {
                        aMap.Add(wq.Name, wq.GetDisplayable());
                    }
                    catch (Exception e) {
                        LogManager.Log.Log(LogLevel.DBADERROR, "WorkQueueManager.GetDisplayable: duplicate symbol: {0}", wq.Name);
                    }
                }
            }
            return aMap;
        }
    }
}
