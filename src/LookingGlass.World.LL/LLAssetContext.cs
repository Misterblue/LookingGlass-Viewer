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
using System.Text.RegularExpressions;
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
    /// <summary>
    /// Linkage between asset requests and the underlying asset server.
    /// This uses the OpenMetaverse connection to the server to load the
    /// asset (texture) into the filesystem.
    /// </summary>
public sealed class LLAssetContext : AssetContextBase {

    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    private OMV.TexturePipeline m_texturePipe;
    private OMV.GridClient m_client;
    private int m_maxRequests;

    // private const string WorldIDMatch = "^(...)(...)(..)-(.)(.*)$";
    // private const string WorldIDReplace = "Texture/$1/$2/$3$4/$1$2$3-$4$5";

    private string m_commName;      // name of the communication module I'm associated with

    private class WaitingInfo : IComparable<WaitingInfo> {
        public OMV.UUID worldID;
        public DownloadFinishedCallback callback;
        public WaitingInfo(OMV.UUID wid, DownloadFinishedCallback cback) {
            worldID = wid;
            callback = cback;
        }
        public int CompareTo(WaitingInfo other) {
            return (worldID.CompareTo(other.worldID));
        }
    }

    private string m_cacheDir;
    public string CacheDir {
        get { return m_cacheDir; }
    }

    private Dictionary<OMV.UUID, WaitingInfo> m_waiting;

    public LLAssetContext() : base() {
        m_Name = "Unknown";
        m_texturePipe = null;
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
    public void InitializeContext(OMV.GridClient gclient, string commName, string cacheDir, int maxrequests) {
        m_waiting = new Dictionary<OMV.UUID, WaitingInfo>();
        m_client = gclient;
        m_commName = commName;
        m_cacheDir = cacheDir;
        m_maxRequests = maxrequests;
        m_client.Settings.MAX_CONCURRENT_TEXTURE_DOWNLOADS = m_maxRequests;
        m_texturePipe = new OMV.TexturePipeline(m_client);
        // m_texturePipe.OnDownloadFinished += new OMV.TexturePipeline.DownloadFinishedCallback(OnACDownloadFinished);
        // m_client.Assets.OnImageReceived += new OMV.AssetManager.ImageReceivedCallback(OnImageReceived);
        m_client.Assets.Cache.ComputeTextureCacheFilename = ComputeTextureFilename;
    }

    public string ComputeTextureFilename(string cacheDir, OMV.UUID textureID) {
        EntityNameLL entName = EntityNameLL.ConvertTextureWorldIDToEntityName(this, textureID);
        string textureFilename = Path.Combine(CacheDir, entName.CacheFilename);
        // m_log.Log(LogLevel.DTEXTUREDETAIL, "ComputeTextureFilename: " + textureFilename);

        // make sure the recieving directory is there for the texture
        // the texture pipeline will do this some day
        string textureDirName = Path.GetDirectoryName(textureFilename);
        if (!Directory.Exists(textureDirName)) {
            Directory.CreateDirectory(textureDirName);
        }
        // m_log.Log(LogLevel.DTEXTUREDETAIL, "ComputerTextureFilename: returning " + textureFilename);
        return textureFilename;
    }

    // we set it in TexturePipeline and then just use that setting
    public override string CacheDirBase {
        get {
            if (m_client != null)
                return m_client.Settings.TEXTURE_CACHE_DIR;
            return null;
        }
    }

    /// <summary>
    /// based only on the name of the texture entity, decide if it's mine.
    /// Here we check for the name of our asset context at the beginning of the
    /// name (where the host part is)
    /// </summary>
    /// <param name="textureEntityName"></param>
    /// <returns></returns>
    public override bool isTextureOwner(string textureEntityName) {
        return textureEntityName.StartsWith(m_Name + EntityName.PartSeparator);
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
    public override void DoTextureLoad(string textureEntityName, DownloadFinishedCallback finishCall) {
        EntityNameLL textureEnt = new EntityNameLL(this, textureEntityName);
        string worldID = textureEnt.ExtractEntityFromEntityName(textureEntityName);
        OMV.UUID binID = new OMV.UUID(worldID);

        // do we already have the file?
        string textureFilename = Path.Combine(CacheDir, textureEnt.CacheFilename);
        if (File.Exists(textureFilename)) {
            m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Texture file alreayd exists for " + worldID);
            finishCall.BeginInvoke(textureEntityName, null, null);
        }
        else {
            bool sendRequest = false;
            lock (m_waiting) {
                // if this is already being requested, don't waste our time
                if (m_waiting.ContainsKey(binID)) {
                    m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Already waiting for " + worldID);
                }
                if (m_texturePipe != null) {
                    m_waiting.Add(binID, new WaitingInfo(binID, finishCall));
                    sendRequest = true;
                }
            }
            if (sendRequest) {
                // this is here because RequestTexture might immediately call the callback
                m_texturePipe.RequestTexture(binID, OMV.ImageType.Normal, 50f, 0, 0, OnACDownloadFinished, false);
                m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Requesting: " + textureEntityName);
            }
        }
        return;
    }

    // Used for texture pipeline
    // returns flag = true if texture was sucessfully downloaded
    private void OnACDownloadFinished(OMV.TextureRequestState state, OMV.Assets.AssetTexture assetTexture) {
        // if texture could not be downloaded, create a fake texture
        OMV.UUID assetWorldID = assetTexture.AssetID;
        if (state == OMV.TextureRequestState.NotFound || state == OMV.TextureRequestState.Timeout) {
            try {
                EntityNameLL tempTexture = EntityNameLL.ConvertTextureWorldIDToEntityName(this, assetWorldID.ToString());
                string tempTextureFilename = Path.Combine(CacheDir, tempTexture.CacheFilename);
                string textureDirName = Path.GetDirectoryName(tempTextureFilename);
                if (!Directory.Exists(textureDirName)) {
                    Directory.CreateDirectory(textureDirName);
                }
                string noTextureFilename = Globals.Configuration.ParamString(m_commName + ".Assets.NoTextureFilename");
                // if we copy the no texture file into the filesystem, we will never retry to
                // fetch the texture. This copy is not a good thing.
                File.Copy(noTextureFilename, tempTextureFilename);
                m_log.Log(LogLevel.DTEXTURE, 
                    "TextureDownload: Texture fetch failed={0}. Using not found texture.", tempTexture.Name);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, 
                    "TextureDownload: Texture fetch failed. Could not create default texture: " + e.ToString());
            }
        }
        List<WaitingInfo> toCall = new List<WaitingInfo>();
        m_log.Log(LogLevel.DTEXTUREDETAIL, "OnACDownloadFinished: Completion for " + assetWorldID.ToString());
        lock (m_waiting) {
            foreach (KeyValuePair<OMV.UUID, WaitingInfo> kvp in m_waiting) {
                if (kvp.Value.worldID == assetWorldID) {
                    m_log.Log(LogLevel.DTEXTUREDETAIL, "OnACDownloadFinished: Found waiting " + kvp.Value.worldID);
                    toCall.Add(kvp.Value);
                }
            }
            // now remove the ones from the list (we cannot remove while transversing the list)
            foreach (WaitingInfo wx in toCall) {
                m_log.Log(LogLevel.DTEXTUREDETAIL, "OnACDownloadFinished: removing from waiting list " + wx.worldID);
                m_waiting.Remove(wx.worldID);
            }
        }
        foreach (WaitingInfo wii in toCall) {
            m_log.Log(LogLevel.DTEXTUREDETAIL, "Download finished callback: " + wii.worldID.ToString());
            EntityName textureEntityName = EntityNameLL.ConvertTextureWorldIDToEntityName(this, assetWorldID);
            wii.callback(textureEntityName.Name);
        }
        toCall.Clear();
        return;
    }

    private void OnImageReceived(OMV.ImageDownload cntl, OMV.Assets.AssetTexture textureID) {
    }

    public override void Dispose() {
        if (m_texturePipe != null) {
            // m_texturePipe.Shutdown();    // pipeline shuts down when disconnected
            m_texturePipe = null;
        }
        base.Dispose();
    }

}
}
