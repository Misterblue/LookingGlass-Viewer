/* Copyright (c) 2009 Robert Adams
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
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using LookingGlass.Comm;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;

namespace LookingGlass.World.OS {
public class OSAssetContextV1 : AssetContextBase {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    string m_basePath;
    string m_proxyPath = null;

    // Some of the asset servers will return just the data if you put 'data' on the
    // end of the URL. That is happening if this is 'true'.
    bool m_dataFetch = false;

    public OSAssetContextV1() : base() {
        m_Name = "Unknown";
    }

    public OSAssetContextV1(string name) : base() {
        m_Name = name;
    }

    public override void InitializeContextFinish() {
        m_basePath = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.V1");
        string requestFormat = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.V1.Request");
        if (requestFormat != null) {
            // does the parameters specify the 'data' binary data request?
            if (requestFormat.ToLower().Equals("data")) {
                m_dataFetch = true;
            }
        }
        if (m_basePath == null) {
            m_log.Log(LogLevel.DBADERROR, "OSAssetContextV1::InitializeContextFinish: NOT BASE PATH SPECIFIED: NOT INITIALIZING");
            return;
        }
        while (m_basePath.EndsWith("/")) {
            m_basePath = m_basePath.Substring(0, m_basePath.Length-1);
        }
        m_proxyPath = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.Proxy");
        try {
            string maxRequests = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.MaxRequests");
            if (maxRequests == null) {
                m_maxOutstandingTextureRequests = 4;
            }
            else {
                m_maxOutstandingTextureRequests = Int32.Parse(maxRequests);
            }
        }
        catch {
            m_maxOutstandingTextureRequests = 4;
        }
        if (m_proxyPath != null && m_proxyPath.Length == 0) m_proxyPath = null;
        m_log.Log(LogLevel.DINIT, "InitializeContextFinish: base={0}, proxy={1}", m_basePath,
                        m_proxyPath == null ? "NULL" : m_proxyPath);
        return;
    }
    
    // if it starts with our region name, it must be ours
    public override bool isTextureOwner(EntityName textureEntityName) {
        return textureEntityName.Name.StartsWith(m_Name + EntityName.PartSeparator);
    }

    public override void DoTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finishCall) {
        EntityNameLL textureEnt = new EntityNameLL(textureEntityName);
        string worldID = textureEnt.EntityPart;
        OMV.UUID binID = new OMV.UUID(worldID);

        if (m_basePath == null) return;

        // do we already have the file?
        string textureFilename = Path.Combine(CacheDirBase, textureEnt.CacheFilename);
        lock (FileSystemAccessLock) {
            if (File.Exists(textureFilename)) {
                m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Texture file already exists for " + worldID);
                bool hasTransparancy = CheckTextureFileForTransparancy(textureFilename);
                // make the callback happen on a new thread so things don't get tangled (caller getting the callback)
                Object[] finishCallParams = { finishCall, textureEntityName.Name, hasTransparancy };
                m_completionWork.DoLater(FinishCallDoLater, finishCallParams);
                // m_completionWork.DoLater(new FinishCallDoLater(finishCall, textureEntityName.Name, hasTransparancy));
            }
            else {
                bool sendRequest = false;
                lock (m_waiting) {
                    // if this is already being requested, don't waste our time
                    if (m_waiting.ContainsKey(binID)) {
                        m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Already waiting for " + worldID);
                    }
                    else {
                        WaitingInfo wi = new WaitingInfo(binID, finishCall);
                        wi.filename = textureFilename;
                        wi.type = typ;
                        m_waiting.Add(binID, wi);
                        sendRequest = true;
                    }
                }
                if (sendRequest) {
                    // this is here because RequestTexture might immediately call the callback
                    //   and we should be outside the lock
                    m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Requesting: " + textureEntityName);
                    // m_texturePipe.RequestTexture(binID, OMV.ImageType.Normal, 50f, 0, 0, OnACDownloadFinished, false);
                    // m_comm.GridClient.Assets.RequestImage(binID, OMV.ImageType.Normal, 101300f, 0, 0, OnACDownloadFinished, false);
                    // m_comm.GridClient.Assets.RequestImage(binID, OMV.ImageType.Normal, 50f, 0, 0, OnACDownloadFinished, false);
                    ThrottleTextureRequests(binID);
                }
            }
        }
        return;
    }

    #region THROTTLE TEXTURES
    // some routines to throttle the number of outstand textures requetst to see if 
    //  libomv is getting overwhelmed by thousands of requests
    Queue<OMV.UUID> m_textureQueue = new Queue<OpenMetaverse.UUID>();
    int m_maxOutstandingTextureRequests = 4;
    int m_currentOutstandingTextureRequests = 0;
    BasicWorkQueue m_doThrottledTextureRequest = new BasicWorkQueue("ThrottledTexture");
    private void ThrottleTextureRequests(OMV.UUID binID) {
        lock (m_textureQueue) {
            m_textureQueue.Enqueue(binID);
        }
        ThrottleTextureRequestsCheck();
    }
    private void ThrottleTextureRequestsCheck() {
        OMV.UUID binID = OMV.UUID.Zero;
        lock (m_textureQueue) {
            if (m_textureQueue.Count > 0 && m_currentOutstandingTextureRequests < m_maxOutstandingTextureRequests) {
                m_currentOutstandingTextureRequests++;
                binID = m_textureQueue.Dequeue();
            }
        }
        if (binID != OMV.UUID.Zero) {
            m_doThrottledTextureRequest.DoLater(ThrottleTextureMakeRequest, binID);
        }
    }

    /// <summary>
    /// On our own thread, make  the synchronous texture request from the asset server
    /// </summary>
    /// <param name="qInstance"></param>
    /// <param name="obinID"></param>
    /// <returns></returns>
    private bool ThrottleTextureMakeRequest(DoLaterBase qInstance, Object obinID) {
        OMV.UUID binID = (OMV.UUID)obinID;

        Uri assetPath = new Uri(m_basePath + "/assets/" + binID.ToString() + (m_dataFetch ? "/data" : ""));
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(assetPath);
        request.MaximumAutomaticRedirections = 4;
        request.MaximumResponseHeadersLength = 4;
        request.Timeout = 30000;    // 30 second timeout
        if (m_proxyPath != null) {
            // configure proxy if necessary
            WebProxy myProxy = new WebProxy();
            myProxy.Address = new Uri(m_proxyPath);
            request.Proxy = myProxy;
        }
        try {
            m_log.Log(LogLevel.DCOMMDETAIL, "ThrottleTextureMakeRequest: requesting '{0}'", assetPath);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                m_log.Log(LogLevel.DCOMMDETAIL, "ThrottleTextureMakeRequest: request returned. resp={0}, l={1}",
                            response.StatusCode, response.ContentLength);
                if (response.StatusCode == HttpStatusCode.OK) {
                    using (Stream receiveStream = response.GetResponseStream()) {
                        OMV.Assets.AssetTexture at;
                        if (m_dataFetch) {
                            // we're getting raw binary data
                            byte[] textureBuff = new byte[response.ContentLength];
                            receiveStream.Read(textureBuff, 0, (int)response.ContentLength);
                            at = new OMV.Assets.AssetTexture(binID, textureBuff);
                        }
                        else {
                            // receiving a serialized package
                            XmlSerializer xserial = new XmlSerializer(typeof(OpenSim.Framework.AssetBase));
                            OpenSim.Framework.AssetBase abase = (OpenSim.Framework.AssetBase)xserial.Deserialize(receiveStream);
                            at = new OMV.Assets.AssetTexture(binID, abase.Data);
                        }
                        ProcessDownloadFinished(OMV.TextureRequestState.Finished, at);
                    }
                }
                else {
                    OMV.Assets.AssetTexture at = new OMV.Assets.AssetTexture(binID, new byte[0]);
                    ProcessDownloadFinished(OMV.TextureRequestState.NotFound, at);
                }
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Error fetching asset: {0}", e);
            OMV.Assets.AssetTexture at = new OMV.Assets.AssetTexture(binID, new byte[0]);
            ProcessDownloadFinished(OMV.TextureRequestState.NotFound, at);
        }
        return true;
    }

    protected override void  CompletionWorkComplete() {
        ThrottleTextureRequestsComplete();
     	base.CompletionWorkComplete();
    }

    private void ThrottleTextureRequestsComplete() {
        m_currentOutstandingTextureRequests--;
        ThrottleTextureRequestsCheck();
    }
    #endregion THROTTLE TEXTURES

}
}
