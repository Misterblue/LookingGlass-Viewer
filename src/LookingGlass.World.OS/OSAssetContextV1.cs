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

    public OSAssetContextV1() : base() {
        m_Name = "Unknown";
    }

    public OSAssetContextV1(string name) : base() {
        m_Name = name;
    }

    public override void InitializeContextFinish() {
        m_basePath = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.V1");
        m_proxyPath = World.Instance.Grids.GridParameter(Grids.Current, "OS.AssetServer.Proxy");
        return;
    }
    
    public override bool isTextureOwner(EntityName textureEntityName) {
        return false;
    }

    public override void DoTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finishCall) {
        EntityNameLL textureEnt = new EntityNameLL(textureEntityName);
        string worldID = textureEnt.EntityPart;
        OMV.UUID binID = new OMV.UUID(worldID);

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

        Uri assetPath = new Uri(m_basePath + "/" + binID.ToString() + "/texture");
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
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        if (response.StatusCode == HttpStatusCode.OK) {
            Stream receiveStream = response.GetResponseStream();
            byte[] textureBuff = new byte[response.ContentLength];
            receiveStream.Read(textureBuff, 0, (int)response.ContentLength);
            OMV.Assets.AssetTexture at = new OMV.Assets.AssetTexture(binID, textureBuff);
            ProcessDownloadFinished(OMV.TextureRequestState.Finished, at);
        }
        else {
            OMV.Assets.AssetTexture at = new OMV.Assets.AssetTexture(binID, new byte[0]);
            ProcessDownloadFinished(OMV.TextureRequestState.NotFound, null);
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
