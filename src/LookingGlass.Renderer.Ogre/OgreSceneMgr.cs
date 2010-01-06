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

namespace LookingGlass.Renderer.Ogr {
    /// <summary>
    /// This is pretty much just a stub since the real scene manager is
    /// kept over in the unmanaged code. For Ogre, this mostly exists
    /// to be compatible with other drivers  that might not keep the
    /// scene manager handle other places. Additionally, someday there
    /// could be multiple Ogre scene managers.
    /// </summary>
    public class OgreSceneMgr : IOgreWrapper {

        protected System.IntPtr m_sceneMgr = System.IntPtr.Zero;
        public System.IntPtr BasePtr { get { return m_sceneMgr; } }

        public OgreSceneNode RootNode() {
            return new OgreSceneNode(Ogr.RootNode(m_sceneMgr));
        }

        public OgreSceneMgr(System.IntPtr sceneMgr) {
            m_sceneMgr = sceneMgr;
        }

        // Create the scene node between frames.
        // This queues the operation to do later when the processing is between frames
        // Returns true if queued or false if we couldn't resolve the parentNodeName to
        // and actual node.
        public bool CreateMeshSceneNodeBF(
                float priority,
                string sceneNodeName,   // name of scene node to create
                string parentNodeName,  // name of the parent to connect it to (or zero)
                string meshName,      // name of the entity (mesh) to add to the scene node
                bool scale, bool orientation,
                float px, float py, float pz, float sx, float sy, float sz,
                float rw, float rx, float ry, float rz) {
            return Ogr.CreateMeshSceneNodeBF(priority, m_sceneMgr, sceneNodeName, parentNodeName, 
                        // EntityNameOgre.ConvertToOgreEntityName(new EntityNameOgre(meshName)),
                        EntityNameOgre.ConvertToOgreEntityName(new EntityNameOgre(sceneNodeName)),
                        meshName,
                        scale, orientation,
                        px, py, pz, sx, sy, sz, rw, rx, ry, rz);
        }
    }
}
