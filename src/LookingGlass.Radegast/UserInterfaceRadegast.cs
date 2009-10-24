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
using LookingGlass.Renderer;

namespace LookingGlass.Radegast {

public class UserInterfaceRadegast : IUserInterfaceProvider {

# pragma warning disable 0067   // disable unused event warning
    public event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
    public event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
    public event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
    public event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;
# pragma warning restore 0067

    public UserInterfaceRadegast() {
    }

    // IUserInterfaceProvider.InputModeCode
    private InputModeCode m_inputMode;
    public InputModeCode InputMode { 
        get { return m_inputMode; }
        set { m_inputMode = value; }
    }

    // IUserInterfaceProvider.LastKeyCode
    private Keys m_lastKeycode = 0;
    public Keys LastKeyCode {
        get { return m_lastKeycode; }
        set { m_lastKeycode = value; }
    }

    // IUserInterfaceProvider.KeyPressed
    private bool m_keyPressed = false;
    public bool KeyPressed {
        get { return m_keyPressed; }
        set { m_keyPressed = value; }
    }

    // IUserInterfaceProvider.LastMouseButtons
    private MouseButtons m_lastButtons = 0;
    public MouseButtons LastMouseButtons {
        get { return m_lastButtons; }
        set { m_lastButtons = value; }
    }

    // IUserInterfaceProvider.KeyRepeatRate
    private float m_repeatRate = 3f;
    public float KeyRepeatRate {
        get { return m_repeatRate; }
        set { m_repeatRate = value; }
    }

    // IUserInterfaceProvider.ReceiveUserIO
    public void ReceiveUserIO(int type, int param1, float param2, float param3) {
        return;
    }

    // IUserInterfaceProvider.NeedsRendererLinkage
    public bool NeedsRendererLinkage() {
        // don't hook me up with the low level stuff
        return false;
    }
}
}
