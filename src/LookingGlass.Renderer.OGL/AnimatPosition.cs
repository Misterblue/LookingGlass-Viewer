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
    /// <summary>
    /// Class to advance a position update.
    /// </summary>
    public sealed class AnimatPosition : AnimatBase {
        private uint m_infoID;
        private OMV.Vector3 m_origionalPosition;
        private OMV.Vector3 m_targetPosition;
        private float m_durationSeconds;
        private OMV.Vector3 m_distanceVector;
        private float m_progress;

        /// <summary>
        /// Create the animation. The passed animation block is expected
        /// to contain a defintion of a fixed rotation. If not, bad things will happen.
        /// </summary>
        /// <param name="anim">The IAnimation block with the info.</param>
        /// <param name="id">localID to lookup the prim in the RegionRenderInfo.renderPrimList</param>
        public AnimatPosition(OMV.Vector3 newPos, float durationSeconds, RegionRenderInfo rri, uint id)
                        : base(AnimatBase.AnimatTypePosition) {
            m_infoID = id;
            RenderablePrim rp = rri.renderPrimList[id];
            m_origionalPosition = rp.Position;
            m_targetPosition = newPos;
            m_durationSeconds = durationSeconds;
            m_distanceVector = m_targetPosition - m_origionalPosition;
            m_progress = 0f;
        }

        /// <summary>
        /// Called for each frame. Advance the position.
        /// </summary>
        /// <param name="timeSinceLastFrame">seconds since last frame display</param>
        /// <param name="rri">RegionRenderInfo for region the animation is in</param>
        /// <returns>true to say we never exit</returns>
        public override bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            bool ret = true;
            float thisProgress = timeSinceLastFrame / m_durationSeconds;
            m_progress += thisProgress;
            RenderablePrim rp = rri.renderPrimList[m_infoID];
            if (m_progress >= 1f) {
                // if progressed all the way, we're at the destination
                rp.Position = m_targetPosition;
                ret = false;    // we're done animating
            }
            else {
                // only part way there
                rp.Position = m_origionalPosition + m_distanceVector * m_progress;
            }
            return ret;
        }
    }
}
