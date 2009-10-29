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

    private IUserInterfaceProvider m_UILink = null;
    private bool m_MouseIn = false;     // true if mouse is over our window
    private float m_MouseLastX = -3456f;
    private float m_MouseLastY = -3456f;

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

        string uiName = m_lgb.AppParams.ParamString("Viewer.UI.Name");
        m_UILink = (IUserInterfaceProvider)m_lgb.ModManager.Module(uiName);
        if (m_UILink == null) {
            LogManager.Log.Log(LogLevel.DBADERROR, "ViewerWindow.Initialize: COULD NOT ATTACH UI INTERFACE '{0};", uiName);
        }
        else {
            LogManager.Log.Log(LogLevel.DVIEWDETAIL, "ViewerWindow.Initialize: Successfully attached UI");
        }

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
    }

    public void LGWindow_Paint(object sender, PaintEventArgs e) {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { m_renderer.RenderOneFrame(false, 100); });
        }
        else {
            m_renderer.RenderOneFrame(false, 100);
        }
        return;
    }

    public void LGWindow_Resize(object sender, EventArgs e) {
        return;
    }

    private void LGWindow_MouseDown(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonDown, butn, 0f, 0f);
        }
    }

    private void LGWindow_MouseMove(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            int butn = ConvertMouseButtonCode(e.Button);
            if (m_MouseLastX == -3456f) m_MouseLastX = e.X;
            if (m_MouseLastY == -3456f) m_MouseLastY = e.Y;
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseMove, butn,
                            e.X - m_MouseLastX, e.Y - m_MouseLastY);
            m_MouseLastX = e.X;
            m_MouseLastY = e.Y;
        }
    }

    private void LGWindow_MouseLeave(object sender, EventArgs e) {
        m_MouseIn = false;
    }

    private void LGWindow_MouseEnter(object sender, EventArgs e) {
        m_MouseIn = true;
    }

    private void LGWindow_MouseUp(object sender, MouseEventArgs e) {
        if (m_UILink != null) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonUp, butn, 0f, 0f);
        }
    }

    private void RadegastWindow_KeyDown(object sender, KeyEventArgs e) {
        if (m_UILink != null) {
            LogManager.Log.Log(LogLevel.DVIEWDETAIL, "RadegastWindow.LGWindow_KeyDown: k={0}", e.KeyCode);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, (int)e.KeyCode, 0f, 0f);
        }
    }

    private void RadegastWindow_KeyUp(object sender, KeyEventArgs e) {
        if (m_UILink != null) {
            LogManager.Log.Log(LogLevel.DVIEWDETAIL, "RadegastWindow.LGWindow_KeyUp: k={0}", e.KeyCode);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyRelease, (int)e.KeyCode, 0f, 0f);
        }
    }

    private int ConvertMouseButtonCode(MouseButtons inCode) {
        if ((inCode & MouseButtons.Left) != 0) return (int)ReceiveUserIOMouseButtonCode.Left;
        if ((inCode & MouseButtons.Right) != 0) return (int)ReceiveUserIOMouseButtonCode.Right;
        if ((inCode & MouseButtons.Middle) != 0) return (int)ReceiveUserIOMouseButtonCode.Middle;
        return 0;
    }

}
}
