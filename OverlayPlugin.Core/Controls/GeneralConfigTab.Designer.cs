namespace RainbowMage.OverlayPlugin
{
    partial class GeneralConfigTab
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralConfigTab));
            this.cbErrorReports = new System.Windows.Forms.CheckBox();
            this.cbUpdateCheck = new System.Windows.Forms.CheckBox();
            this.btnUpdateCheck = new System.Windows.Forms.Button();
            this.cbHideOverlaysWhenNotActive = new System.Windows.Forms.CheckBox();
            this.cbHideOverlaysDuringCutscene = new System.Windows.Forms.CheckBox();
            this.lblGithub = new System.Windows.Forms.Label();
            this.lnkGithubRepo = new System.Windows.Forms.LinkLabel();
            this.lblNewUserWelcome = new System.Windows.Forms.Label();
            this.lblReadMe = new System.Windows.Forms.Label();
            this.btnCactbotUpdate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbErrorReports
            // 
            resources.ApplyResources(this.cbErrorReports, "cbErrorReports");
            this.cbErrorReports.Name = "cbErrorReports";
            this.cbErrorReports.UseVisualStyleBackColor = true;
            // 
            // cbUpdateCheck
            // 
            resources.ApplyResources(this.cbUpdateCheck, "cbUpdateCheck");
            this.cbUpdateCheck.Name = "cbUpdateCheck";
            this.cbUpdateCheck.UseVisualStyleBackColor = true;
            // 
            // btnUpdateCheck
            // 
            resources.ApplyResources(this.btnUpdateCheck, "btnUpdateCheck");
            this.btnUpdateCheck.Name = "btnUpdateCheck";
            this.btnUpdateCheck.UseVisualStyleBackColor = true;
            this.btnUpdateCheck.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnUpdateCheck_MouseClick);
            // 
            // cbHideOverlaysWhenNotActive
            // 
            resources.ApplyResources(this.cbHideOverlaysWhenNotActive, "cbHideOverlaysWhenNotActive");
            this.cbHideOverlaysWhenNotActive.Name = "cbHideOverlaysWhenNotActive";
            this.cbHideOverlaysWhenNotActive.UseVisualStyleBackColor = true;
            // 
            // cbHideOverlaysDuringCutscene
            // 
            resources.ApplyResources(this.cbHideOverlaysDuringCutscene, "cbHideOverlaysDuringCutscene");
            this.cbHideOverlaysDuringCutscene.Name = "cbHideOverlaysDuringCutscene";
            this.cbHideOverlaysDuringCutscene.UseVisualStyleBackColor = true;
            // 
            // lblGithub
            // 
            resources.ApplyResources(this.lblGithub, "lblGithub");
            this.lblGithub.Name = "lblGithub";
            // 
            // lnkGithubRepo
            // 
            resources.ApplyResources(this.lnkGithubRepo, "lnkGithubRepo");
            this.lnkGithubRepo.Name = "lnkGithubRepo";
            this.lnkGithubRepo.TabStop = true;
            this.lnkGithubRepo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkGithubRepo_LinkClicked);
            // 
            // lblNewUserWelcome
            // 
            resources.ApplyResources(this.lblNewUserWelcome, "lblNewUserWelcome");
            this.lblNewUserWelcome.Name = "lblNewUserWelcome";
            this.lblNewUserWelcome.Click += new System.EventHandler(this.newUserWelcome_Click);
            // 
            // lblReadMe
            // 
            resources.ApplyResources(this.lblReadMe, "lblReadMe");
            this.lblReadMe.ForeColor = System.Drawing.Color.Red;
            this.lblReadMe.Name = "lblReadMe";
            // 
            // btnCactbotUpdate
            // 
            resources.ApplyResources(this.btnCactbotUpdate, "btnCactbotUpdate");
            this.btnCactbotUpdate.Name = "btnCactbotUpdate";
            this.btnCactbotUpdate.UseVisualStyleBackColor = true;
            this.btnCactbotUpdate.Click += new System.EventHandler(this.btnCactbotUpdate_Click);
            // 
            // GeneralConfigTab
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.btnCactbotUpdate);
            this.Controls.Add(this.lblReadMe);
            this.Controls.Add(this.lblNewUserWelcome);
            this.Controls.Add(this.lnkGithubRepo);
            this.Controls.Add(this.lblGithub);
            this.Controls.Add(this.btnUpdateCheck);
            this.Controls.Add(this.cbErrorReports);
            this.Controls.Add(this.cbUpdateCheck);
            this.Controls.Add(this.cbHideOverlaysDuringCutscene);
            this.Controls.Add(this.cbHideOverlaysWhenNotActive);
            this.Name = "GeneralConfigTab";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox cbErrorReports;
        private System.Windows.Forms.CheckBox cbUpdateCheck;
        private System.Windows.Forms.Button btnUpdateCheck;
        private System.Windows.Forms.CheckBox cbHideOverlaysWhenNotActive;
        private System.Windows.Forms.CheckBox cbHideOverlaysDuringCutscene;
        private System.Windows.Forms.Label lblGithub;
        private System.Windows.Forms.LinkLabel lnkGithubRepo;
        private System.Windows.Forms.Label lblNewUserWelcome;
        private System.Windows.Forms.Label lblReadMe;
        private System.Windows.Forms.Button btnCactbotUpdate;
    }
}
