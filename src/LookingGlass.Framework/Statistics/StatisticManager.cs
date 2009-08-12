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
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.Statistics {
    /// <summary>
    /// Manages a group of counters and presents one REST interface to read
    /// this group of counters.
    /// </summary>
public class StatisticManager : IDisplayable {

    private List<ICounter> m_counters = new List<ICounter>();

    public StatisticManager(string statisticGroupName) {
    }

    public ICounter GetCounter(string counterName) {
        ICounter newCounter = new StatCounter(counterName);
        m_counters.Add(newCounter);
        return newCounter;
    }

    public IIntervalCounter GetIntervalCounter(string counterName) {
        IIntervalCounter newCounter = new IntervalCounter(counterName);
        m_counters.Add(newCounter);
        return newCounter;
    }

    /// <summary>
    /// A statistics collection returns an OSD structure which is a map
    /// of maps. The top level map are the individual counters and
    /// their value is a map of the variables that make up the counter.
    /// </summary>
    /// <returns></returns>
    public OMVSD.OSDMap GetDisplayable() {
        OMVSD.OSDMap values = new OMVSD.OSDMap();
        foreach (ICounter cntr in m_counters) {
            try {
                OMVSD.OSDMap ivals = new OMVSD.OSDMap();
                ivals.Add("count", new OMVSD.OSDInteger((int)cntr.Count));
                if (cntr is IIntervalCounter) {
                    IIntervalCounter icntr = (IIntervalCounter)cntr;
                    ivals.Add("average", new OMVSD.OSDInteger((int)icntr.Average));
                    ivals.Add("low", new OMVSD.OSDInteger((int)icntr.Low));
                    ivals.Add("high", new OMVSD.OSDInteger((int)icntr.High));
                    ivals.Add("last", new OMVSD.OSDInteger((int)icntr.Last));
                    ivals.Add("total", new OMVSD.OSDInteger((int)icntr.Total));
                }
                values.Add(cntr.Name, ivals);
            }
            catch (Exception e) {
                Logging.LogManager.Log.Log(LookingGlass.Framework.Logging.LogLevel.DBADERROR,
                    "FAILURE getting Displayable value: n={0}, {1}", cntr.Name, e.ToString());
            }
        }
        return values;
    }

}
}
