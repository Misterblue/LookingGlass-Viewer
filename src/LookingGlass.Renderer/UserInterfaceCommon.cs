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
using System.Threading;
using System.Windows.Forms;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Renderer;
using LookingGlass.Framework.WorkQueue;

namespace LookingGlass.Renderer {
public class UserInterfaceCommon : IUserInterfaceProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

# pragma warning disable 0067   // disable unused event warning
    public event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
    public event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
    public event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
    public event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;
# pragma warning restore 0067

    private InputModeCode m_inputMode;
    public InputModeCode InputMode {
        get { return m_inputMode; }
        set { m_inputMode = value; }
    }

    /// <summary>
    ///  Remember the last key code we returned. Mostly to remember which modifier
    ///  keys are on.
    /// </summary>
    private Keys m_lastKeycode = 0;
    public Keys LastKeyCode {
        get { return m_lastKeycode; }
        set { m_lastKeycode = value; }
    }

    /// <summary>
    ///  Whether a key is up or pressed at the moment.
    /// </summary>
    private bool m_keyPressed = false;
    public bool KeyPressed {
        get { return m_keyPressed; }
        set { m_keyPressed = value; }
    }

    /// <summary>
    /// Remember the last (current) mouse button positions for easy checking
    /// </summary>
    private MouseButtons m_lastButtons = 0;
    public MouseButtons LastMouseButtons {
        get { return m_lastButtons; }
        set { m_lastButtons = value; }
    }

    /// <summary>
    /// The rate to repeat the keys (repeats per second). Zero says no repeat
    /// </summary>
    private float m_keyRepeatRate = 3f;
    public float KeyRepeatRate {
        get { return m_keyRepeatRate; }
        set { 
            m_keyRepeatRate = value;
            m_keyRepeatMs = (int)(1000f / m_keyRepeatRate);
        }
    }
    private int m_keyRepeatMs = 333;
    public int KeyRepeatMs { get { return m_keyRepeatMs; } }
    public System.Threading.Timer m_repeatTimer;
    public int m_repeatKey;    // the raw key code that is being repeated

    BasicWorkQueue m_workQueue;

    public UserInterfaceCommon() {
        m_workQueue = new BasicWorkQueue("UserInterfaceCommon");
        m_repeatTimer = new System.Threading.Timer(OnRepeatTimer); 
    }

    // I need the hooks to the lowest levels
    public bool NeedsRendererLinkage() {
        return false;
    }

    // If the key is still held down, fake a key press
    private void OnRepeatTimer(Object xx) {
        if (this.KeyPressed) {
            // fake receiving another key press
            ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, m_repeatKey, 0f, 0f);
            // m_log.Log(LogLevel.DBADERROR, "OnRepeatTimer: Faking key {0}", m_repeatKey);
        }
        else { 
            // key not pressed so don't repeat any more
            m_repeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            // m_log.Log(LogLevel.DBADERROR, "OnRepeatTimer: Disabling timer");
        }
    }

    // IUserInterfaceProvider.ReceiveUserIO
    /// <summary>
    /// One of the input subsystems has received a charaaction or mouse. Queue the
    /// processing to delink us from the IO thread.
    /// If a typed char, we use it to update the modifiers (alt, ...) and  then
    /// assemble the keycode or'ed with the current modifers into this.LastKeyCode.
    /// Then, anyone waiting is given a callback.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    public void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3) {
        Object[] receiveLaterParams = { type, param1, param2, param3 };
        m_workQueue.DoLater(ReceiveLater, receiveLaterParams);
        return;
    }

    /// <summary>
    /// One of the input subsystems has received a key or mouse press.
    /// </summary>
    /// <param name="qInstance"></param>
    /// <param name="parms"></param>
    /// <returns></returns>
    private bool ReceiveLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        ReceiveUserIOInputEventTypeCode typ = (ReceiveUserIOInputEventTypeCode)loadParams[0];
        int param1 = (int)loadParams[1];
        float param2 = (float)loadParams[2];
        float param3 = (float)loadParams[3];
        
        switch (typ) {
            case ReceiveUserIOInputEventTypeCode.KeyPress:
                param1 = param1 & (int)Keys.KeyCode; // remove extra cruft
                m_log.Log(LogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: KeyPress: {0}", param1);
                this.UpdateModifier(param1, true);
                AddKeyToLastKeyCode(param1);
                this.m_repeatKey = param1;
                this.KeyPressed = true;
                this.m_repeatTimer.Change(this.KeyRepeatMs, this.KeyRepeatMs);
                if (this.OnUserInterfaceKeypress != null)
                    this.OnUserInterfaceKeypress(this.LastKeyCode, true);
                break;
            case ReceiveUserIOInputEventTypeCode.KeyRelease:
                param1 = param1 & (int)Keys.KeyCode; // remove extra cruft
                m_log.Log(LogLevel.DVIEWDETAIL, "UserInterfaceCommon: ReceiveLater: KeyRelease: {0}", param1);
                this.UpdateModifier(param1, false);
                AddKeyToLastKeyCode(param1);
                // this.LastKeyCode = (Keys)param1;
                this.KeyPressed = false;
                this.m_repeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                if (this.OnUserInterfaceKeypress != null)
                    this.OnUserInterfaceKeypress(this.LastKeyCode, false);
                break;
            case ReceiveUserIOInputEventTypeCode.MouseButtonDown:
                this.UpdateMouseModifier(param1, true);
                if (this.OnUserInterfaceMouseButton != null) this.OnUserInterfaceMouseButton(
                                ThisMouseButtonCode(param1), false);
                break;
            case ReceiveUserIOInputEventTypeCode.MouseButtonUp:
                this.UpdateMouseModifier(param1, true);
                if (this.OnUserInterfaceMouseButton != null) this.OnUserInterfaceMouseButton(
                                ThisMouseButtonCode(param1), true);
                break;
            case ReceiveUserIOInputEventTypeCode.MouseMove:
                // pass the routine tracking the raw position information
                // param1 is usually zero (actually mouse selector but we have only one at the moment)
                // param2 is the X movement
                // param3 is the Y movement
                if (this.OnUserInterfaceMouseMove != null) this.OnUserInterfaceMouseMove(param1, param2, param3);
                break;
        }
        // successful
        return true;
    }

    private void AddKeyToLastKeyCode(int kee) {
        this.LastKeyCode = (this.LastKeyCode & Keys.Modifiers) | (Keys)kee;
    }

    /// <summary>
    /// Keep the modifier key information in a place that is easy to check later
    /// </summary>
    /// <param name="param1">OISKeyCode of the key pressed</param>
    /// <param name="updown">true if the key is down, false otherwise</param>
    private void UpdateModifier(int param1, bool updown) {
        Keys kparam1 = (Keys)param1;
        if (kparam1 == Keys.LMenu || kparam1 == Keys.RMenu) {
            if (updown && ((LastKeyCode & Keys.Alt) == 0)) {
                LastKeyCode |= Keys.Alt;
            }
            if (!updown && ((LastKeyCode & Keys.Alt) != 0)) {
                LastKeyCode ^= Keys.Alt;
            }
        }
        if (kparam1 == Keys.RShiftKey || kparam1 == Keys.LShiftKey) {
            if (updown && ((LastKeyCode & Keys.Shift) == 0)) {
                LastKeyCode |= Keys.Shift;
            }
            if (!updown && ((LastKeyCode & Keys.Shift) != 0)) {
                LastKeyCode ^= Keys.Shift;
            }
        }
        if (kparam1 == Keys.LControlKey || kparam1 == Keys.RControlKey) {
            if (updown && ((LastKeyCode & Keys.Control) == 0)) {
                LastKeyCode |= Keys.Control;
            }
            if (!updown && ((LastKeyCode & Keys.Control) != 0)) {
                LastKeyCode ^= Keys.Control;
            }
        }
    }

    private static MouseButtons ThisMouseButtonCode(int iosCode) {
        MouseButtons ret = MouseButtons.None;
        switch ((ReceiveUserIOMouseButtonCode)iosCode) {
            case ReceiveUserIOMouseButtonCode.Left:
                ret = MouseButtons.Left;
                break;
            case ReceiveUserIOMouseButtonCode.Right:
                ret = MouseButtons.Right;
                break;
            case ReceiveUserIOMouseButtonCode.Middle:
                ret = MouseButtons.Middle;
                break;
        }
        return ret;
    }

    /// <summary>
    /// Keep the mouse button state in a varaible for easy reference
    /// </summary>
    /// <param name="param1">OISKeyCode of the key pressed</param>
    /// <param name="updown">true if the key is down, false otherwise</param>
    private void UpdateMouseModifier(int param1, bool updown) {
        if (param1 == (int)ReceiveUserIOMouseButtonCode.Left) {
            if (updown) m_lastButtons |= MouseButtons.Left;
            if (!updown && (m_lastButtons & MouseButtons.Left) != 0) m_lastButtons ^= MouseButtons.Left;
        }
        if (param1 == (int)ReceiveUserIOMouseButtonCode.Right) {
            if (updown) m_lastButtons |= MouseButtons.Right;
            if (!updown && (m_lastButtons & MouseButtons.Right) != 0) m_lastButtons ^= MouseButtons.Right;
        }
        if (param1 == (int)ReceiveUserIOMouseButtonCode.Middle) {
            if (updown) m_lastButtons |= MouseButtons.Middle;
            if (!updown && (m_lastButtons & MouseButtons.Middle) != 0) m_lastButtons ^= MouseButtons.Middle;
        }
    }
}
}
