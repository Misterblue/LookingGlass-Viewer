/* Copyright (c) 2010 Robert Adams
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
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.Renderer.OGL {
    public sealed class AnimatFixedRotation : AnimatBase {
        private uint m_infoID;
        private float m_rotationsPerSecond;
        private OMV.Vector3 m_rotationAxis;

        public AnimatFixedRotation(IAnimation anim, uint id)
                        : base(AnimatBase.AnimatTypeFixedRotation) {
            m_infoID = id;
            if (anim.DoStaticRotation) {
                m_rotationsPerSecond = anim.StaticRotationRotPerSec;
                m_rotationAxis = anim.StaticRotationAxis;
            }
            else {
                // shouldn't get here
                m_rotationsPerSecond = 1;
                m_rotationAxis = OMV.Vector3.UnitX;
            }
        }

        public override bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            float nextStep = m_rotationsPerSecond * timeSinceLastFrame;
            float nextIncrement = Constants.TWOPI * nextStep;
            while (nextIncrement > Constants.TWOPI) nextIncrement -= Constants.TWOPI;
            OMV.Quaternion newRotation = OMV.Quaternion.CreateFromAxisAngle(m_rotationAxis, nextIncrement);
            lock (rri) {
                try {
                    RenderablePrim rp = rri.renderPrimList[m_infoID];
                    rp.Rotation = newRotation * rp.Rotation;
                }
                catch (Exception e) {
                    LogManager.Log.Log(LogLevel.DBADERROR, "Did not find prim for FixedRotation: {0}", e);
                }
            }
            return true;        // we never exit
        }
    }
}
