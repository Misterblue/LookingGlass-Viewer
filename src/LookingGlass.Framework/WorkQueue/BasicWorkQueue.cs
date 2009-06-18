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

namespace LookingGlass.Framework.WorkQueue {
    // An odd mish mash of dynamic and static. The idea is that different work
    // queues can be build (priorities, ...). For the moment, they are all
    // here using several static methods that implement the redo and scheduling.
public class BasicWorkQueue : IWorkQueue {
    private long m_totalRequests = 0;
    public long TotalQueued { get { return m_totalRequests; } }

    private string m_queueName = "";
    public string Name { get { return m_queueName; } }

    private long m_currentRequests;
    public long CurrentQueued { get { return (long)(m_currentRequests + doEvenLater.Count); } }

    public BasicWorkQueue(string nam) {
        m_queueName = nam;
        m_totalRequests = 0;
        WorkQueueManager.Register(this);
    }

    public void DoLater(DoLaterBase w){
        m_currentRequests++;
        if ((m_totalRequests++ % 100) == 0) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}DoLater: Queuing, c={1}", m_queueName, m_totalRequests);
        }
        w.containingClass = this;
        ThreadPool.QueueUserWorkItem(new WaitCallback(this.DoWork), (object)w);
    }

    private void DoWork(object w) {
        DoLaterBase ww = (DoLaterBase)w;
        m_currentRequests--;
        if (!ww.DoIt()) {
            LogManager.Log.Log(LogLevel.DRENDERDETAIL, "{0}.DoLater: DoWork: DoEvenLater", m_queueName);
            DoItEvenLater(ww);
        }
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
    private static List<DoLaterBase> doEvenLater = new List<DoLaterBase>();
    private static Thread doEvenLaterThread = null;
    private static void DoItEvenLater(DoLaterBase w) {
        lock (doEvenLater) {
            w.remainingWait = System.Environment.TickCount + w.requeueWait;    // wait at least the total time
            doEvenLater.Add(w);
            if (doEvenLaterThread == null) {    // is there another thread doing the requeuing?
                doEvenLaterThread = Thread.CurrentThread;   // no, looks like I'm stuck
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, "DoItEvenLater: becoming the one thread");
            }
        }

        // if it's me, I have the job of empting the queue
        while (doEvenLaterThread != null && doEvenLaterThread == Thread.CurrentThread) {
            List<DoLaterBase> doneWaiting = null;
            int sleepTime = 0;
            int now = System.Environment.TickCount;
            lock (doEvenLater) {    // protects both doEvenLater and doEvenLaterThread
                if (doEvenLater.Count > 0) {
                    // remove the last waiting time from each waiter
                    // if waiting is up, remember which ones to remove
                    foreach (DoLaterBase ii in doEvenLater) {
                        if (ii.remainingWait < now) {
                            if (doneWaiting == null) doneWaiting = new List<DoLaterBase>();
                            doneWaiting.Add(ii);
                        }
                    }
                    // remove and requeue the ones done waiting
                    if (doneWaiting != null) {
                        foreach (DoLaterBase jj in doneWaiting) {
                            doEvenLater.Remove(jj);
                        }
                    }
                }
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, "DoEvenLater: Removing {0} from list of size {1}",
                            doneWaiting.Count, doEvenLater.Count);
                if (doEvenLater.Count > 0) {
                    // find how much time to wait for the remaining
                    sleepTime = int.MaxValue;
                    foreach (DoLaterBase jj in doEvenLater) {
                        sleepTime = Math.Min(sleepTime, jj.remainingWait - now);
                    }
                }
                else {
                    // if no more to wait on, this thread is free
                    doEvenLaterThread = null;
                }
            }
            // if there are some things done waiting, let them free outside the lock
            if (doneWaiting != null) {
                foreach (DoLaterBase ll in doneWaiting) {
                    ((BasicWorkQueue)ll.containingClass).DoLater(ll);
                }
                doneWaiting.Clear();
                doneWaiting = null;
            }
            // if this thread is still working on sleeping, do the sleeping
            if (doEvenLaterThread == Thread.CurrentThread) {
                // wait the remaining time
                LogManager.Log.Log(LogLevel.DRENDERDETAIL, "DoEvenLater: Sleep for {0}", sleepTime);
                if (sleepTime > 0) {
                    Thread.Sleep(sleepTime);
                }
            }
        }
    }
}
}
