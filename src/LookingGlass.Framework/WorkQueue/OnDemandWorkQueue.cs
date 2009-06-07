﻿/* Copyright (c) Robert Adams
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

namespace LookingGlass.Framework.WorkQueue {
    // An odd mish mash of dynamic and static. The idea is that different work
    // queues can be build (priorities, ...). For the moment, they are all
    // here using several static methods that implement the redo and scheduling.

    /// <summary>
    /// OnDemandWorkQueue is one where routines queue work for later and at some
    /// point a thread comes in and does the work. The main user is the renderer
    /// who queues work to happen between frames
    /// </summary>

public class OnDemandWorkQueue : IWorkQueue {

    private long m_totalRequests = 0;
    public long TotalQueued { get { return m_totalRequests; } }

    private string m_queueName = "";
    public string Name { get { return m_queueName; } }

    public long CurrentQueued { get { return (long)m_workQueue.Count; } }

    protected List<DoLaterBase> m_workQueue;

    public OnDemandWorkQueue(string nam) {
        m_queueName = nam;
        m_totalRequests = 0;
        m_workQueue = new List<DoLaterBase>();
        WorkQueueManager.Register(this);
    }

    public void DoLater(DoLaterBase w){
        if (((m_totalRequests++) % 100) == 0) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: Queuing. requests={1}, queueSize={2}",
                m_queueName, m_totalRequests, m_workQueue.Count);
        }
        w.containingClass = this;
        w.remainingWait = 0;    // the first time through, do it now
        w.timesRequeued = 0;
        lock (m_workQueue) m_workQueue.Add(w);
    }

    // requeuing the work item. Since requeuing, add the delay
    public void DoLaterRequeue(ref DoLaterBase w) {
        w.timesRequeued++;
        long nextTime = Math.Min(w.requeueWait * w.timesRequeued, 20000);
        w.remainingWait = (DateTime.Now.ToFileTimeUtc() / 10000) + nextTime;
        LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: Requeuing. times={1}, wait={2}",
                m_queueName, w.timesRequeued, nextTime);
        lock (m_workQueue) m_workQueue.Add(w);
    }

    public void ProcessQueue() {
        ProcessQueue(50);
    }

    // A thread from the outside world calls in here to do some work on the queue
    // We process work items on the queue until the queue is empty or we reach 'maximumCost'.
    // Each queued item has a delay (a time in the future when it can be done) and a 
    // cost. As the work items are done, the cost is added up.
    // This means the thread coming in can count on being here only a limited amount
    // of time.
    public void ProcessQueue(int maximumCost) {
        int totalCost = 0;
        int totalCounter = 100;
        long now = DateTime.Now.ToFileTimeUtc() / 10000;
        DoLaterBase found = null;
        while ((totalCost < maximumCost) && (totalCounter > 0) && (m_workQueue.Count > 0)) {
            lock (m_workQueue) {
                // find an entry in the list who's time has come
                foreach (DoLaterBase ww in m_workQueue) {
                    if (ww.remainingWait < now) {
                        found = ww;
                        break;
                    }
                }
                if (found != null) {
                    // if found, remove from list
                    m_workQueue.Remove(found);
                }
            }
            if (found == null) {
                // if nothing found, we're done
                break;
            }
            else {
                // try to do the operation
                totalCounter--;
                if (found.DoIt()) {
                    // if it worked, count it as successful
                    totalCost += found.cost;
                }
                else {
                    // if it didn't work, requeue it for later
                    ((OnDemandWorkQueue)found.containingClass).DoLaterRequeue(ref found);
                }
            }
        }
    }
}
}
