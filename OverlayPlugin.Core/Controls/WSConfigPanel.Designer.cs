namespace RainbowMage.OverlayPlugin
{
    partial class WSConfigPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WSConfigPanel));
            this.darkSectionPanel1 = new DarkUI.Controls.DarkSectionPanel();
            this.txtOverlayUrl1 = new DarkUI.Controls.DarkTextBox();
            this.darkLabel2 = new DarkUI.Controls.DarkLabel();
            this.cbOverlay1 = new DarkUI.Controls.DarkComboBox();
            this.darkLabel1 = new DarkUI.Controls.DarkLabel();
            this.darkSectionPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // darkSectionPanel1
            // 
            this.darkSectionPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.darkSectionPanel1.Controls.Add(this.txtOverlayUrl1);
            this.darkSectionPanel1.Controls.Add(this.darkLabel2);
            this.darkSectionPanel1.Controls.Add(this.cbOverlay1);
            this.darkSectionPanel1.Controls.Add(this.darkLabel1);
            resources.ApplyResources(this.darkSectionPanel1, "darkSectionPanel1");
            this.darkSectionPanel1.Name = "darkSectionPanel1";
            this.darkSectionPanel1.SectionHeader = "Overlay URL Generator";
            // 
            // txtOverlayUrl1
            // 
            resources.ApplyResources(this.txtOverlayUrl1, "txtOverlayUrl1");
            this.txtOverlayUrl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtOverlayUrl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOverlayUrl1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtOverlayUrl1.Name = "txtOverlayUrl1";
            this.txtOverlayUrl1.ReadOnly = true;
            this.txtOverlayUrl1.Click += new System.EventHandler(this.txtOverlayUrl1_Click);
            // 
            // darkLabel2
            // 
            resources.ApplyResources(this.darkLabel2, "darkLabel2");
            this.darkLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel2.Name = "darkLabel2";
            // 
            // cbOverlay1
            // 
            this.cbOverlay1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.cbOverlay1.FormattingEnabled = true;
            resources.ApplyResources(this.cbOverlay1, "cbOverlay1");
            this.cbOverlay1.Name = "cbOverlay1";
            this.cbOverlay1.SelectedIndexChanged += new System.EventHandler(this.cbOverlay1_SelectedIndexChanged);
            // 
            // darkLabel1
            // 
            resources.ApplyResources(this.darkLabel1, "darkLabel1");
            this.darkLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel1.Name = "darkLabel1";
            // 
            // WSConfigPanel
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.darkSectionPanel1);
            this.Name = "WSConfigPanel";
            this.darkSectionPanel1.ResumeLayout(false);
            this.darkSectionPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private DarkUI.Controls.DarkSectionPanel darkSectionPanel1;
        private DarkUI.Controls.DarkTextBox txtOverlayUrl1;
        private DarkUI.Controls.DarkLabel darkLabel2;
        private DarkUI.Controls.DarkComboBox cbOverlay1;
        private DarkUI.Controls.DarkLabel darkLabel1;
    }
}
