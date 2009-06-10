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

namespace LookingGlass.View {

public delegate void CameraControlUpdateCallback(CameraControl cam);

public class CameraControl {

    public event CameraControlUpdateCallback OnCameraUpdate;

    public CameraControl() {
        m_heading = new OMV.Quaternion(OMV.Vector3.UnitY, 0f);
        m_globalPosition = new OMV.Vector3d(0d, 20d, 30d);   // World coordinates (Z up)
        m_zoom = 1.0f;
        m_far = 300.0f;
        m_yawFixed = true;
    }

    protected OMV.Vector3d m_globalPosition;
    public OMV.Vector3d GlobalPosition { 
        get { return m_globalPosition; } 
        set {
            bool changed = (m_globalPosition != value);
            m_globalPosition = value;
            if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
        } 
    }

    protected OMV.Quaternion m_heading;
    public OMV.Quaternion Heading { 
        get { return m_heading; } 
        set {
            bool changed = (m_heading != value);
            m_heading = value;
            if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
        } 
    }

    protected float m_zoom;
    public float Zoom { 
        get { return m_zoom; } 
        set {
            bool changed = (m_zoom != value);
            m_zoom = value; 
            if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
        } 
    }

    protected float m_far;
    public float Far { 
        get { return m_far; } 
        set { 
            bool changed = (m_far != value);
            m_far = value; 
            if (changed && (OnCameraUpdate != null)) OnCameraUpdate(this);
        } 
    }

    // true if the camera does not tilt side to side
    protected OMV.Vector3 m_yawFixedAxis = OMV.Vector3.UnitY;
    protected bool m_yawFixed;
    public bool YawFixed { get { return m_yawFixed; } set { m_yawFixed = value; } }

    // rotate the camera by the given quaternion
    public void rotate(OMV.Quaternion rot) {
        rot.Normalize();
        m_heading = rot * m_heading;
        if (OnCameraUpdate != null) OnCameraUpdate(this);
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
        if (YawFixed) {
            // some of the rotation is around X
            OMV.Quaternion xvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitX, X);
            xvec.Normalize();
            // some of the rotation is around Z
            OMV.Quaternion zvec = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitY, Z);
            zvec.Normalize();
            m_heading = zvec * m_heading;
            m_heading = m_heading * xvec;
        }
        else {
            OMV.Quaternion rot = new OMV.Quaternion(X, Y, Z);
            rot.Normalize();
            rotate(rot);
        }
        if (OnCameraUpdate != null) OnCameraUpdate(this);
    }
}
}
