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
using LookingGlass.Comm;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.WorkQueue;
using OMV = OpenMetaverse;
using OMVI = OpenMetaverse.Imaging;

namespace LookingGlass.World {
public abstract class AssetContextBase : IDisposable {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    // When a requested download is finished, you can be called with the ID of the
    // completed asset and the entityName of ??
    public delegate void DownloadFinishedCallback(string entName, bool hasTransparancy);
    public delegate void DownloadProgressCallback(string entName);

# pragma warning disable 0067   // disable unused event warning
    public event DownloadFinishedCallback OnDownloadFinished;
    public event DownloadProgressCallback OnDownloadProgress;
# pragma warning restore 0067

    // =========================================================
    protected class WaitingInfo : IComparable<WaitingInfo> {
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

    public enum AssetType {
        Texture,
        SculptieTexture
    }

    protected string m_Name;
    public string Name { get { return m_Name; } }

    protected string m_cacheDir;
    public string CacheDirBase {
        get { return m_cacheDir; }
    }

    public static List<AssetContextBase> AssetContexts;

    // used to lock access to the filesystem so the threads and instances of this don't get too tangled
    protected static readonly object FileSystemAccessLock = new object();

    protected int m_maxRequests;
    protected BasicWorkQueue m_completionWork;
    protected Dictionary<OMV.UUID, WaitingInfo> m_waiting;
    protected static int m_numAssetContextBase = 0;

    protected ICommProvider m_comm;       // handle to the underlying comm provider

    static AssetContextBase() {
        AssetContexts = new List<AssetContextBase>();
    }

    public AssetContextBase(string name) {
        m_numAssetContextBase++;
        m_Name = name;
        // remember all the contexts
        lock (AssetContexts) {
            if (!AssetContexts.Contains(this)) {
                AssetContexts.Add(this);
            }
        }
        m_waiting = new Dictionary<OMV.UUID, WaitingInfo>();
        m_completionWork = new BasicWorkQueue("AssetCompletion" + m_numAssetContextBase.ToString());
    }


    public void InitializeContext(ICommProvider comm, string cacheDir, int maxrequests) {
        m_comm = comm;
        m_cacheDir = cacheDir;
        m_maxRequests = maxrequests;
        InitializeContextFinish();
    }

    public virtual void InitializeContextFinish() {}

    /// <summary>
    /// Given a context and a world specific identifier, return the filename
    /// (without the CacheDirBase included) of the texture file. This may start
    /// the loading of the texture so the texture file will be updated and
    /// call to the OnDownload* events will show it's progress.
    /// </summary>
    /// <param name="textureEntityName">the entity name of this texture</param>
    public abstract void DoTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finished);

    /// <summary>
    /// Just get the real texture and return it to us. If the texture is not immediately available
    /// (that is, is not on the local computer's memory or disk) we return a null pointer.
    /// Get the texture right now. If the texture is not immediately available (not on local
    /// computer's disk or memory), return null saying it's not here.
    /// </summary>
    /// <param name="textureEnt"></param>
    /// <returns></returns>
    public virtual System.Drawing.Bitmap GetTexture(EntityName textureEnt) {
        Bitmap bitmap = null;
        try {
            string textureFilename = Path.Combine(CacheDirBase, textureEnt.CacheFilename);
            lock (FileSystemAccessLock) {
                if (File.Exists(textureFilename)) {
                    bitmap = (Bitmap)Bitmap.FromFile(textureFilename);
                }
                else {
                    // the texture is not there yet. Return null to tell the caller they are out of luck
                    bitmap = null;
                }
            }
        }
        catch (OutOfMemoryException) {
            // m_log.Log(LogLevel.DBADERROR, "GetTexture: OUT OF MEMORY!!!");
            bitmap = null;
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "GetTexture: Exception getting texture {0}: {1}",
                textureEnt.Name, e.ToString());
            bitmap = null;
        }
        return bitmap;
    }


    /// <summary>
    /// the caller didn't know who the owner of the texture was. We take apart the entity
    /// name to try and find who it belongs to. This is static since we are using the static
    /// asset context structures. When we find the real asset context of the texture, we
    /// call that instance.
    /// </summary>
    /// <param name="textureEntityName"></param>
    /// <param name="finished"></param>
    /// <returns></returns>
    public static void RequestTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finished) {
        AssetContextBase textureOwner = null;
        lock (AssetContexts) {
            foreach (AssetContextBase acb in AssetContexts) {
                if (acb.isTextureOwner(textureEntityName)) {
                    textureOwner = acb;
                    break;
                }
            }
        }
        if (textureOwner != null) {
            textureOwner.DoTextureLoad(textureEntityName, typ, finished);
        }
        else {
            LogManager.Log.Log(LogLevel.DBADERROR, "RequestTextureLoad: found not asset context for texture " + textureEntityName);
        }
    }

    /// <summary>
    /// based only on the name of the texture entity, have te asset context decide if it
    /// is the owner of this texture.
    /// </summary>
    /// <param name="textureEntityName"></param>
    /// <returns></returns>
    public abstract bool isTextureOwner(EntityName textureEntityName);

    /// <summary>
    /// Check of the passed resource is already cached. Usually used to see if the cached
    /// mesh is in the filesystem.
    /// </summary>
    /// <param name="ent">Context entity</param>
    /// <param name="resource">Name of resource to check</param>
    /// <returns>true if cached, false otherwise</returns>
    public virtual bool CheckIfCached(IEntity contextEntity, EntityName resource) {
        string meshFilename = Path.Combine(contextEntity.AssetContext.CacheDirBase, resource.CacheFilename);
        if (File.Exists(meshFilename)) {
            return true;
        }
        // could it be a preloaded file?
        if (resource.HostPart.ToLower() == "preload") {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Given a fully qualified filename, make sure all the parent directies exist
    /// </summary>
    /// <param name="filename"></param>
    protected static void MakeParentDirectoriesExist(string filename) {
        string textureDirName = Path.GetDirectoryName(filename);
        lock (AssetContextBase.FileSystemAccessLock) {
            if (!Directory.Exists(textureDirName)) {
                Directory.CreateDirectory(textureDirName);
            }
        }
    }

    /// Check the file at the specified filename for transparancy. We presume the texture is
    /// a JPEG2000 image. If we can't figure it out, we presume it has transparancy.
    /// </summary>
    /// <param name="textureFilename"></param>
    /// <returns></returns>
    protected bool CheckTextureFileForTransparancy(string textureFilename) {
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
    protected static bool CheckAssetTextureForTransparancy(OMV.Assets.AssetTexture assetTexture) {
        bool hasTransparancy = false;
        try {
            if (assetTexture.Image == null) {
                assetTexture.Decode();
            }
            if (assetTexture.Image !=null && assetTexture.Image.Alpha != null)
            {
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

    // Call the callback on a separate thread to keep from getting tangled
    protected bool FinishCallDoLater(DoLaterBase qInstance, Object parm) {
        Object[] lParams = (Object[])parm;
        DownloadFinishedCallback m_callback = (DownloadFinishedCallback)lParams[0];
        string m_textureEntityName = (string)lParams[1];
        bool m_hasTransparancy = (bool)lParams[2];

        m_callback(m_textureEntityName, m_hasTransparancy);
        return true;
    }

    // implementation function to get comm specific entity names from received texture information
    protected virtual EntityName ConvertToEntityName(AssetContextBase acb, string id) {
        return new EntityName(acb, id);
    }

    // implementation function so underlying class knows when processing is complete
    protected virtual void CompletionWorkComplete() {
        return;
    }

    // Used for texture pipeline
    // returns flag = true if texture was sucessfully downloaded
    protected void ProcessDownloadFinished(OMV.TextureRequestState state, OMV.Assets.AssetTexture assetTexture) {
        // if texture could not be downloaded, create a fake texture
        OMV.UUID assetWorldID = assetTexture.AssetID;
        List<WaitingInfo> toCall = new List<WaitingInfo>();
        m_log.Log(LogLevel.DTEXTUREDETAIL, "ProcessDownloadFinished: Completion for " + assetWorldID.ToString());
        lock (m_waiting) {
            foreach (KeyValuePair<OMV.UUID, WaitingInfo> kvp in m_waiting) {
                if (kvp.Value.worldID == assetWorldID) {
                    // sneak new values into the queued items 
                    toCall.Add(kvp.Value);
                }
            }
            // now remove the ones from the list (we cannot remove while transversing the list)
            // only remove them if the code is not for just a progress update
            if (state != OMV.TextureRequestState.Progress) {
                foreach (WaitingInfo wx in toCall) {
                    m_waiting.Remove(wx.worldID);
                }
            }
        }

        // if the texture fetch failed, create the not-found file
        if ((state == OMV.TextureRequestState.NotFound) || (state == OMV.TextureRequestState.Timeout)) {
            foreach (WaitingInfo wi in toCall) {
                try {
                    lock (FileSystemAccessLock) {
                        MakeParentDirectoriesExist(wi.filename);
                        string noTextureFilename = LookingGlassBase.Instance.AppParams.ParamString(m_comm.Name + ".Assets.NoTextureFilename");
                        // if we copy the no texture file into the filesystem, we will never retry to
                        // fetch the texture. This copy is not a good thing.
                        // if (!File.Exists(wi.filename)) {
                            File.Copy(noTextureFilename, wi.filename);
                        // }
                    }
                    m_log.Log(LogLevel.DTEXTURE, 
                        "ProcessDownloadFinished: Texture fetch failed={0}. Using not found texture.", wi.worldID.ToString());
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, 
                        "ProcessDownloadFinished: Texture fetch failed. Could not create default texture: " + e.ToString());
                }
            }
        }

        // Queue the actual completion call for another thread to let this one return
        Object[] completeDownloadParams = { assetTexture, toCall, m_comm.Name };
        m_completionWork.DoLater(CompleteDownloadLater, completeDownloadParams);
        return;
    }

    private bool CompleteDownloadLater(DoLaterBase qInstance, Object parms) {
        Object[] lParams = (Object[])parms;
        OMV.Assets.AssetTexture m_assetTexture = (OMV.Assets.AssetTexture)lParams[0];
        List<WaitingInfo> m_completeWork = (List<WaitingInfo>)lParams[1];
        string m_commName = (string)lParams[2];
        bool hasTransparancy;

        foreach (WaitingInfo wii in m_completeWork) {
            EntityName textureEntityName = ConvertToEntityName(this, wii.worldID.ToString());
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
                                m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: Failed to export and load TGA data from decoded image: {0}", 
                                    e.ToString());
                            }
                            try {
                                using (Bitmap textureBitmap = new Bitmap(tempImage.Width, tempImage.Height,
                                            System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                                    using (Graphics graphics = Graphics.FromImage(textureBitmap)) {
                                        graphics.DrawImage(tempImage, 0, 0);
                                        graphics.Flush();
                                    }
                                    lock (FileSystemAccessLock) {
                                        using (FileStream fileStream = File.Open(wii.filename, FileMode.Create)) {
                                            textureBitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                                            fileStream.Flush();
                                            fileStream.Close();
                                        }
                                    }
                                }
                            }
                            catch (Exception e) {
                                m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: TEXTURE DOWNLOAD COMPLETE. FAILED PNG FILE CREATION FOR {0}: {1}", 
                                        textureEntityName.Name, e.ToString());
                            }
                        }
                        else {
                            m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: TEXTURE DOWNLOAD COMPLETE. FAILED JPEG2 DECODE FOR {0}", textureEntityName.Name);
                        }
                    }
                    else {
                        // Just save the JPEG2000 file
                        try {
                            lock (FileSystemAccessLock) {
                                using (FileStream fileStream = File.Open(wii.filename, FileMode.Create)) {
                                    fileStream.Write(m_assetTexture.AssetData, 0, m_assetTexture.AssetData.Length);
                                    fileStream.Flush();
                                    fileStream.Close();
                                }
                            }
                        }
                        catch (Exception e) {
                            m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: TEXTURE DOWNLOAD COMPLETE. ERROR JPEG2000 FILE CREATION FOR {0}: {1}", 
                                        textureEntityName.Name, e.ToString());
                        }
                    }
                    m_log.Log(LogLevel.DTEXTUREDETAIL, "ProcessDownloadFinished: Download finished callback: " + wii.worldID.ToString());
                    // wii.callback(textureEntityName.Name, hasTransparancy);
                    // schedule callback on another thread (it could call back into this routine)
                    Object[] finishCallParams = { wii.callback, textureEntityName.Name, hasTransparancy };
                    m_completionWork.DoLater(FinishCallDoLater, finishCallParams);
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: TEXTURE DOWNLOAD COMPLETE. UNKNOWN FAILURE CREATING FILE FOR {0}: {1}", 
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
                        // convert decoded JPEG into a bitmap
                        try {
                            tempImage = OMVI.LoadTGAClass.LoadTGA(new MemoryStream(managedImage.ExportTGA()));
                        }
                        catch (Exception e) {
                            m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: Failed to export and load TGA data from decoded image: {0}", 
                                e.ToString());
                        }

                        MakeParentDirectoriesExist(wii.filename);
                        using (Bitmap textureBitmap = new Bitmap(tempImage.Width, tempImage.Height,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                            using (Graphics graphics = Graphics.FromImage(textureBitmap)) {
                                graphics.DrawImage(tempImage, 0, 0);
                                graphics.Flush();
                            }

                            try {
                                lock (FileSystemAccessLock) {
                                    string tempFilename = wii.filename + ".tmp";
                                    using (FileStream fileStream = File.Open(tempFilename, FileMode.Create)) {
                                        textureBitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                                        fileStream.Flush();
                                        fileStream.Close();
                                        // attempt to make the creation of the file almost atomic
                                        FileInfo fi = new FileInfo(tempFilename);
                                        fi.MoveTo(wii.filename);
                                    }
                                }
                            }
                            catch (Exception e) {
                                m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: SCULPTIE TEXTURE DOWNLOAD COMPLETE. FAILED FILE CREATION FOR {0}: {1}", 
                                        textureEntityName.Name, e.ToString());
                                // the usual error is 'file already exists' so let the system use it
                            }
                        }
                        m_log.Log(LogLevel.DTEXTUREDETAIL, "ProcessDownloadFinished: Download sculpty finished callback: " + wii.worldID.ToString());
                        Object[] finishCallParams = { wii.callback, textureEntityName.Name, false };
                        m_completionWork.DoLater(FinishCallDoLater, finishCallParams);
                    }
                    else {
                        m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: SCULPTIE TEXTURE DOWNLOAD COMPLETE. FAILED JPEG2 DECODE FOR {0}", textureEntityName.Name);
                    }
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "ProcessDownloadFinished: SCULPTIE TEXTURE DOWNLOAD COMPLETE. UNKNOWN ERROR PROCESSING {0}: {1}", 
                        textureEntityName.Name, e.ToString());
                }
            }
        }
        m_completeWork.Clear();
        CompletionWorkComplete();
        return true;
    }


    virtual public void Dispose() {
        lock (AssetContexts) {
            AssetContexts.Remove(this);
        }
        return;
    }
}
}
