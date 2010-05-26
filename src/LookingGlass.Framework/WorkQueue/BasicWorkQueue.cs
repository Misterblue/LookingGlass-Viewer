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
using System.Threading;
using LookingGlass.Framework.Logging;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.WorkQueue {
    // An odd mish mash of dynamic and static. The idea is that different work
    // queues can be build (priorities, ...). For the moment, they are all
    // here using several static methods that implement the redo and scheduling.
public class BasicWorkQueue : IWorkQueue {
    // IWorkQueue.TotalQueued()
    private long m_totalRequests = 0;
    public long TotalQueued { get { return m_totalRequests; } }

    // IWorkQueue.Name()
    private string m_queueName = "";
    public string Name { get { return m_queueName; } }

    private Queue<DoLaterBase> m_workItems;
    private int m_activeWorkProcessors;
    public int ActiveWorkProcessors { get { return m_activeWorkProcessors; } }
    private int m_workProcessorsMax = 10;
    public int MaxWorkProcessors { get { return m_workProcessorsMax; } set { m_workProcessorsMax = value; } }

    // IWorkQueue.CurrentQueued()
    public long CurrentQueued { get { return (long)m_workItems.Count; } }

    public BasicWorkQueue(string nam) {
        m_workItems = new Queue<DoLaterBase>();
        m_queueName = nam;
        m_totalRequests = 0;
        WorkQueueManager.Instance.Register(this);
        if (LookingGlassBase.Instance.AppParams.HasParameter("Framework.WorkQueue.MaxThreads")) {
            m_workProcessorsMax = LookingGlassBase.Instance.AppParams.ParamInt("Framework.WorkQueue.MaxThreads");
        }
        if (LookingGlassBase.Instance.AppParams.HasParameter(m_queueName+".WorkQueue.MaxThreads")) {
            m_workProcessorsMax = LookingGlassBase.Instance.AppParams.ParamInt(m_queueName+".WorkQueue.MaxThreads");
        }
        lock (m_doEvenLater) {
            if (m_doEvenLaterThread == null) {
                m_doEvenLaterThread = new Thread(DoItEventLaterProcessing);
                m_doEvenLaterThread.Name = "DoEvenLaterProcessing" + "." + m_queueName;
                LogManager.Log.Log(LogLevel.DINIT, "Starting do even later thread for '{0}'", m_queueName);
                m_doEvenLaterThread.Start();
            }
        }
    }

    // IWorkQueue.DoLater()
    public void DoLater(DoLaterBase w){
        w.containingClass = this;
        w.remainingWait = 0;    // the first time through, do it now
        w.timesRequeued = 0;
        AddWorkItemToQueue(w);
    }

    // Doing the work didn't work the first time so we again add it to the queue
    public void DoLaterRequeue(DoLaterBase w){
        AddWorkItemToQueue(w);
    }

    /// <summary>
    /// Entry which doesn't force the caller to create an
    /// instance of a DoLaterBase class but to use a delegate. The calling sequence
    /// would be something like:
    /// <pre>
    ///     Object[] parms = { localParam1, localParam2, ...};
    ///     m_workQueue.DoLater(CallbackRoutine, parms);
    /// </pre>
    /// </summary>
    /// <param name="dlcb"></param>
    public void DoLater(DoLaterCallback dlcb, Object parms) {
        this.DoLater(new DoLaterDelegateCaller(dlcb, parms));
    }

    // do the item but do  the delay first. This puts it in the wait queue and it will
    // get done after the work item delay.
    public void DoLaterInitialDelay(DoLaterCallback dlcb, Object parms) {
        DoLaterBase w = new DoLaterDelegateCaller(dlcb, parms);
        w.containingClass = this;
        w.remainingWait = 0;    // the first time through, do it now
        w.timesRequeued = 0;
        DoItEvenLater(w);

    }

    public void DoLater(float priority, DoLaterCallback dlcb, Object parms) {
        DoLaterBase newDoer = new DoLaterDelegateCaller(dlcb, parms);
        newDoer.priority = priority;
        this.DoLater(newDoer);
    }

    private class DoLaterDelegateCaller : DoLaterBase {
        DoLaterCallback m_dlcb;
        Object m_parameters;
        public DoLaterDelegateCaller(DoLaterCallback dlcb, Object parms) {
            m_dlcb = dlcb;
            m_parameters = parms;
        }
        public override bool DoIt() {
            return m_dlcb(this, m_parameters);
        }
    }

    // Add a new work item to the work queue. If we don't have the maximum number
    // of worker threads already working on the queue, start a new thread from
    // the pool to empty the queue.
    private void AddWorkItemToQueue(DoLaterBase w) {
        if ((m_totalRequests++ % 100) == 0) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: Queuing, c={1}", m_queueName, m_totalRequests);
        }
        lock (m_workItems) {
            m_workItems.Enqueue(w);
            if (m_activeWorkProcessors < m_workProcessorsMax) {
                m_activeWorkProcessors++;
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.DoWork), null);
                // ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.DoWork), null);
            }
        }
    }

    // Worker threads come here to take items off the work queue.
    // Multiple threads loop around here taking items off the work queue and
    //   processing them. When there are no more things to do, the threads
    //   return which puts them back in the thread pool.
    private void DoWork(object x) {
        while (m_workItems.Count > 0) {
            DoLaterBase w = null;
            lock (m_workItems) {
                if (m_workItems.Count > 0) {
                    w = m_workItems.Dequeue();
                }
            }
            if (w != null) {
                try {
                    if (!w.DoIt()) {
                        // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: DoWork: DoEvenLater", m_queueName);
                        DoItEvenLater(w);
                    }
                }
                catch (Exception e) {
                    LogManager.Log.Log(LogLevel.DBADERROR, "{0}.DoLater: DoWork: EXCEPTION: {1}", 
                                m_queueName, e);
                    // we drop the work item in  the belief that it will exception again next time
                }
            }
        }
        m_activeWorkProcessors--;
    }

    /// <summary>
    /// A horrible kludge (there has to be a standard library for this) that
    /// captures one thread to do the waiting and requeuing for DoLaterBase objects.
    /// If an DoLaterBase object cannot be completed now, it has a time interval to
    /// wait before trying again. This routine puts all the waiting objects into
    /// a list and counts down their wait time. When wait time is up, the object
    /// is requeued into the ThreadPool.
    /// This is done this way because we only want one thread hanging in here
    /// doing the Thread.Sleep(). If I just did a sleep for each delayed object,
    /// we could have a situation where all the threads from the pool are waiting.
    /// </summary>
    private List<DoLaterBase> m_doEvenLater = new List<DoLaterBase>();
    private Thread m_doEvenLaterThread = null;
    private void DoItEvenLater(DoLaterBase w) {
        w.timesRequeued++;
        lock (m_doEvenLater) {
            int nextTime = Math.Min(w.requeueWait * w.timesRequeued, 10000);
            nextTime = Math.Max(nextTime, 100);     // never less than this
            w.remainingWait = (System.Environment.TickCount  & 0x3fffffff) + nextTime;
            m_doEvenLater.Add(w);
        }
    }

    // Thread that keeps going around and processing the thing queued from long ago
    private void DoItEventLaterProcessing() {
        while (LookingGlassBase.Instance.KeepRunning) {
            List<DoLaterBase> doneWaiting = null;
            int sleepTime = 200;
            int now = System.Environment.TickCount & 0x3fffffff;
            lock (m_doEvenLater) {
                if (m_doEvenLater.Count > 0) {
                    // remove the last waiting time from each waiter
                    // if waiting is up, remember which ones to remove
                    foreach (DoLaterBase ii in m_doEvenLater) {
                        if (ii.remainingWait < now) {
                            if (doneWaiting == null) doneWaiting = new List<DoLaterBase>();
                            doneWaiting.Add(ii);
                        }
                    }
                    // remove and requeue the ones done waiting
                    if (doneWaiting != null) {
                        foreach (DoLaterBase jj in doneWaiting) {
                            m_doEvenLater.Remove(jj);
                        }
                    }
                }
                // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "DoEvenLater: Removing {0} from list of size {1}",
                //             doneWaiting.Count, m_doEvenLater.Count);
                if (m_doEvenLater.Count > 0) {
                    // find how much time to wait for the remaining
                    sleepTime = int.MaxValue;
                    foreach (DoLaterBase jj in m_doEvenLater) {
                        sleepTime = Math.Min(sleepTime, jj.remainingWait);
                    }
                    sleepTime -= now;
                    if (sleepTime < 0) sleepTime = 100;
                }
            }
            // if there are some things done waiting, let them free outside the lock
            if (doneWaiting != null) {
                foreach (DoLaterBase ll in doneWaiting) {
                    ((BasicWorkQueue)ll.containingClass).DoLaterRequeue(ll);
                }
                doneWaiting.Clear();
                doneWaiting = null;
            }
            if (sleepTime > 0) {
                Thread.Sleep(sleepTime);
            }
        }
    }
    
    public OMVSD.OSDMap GetDisplayable() {
        OMVSD.OSDMap aMap = new OMVSD.OSDMap();
        aMap.Add("Name", new OMVSD.OSDString(this.Name));
        aMap.Add("Total", new OMVSD.OSDInteger((int)this.TotalQueued));
        aMap.Add("Current", new OMVSD.OSDInteger((int)this.CurrentQueued));
        aMap.Add("Later", new OMVSD.OSDInteger(m_doEvenLater.Count));
        aMap.Add("Active", new OMVSD.OSDInteger(this.ActiveWorkProcessors));
        // Logging.LogManager.Log.Log(LogLevel.DRESTDETAIL, "BasicWorkQueue: GetDisplayable: out={0}", aMap.ToString());
        return aMap;
    }
}
}
