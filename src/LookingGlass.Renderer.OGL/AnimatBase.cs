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
using LookingGlass.Framework;
using LookingGlass.World;

namespace LookingGlass.Renderer.OGL {
    public class AnimatBase {
        public int AnimatType;
        public const int AnimatTypeAny             = 0;
        public const int AnimatTypeFixedRotation   = 1;
        public const int AnimatTypeRotation        = 2;
        public const int AnimatTypePostion         = 3;

        public AnimatBase(int type) {
            AnimatType = type;
        }

        public virtual bool Process(float timeSinceLastFrame, RegionRenderInfo rri) {
            return false;   // false saying to delete
        }

        /// <summary>
        /// Loop through the list of animations for this region and call their "Process" routines
        /// </summary>
        /// <param name="timeSinceLastFrame">seconds since list frame</param>
        /// <param name="rri">RegionRenderInfo for the region</param>
        public static void ProcessAnimations(float timeSinceLastFrame, RegionRenderInfo rri) {
            lock (rri) {
                List<AnimatBase> removeAnimations = new List<AnimatBase>();
                foreach (AnimatBase ab in rri.animations) {
                    try {
                        if (!ab.Process(timeSinceLastFrame, rri)) {
                            removeAnimations.Add(ab);   // remember so we can remove later
                        }
                    }
                    catch {
                    }
                }
                // since we can't remove animations while interating the list, do it now
                foreach (AnimatBase ab in removeAnimations) {
                    rri.animations.Remove(ab);
                }
            }
        }

        /// <summary>
        /// Given an IAnimation instance (passed from comm or world), build the animation
        /// type needed.
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="id">localID of prim that looks up in RegionRenderInfo.renderPrimList</param>
        /// <returns></returns>
        public static AnimatBase CreateAnimation(IAnimation anim, uint id) {
            if (anim.DoStaticRotation) {
                // the only programmable animation we know how to do is fixed axis rotation
                return new AnimatFixedRotation(anim, id);
            }
            // default is an animation that will just exit when used
            return new AnimatBase(AnimatBase.AnimatTypeFixedRotation);
        }

    }
}
