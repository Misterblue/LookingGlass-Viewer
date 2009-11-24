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
using System.IO;
using System.Text;
using System.Threading;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Statistics;
using OMV = OpenMetaverse;

namespace LookingGlass.Comm.LLLP {

public delegate void AssetFetcherCompletionCallback(OMV.UUID id, string filename);

    /// <summary>
    /// WORK IN PROGRESS.
    /// This is not complete and not used anywhere yet. Someday LG will have to generalize
    /// the asset fetching. Currently, texture fetching is in LLAssetContext but, even though
    /// it is LL specific, the protocol implementation is tied to libomv. This could change.
    /// Thus, might want to pull out the protocol side of asset fetching so it can be
    /// plugged also.  This routine was a start of that and this code could either grow
    /// or be thrown out.
    /// </summary>
public class AssetFetcher {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // number of texture fetchings we set running in parallel
    public int OutStandingRequests {
        get { return m_requests.Count; }
    }

    private int m_maxParallelRequests = 5;
    public int MaxParallelRequests {
        get { return m_maxParallelRequests; }
        set { m_maxParallelRequests = value; }
    }

    public StatisticManager m_stats;
    public ICounter m_totalRequests;         // count of total requests
    public ICounter m_duplicateRequests;     // count of requests for things we're already queued for
    public ICounter m_requestsForExisting;   // count of requests for assets that are already in files

    struct TRequest {
        public OMV.UUID ID;
        public string Filename;
        public int QueueTime;
        // public int RequestTime;
        public OMV.AssetType Type;
        public AssetFetcherCompletionCallback DoneCall;
    };

    private Dictionary<string, TRequest> m_requests;
    private List<TRequest> m_outstandingRequests;
    private OMV.GridClient m_client;

    public AssetFetcher(OMV.GridClient grid) {
        m_client = grid;
        // m_client.Assets.OnAssetReceived += new OMV.AssetManager.AssetReceivedCallback(Assets_OnAssetReceived);
        m_requests = new Dictionary<string, TRequest>();
        m_outstandingRequests = new List<TRequest>();
        m_stats = new StatisticManager("AssetFetcher");
        m_totalRequests = m_stats.GetCounter("TotalRequests");
        m_duplicateRequests = m_stats.GetCounter("DuplicateRequests");
        m_requestsForExisting = m_stats.GetCounter("RequestsForExistingAsset");
    }

    public void AssetIntoFile(OMV.UUID getID, OMV.AssetType type, string filename, AssetFetcherCompletionCallback doneCall) {
        m_totalRequests.Event();
        if (File.Exists(filename)) {
            m_requestsForExisting.Event();
            // doneCall.BeginInvoke(getID, filename, null, null);
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(Object x) {
            // ThreadPool.UnsafeQueueUserWorkItem((WaitCallback)delegate(Object x) {
                doneCall(getID, filename);
            }, null);


        }
        lock (m_requests) {
            if (!m_requests.ContainsKey(filename)) {
                TRequest treq = new TRequest();
                treq.ID = getID;
                treq.Filename = filename;
                treq.Type = type;
                treq.DoneCall = doneCall;
                treq.QueueTime = System.Environment.TickCount;
                m_requests.Add(filename, treq);
            }
            else {
                m_duplicateRequests.Event();
            }
        }
        PushRequests();
    }

    private void PushRequests() {
        lock (m_requests) {
            if ((m_outstandingRequests.Count < m_maxParallelRequests) && (m_requests.Count > 0)) {
                // there is room for more requests
                // TODO: Move some requests from m_requests to m_outstandingRequests and start the request
            }
        }
    }

    private void Assets_OnAssetReceived() {
    }
}
}
