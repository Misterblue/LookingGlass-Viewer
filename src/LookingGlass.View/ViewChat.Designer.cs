namespace LookingGlass.View {
    partial class ViewChat {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewChat));
            this.WindowChat = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // WindowChat
            // 
            this.WindowChat.AllowNavigation = false;
            this.WindowChat.AllowWebBrowserDrop = false;
            this.WindowChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WindowChat.IsWebBrowserContextMenuEnabled = false;
            this.WindowChat.Location = new System.Drawing.Point(0, 0);
            this.WindowChat.MinimumSize = new System.Drawing.Size(20, 20);
            this.WindowChat.Name = "WindowChat";
            this.WindowChat.Size = new System.Drawing.Size(534, 246);
            this.WindowChat.TabIndex = 0;
            this.WindowChat.WebBrowserShortcutsEnabled = false;
            // 
            // ViewChat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 246);
            this.Controls.Add(this.WindowChat);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ViewChat";
            this.Text = "ViewChat";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser WindowChat;
    }
}