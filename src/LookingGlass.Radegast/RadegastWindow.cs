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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Radegast;
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Renderer;

namespace LookingGlass.Radegast {

public partial class RadegastWindow : Form {

    private RadegastInstance m_radInstance;
    private LookingGlassBase m_lgb;
    private Panel m_renderPanel;
    private IRenderProvider m_renderer;
    private System.Threading.Timer m_refreshTimer;

    private PaintEventHandler m_paintEventHandler = null;
    private EventHandler m_resizeEventHandler = null;

    // Called before LookingGlass is initialized
    public RadegastWindow(RadegastInstance rinst, LookingGlassBase lgbase) {
        m_radInstance = rinst;
        m_lgb = lgbase;

        InitializeComponent();
    }

    // Called after LookingGlass is initialized
    public void Initialize() {
        // get a handle to the renderer module in LookingGlass
        string rendererName = m_lgb.AppParams.ParamString("Radegast.Renderer.Name");
        m_renderer = (IRenderProvider)m_lgb.ModManager.Module(rendererName);

        Control[] subControls = this.Controls.Find("LGWindow", true);
        if (subControls.Length == 1) {
            m_renderPanel = (Panel)subControls[0];
        }
        m_paintEventHandler = new System.Windows.Forms.PaintEventHandler(LGWindow_Paint);
        m_renderPanel.Paint += m_paintEventHandler;
        m_resizeEventHandler = new System.EventHandler(LGWindow_Resize);
        m_renderPanel.Resize += m_resizeEventHandler;

        m_refreshTimer = new System.Threading.Timer(delegate(Object param) {
            m_renderPanel.Invalidate();
        }, null, 2000, 100);

    }

    public void Shutdown() {
        // Stop LookingGlass
        m_lgb.KeepRunning = false;
        // Make sure I don't update any more
        if (m_refreshTimer != null) {
            m_refreshTimer.Dispose();
            m_refreshTimer = null;
        }
        // Those forms events are needed either
        if (m_paintEventHandler != null) m_renderPanel.Paint -= m_paintEventHandler;
        if (m_resizeEventHandler != null) m_renderPanel.Resize -= m_resizeEventHandler;
    }

    private void LGWindow_Paint(object sender, PaintEventArgs e) {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { m_renderer.RenderOneFrame(false, 100); });
        }
        else {
            m_renderer.RenderOneFrame(false, 100);
        }
        return;
    }

    private void LGWindow_Resize(object sender, EventArgs e) {
        return;
    }

    private void LGWindow_MouseDown(object sender, MouseEventArgs e) {

    }

    private void LGWindow_MouseMove(object sender, MouseEventArgs e) {

    }

    private void LGWindow_MouseLeave(object sender, EventArgs e) {

    }

    private void LGWindow_MouseEnter(object sender, EventArgs e) {

    }

    private void LGWindow_MouseUp(object sender, MouseEventArgs e) {

    }

    private void LGWindow_MouseClick(object sender, MouseEventArgs e) {

    }

    private void RadegastWindow_KeyDown(object sender, KeyEventArgs e) {

    }

    private void RadegastWindow_KeyUp(object sender, KeyEventArgs e) {

    }

}
}
