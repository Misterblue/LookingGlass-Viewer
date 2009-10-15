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
        m_paintEventHandler = new System.Windows.Forms.PaintEventHandler(radControl_Paint);
        m_renderPanel.Paint += m_paintEventHandler;
        m_resizeEventHandler = new System.EventHandler(radControl_Resize);
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

    public void radControl_Paint(object sender, PaintEventArgs e) {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { m_renderer.RenderOneFrame(false, 100); });
        }
        else {
            m_renderer.RenderOneFrame(false, 100);
        }
        return;
    }

    public void radControl_Resize(object sender, EventArgs e) {
        return;
    }
}
}
