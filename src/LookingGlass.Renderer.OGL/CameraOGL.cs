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
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OMV = OpenMetaverse;

namespace LookingGlass.Renderer.OGL {
public class CameraOGL {
    private bool m_updated = true;
    /// <summary>
    /// True of the camera position or lookat have been changed. The caller must
    /// be the one to set this back to false;
    /// </summary>
    public bool Updated {
        get { return m_updated; }
        set { m_updated = value; }
    }
    private OMV.Vector3 m_position;
    public OMV.Vector3 Position {
        get { return m_position; }
        set { m_position = value; m_updated = true;  }
    }
    private OMV.Vector3 m_focalPoint;
    public OMV.Vector3 FocalPoint {
        get { return m_focalPoint; }
        set { m_focalPoint = value; m_updated = true; }
    }
    public double Zoom;
    public double Far;

    private float[,] m_frustum;

    public void ComputeFrustum() {
        // m_frustum = ExtractFrustum();
    }

    public bool isVisible(float x, float y, float z, float radius) {
        // return SphereInFrustum(m_frustum, x, y, z, radius);
        return true;
    }

    // http://www.crownandcutlass.com/features/technicaldetails/frustum.html
    private bool PointInFrustum(float[,] frustum, float x, float y, float z) {
        for (int p = 0; p < 6; p++) {
             if( frustum[p,0] * x + frustum[p,1] * y + frustum[p,2] * z + frustum[p,3] <= 0 )
                 return false;
        }
        return true;
    }

    private bool SphereInFrustum( float[,] frustum, float x, float y, float z, float radius ) {
        for(int p = 0; p < 6; p++ )
            if( frustum[p,0] * x + frustum[p,1] * y + frustum[p,2] * z + frustum[p,3] <= -radius )
                return false;
        return true;
    }

    private bool CubeInFrustum(float[,] frustum, float x, float y, float z, float size ) {
        for( int p = 0; p < 6; p++ ) {
            if( frustum[p,0] * (x - size) + frustum[p,1] * (y - size) + frustum[p,2] * (z - size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x + size) + frustum[p,1] * (y - size) + frustum[p,2] * (z - size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x - size) + frustum[p,1] * (y + size) + frustum[p,2] * (z - size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x + size) + frustum[p,1] * (y + size) + frustum[p,2] * (z - size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x - size) + frustum[p,1] * (y - size) + frustum[p,2] * (z + size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x + size) + frustum[p,1] * (y - size) + frustum[p,2] * (z + size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x - size) + frustum[p,1] * (y + size) + frustum[p,2] * (z + size) + frustum[p,3] > 0 )
                continue;
            if( frustum[p,0] * (x + size) + frustum[p,1] * (y + size) + frustum[p,2] * (z + size) + frustum[p,3] > 0 )
                continue;
            return false;
        }
        return true;
    }

    private float[,] ExtractFrustum() {
        float[]   proj = new float[16];
        float[]   modl = new float[16];
        float[]   clip = new float[16];
        float   t;
        float[,]   frustum = new float[6,4];

       /* Get the current PROJECTION matrix from OpenGL */
       GL.GetFloat(GetPName.ProjectionMatrix, proj );

       /* Get the current MODELVIEW matrix from OpenGL */
       GL.GetFloat(GetPName.ModelviewMatrix, modl);

       /* Combine the two matrices (multiply projection by modelview) */
       clip[ 0] = modl[ 0] * proj[ 0] + modl[ 1] * proj[ 4] + modl[ 2] * proj[ 8] + modl[ 3] * proj[12];
       clip[ 1] = modl[ 0] * proj[ 1] + modl[ 1] * proj[ 5] + modl[ 2] * proj[ 9] + modl[ 3] * proj[13];
       clip[ 2] = modl[ 0] * proj[ 2] + modl[ 1] * proj[ 6] + modl[ 2] * proj[10] + modl[ 3] * proj[14];
       clip[ 3] = modl[ 0] * proj[ 3] + modl[ 1] * proj[ 7] + modl[ 2] * proj[11] + modl[ 3] * proj[15];

       clip[ 4] = modl[ 4] * proj[ 0] + modl[ 5] * proj[ 4] + modl[ 6] * proj[ 8] + modl[ 7] * proj[12];
       clip[ 5] = modl[ 4] * proj[ 1] + modl[ 5] * proj[ 5] + modl[ 6] * proj[ 9] + modl[ 7] * proj[13];
       clip[ 6] = modl[ 4] * proj[ 2] + modl[ 5] * proj[ 6] + modl[ 6] * proj[10] + modl[ 7] * proj[14];
       clip[ 7] = modl[ 4] * proj[ 3] + modl[ 5] * proj[ 7] + modl[ 6] * proj[11] + modl[ 7] * proj[15];

       clip[ 8] = modl[ 8] * proj[ 0] + modl[ 9] * proj[ 4] + modl[10] * proj[ 8] + modl[11] * proj[12];
       clip[ 9] = modl[ 8] * proj[ 1] + modl[ 9] * proj[ 5] + modl[10] * proj[ 9] + modl[11] * proj[13];
       clip[10] = modl[ 8] * proj[ 2] + modl[ 9] * proj[ 6] + modl[10] * proj[10] + modl[11] * proj[14];
       clip[11] = modl[ 8] * proj[ 3] + modl[ 9] * proj[ 7] + modl[10] * proj[11] + modl[11] * proj[15];

       clip[12] = modl[12] * proj[ 0] + modl[13] * proj[ 4] + modl[14] * proj[ 8] + modl[15] * proj[12];
       clip[13] = modl[12] * proj[ 1] + modl[13] * proj[ 5] + modl[14] * proj[ 9] + modl[15] * proj[13];
       clip[14] = modl[12] * proj[ 2] + modl[13] * proj[ 6] + modl[14] * proj[10] + modl[15] * proj[14];
       clip[15] = modl[12] * proj[ 3] + modl[13] * proj[ 7] + modl[14] * proj[11] + modl[15] * proj[15];

       /* Extract the numbers for the RIGHT plane */
       frustum[0,0] = clip[ 3] - clip[ 0];
       frustum[0,1] = clip[ 7] - clip[ 4];
       frustum[0,2] = clip[11] - clip[ 8];
       frustum[0,3] = clip[15] - clip[12];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[0,0] * frustum[0,0] + frustum[0,1] * frustum[0,1] + frustum[0,2] * frustum[0,2] );
       frustum[0,0] /= t;
       frustum[0,1] /= t;
       frustum[0,2] /= t;
       frustum[0,3] /= t;

       /* Extract the numbers for the LEFT plane */
       frustum[1,0] = clip[ 3] + clip[ 0];
       frustum[1,1] = clip[ 7] + clip[ 4];
       frustum[1,2] = clip[11] + clip[ 8];
       frustum[1,3] = clip[15] + clip[12];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[1,0] * frustum[1,0] + frustum[1,1] * frustum[1,1] + frustum[1,2] * frustum[1,2] );
       frustum[1,0] /= t;
       frustum[1,1] /= t;
       frustum[1,2] /= t;
       frustum[1,3] /= t;

       /* Extract the BOTTOM plane */
       frustum[2,0] = clip[ 3] + clip[ 1];
       frustum[2,1] = clip[ 7] + clip[ 5];
       frustum[2,2] = clip[11] + clip[ 9];
       frustum[2,3] = clip[15] + clip[13];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[2,0] * frustum[2,0] + frustum[2,1] * frustum[2,1] + frustum[2,2] * frustum[2,2] );
       frustum[2,0] /= t;
       frustum[2,1] /= t;
       frustum[2,2] /= t;
       frustum[2,3] /= t;

       /* Extract the TOP plane */
       frustum[3,0] = clip[ 3] - clip[ 1];
       frustum[3,1] = clip[ 7] - clip[ 5];
       frustum[3,2] = clip[11] - clip[ 9];
       frustum[3,3] = clip[15] - clip[13];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[3,0] * frustum[3,0] + frustum[3,1] * frustum[3,1] + frustum[3,2] * frustum[3,2] );
       frustum[3,0] /= t;
       frustum[3,1] /= t;
       frustum[3,2] /= t;
       frustum[3,3] /= t;

       /* Extract the FAR plane */
       frustum[4,0] = clip[ 3] - clip[ 2];
       frustum[4,1] = clip[ 7] - clip[ 6];
       frustum[4,2] = clip[11] - clip[10];
       frustum[4,3] = clip[15] - clip[14];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[4,0] * frustum[4,0] + frustum[4,1] * frustum[4,1] + frustum[4,2] * frustum[4,2] );
       frustum[4,0] /= t;
       frustum[4,1] /= t;
       frustum[4,2] /= t;
       frustum[4,3] /= t;

       /* Extract the NEAR plane */
       frustum[5,0] = clip[ 3] + clip[ 2];
       frustum[5,1] = clip[ 7] + clip[ 6];
       frustum[5,2] = clip[11] + clip[10];
       frustum[5,3] = clip[15] + clip[14];

       /* Normalize the result */
       t = (float)Math.Sqrt( frustum[5,0] * frustum[5,0] + frustum[5,1] * frustum[5,1] + frustum[5,2] * frustum[5,2] );
       frustum[5,0] /= t;
       frustum[5,1] /= t;
       frustum[5,2] /= t;
       frustum[5,3] /= t;

        return frustum;
    }

}
}
