namespace LookingGlass {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.renderingPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // renderingPanel
            // 
            this.renderingPanel.Location = new System.Drawing.Point(5, 5);
            this.renderingPanel.Name = "renderingPanel";
            this.renderingPanel.Size = new System.Drawing.Size(800, 600);
            this.renderingPanel.TabIndex = 0;
            // 
            // ViewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 608);
            this.Controls.Add(this.renderingPanel);
            this.Name = "ViewWindow";
            this.Text = "ViewWindow";
            this.Load += new System.EventHandler(this.ViewWindow_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel renderingPanel;
    }
}