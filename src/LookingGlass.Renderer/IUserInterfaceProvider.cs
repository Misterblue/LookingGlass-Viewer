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
using LookingGlass.World;
using System.Windows.Forms;

namespace LookingGlass.Renderer {

    /// <summary>
    /// Key changed state. Fired on state change
    /// </summary>
    /// <param name="key">key code</param>
    /// <param name="updown">true if key down, false if key up</param>
    public delegate void UserInterfaceKeypressCallback(System.Windows.Forms.Keys key, bool updown);
    /// <summary>
    /// Mouse moved
    /// </summary>
    /// <param name="param">Mouse selection (only zero these days)</param>
    /// <param name="x">relative mouse movement in X direction</param>
    /// <param name="y">relative mouse movement in Y direction</param>
    public delegate void UserInterfaceMouseMoveCallback(int param, float x, float y);
    /// <summary>
    /// Mouse button changed state. This tells you about one button changing. The OR of the button
    /// states is kept in LastMouseButtons
    /// </summary>
    /// <param name="param">button codes OR'ed together</param>
    /// <param name="updown">true means down, false means went off</param>
    public delegate void UserInterfaceMouseButtonCallback(System.Windows.Forms.MouseButtons param, bool updown);
    public delegate void UserInterfaceEntitySelectedCallback(IEntity ent);

    public enum ReceiveUserIOInputEventTypeCode {
        KeyPress=1,     // p1=keycode
        KeyRelease,     // p1=keycode
        MouseMove,      // p2=x move sin last, p3=y move since last
        MouseButtonDown,// p1=button number
        MouseButtonUp,  // p1=button number
        FocusToOverlay,
        SelectEntity,
    };
    // happens to be  the same as OIS::MouseButtonID
    public enum ReceiveUserIOMouseButtonCode {
        Left = 0,
        Right,
        Middle,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7
    };

    public interface IUserInterfaceProvider : IDisposable {
        // =======================================================
        // INPUT CONTROL
        event UserInterfaceKeypressCallback OnUserInterfaceKeypress;
        event UserInterfaceMouseMoveCallback OnUserInterfaceMouseMove;
        event UserInterfaceMouseButtonCallback OnUserInterfaceMouseButton;
        event UserInterfaceEntitySelectedCallback OnUserInterfaceEntitySelected;

        InputModeCode InputMode { get; set; }
        Keys LastKeyCode { get; set; }
        bool KeyPressed { get; set; }
        MouseButtons LastMouseButtons { get; set; }

        // times per second to do key repeat
        float KeyRepeatRate { get; set; }

        // process something from the input device
        void ReceiveUserIO(ReceiveUserIOInputEventTypeCode type, int param1, float param2, float param3);
        // kludge that tells the renderer that this IO system needs low level interfaces
        bool NeedsRendererLinkage();

        void Dispose();
    }
}
