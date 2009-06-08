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
using OMV = OpenMetaverse;

namespace LookingGlass.World {
    public class EntityCamera : EntityBase {

        // we use the OMV.CoordinateFrame code. Our camera is always at global 0,0,0
        protected OMV.CoordinateFrame m_frame = new OMV.CoordinateFrame(new OMV.Vector3(0f, 0f, 0f));

        public OMV.Vector3 InitDirection { 
            get { return m_frame.Origin; }
            set { m_frame.Origin = value; }
        }

        // true if the camera does not tilt side to side
        protected bool m_yawFixed;
        public bool YawFixed {
            get { return m_yawFixed; }
            set { m_yawFixed = value; }
        }

        public override OMV.Quaternion Heading {
            get {
                OMV.Matrix4 head = new OMV.Matrix4();
                head.AtAxis = m_frame.YAxis;
                head.LeftAxis = m_frame.XAxis;
                head.UpAxis = m_frame.ZAxis;
                return head.GetQuaternion();
            }
            set {
                m_frame.ResetAxes();
                m_frame.Rotate(Heading);
            }
        }

        // rotate the camera by the given quaternion
        public void rotate(OMV.Quaternion rot) {
            m_frame.Rotate(rot);
        }

        public void rotate(OMV.Vector3 dir) {
            rotate(dir.X, dir.Y, dir.Z);
        }

        /// <summary>
        /// rotate the specified amounts around the camera's local axis
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public void rotate(float X, float Y, float Z) {
            m_frame.Roll(X);
            if (!YawFixed) {
                m_frame.Pitch(Y);
            }
            m_frame.Pitch(Z);
        }

        protected OMV.Vector3 m_globalFocalPoint = new OMV.Vector3();
        public OMV.Vector3 GlobalFocalPoint {
            get { return m_globalFocalPoint; }
            set { m_globalFocalPoint = value; }
        }

        public void LookAtFocalPoint() {
            m_frame.LookAt(m_frame.Origin, m_globalFocalPoint);
        }

        public void LookAt(OMV.Vector3 target) {
            m_frame.LookAt(m_frame.Origin, target);
        }

        protected float m_zoom;
        public float Zoom { get { return m_zoom; } set { m_zoom = value; } }

        protected float m_far;
        public float Far { get { return m_far; } set { m_far = value; } }

        public EntityCamera(RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
            m_yawFixed = true;
            m_globalPosition = new OMV.Vector3d(10d, 10d, 10d);
            // m_heading = new OMV.Quaternion(0f, 1f, 0f);
        }

        public override void Dispose() {
            throw new NotImplementedException();
        }
    }
}
