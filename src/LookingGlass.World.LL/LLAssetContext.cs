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
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LookingGlass.Comm;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using OMVI = OpenMetaverse.Imaging;

namespace LookingGlass.World.LL {
    /// <summary>
    /// Linkage between asset requests and the underlying asset server.
    /// This uses the OpenMetaverse connection to the server to load the
    /// asset (texture) into the filesystem.
    /// </summary>
public sealed class LLAssetContext : AssetContextBase {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // private const string WorldIDMatch = "^(...)(...)(..)-(.)(.*)$";
    // private const string WorldIDReplace = "Texture/$1/$2/$3$4/$1$2$3-$4$5";

    public LLAssetContext() : base() {
        m_Name = "Unknown";
    }

    public LLAssetContext(string name) : base() {
        m_Name = name;
    }

    /// <summary>
    /// Initializing the asset context is a two step process: create it and
    /// then initialize it. This is because we don't have everything we need
    /// when it is first created.
    /// </summary>
    /// <param name="gclient"></param>
    /// <param name="maxrequests"></param>
    public override void InitializeContextFinish() {
        m_comm.GridClient.Settings.MAX_CONCURRENT_TEXTURE_DOWNLOADS = m_maxRequests;
        // m_client.Assets.OnImageReceived += new OMV.AssetManager.ImageReceivedCallback(OnImageReceived);
        m_comm.GridClient.Assets.Cache.ComputeAssetCacheFilename = ComputeTextureFilename;

        m_comm.CommStatistics().Add("TexturesWaitingFor",
            delegate(string xx) { return new OMVSD.OSDString(m_waiting.Count.ToString()); },
            "Number of unique textures requests outstanding");
        m_comm.CommStatistics().Add("CurrentOutstandingTextureRequests",
            delegate(string xx) { return new OMVSD.OSDString(m_currentOutstandingTextureRequests.ToString()); },
            "Number of texture requests that have been passed to libomv");
    }

    public string ComputeTextureFilename(string cacheDir, OMV.UUID textureID) {
        EntityNameLL entName = EntityNameLL.ConvertTextureWorldIDToEntityName(this, textureID);
        string textureFilename = Path.Combine(CacheDirBase, entName.CacheFilename);
        // m_log.Log(LogLevel.DTEXTUREDETAIL, "ComputeTextureFilename: " + textureFilename);

        // make sure the recieving directory is there for the texture
        MakeParentDirectoriesExist(textureFilename);

        // m_log.Log(LogLevel.DTEXTUREDETAIL, "ComputerTextureFilename: returning " + textureFilename);
        return textureFilename;
    }

    /// <summary>
    /// based only on the name of the texture entity, decide if it's mine.
    /// Here we check for the name of our asset context at the beginning of the
    /// name (where the host part is)
    /// </summary>
    /// <param name="textureEntityName"></param>
    /// <returns></returns>
    public override bool isTextureOwner(EntityName textureEntityName) {
        return textureEntityName.Name.StartsWith(m_Name + EntityName.PartSeparator);
    }

    /// <summary>
    /// request a texture file to appear in the cache.
    /// </summary>
    /// <param name="ent">Entity the provides context for the request (asset server)</param>
    /// <param name="worldID">The world ID for the requested texture</param>
    /// <param name="finishCall">Where to call when the texture is in the cache</param>
    // TODO: if we get a request for the same texture by two different routines
    // at the same time, this doesn't do all the callbacks
    // To enable this feature, remove the dictionary and checks for already fetching
    public override void DoTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finishCall) {
        EntityNameLL textureEnt = new EntityNameLL(textureEntityName);
        string worldID = textureEnt.EntityPart;
        OMV.UUID binID = new OMV.UUID(worldID);

        // do we already have the file?
        string textureFilename = Path.Combine(CacheDirBase, textureEnt.CacheFilename);
        lock (FileSystemAccessLock) {
            if (File.Exists(textureFilename)) {
                m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Texture file alreayd exists for " + worldID);
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
    private bool ThrottleTextureMakeRequest(DoLaterBase qInstance, Object obinID) {
        OMV.UUID binID = (OMV.UUID)obinID;
        m_comm.GridClient.Assets.RequestImage(binID, OMV.ImageType.Normal, 101300f, 0, 0, OnACDownloadFinished, false);
        return true;
    }
    private void ThrottleTextureRequestsComplete() {
        m_currentOutstandingTextureRequests--;
        ThrottleTextureRequestsCheck();
    }

    #endregion THROTTLE TEXTURES

    // Used for texture pipeline
    // returns flag = true if texture was sucessfully downloaded
    private void OnACDownloadFinished(OMV.TextureRequestState state, OMV.Assets.AssetTexture assetTexture) {
        ProcessDownloadFinished(state, assetTexture);
    }

    /// <summary>
    /// Implementation routine that the parent class uses to create communication specific entity
    /// names.
    /// </summary>
    /// <param name="acb"></param>
    /// <param name="at"></param>
    protected override EntityName ConvertToEntityName(AssetContextBase acb, string worldID) {
        return EntityNameLL.ConvertTextureWorldIDToEntityName(acb, worldID);
    }
    protected override void CompletionWorkComplete() {
        ThrottleTextureRequestsComplete();
    }

    private void OnImageReceived(OMV.ImageDownload cntl, OMV.Assets.AssetTexture textureID) {
    }

    public override void Dispose() {
        base.Dispose();
    }

}
}
