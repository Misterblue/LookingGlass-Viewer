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
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Renderer;

namespace LookingGlass.View {
public partial class ViewWindow : Form {

    private LookingGlassBase m_lgb;
    private Panel m_renderPanel;
    private IRenderProvider m_renderer;
    private System.Threading.Timer m_refreshTimer;
    private int m_framesPerSec;

    private PaintEventHandler m_paintEventHandler = null;
    private EventHandler m_resizeEventHandler = null;

    public ViewWindow(LookingGlassBase lgbase) {
        m_lgb = lgbase;

        InitializeComponent();
    }

    // Called after LookingGlass is initialized
    public void Initialize() {
        // get a handle to the renderer module in LookingGlass
        string rendererName = m_lgb.AppParams.ParamString("Viewer.Renderer.Name");
        m_framesPerSec = m_lgb.AppParams.ParamInt("Viewer.FramesPerSec");
        m_renderer = (IRenderProvider)m_lgb.ModManager.Module(rendererName);

        Control[] subControls = this.Controls.Find("viewPanel", true);
        if (subControls.Length == 1) {
            m_renderPanel = (Panel)subControls[0];
        }
        m_paintEventHandler = new System.Windows.Forms.PaintEventHandler(viewControl_Paint);
        m_renderPanel.Paint += m_paintEventHandler;
        m_resizeEventHandler = new System.EventHandler(viewControl_Resize);
        m_renderPanel.Resize += m_resizeEventHandler;
        // m_renderPanel.MouseClick
        // m_renderPanel.MouseWheel
        // m_renderPanel.MouseMove
        // m_renderPanel.MouseLeave
        // m_renderPanel.MouseDown
        // m_renderPanel.MouseUp

        m_refreshTimer = new System.Threading.Timer(delegate(Object param) {
            m_renderPanel.Invalidate();
        }, null, 2000, 100);

    }
    
    private void ViewWindow_Load(object sender, EventArgs e) {
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

    public void viewControl_Paint(object sender, PaintEventArgs e) {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { m_renderer.RenderOneFrame(false, 100); });
        }
        else {
            m_renderer.RenderOneFrame(false, 100);
        }
        return;
    }

    public void viewControl_Resize(object sender, EventArgs e) {
        return;
    }
}
}
