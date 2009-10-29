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
        private class DBPanel : System.Windows.Forms.Panel {
            public DBPanel()
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
            this.renderingPanel2 = new LookingGlass.View.ViewWindow.DBPanel();
            this.SuspendLayout();
            // 
            // renderingPanel2
            // 
            this.renderingPanel2.Location = new System.Drawing.Point(5, 5);
            this.renderingPanel2.Name = "renderingPanel2";
            this.renderingPanel2.Size = new System.Drawing.Size(800, 600);
            this.renderingPanel2.TabIndex = 0;
            this.renderingPanel2.MouseLeave += new System.EventHandler(this.LGWindow_MouseLeave);
            this.renderingPanel2.Paint += new System.Windows.Forms.PaintEventHandler(this.LGWindow_Paint);
            this.renderingPanel2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseMove);
            this.renderingPanel2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseDown);
            this.renderingPanel2.Resize += new System.EventHandler(this.LGWindow_Resize);
            this.renderingPanel2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LGWindow_MouseUp);
            this.renderingPanel2.MouseEnter += new System.EventHandler(this.LGWindow_MouseEnter);
            // 
            // ViewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 608);
            this.Controls.Add(this.renderingPanel2);
            this.Name = "ViewWindow";
            this.Text = "ViewWindow";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RadegastWindow_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RadegastWindow_KeyDown);
            this.Load += new System.EventHandler(this.ViewWindow_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel renderingPanel;
        private ViewWindow.DBPanel renderingPanel2;
    }
}