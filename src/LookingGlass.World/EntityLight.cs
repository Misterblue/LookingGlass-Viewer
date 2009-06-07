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
using OpenMetaverse;

namespace LookingGlass.World {
    /// <summary>
    /// Lights that fill the world. Used for sun and moon. Individual object 
    /// lighting is done by the entities themselves.
    /// </summary>
    public class EntityLight : EntityBase {
        protected bool m_visible = false;
        virtual public bool Visible { get { return m_visible; } set { m_visible = value; } }

        protected Color4 m_color;
        virtual public Color4 Color { get { return m_color; } set { m_color = value; } }

        protected Vector3 m_position;
        virtual public Vector3 Position { get { return m_position; } set { m_position = value; } }

        protected Vector3 m_target;
        virtual public Vector3 Target { get { return m_target; } set { m_target = value; } }

        public EntityLight(RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }

    }
}
