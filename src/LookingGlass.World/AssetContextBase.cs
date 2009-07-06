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
using System.Text;
using LookingGlass.Framework.Logging;
using OMV = OpenMetaverse;

namespace LookingGlass.World {
public abstract class AssetContextBase : IDisposable {

    public enum AssetType {
        Texture,
        SculptieTexture
    }

    protected string m_Name;
    public string Name { get { return m_Name; } }

    public abstract string CacheDirBase { get; }

    public static List<AssetContextBase> AssetContexts;

    // When a requested download is finished, you can be called with the ID of the
    // completed asset and the entityName of ??
    public delegate void DownloadFinishedCallback(string entName);
    public delegate void DownloadProgressCallback(string entName);

# pragma warning disable 0067   // disable unused event warning
    public event DownloadFinishedCallback OnDownloadFinished;
    public event DownloadProgressCallback OnDownloadProgress;
# pragma warning restore 0067

    static AssetContextBase() {
        AssetContexts = new List<AssetContextBase>();
    }

    public AssetContextBase() {
        // remember all the contexts
        lock (AssetContexts) {
            if (!AssetContexts.Contains(this)) {
                AssetContexts.Add(this);
            }
        }
    }

    /// <summary>
    /// Given a context and a world specific identifier, return the filename
    /// (without the CacheDirBase included) of the texture file. This may start
    /// the loading of the texture so the texture file will be updated and
    /// call to the OnDownload* events will show it's progress.
    /// </summary>
    /// <param name="textureEntityName">the entity name of this texture</param>
    public abstract void DoTextureLoad(string textureEntityName, AssetType typ, DownloadFinishedCallback finished);

    /// <summary>
    /// Just get the real texture and return it to us. If the texture is not immediately available
    /// (that is, is not on the local computer's memory or disk) we return a null pointer.
    /// </summary>
    /// <param name="textureEnt">name of the texture to get</param>
    /// <returns>Bitmap of image or 'null' if image not available now</returns>
    public abstract System.Drawing.Bitmap GetTexture(EntityName textureEnt);

    /// <summary>
    /// the caller didn't know who the owner of the texture was. We take apart the entity
    /// name to try and find who it belongs to. This is static since we are using the static
    /// asset context structures. When we find the real asset context of the texture, we
    /// call that instance.
    /// </summary>
    /// <param name="textureEntityName"></param>
    /// <param name="finished"></param>
    /// <returns></returns>
    public static void RequestTextureLoad(string textureEntityName, AssetType typ, DownloadFinishedCallback finished) {
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
    public abstract bool isTextureOwner(string textureEntityName);

    virtual public void Dispose() {
        lock (AssetContexts) {
            AssetContexts.Remove(this);
        }
        return;
    }
}
}
