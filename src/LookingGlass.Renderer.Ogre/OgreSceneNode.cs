/* Copyright (c) Robert Adams
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
using OMV = OpenMetaverse;

namespace LookingGlass.Renderer.Ogr {
    /// <summary>
    /// A managed wrapper class for the real Ogre scene node.
    /// I decided that rather than try to hide and abstract the innards
    /// of Ogre from Renderer.Ogr, I will expose it in Mogre like classes
    /// that I can pass freely around inside the managed code. Unlike
    /// Mogre, the connection to Ogre is through my custom P/Invoke
    /// interface.
    /// The presumption is that few calls will need to be made to the
    /// scene graph especially compared to the generation of frames by
    /// Ogre.
    /// </summary>
    /// 
    public class OgreSceneNode : IOgreWrapper {

        public enum TransformSpace {
            TS_LOCAL, TS_PARENT, TS_WORLD
        };

        protected string m_name;
        public string Name { get { return m_name; } }

        protected System.IntPtr m_realSceneNode;
        public System.IntPtr BasePtr { get { return m_realSceneNode; } }

        public OgreSceneNode() {
        }

        public OgreSceneNode(System.IntPtr node) {
            m_realSceneNode = node;
            m_name = OMV.UUID.Random().ToString();

        }

        public OgreSceneNode(System.IntPtr node, string name) {
            m_realSceneNode = node;
            m_name = name;
            return;
        }

    }
}
