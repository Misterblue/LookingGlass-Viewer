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
using OMVSD = OpenMetaverse.StructuredData;

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

    protected LinkedList<DoLaterBase> m_workQueue;

    public OnDemandWorkQueue(string nam) {
        m_queueName = nam;
        m_totalRequests = 0;
        m_workQueue = new LinkedList<DoLaterBase>();
        WorkQueueManager.Instance.Register(this);
    }

    public void DoLater(DoLaterBase w){
        if (((m_totalRequests++) % 100) == 0) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: Queuing. requests={1}, queueSize={2}",
                m_queueName, m_totalRequests, m_workQueue.Count);
        }
        w.containingClass = this;
        w.remainingWait = 0;    // the first time through, do it now
        w.timesRequeued = 0;
        AddToWorkQueue(w);
    }

    /// <summary>
    /// Experimental, untested entry which doesn't force the caller to create an
    /// instance of a DoLaterBase class but to use s delegate. The calling sequence
    /// would be something like:
    /// m_workQueue.DoLater((DoLaterCallback)delegate() { 
    ///     return LocalMethod(localParam1, localParam2, ...); 
    /// });
    /// </summary>
    /// <param name="dlcb"></param>
    public void DoLater(DoLaterCallback dlcb) {
        this.DoLater(new DoLaterDelegateCaller(dlcb));
    }

    public void DoLater(int priority, DoLaterCallback dlcb) {
        DoLaterBase newDoer = new DoLaterDelegateCaller(dlcb);
        newDoer.order = priority;
        this.DoLater(newDoer);
    }

    private class DoLaterDelegateCaller : DoLaterBase {
        DoLaterCallback m_dlcb;
        public DoLaterDelegateCaller(DoLaterCallback dlcb) {
            m_dlcb = dlcb;
        }
        public override bool DoIt() {
            return m_dlcb();
        }
    }


    // requeuing the work item. Since requeuing, add the delay
    public void DoLaterRequeue(ref DoLaterBase w) {
        w.timesRequeued++;
        int nextTime = Math.Min(w.requeueWait * w.timesRequeued, 5000);
        w.remainingWait = System.Environment.TickCount + nextTime;
        AddToWorkQueue(w);
    }

    /// <summary>
    /// Add the work item to the queue in the order order
    /// </summary>
    /// <param name="w"></param>
    private void AddToWorkQueue(DoLaterBase w) {
        lock (m_workQueue) {
            /*
            // Experimental code trying to give some order to the requests
            LinkedListNode<DoLaterBase> foundItem = null;
            for (LinkedListNode<DoLaterBase> ii = m_workQueue.First; ii != null; ii = ii.Next) {
                if (w.order < ii.Value.order) {
                    foundItem = ii;
                    break;
                }
            }
            if (foundItem != null) {
                // we're pointing to an element to put our element before
                m_workQueue.AddBefore(foundItem, w);
            }
            else {
                // just put it on the end
                m_workQueue.AddLast(w);
            }
            */
            m_workQueue.AddLast(w);
        }
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
        int now = System.Environment.TickCount;
        DoLaterBase found = null;
        while ((totalCost < maximumCost) && (totalCounter > 0) && (m_workQueue.Count > 0)) {
            now = System.Environment.TickCount;
            found = null;
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

    public OMVSD.OSDMap GetDisplayable() {
        OMVSD.OSDMap aMap = new OMVSD.OSDMap();
        aMap.Add("Name", new OMVSD.OSDString(this.Name));
        aMap.Add("Total", new OMVSD.OSDInteger((int)this.TotalQueued));
        aMap.Add("Current", new OMVSD.OSDInteger((int)this.CurrentQueued));
        return aMap;
    }
}
}
