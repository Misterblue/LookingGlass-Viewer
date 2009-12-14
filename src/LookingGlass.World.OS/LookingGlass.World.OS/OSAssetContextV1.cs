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
using System.Text;
using LookingGlass.Comm;
using LookingGlass.Framework;
using LookingGlass.World;

namespace LookingGlass.World.OS {
public class OSAssetContextV1 : AssetContextBase {

    public OSAssetContextV1() : base() {
        m_Name = "Unknown";
    }

    public OSAssetContextV1(string name) : base() {
        m_Name = name;
    }

    public override void InitializeContextFinish() {
        return;
    }

    public override bool isTextureOwner(EntityName textureEntityName) {
        return false;
    }

    public override void DoTextureLoad(EntityName textureEntityName, AssetType typ, DownloadFinishedCallback finishCall) {
    }

    private void OnTextureLoadCompletion() {
        // ProcessDownloadFinished(OMV.TextureRequestState state, OMV.Assets.AssetTexture assetTexture) {
    }

}
}
