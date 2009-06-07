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
using LookingGlass.Framework.Logging;
using LookingGlass.Renderer;
using LookingGlass.Framework.WorkQueue;

namespace LookingGlass.Renderer.Ogr {
public class UserInterfaceOgre : IUserInterfaceProvider {
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

    // the key codes as they come from OIS
    public enum OISKeyCode {
		KC_UNASSIGNED  = 0x00,
		KC_ESCAPE      = 0x01,
		KC_1           = 0x02,
		KC_2           = 0x03,
		KC_3           = 0x04,
		KC_4           = 0x05,
		KC_5           = 0x06,
		KC_6           = 0x07,
		KC_7           = 0x08,
		KC_8           = 0x09,
		KC_9           = 0x0A,
		KC_0           = 0x0B,
		KC_MINUS       = 0x0C,    // - on main keyboard
		KC_EQUALS      = 0x0D,
		KC_BACK        = 0x0E,    // backspace
		KC_TAB         = 0x0F,
		KC_Q           = 0x10,
		KC_W           = 0x11,
		KC_E           = 0x12,
		KC_R           = 0x13,
		KC_T           = 0x14,
		KC_Y           = 0x15,
		KC_U           = 0x16,
		KC_I           = 0x17,
		KC_O           = 0x18,
		KC_P           = 0x19,
		KC_LBRACKET    = 0x1A,
		KC_RBRACKET    = 0x1B,
		KC_RETURN      = 0x1C,    // Enter on main keyboard
		KC_LCONTROL    = 0x1D,
		KC_A           = 0x1E,
		KC_S           = 0x1F,
		KC_D           = 0x20,
		KC_F           = 0x21,
		KC_G           = 0x22,
		KC_H           = 0x23,
		KC_J           = 0x24,
		KC_K           = 0x25,
		KC_L           = 0x26,
		KC_SEMICOLON   = 0x27,
		KC_APOSTROPHE  = 0x28,
		KC_GRAVE       = 0x29,    // accent
		KC_LSHIFT      = 0x2A,
		KC_BACKSLASH   = 0x2B,
		KC_Z           = 0x2C,
		KC_X           = 0x2D,
		KC_C           = 0x2E,
		KC_V           = 0x2F,
		KC_B           = 0x30,
		KC_N           = 0x31,
		KC_M           = 0x32,
		KC_COMMA       = 0x33,
		KC_PERIOD      = 0x34,    // . on main keyboard
		KC_SLASH       = 0x35,    // / on main keyboard
		KC_RSHIFT      = 0x36,
		KC_MULTIPLY    = 0x37,    // * on numeric keypad
		KC_LMENU       = 0x38,    // left Alt
		KC_SPACE       = 0x39,
		KC_CAPITAL     = 0x3A,
		KC_F1          = 0x3B,
		KC_F2          = 0x3C,
		KC_F3          = 0x3D,
		KC_F4          = 0x3E,
		KC_F5          = 0x3F,
		KC_F6          = 0x40,
		KC_F7          = 0x41,
		KC_F8          = 0x42,
		KC_F9          = 0x43,
		KC_F10         = 0x44,
		KC_NUMLOCK     = 0x45,
		KC_SCROLL      = 0x46,    // Scroll Lock
		KC_NUMPAD7     = 0x47,
		KC_NUMPAD8     = 0x48,
		KC_NUMPAD9     = 0x49,
		KC_SUBTRACT    = 0x4A,    // - on numeric keypad
		KC_NUMPAD4     = 0x4B,
		KC_NUMPAD5     = 0x4C,
		KC_NUMPAD6     = 0x4D,
		KC_ADD         = 0x4E,    // + on numeric keypad
		KC_NUMPAD1     = 0x4F,
		KC_NUMPAD2     = 0x50,
		KC_NUMPAD3     = 0x51,
		KC_NUMPAD0     = 0x52,
		KC_DECIMAL     = 0x53,    // . on numeric keypad
		KC_OEM_102     = 0x56,    // < > | on UK/Germany keyboards
		KC_F11         = 0x57,
		KC_F12         = 0x58,
		KC_F13         = 0x64,    //                     (NEC PC98)
		KC_F14         = 0x65,    //                     (NEC PC98)
		KC_F15         = 0x66,    //                     (NEC PC98)
		KC_KANA        = 0x70,    // (Japanese keyboard)
		KC_ABNT_C1     = 0x73,    // / ? on Portugese (Brazilian) keyboards
		KC_CONVERT     = 0x79,    // (Japanese keyboard)
		KC_NOCONVERT   = 0x7B,    // (Japanese keyboard)
		KC_YEN         = 0x7D,    // (Japanese keyboard)
		KC_ABNT_C2     = 0x7E,    // Numpad . on Portugese (Brazilian) keyboards
		KC_NUMPADEQUALS= 0x8D,    // = on numeric keypad (NEC PC98)
		KC_PREVTRACK   = 0x90,    // Previous Track (KC_CIRCUMFLEX on Japanese keyboard)
		KC_AT          = 0x91,    //                     (NEC PC98)
		KC_COLON       = 0x92,    //                     (NEC PC98)
		KC_UNDERLINE   = 0x93,    //                     (NEC PC98)
		KC_KANJI       = 0x94,    // (Japanese keyboard)
		KC_STOP        = 0x95,    //                     (NEC PC98)
		KC_AX          = 0x96,    //                     (Japan AX)
		KC_UNLABELED   = 0x97,    //                        (J3100)
		KC_NEXTTRACK   = 0x99,    // Next Track
		KC_NUMPADENTER = 0x9C,    // Enter on numeric keypad
		KC_RCONTROL    = 0x9D,
		KC_MUTE        = 0xA0,    // Mute
		KC_CALCULATOR  = 0xA1,    // Calculator
		KC_PLAYPAUSE   = 0xA2,    // Play / Pause
		KC_MEDIASTOP   = 0xA4,    // Media Stop
		KC_VOLUMEDOWN  = 0xAE,    // Volume -
		KC_VOLUMEUP    = 0xB0,    // Volume +
		KC_WEBHOME     = 0xB2,    // Web home
		KC_NUMPADCOMMA = 0xB3,    // , on numeric keypad (NEC PC98)
		KC_DIVIDE      = 0xB5,    // / on numeric keypad
		KC_SYSRQ       = 0xB7,
		KC_RMENU       = 0xB8,    // right Alt
		KC_PAUSE       = 0xC5,    // Pause
		KC_HOME        = 0xC7,    // Home on arrow keypad
		KC_UP          = 0xC8,    // UpArrow on arrow keypad
		KC_PGUP        = 0xC9,    // PgUp on arrow keypad
		KC_LEFT        = 0xCB,    // LeftArrow on arrow keypad
		KC_RIGHT       = 0xCD,    // RightArrow on arrow keypad
		KC_END         = 0xCF,    // End on arrow keypad
		KC_DOWN        = 0xD0,    // DownArrow on arrow keypad
		KC_PGDOWN      = 0xD1,    // PgDn on arrow keypad
		KC_INSERT      = 0xD2,    // Insert on arrow keypad
		KC_DELETE      = 0xD3,    // Delete on arrow keypad
		KC_LWIN        = 0xDB,    // Left Windows key
		KC_RWIN        = 0xDC,    // Right Windows key
		KC_APPS        = 0xDD,    // AppMenu key
		KC_POWER       = 0xDE,    // System Power
		KC_SLEEP       = 0xDF,    // System Sleep
		KC_WAKE        = 0xE3,    // System Wake
		KC_WEBSEARCH   = 0xE5,    // Web Search
		KC_WEBFAVORITES= 0xE6,    // Web Favorites
		KC_WEBREFRESH  = 0xE7,    // Web Refresh
		KC_WEBSTOP     = 0xE8,    // Web Stop
		KC_WEBFORWARD  = 0xE9,    // Web Forward
		KC_WEBBACK     = 0xEA,    // Web Back
		KC_MYCOMPUTER  = 0xEB,    // My Computer
		KC_MAIL        = 0xEC,    // Mail
		KC_MEDIASELECT = 0xED     // Media Select
	};
    public static Keys[] keyConversion = {
                   /* 0x00 */
                   0x00, Keys.Escape, Keys.D1, Keys.D2,
                   Keys.D3, Keys.D4, Keys.D5, Keys.D6,
                   Keys.D7, Keys.D8, Keys.D9, Keys.D0,
                   Keys.OemMinus, (Keys)0x3D /*equals*/, Keys.Back, Keys.Tab,
                   /* 0x10 */
                   Keys.Q, Keys.W, Keys.E, Keys.R,
                   Keys.T, Keys.Y, Keys.U, Keys.I,
                   Keys.O, Keys.P, (Keys)0x1A /*LBracket*/, (Keys)0x1B /*RBracket*/,
                   Keys.Enter, Keys.LControlKey, Keys.A, Keys.S,
                   /* 0x20 */
                   Keys.D, Keys.F, Keys.G, Keys.H,
                   Keys.J, Keys.K, Keys.L, Keys.OemSemicolon,
                   (Keys)0x28/*Apostrophe*/, (Keys)0x29/*Grave*/, Keys.LShiftKey, Keys.OemBackslash,
                   Keys.Z, Keys.X, Keys.C, Keys.V,
                   /* 0x30 */
                   Keys.B, Keys.N, Keys.M, Keys.Oemcomma,
                   Keys.OemPeriod, (Keys)0x35/*Slash*/, Keys.RShiftKey, Keys.Multiply,
                   Keys.LMenu, Keys.Space, Keys.Capital, Keys.F1,
                   Keys.F2, Keys.F3, Keys.F4, Keys.F5,
                   /* 0x40 */
                   Keys.F6, Keys.F7, Keys.F8, Keys.F9,
                   Keys.F10, Keys.NumLock, Keys.Scroll, Keys.NumPad7,
                   Keys.NumPad8, Keys.NumPad9, Keys.Subtract, Keys.NumPad4,
                   Keys.NumPad5, Keys.NumPad6, Keys.Add, Keys.NumPad1,
                   /* 0x50 */
                   Keys.NumPad2, Keys.NumPad3, Keys.NumPad0, Keys.Decimal,
                   (Keys)0x54, (Keys)0x55, Keys.Oem102, Keys.F11, 
                   Keys.F12, (Keys)0x59, (Keys)0x5A, (Keys)0x5B,
                   (Keys)0x5C, (Keys)0x5D, (Keys)0x5E, (Keys)0x5F,
                   /* 0x60 */
                   (Keys)0x60, (Keys)0x61, (Keys)0x63, (Keys)0x63,
                   Keys.F13, Keys.F14, Keys.F15, (Keys)0x67,
                   (Keys)0x68, (Keys)0x69, (Keys)0x6A, (Keys)0x6B,
                   (Keys)0x6C, (Keys)0x6D, (Keys)0x6E, (Keys)0x6F,
                   /* 0x70 */
                   Keys.KanaMode, (Keys)0x71, (Keys)0x72, (Keys)0x73/*ABNT_C1*/,
                   (Keys)0x74, (Keys)0x75, (Keys)0x76, (Keys)0x77,
                   (Keys)0x78, (Keys)0x79/*convert*/, (Keys)0x7A, (Keys)0x7B/*noconvert*/,
                   (Keys)0x7C, (Keys)0x7D/*yen*/, (Keys)0x7E/*abnt_c2*/, (Keys)0x7F,
                   /* 0x80 */
                   (Keys)0x80, (Keys)0x81, (Keys)0x82, (Keys)0x83,
                   (Keys)0x84, (Keys)0x85, (Keys)0x86, (Keys)0x87,
                   (Keys)0x88, (Keys)0x89, (Keys)0x8A, (Keys)0x8B,
                   (Keys)0x8C, (Keys)0x8D/*numpadequals*/, (Keys)0x8E, (Keys)0x8F,
                   /* 0x90 */
                   Keys.MediaPreviousTrack, (Keys)0x91/*at*/, (Keys)0x92/*colon*/, (Keys)0x93/*underline*/,
                   Keys.KanjiMode, Keys.MediaStop, (Keys)0x96/*ax*/, (Keys)0x97/*unlabeled*/,
                   (Keys)0x99, Keys.MediaNextTrack, (Keys)0x9A, (Keys)0x9B,
                   (Keys)0x9C/*numpadEnter*/, Keys.RControlKey, (Keys)0x9E, (Keys)0x9F,
                   /* 0xA0 */
                   Keys.VolumeMute, (Keys)0xA1/*calculator*/, Keys.MediaPlayPause, Keys.MediaStop,
                   (Keys)0xA4, (Keys)0xA5, (Keys)0xA6, (Keys)0xA7,
                   (Keys)0xA8, (Keys)0xA9, (Keys)0xAA, (Keys)0xAB,
                   (Keys)0xAC, (Keys)0xAD, Keys.VolumeDown, (Keys)0xAF,
                   /* 0xB0 */
                   Keys.VolumeUp, (Keys)0xB1, (Keys)0xB2/*webhome*/, (Keys)0xB3/*numpadComma*/,
                   (Keys)0xB4, Keys.Divide, (Keys)0xB6, (Keys)0xB7/*sysrq*/,
                   Keys.RMenu, (Keys)0xB9, (Keys)0xBA, (Keys)0xBB,
                   (Keys)0xBC, (Keys)0xBD, (Keys)0xBE, (Keys)0xBF,
                   /* 0xC0 */
                   (Keys)0xC0, (Keys)0xC1, (Keys)0xC2, (Keys)0xC3,
                   (Keys)0xC4, Keys.Pause, (Keys)0xC6, Keys.Home,
                   Keys.Up, Keys.PageUp, (Keys)0xCA, Keys.Left,
                   (Keys)0xCC, Keys.Right, (Keys)0xCE, Keys.End,
                   /* 0xD0 */
                   Keys.Down, Keys.PageDown, Keys.Insert, Keys.Delete,
                   (Keys)0xD4, (Keys)0xD5, (Keys)0xD6, (Keys)0xD7,
                   (Keys)0xD8, (Keys)0xD9, (Keys)0xDA, Keys.LWin,
                   Keys.RWin, Keys.Apps, (Keys)0xDE/*power*/, Keys.Sleep,
                   /* 0xE0 */
                   (Keys)0xE0, (Keys)0xE1, (Keys)0xE2, (Keys)0xE3/*wake*/,
                   (Keys)0xE4, (Keys)0xE5/*websearch*/, (Keys)0xE6/*webfavorites*/, (Keys)0xE7/*webrefresh*/,
                   (Keys)0xE8/*webstop*/, (Keys)0xE9/*webforward*/, (Keys)0xEA/*webback*/, (Keys)0xEB/*mycomputer*/,
                   (Keys)0xEC/*mail*/, Keys.SelectMedia, (Keys)0xEE, (Keys)0xEF,
                   /* 0xF0 */
                   (Keys)0xF0, (Keys)0xF1, (Keys)0xF2, (Keys)0xF3,
                   (Keys)0xF4, (Keys)0xF5, (Keys)0xF6, (Keys)0xF7,
                   (Keys)0xF8, (Keys)0xF9, (Keys)0xFA, (Keys)0xFB,
                   (Keys)0xFC, (Keys)0xFD, (Keys)0xFE, (Keys)0xFF,
    };

    BasicWorkQueue m_workQueue;

    public UserInterfaceOgre() {
        m_workQueue = new BasicWorkQueue("UIOgreWork");
    }

    public void ReceiveUserIO(int type, int param1, float param2, float param3) {
        // m_log.Log(LogLevel.DRENDERDETAIL, "User input:"
        //     +  " " + type.ToString()
        //     + ", " + param1.ToString()
        //     + ", " + param2.ToString()
        //     + ", " + param3.ToString() );
        // put the work into another thread so the renderer can get back to business
        m_workQueue.DoLater(new DoLaterProcessUserIO(this, type, param1, param2, param3));
        return;
    }

    private sealed class DoLaterProcessUserIO : DoLaterBase {
        UserInterfaceOgre m_uinterface;
        int m_type, m_param1;
        float m_param2, m_param3;
        public DoLaterProcessUserIO(UserInterfaceOgre uinterface, int t, int p1, float p2, float p3) {
            m_uinterface = uinterface;
            m_type = t;
            m_param1 = p1;
            m_param2 = p2;
            m_param3 = p3;
        }
        override public bool DoIt() {
            // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "DoLaterProcessUserIO: type=" + m_type.ToString());
            switch (m_type) {
                case Ogr.IOTypeKeyPressed:
                    m_uinterface.UpdateModifier(m_param1, true);
                    m_uinterface.LastKeyCode = m_uinterface.ConvertScanCodeModifiers(m_param1);
                    if (m_uinterface.OnUserInterfaceKeypress != null) 
                        m_uinterface.OnUserInterfaceKeypress(m_uinterface.LastKeyCode, true);
                    break;
                case Ogr.IOTypeKeyReleased:
                    m_uinterface.UpdateModifier(m_param1, false);
                    m_uinterface.LastKeyCode = m_uinterface.ConvertScanCodeModifiers(m_param1);
                    if (m_uinterface.OnUserInterfaceKeypress != null) 
                        m_uinterface.OnUserInterfaceKeypress(m_uinterface.LastKeyCode, false);
                    break;
                case Ogr.IOTypeMouseButtonDown:
                    if (m_uinterface.OnUserInterfaceMouseButton != null) m_uinterface.OnUserInterfaceMouseButton(
                                    m_param1, false);
                    break;
                case Ogr.IOTypeMouseButtonUp:
                    if (m_uinterface.OnUserInterfaceMouseButton != null) m_uinterface.OnUserInterfaceMouseButton(
                                    m_param1, true);
                    break;
                case Ogr.IOTypeMouseMove:
                    if (m_uinterface.OnUserInterfaceMouseMove != null) m_uinterface.OnUserInterfaceMouseMove(m_param1, m_param2, m_param3);
                    break;
            }
            return true;
        }
    }

    public void UpdateModifier(int param1, bool updown) {
        if (param1 == (int)OISKeyCode.KC_RMENU || param1 == (int)OISKeyCode.KC_LMENU) {
            if (updown && ((LastKeyCode & Keys.Alt) == 0)) {
                LastKeyCode |= Keys.Alt;
            }
            if (!updown && ((LastKeyCode & Keys.Alt) != 0)) {
                LastKeyCode ^= Keys.Alt;
            }
        }
        if (param1 == (int)OISKeyCode.KC_RSHIFT || param1 == (int)OISKeyCode.KC_LSHIFT) {
            if (updown && ((LastKeyCode & Keys.Shift) == 0)) {
                LastKeyCode |= Keys.Shift;
            }
            if (!updown && ((LastKeyCode & Keys.Shift) != 0)) {
                LastKeyCode ^= Keys.Shift;
            }
        }
        if (param1 == (int)OISKeyCode.KC_RCONTROL || param1 == (int)OISKeyCode.KC_LCONTROL) {
            if (updown && ((LastKeyCode & Keys.Control) == 0)) {
                LastKeyCode |= Keys.Control;
            }
            if (!updown && ((LastKeyCode & Keys.Control) != 0)) {
                LastKeyCode ^= Keys.Control;
            }
        }
    }

    public System.Windows.Forms.Keys ConvertScanCode(int code) {
        if (code < 0 || code >= keyConversion.Length) {
            return Keys.NoName;
        }
        return keyConversion[code];
    }

    public System.Windows.Forms.Keys ConvertScanCodeModifiers(int code) {
        return ConvertScanCode(code) | (LastKeyCode & Keys.Modifiers);
    }
}
}
