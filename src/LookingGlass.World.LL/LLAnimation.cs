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
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
public class LLAnimation : IAnimation {

    // for the moment, there is not much to an animation
    private OMV.Vector3 m_angularVelocity = OMV.Vector3.Zero;
    public OMV.Vector3 AngularVelocity {
        get { return m_angularVelocity; }
        set { m_angularVelocity = value; }
    }

    private bool m_doStaticRotation = false;
    public bool DoStaticRotation {
        get { return m_doStaticRotation; }
        set { m_doStaticRotation = value; }
    }
    private OMV.Vector3 m_staticRotationAxis = OMV.Vector3.Zero;
    public OMV.Vector3 StaticRotationAxis {
        get { return m_staticRotationAxis; }
        set { m_staticRotationAxis = value; }
    }
    private float m_staticRotationRotPerSec = 0f;
    public float StaticRotationRotPerSec {
        get { return m_staticRotationRotPerSec; }
        set { m_staticRotationRotPerSec = value; }
    }


}
}
