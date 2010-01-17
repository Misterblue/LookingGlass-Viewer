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
namespace LookingGlass.View {
    partial class ViewWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Override class that wraps Panel and makes sure it doesn't get
        /// painted so the OnPaint operation only is the callback taht we
        /// registered in the regular RadegastWindow class.
        /// </summary>
        private class LGPanel : System.Windows.Forms.Panel {
            public LGPanel()
                : base() {
                // DoubleBuffered = true;
                SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(System.Windows.Forms.ControlStyles.UserPaint, true);
                // SetStyle(System.Windows.Forms.ControlStyles.Opaque, true);
            }
            protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e) {
                // base.OnPaintBackground(e);
            }
            
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewWindow));
            this.LGWindow = new LookingGlass.View.ViewWindow.LGPanel();
            this.SuspendLayout();
            // 
            // LGWindow
            // 
            this.LGWindow.Location = new System.Drawing.Point(4, 4);
            this.LGWindow.Name = "LGWindow";
            this.LGWindow.Size = new System.Drawing.Size(800, 600);
            this.LGWindow.TabIndex = 0;
            this.LGWindow.MouseLeave += new System.EventHandler(this.LGWindow_MouseLeave);
            this.LGWindow.Paint += new System.Windows.Forms.PaintEventHandler(this.LGWindow_Paint);
            this.LGWindow.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseMove);
            this.LGWindow.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseDown);
            this.LGWindow.Resize += new System.EventHandler(this.LGWindow_Resize);
            this.LGWindow.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseUp);
            this.LGWindow.MouseEnter += new System.EventHandler(this.LGWindow_MouseEnter);
            // 
            // ViewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 608);
            this.Controls.Add(this.LGWindow);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ViewWindow";
            this.Text = "World -- LookingGlass";
            this.Load += new System.EventHandler(this.ViewWindow_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LGWindow_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LGWindow_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private ViewWindow.LGPanel LGWindow;
    }
}