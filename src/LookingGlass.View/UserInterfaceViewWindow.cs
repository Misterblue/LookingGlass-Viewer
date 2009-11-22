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
using System.Windows.Forms;
using LookingGlass.Framework;
using LookingGlass.Framework.Modules;
using LookingGlass.Renderer;
using LookingGlass.World;

namespace LookingGlass.View {

public class UserInterfaceViewWindow : ModuleBase, IUserInterfaceProvider  {

# pragma warning disable 0067   // disable unused event warning
    public event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
    public event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
    public event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
    public event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;
# pragma warning restore 0067

    public UserInterfaceViewWindow() {
    }

    IUserInterfaceProvider m_ui;

    #region ModuleBase
    // IModule.OnLoad
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        m_ui = new UserInterfaceCommon();
        m_ui.OnUserInterfaceKeypress += UI_OnUserInterfaceKeypress;
        m_ui.OnUserInterfaceMouseMove += UI_OnUserInterfaceMouseMove;
        m_ui.OnUserInterfaceMouseButton += UI_OnUserInterfaceMouseButton;
        m_ui.OnUserInterfaceEntitySelected += UI_OnUserInterfaceEntitySelected;
    }

    private void UI_OnUserInterfaceKeypress(Keys key, bool updown) {
        if (OnUserInterfaceKeypress != null) OnUserInterfaceKeypress(key, updown);
    }
    private void UI_OnUserInterfaceMouseMove(int parm, float x, float y) {
        if (OnUserInterfaceMouseMove != null) OnUserInterfaceMouseMove(parm, x, y);
    }
    private void UI_OnUserInterfaceMouseButton(MouseButtons mbut, bool updown) {
        if (OnUserInterfaceMouseButton != null) OnUserInterfaceMouseButton(mbut, updown);
    }
    private void UI_OnUserInterfaceEntitySelected(IEntity ent) {
        if (OnUserInterfaceEntitySelected != null) OnUserInterfaceEntitySelected(ent);
    }

    // IModule.AfterAllModulesLoaded
    public override bool AfterAllModulesLoaded() {
        return true;
    }

    // IModule.Start
    public override void Start() { }

    // IModule.Stop
    public override void Stop() { }
    #endregion IModule

    #region IUserInterfaceProvider
    // IUserInterfaceProvider.InputModeCode
    public InputModeCode InputMode { get { return m_ui.InputMode; } set { m_ui.InputMode = value; } }

    // IUserInterfaceProvider.LastKeyCode
    public Keys LastKeyCode { get { return m_ui.LastKeyCode; } set { m_ui.LastKeyCode = value; } }

    // IUserInterfaceProvider.KeyPressed
    public bool KeyPressed { get { return m_ui.KeyPressed; } set { m_ui.KeyPressed = value; } }

    // IUserInterfaceProvider.LastMouseButtons
    public MouseButtons LastMouseButtons { get { return m_ui.LastMouseButtons; } set { m_ui.LastMouseButtons = value; } }

    // IUserInterfaceProvider.KeyRepeatRate
    public float KeyRepeatRate { get { return m_ui.KeyRepeatRate; } set { m_ui.KeyRepeatRate = value; } }

    // IUserInterfaceProvider.ReceiveUserIO
    public void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3) {
        m_ui.ReceiveUserIO(type, param1, param2, param3);
    }

    // IUserInterfaceProvider.NeedsRendererLinkage
    public bool NeedsRendererLinkage() {
        // don't hook me up with the low level stuff
        return false;
    }
    #endregion IUserInterfaceProvider

    public void Dispose() {
        if (m_ui != null) {
            m_ui.Dispose();
            m_ui = null;
        }
        return;
    }
}
}
