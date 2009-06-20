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

namespace LookingGlass.Framework.Statistics {
    class IntervalCounter : StatCounter, IIntervalCounter {
        public IntervalCounter(string name) : base(name) {
        }

        // called when entering a timed region
        public int In() {
            return System.Environment.TickCount;
        }

        // called when exiting a timed region
        private Object lockThing = new Object();
        public void Out(int inValue) {
            lock (lockThing) {
                int period = System.Environment.TickCount - inValue;
                m_total += period;
                m_last = period;
                m_low = Math.Min(m_low, period);
                m_high = Math.Max(m_high, period);
                m_count++;
            }
        }

        private long m_total = 0;
        public long Total { get { return m_total; } }  // total amount of time spent (in ticks)

        private long m_last = 0;
        public long Last { get { return m_last; } }   // the length of the last period (in ticks)

        // the average period
        public long Average { 
            get {
                return m_total / m_count;
            } 
        }

        private long m_high = 0;
        public long High { get { return m_high; } }   // the largest period

        private long m_low = 0;
        public long Low { get { return m_low; } }    // the smallest period
    }
}
