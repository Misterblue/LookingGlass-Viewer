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
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
    public sealed class LLEntityAvatar : LLEntityBase, IEntityAvatar {

        private OMV.Avatar m_avatar = null;
        public OMV.Avatar Avatar { 
            get { return m_avatar; } 
            set { m_avatar = value; } 
        }

        private OMV.AvatarManager m_avatarManager = null;
        public OMV.AvatarManager AvatarManager {
            get { return m_avatarManager; }
            set { m_avatarManager = value; }
        }

        public LLEntityAvatar(AssetContextBase acontext, LLRegionContext rcontext, 
                ulong regionHandle, OMV.Avatar av) : base(rcontext, acontext) {
            // let people looking at IEntity's get at my avatarness
            RegisterInterface<IEntityAvatar>(this);
            m_simulator= rcontext.Simulator;
            m_regionHandle = regionHandle;
            m_avatar = av;
            m_name = AvatarEntityNameFromID(acontext, m_avatar.ID);
        }

        public static EntityName AvatarEntityNameFromID(AssetContextBase acontext, OMV.UUID ID) {
            return new EntityNameLL(acontext, "Avatar/" + ID.ToString());
        }

        #region POSITION
        override public OMV.Quaternion Heading {
            get {
                return m_avatar.Rotation;
            }
            set {
            }
        }

        override public OMV.Vector3 RelativePosition {
            get {
                return m_avatar.Position;
            }
            set {
            }
        }
        override public OMV.Vector3d GlobalPosition {
            get {
                return m_globalPosition;
            }
            set {
            }
        }
        #endregion POSITION

        public override void Update(UpdateCodes what) {
            base.Update(what);
            // if we are the agent in the world, also update the agent
            if (this == World.Instance.Agent.AssociatedAvatar) {
                World.Instance.UpdateAgent(what);
            }
        }
        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}
