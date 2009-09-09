namespace LookingGlass.Radegast {
    partial class RadegastWindow {
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

        #region Windows Form Designer generated code

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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            // this.LGWindow = new System.Windows.Forms.Panel();
            this.LGWindow = new DBPanel();
            this.SuspendLayout();
            // 
            // LGWindow
            // 
            this.LGWindow.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.LGWindow.Location = new System.Drawing.Point(1, 1);
            this.LGWindow.Name = "LGWindow";
            this.LGWindow.Size = new System.Drawing.Size(802, 582);
            this.LGWindow.TabIndex = 0;
            // 
            // RadegastWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 585);
            this.Controls.Add(this.LGWindow);
            this.Name = "RadegastWindow";
            this.Text = "LookingGlass View";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel LGWindow;
    }
}
