﻿/* Copyright (c) 2008 Robert Adams
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
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.World;
using OMV = OpenMetaverse;
using OMVI = OpenMetaverse.Imaging;

namespace LookingGlass.World.LL {
    /// <summary>
    /// Linkage between asset requests and the underlying asset server.
    /// This uses the OpenMetaverse connection to the server to load the
    /// asset (texture) into the filesystem.
    /// </summary>
public sealed class LLAssetContext : AssetContextBase {

    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    private OMV.GridClient m_client;
    private int m_maxRequests;
    private BasicWorkQueue m_completionWork;

    // private const string WorldIDMatch = "^(...)(...)(..)-(.)(.*)$";
    // private const string WorldIDReplace = "Texture/$1/$2/$3$4/$1$2$3-$4$5";

    private string m_commName;      // name of the communication module I'm associated with

    private class WaitingInfo : IComparable<WaitingInfo> {
        public OMV.UUID worldID;
        public DownloadFinishedCallback callback;
        public string filename;
        public AssetType type;
        public WaitingInfo(OMV.UUID wid, DownloadFinishedCallback cback) {
            worldID = wid;
            callback = cback;
        }
        public int CompareTo(WaitingInfo other) {
            return (worldID.CompareTo(other.worldID));
        }
    }

    private string m_cacheDir;
    public override string CacheDirBase {
        get { return m_cacheDir; }
    }

    private Dictionary<OMV.UUID, WaitingInfo> m_waiting;

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
    public void InitializeContext(OMV.GridClient gclient, string commName, string cacheDir, int maxrequests) {
        m_waiting = new Dictionary<OMV.UUID, WaitingInfo>();
        m_completionWork = new BasicWorkQueue("LLAssetContextCompletion");
        m_client = gclient;
        m_commName = commName;
        m_cacheDir = cacheDir;
        m_maxRequests = maxrequests;
        m_client.Settings.MAX_CONCURRENT_TEXTURE_DOWNLOADS = m_maxRequests;
        // m_client.Assets.OnImageReceived += new OMV.AssetManager.ImageReceivedCallback(OnImageReceived);
        m_client.Assets.Cache.ComputeAssetCacheFilename = ComputeTextureFilename;
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
    /// Given a fully qualified filename, make sure all the parent directies exist
    /// </summary>
    /// <param name="filename"></param>
    private static void MakeParentDirectoriesExist(string filename) {
        string textureDirName = Path.GetDirectoryName(filename);
        if (!Directory.Exists(textureDirName)) {
            Directory.CreateDirectory(textureDirName);
        }
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
        if (File.Exists(textureFilename)) {
            m_log.Log(LogLevel.DTEXTUREDETAIL, "DoTextureLoad: Texture file alreayd exists for " + worldID);
            bool hasTransparancy = CheckTextureFileForTransparancy(textureFilename);
            // make the callback happen on a new thread so things don't get tangled (caller getting the callback)
            m_completionWork.DoLater(new FinishCallDoLater(finishCall, textureEntityName.Name, hasTransparancy));
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
                m_client.Assets.RequestImage(binID, OMV.ImageType.Normal, 50f, 0, 0, OnACDownloadFinished, false);
            }
        }
        return;
    }

    private class FinishCallDoLater : DoLaterBase {
        DownloadFinishedCallback m_callback;
        string m_textureEntityName;
        bool m_hasTransparancy;
        public FinishCallDoLater(DownloadFinishedCallback cb, string tName, bool hasT) {
            m_callback = cb;
            m_textureEntityName = tName;
            m_hasTransparancy = hasT;
        }
        public override bool DoIt() {
            m_callback(m_textureEntityName, m_hasTransparancy);
            return true;
        }
    }

    /// <summary>
    /// Check the file at the specified filename for transparancy. We presume the texture is
    /// a JPEG2000 image. If we can't figure it out, we presume it has transparancy.
    /// </summary>
    /// <param name="textureFilename"></param>
    /// <returns></returns>
    private bool CheckTextureFileForTransparancy(string textureFilename) {
        bool ret = true;    // assume the worst about  this texture
        try {
            byte[] data = File.ReadAllBytes(textureFilename);
            OMV.Assets.AssetTexture assetTexture = new OMV.Assets.AssetTexture(OMV.UUID.Zero, data);
            ret = CheckAssetTextureForTransparancy(assetTexture);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DTEXTURE, "CheckTextureFileForTransparancy: error checking {0}: {1}",
                textureFilename, e.ToString());
            ret = true;
        }
        return ret;
    }

    /// <summary>
    /// Given a decoded asset texture, return whether there is any transparancy therein.
    /// </summary>
    /// <param name="assetTexture"></param>
    /// <returns>true if there is transparancy in the texture</returns>
    private static bool CheckAssetTextureForTransparancy(OMV.Assets.AssetTexture assetTexture) {
        bool hasTransparancy = false;
        try {
            if (assetTexture.Image == null) {
                assetTexture.Decode();
            }
            if (assetTexture.Image.Alpha != null) {
                for (int ii = 0; ii < assetTexture.Image.Alpha.Length; ii++) {
                    if (assetTexture.Image.Alpha[ii] != 255) {
                        hasTransparancy = true;
                        break;
                    }
                }
            }
        }
        catch {
            hasTransparancy = false;
        }
        return hasTransparancy;
    }

    // Used for texture pipeline
    // returns flag = true if texture was sucessfully downloaded
    private void OnACDownloadFinished(OMV.TextureRequestState state, OMV.Assets.AssetTexture assetTexture) {
        // if texture could not be downloaded, create a fake texture
        OMV.UUID assetWorldID = assetTexture.AssetID;
        if ((state == OMV.TextureRequestState.NotFound) || (state == OMV.TextureRequestState.Timeout)) {
            try {
                EntityNameLL tempTexture = EntityNameLL.ConvertTextureWorldIDToEntityName(this, assetWorldID.ToString());
                string tempTextureFilename = Path.Combine(CacheDirBase, tempTexture.CacheFilename);
                string textureDirName = Path.GetDirectoryName(tempTextureFilename);
                if (!Directory.Exists(textureDirName)) {
                    Directory.CreateDirectory(textureDirName);
                }
                string noTextureFilename = LookingGlassBase.Instance.AppParams.ParamString(m_commName + ".Assets.NoTextureFilename");
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
                    // sneak new values into the queued items 
                    toCall.Add(kvp.Value);
                }
            }
            // now remove the ones from the list (we cannot remove while transversing the list)
            foreach (WaitingInfo wx in toCall) {
                m_waiting.Remove(wx.worldID);
            }
        }
        m_completionWork.DoLater(new CompleteDownloadLater(this, assetTexture, toCall, m_commName, m_log));
        return;
    }

    private sealed class CompleteDownloadLater : DoLaterBase {
        private List<WaitingInfo> m_completeWork;
        private AssetContextBase m_acontext;
        private OMV.Assets.AssetTexture m_assetTexture;
        private bool hasTransparancy;
        private ILog m_logg;
        private string m_commName;
        public CompleteDownloadLater(AssetContextBase acontext, OMV.Assets.AssetTexture aTexture, 
                        List<WaitingInfo> completeWork, string commName, ILog logg) {
            m_acontext = acontext;
            m_completeWork = completeWork;
            m_assetTexture = aTexture;
            m_commName = commName;
            m_logg = logg;
        }

        public override bool  DoIt() {
            foreach (WaitingInfo wii in m_completeWork) {
                EntityName textureEntityName = EntityNameLL.ConvertTextureWorldIDToEntityName(m_acontext, wii.worldID);
                bool m_convertToPng = LookingGlassBase.Instance.AppParams.ParamBool(m_commName + ".Assets.ConvertPNG");
                OMV.Imaging.ManagedImage managedImage;
                System.Drawing.Image tempImage = null;
                if (wii.type == AssetType.Texture) {
                    // a regular texture we write out as it's JPEG2000 image
                    try {
                        hasTransparancy = CheckAssetTextureForTransparancy(m_assetTexture);
                        MakeParentDirectoriesExist(wii.filename);
                        if (m_convertToPng) {
                            // This PNG code kinda works but PNGs are larger than the JPEG files and
                            // there are occasional 'out of memory'. It also uses WAY MORE disk space.
                            if (OMVI.OpenJPEG.DecodeToImage(m_assetTexture.AssetData, out managedImage)) {
                                try {
                                    tempImage = OMVI.LoadTGAClass.LoadTGA(new MemoryStream(managedImage.ExportTGA()));
                                }
                                catch (Exception e) {
                                    m_logg.Log(LogLevel.DBADERROR, "Failed to export and load TGA data from decoded image: {0}", 
                                        e.ToString());
                                }
                                using (Bitmap textureBitmap = new Bitmap(tempImage.Width, tempImage.Height,
                                            System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                                    using (Graphics graphics = Graphics.FromImage(textureBitmap)) {
                                        graphics.DrawImage(tempImage, 0, 0);
                                        graphics.Flush();
                                    }
                                    using (FileStream fileStream = File.Open(wii.filename, FileMode.Create)) {
                                        textureBitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                                        fileStream.Flush();
                                        fileStream.Close();
                                    }
                                }
                            }
                            else {
                                m_logg.Log(LogLevel.DBADERROR, "TEXTURE DOWNLOAD COMPLETE. FAILED JPEG2 DECODE FOR {0}", textureEntityName.Name);
                            }
                        }
                        else {
                            // Just save the JPEG2000 file
                            using (FileStream fileStream = File.Open(wii.filename, FileMode.Create)) {
                                fileStream.Flush();
                                fileStream.Write(m_assetTexture.AssetData, 0, m_assetTexture.AssetData.Length);
                                fileStream.Close();
                            }
                        }
                        m_logg.Log(LogLevel.DTEXTUREDETAIL, "Download finished callback: " + wii.worldID.ToString());
                        wii.callback(textureEntityName.Name, hasTransparancy);
                    }
                    catch (Exception e) {
                        m_logg.Log(LogLevel.DBADERROR, "TEXTURE DOWNLOAD COMPLETE. FAILED FILE CREATION FOR {0}: {1}", 
                            textureEntityName.Name, e.ToString());
                    }
                }
                if (wii.type == AssetType.SculptieTexture) {
                    // for sculpties, we clear the alpha channel and write out a PNG
                    try {
                        if (OMVI.OpenJPEG.DecodeToImage(m_assetTexture.AssetData, out managedImage)) {

                            // have to clear the alpha channel for sculptie textures because of the TGA conversion
                            if (managedImage.Alpha != null) {
                                for (int ii = 0; ii < managedImage.Alpha.Length; ii++) managedImage.Alpha[ii] = 255;
                            }
                            try {
                                tempImage = OMVI.LoadTGAClass.LoadTGA(new MemoryStream(managedImage.ExportTGA()));
                            }
                            catch (Exception e) {
                                m_logg.Log(LogLevel.DBADERROR, "Failed to export and load TGA data from decoded image: {0}", 
                                    e.ToString());
                            }

                            MakeParentDirectoriesExist(wii.filename);
                            using (Bitmap textureBitmap = new Bitmap(tempImage.Width, tempImage.Height,
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                                using (Graphics graphics = Graphics.FromImage(textureBitmap)) {
                                    graphics.DrawImage(tempImage, 0, 0);
                                    graphics.Flush();
                                }

                                using (FileStream fileStream = File.Open(wii.filename, FileMode.Create)) {
                                    textureBitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                                    fileStream.Flush();
                                    fileStream.Close();
                                }
                            }
                            m_logg.Log(LogLevel.DTEXTUREDETAIL, "Download sculpty finished callback: " + wii.worldID.ToString());
                            wii.callback(textureEntityName.Name, false);
                        }
                        else {
                            m_logg.Log(LogLevel.DBADERROR, "TEXTURE DOWNLOAD COMPLETE. FAILED JPEG2 DECODE FOR {0}", textureEntityName.Name);
                        }
                    }
                    catch (Exception e) {
                        m_logg.Log(LogLevel.DBADERROR, "TEXTURE DOWNLOAD COMPLETE. FAILED FILE CREATION FOR {0}: {1}", 
                            textureEntityName.Name, e.ToString());
                    }
                }
            }
            m_completeWork.Clear();
            return true;
        }
    }

    /// <summary>
    /// Get the texture right now. If the texture is not immediately available (not on local
    /// computer's disk or memory), return null saying it's not here.
    /// </summary>
    /// <param name="textureEnt"></param>
    /// <returns></returns>
    public override System.Drawing.Bitmap GetTexture(EntityName textureEnt) {
        Bitmap bitmap = null;
        try {
            string textureFilename = Path.Combine(CacheDirBase, textureEnt.CacheFilename);
            if (File.Exists(textureFilename)) {
                bitmap = (Bitmap)Bitmap.FromFile(textureFilename);
            }
            else {
                // the texture is not there yet. Return null to tell the caller they are out of luck
                bitmap = null;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "GetTexture: Exception getting texture {0}: {1}",
                textureEnt.Name, e.ToString());
            bitmap = null;
        }
        return bitmap;
    }

    private void OnImageReceived(OMV.ImageDownload cntl, OMV.Assets.AssetTexture textureID) {
    }

    public override void Dispose() {
        base.Dispose();
    }

}
}
