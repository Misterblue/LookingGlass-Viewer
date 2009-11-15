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
using System.Text;
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
    public class LLEntityAttachment : LLEntityBase, IEntityAttachment {
        private OMV.Avatar m_avatar = null;
        public OMV.Avatar Avatar { 
            get { return m_avatar; } 
            set { m_avatar = value; } 
        }

        public LLEntityAttachment(AssetContextBase acontext, LLRegionContext rcontext, 
                ulong regionHandle, OMV.Avatar av) : base(rcontext, acontext) {
            // let people looking at IEntity's get at my attachment-ness
            RegisterInterface<IEntityAttachment>(this);
            this.Sim = rcontext.Simulator;
            this.RegionHandle = regionHandle;
            this.Avatar = av;
            this.LocalID = av.LocalID;
            this.Name = AttachmentEntityNameFromID(acontext, m_avatar.ID);
            LogManager.Log.Log(LogLevel.DCOMMDETAIL, "LLEntityAttachment: create id={0}, lid={1}",
                            av.ID.ToString(), this.LocalID);
        }

        public static EntityName AttachmentEntityNameFromID(AssetContextBase acontext, OMV.UUID ID) {
            return new EntityNameLL(acontext, "Attachment/" + ID.ToString());
        }


        public override void Dispose() {
            return;
        }
    }
}
