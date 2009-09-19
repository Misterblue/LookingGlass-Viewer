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
using LookingGlass.Framework.WorkQueue;

namespace LookingGlass.World {

public enum RegionStateCode : uint {
    None = 0,
    Uninitialized = 1 << 0,
    Connected =     1 << 1, // we've heard about the region but have not received 'connected' message
    Online =        1 << 2, // fully connected and running
    Disconnected =  1 << 3, // we lost the connection. It's here but we can't talk to it
    LowRez =        1 << 4, // a disconnected region that's here as a low rez representation
    ShuttingDown =  1 << 5, // region is shutting down
    Down =          1 << 6, // disconnected and probably getting freed
}

public delegate void RegionStateChangedCallback(RegionStateCode code);
public delegate void RegionStateCheckCallback();

public class RegionState {
    public event RegionStateChangedCallback OnStateChanged;

    // one work queue for all the state update work
    private static BasicWorkQueue m_stateWork = null;

    private RegionStateCode m_regionState;
    private Object m_regionStateLock;

    public RegionStateCode State {
        get { return m_regionState; }
        set {
            RegionStateCode newState = value;
            lock (m_regionStateLock) {
                if (m_regionState != newState) {
                    m_regionState = newState;
                    // if (OnStateChanged != null) OnStateChanged(m_regionState);
                    if (OnStateChanged != null) {
                        // queue the state changed event to happen on another thread
                        m_stateWork.DoLater(new OnStateChangedLater(OnStateChanged, m_regionState));
                    }
                }
            }
        }
    }

    private class OnStateChangedLater : DoLaterBase {
        RegionStateChangedCallback m_callback;
        RegionStateCode m_code;
        public OnStateChangedLater(RegionStateChangedCallback c, RegionStateCode r) {
            m_callback = c;
            m_code = r;
        }
        public override bool DoIt() {
            m_callback(m_code);
            return true;
        }
    }

    public RegionState() {
        if (m_stateWork == null) {
            m_stateWork = new BasicWorkQueue("OnStateChanged");
        }
        m_regionState = RegionStateCode.Uninitialized;
        m_regionStateLock = new Object();
    }

    /// <summary>
    /// Return 'true' if the region is online, running and fully usable.
    /// If 'false' is returned, the region is either being initialized (after
    /// being created but before we're received the 'connected' message)
    /// or is being shutdown.
    /// </summary>
    public bool isOnline {
        get { return ((m_regionState & (RegionStateCode.Online)) != 0); }
    }

    // Will perform the callback if we're not online. The callback is done while
    // the state is locked thus preventing race conditions.
    // Returns 'true' if we called the delegate
    public bool IfNotOnline(RegionStateCheckCallback rscc) {
        bool ret = false;
        lock (m_regionStateLock) {
            if (!isOnline) {
                rscc();
                ret = true;
            }
        }
        return ret;
    }
    // Will perform the callback if we're online. The callback is done while
    // the state is locked thus preventing race conditions.
    // Returns 'true' if we called the delegate
    public bool IfOnline(RegionStateCheckCallback rscc) {
        bool ret = false;
        lock (m_regionStateLock) {
            if (isOnline) {
                rscc();
                ret = true;
            }
        }
        return ret;
    }
}

}
