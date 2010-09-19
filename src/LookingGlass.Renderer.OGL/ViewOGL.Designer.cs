namespace LookingGlass.Renderer.OGL {
    partial class ViewOGL {
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.glControl = new OpenTK.GLControl();
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.BackColor = System.Drawing.Color.Black;
            this.glControl.Location = new System.Drawing.Point(5, 5);
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(800, 600);
            this.glControl.TabIndex = 0;
            this.glControl.VSync = false;
            this.glControl.Load += new System.EventHandler(this.GLWindow_Load);
            this.glControl.MouseLeave += new System.EventHandler(this.GLWindow_MouseLeave);
            this.glControl.Paint += new System.Windows.Forms.PaintEventHandler(this.GLWindow_Paint);
            this.glControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GLWindow_MouseMove);
            this.glControl.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GLWindow_KeyUp);
            this.glControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GLWindow_MouseDown);
            this.glControl.Resize += new System.EventHandler(this.GLWindow_Resize);
            this.glControl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GLWindow_KeyDown);
            this.glControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GLWindow_MouseUp);
            this.glControl.MouseEnter += new System.EventHandler(this.GLWindow_MouseEnter);
            // 
            // ViewOGL
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(812, 613);
            this.Controls.Add(this.glControl);
            this.Name = "ViewOGL";
            this.Text = "LookingGlass -- World";
            this.Load += new System.EventHandler(this.GLWindow_Load);
            this.ResizeEnd += new System.EventHandler(this.GLWindow_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl glControl;

    }
}