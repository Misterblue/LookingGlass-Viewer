namespace LookingGlass.View {
    partial class FormAvatars {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAvatars));
            this.WindowAvatars = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // WindowAvatars
            // 
            this.WindowAvatars.AllowNavigation = false;
            this.WindowAvatars.AllowWebBrowserDrop = false;
            this.WindowAvatars.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WindowAvatars.Location = new System.Drawing.Point(0, 0);
            this.WindowAvatars.MinimumSize = new System.Drawing.Size(20, 20);
            this.WindowAvatars.Name = "WindowAvatars";
            this.WindowAvatars.Size = new System.Drawing.Size(490, 279);
            this.WindowAvatars.TabIndex = 0;
            this.WindowAvatars.WebBrowserShortcutsEnabled = false;
            // 
            // FormAvatars
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(490, 279);
            this.Controls.Add(this.WindowAvatars);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormAvatars";
            this.Text = "LookingGlass -- Avatars";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser WindowAvatars;
    }
}