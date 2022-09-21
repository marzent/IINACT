using System.ComponentModel;
using DarkUI.Controls;

namespace IINACT {
    partial class SettingsForm {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private IContainer components = null;


        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.label1 = new System.Windows.Forms.Label();
            this.darkSectionPanel1 = new DarkUI.Controls.DarkSectionPanel();
            this.darkLabel14 = new DarkUI.Controls.DarkLabel();
            this.checkBoxDebug = new DarkUI.Controls.DarkCheckBox();
            this.checkBoxDotTick = new DarkUI.Controls.DarkCheckBox();
            this.checkBoxDotCrit = new DarkUI.Controls.DarkCheckBox();
            this.checkBoxPets = new DarkUI.Controls.DarkCheckBox();
            this.checkBoxRpcap = new DarkUI.Controls.DarkCheckBox();
            this.checkBoxShield = new DarkUI.Controls.DarkCheckBox();
            this.darkLabel2 = new DarkUI.Controls.DarkLabel();
            this.comboBoxFilter = new DarkUI.Controls.DarkComboBox();
            this.comboBoxLang = new DarkUI.Controls.DarkComboBox();
            this.darkLabel1 = new DarkUI.Controls.DarkLabel();
            this.rpcapSectionPanel = new DarkUI.Controls.DarkSectionPanel();
            this.darkLabel12 = new DarkUI.Controls.DarkLabel();
            this.darkLabel11 = new DarkUI.Controls.DarkLabel();
            this.darkLabel10 = new DarkUI.Controls.DarkLabel();
            this.darkLabel9 = new DarkUI.Controls.DarkLabel();
            this.darkLabel8 = new DarkUI.Controls.DarkLabel();
            this.darkLabel7 = new DarkUI.Controls.DarkLabel();
            this.textBoxPassword = new DarkUI.Controls.DarkTextBox();
            this.textBoxUsername = new DarkUI.Controls.DarkTextBox();
            this.darkLabel6 = new DarkUI.Controls.DarkLabel();
            this.darkLabel5 = new DarkUI.Controls.DarkLabel();
            this.textBoxPort = new DarkUI.Controls.DarkTextBox();
            this.darkLabel4 = new DarkUI.Controls.DarkLabel();
            this.textBoxHost = new DarkUI.Controls.DarkTextBox();
            this.darkLabel3 = new DarkUI.Controls.DarkLabel();
            this.darkSectionPanel4 = new DarkUI.Controls.DarkSectionPanel();
            this.debugBox = new DarkUI.Controls.DarkTextBox();
            this.opPanel = new System.Windows.Forms.Panel();
            this.opLabel = new DarkUI.Controls.DarkLabel();
            this.logFileButton = new DarkUI.Controls.DarkButton();
            this.logFileLabel = new DarkUI.Controls.DarkLabel();
            this.logFolderBrowserDialog = new FolderBrowserDialog();
            this.darkSectionPanel1.SuspendLayout();
            this.rpcapSectionPanel.SuspendLayout();
            this.darkSectionPanel4.SuspendLayout();
            this.opPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label1.Location = new System.Drawing.Point(0, 676);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 15);
            this.label1.TabIndex = 1;
            // 
            // darkSectionPanel1
            // 
            this.darkSectionPanel1.Controls.Add(this.darkLabel14);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDebug);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDotTick);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDotCrit);
            this.darkSectionPanel1.Controls.Add(this.checkBoxPets);
            this.darkSectionPanel1.Controls.Add(this.checkBoxRpcap);
            this.darkSectionPanel1.Controls.Add(this.checkBoxShield);
            this.darkSectionPanel1.Controls.Add(this.logFileLabel);
            this.darkSectionPanel1.Controls.Add(this.logFileButton);
            this.darkSectionPanel1.Controls.Add(this.darkLabel2);
            this.darkSectionPanel1.Controls.Add(this.comboBoxFilter);
            this.darkSectionPanel1.Controls.Add(this.comboBoxLang);
            this.darkSectionPanel1.Controls.Add(this.darkLabel1);
            this.darkSectionPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.darkSectionPanel1.Location = new System.Drawing.Point(0, 0);
            this.darkSectionPanel1.Name = "darkSectionPanel1";
            this.darkSectionPanel1.SectionHeader = "Parse Settings";
            this.darkSectionPanel1.Size = new System.Drawing.Size(803, 220);
            this.darkSectionPanel1.TabIndex = 2;
            // 
            // darkLabel14
            // 
            this.darkLabel14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.darkLabel14.AutoSize = true;
            this.darkLabel14.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel14.Location = new System.Drawing.Point(471, 38);
            this.darkLabel14.Name = "darkLabel14";
            this.darkLabel14.Size = new System.Drawing.Size(320, 15);
            this.darkLabel14.TabIndex = 9;
            this.darkLabel14.Text = "Changing Parse Settings may requires an application restart";
            // 
            // checkBoxDebug
            // 
            this.checkBoxDebug.AutoSize = true;
            this.checkBoxDebug.Location = new System.Drawing.Point(12, 182);
            this.checkBoxDebug.Name = "checkBoxDebug";
            this.checkBoxDebug.Size = new System.Drawing.Size(250, 19);
            this.checkBoxDebug.TabIndex = 8;
            this.checkBoxDebug.Text = "(DEBUG) Forward Debug Fields to Overlays";
            this.checkBoxDebug.CheckedChanged += new System.EventHandler(this.checkBoxDebug_CheckedChanged);
            // 
            // checkBoxDotTick
            // 
            this.checkBoxDotTick.AutoSize = true;
            this.checkBoxDotTick.Location = new System.Drawing.Point(416, 157);
            this.checkBoxDotTick.Name = "checkBoxDotTick";
            this.checkBoxDotTick.Size = new System.Drawing.Size(213, 19);
            this.checkBoxDotTick.TabIndex = 7;
            this.checkBoxDotTick.Text = "(DEBUG) Also Show \'Real\' DoT Ticks";
            this.checkBoxDotTick.CheckedChanged += new System.EventHandler(this.checkBoxDotTick_CheckedChanged);
            // 
            // checkBoxDotCrit
            // 
            this.checkBoxDotCrit.AutoSize = true;
            this.checkBoxDotCrit.Location = new System.Drawing.Point(12, 157);
            this.checkBoxDotCrit.Name = "checkBoxDotCrit";
            this.checkBoxDotCrit.Size = new System.Drawing.Size(226, 19);
            this.checkBoxDotCrit.TabIndex = 6;
            this.checkBoxDotCrit.Text = "(DEBUG) Simulate Individual DoT Crits";
            this.checkBoxDotCrit.CheckedChanged += new System.EventHandler(this.checkBoxDotCrit_CheckedChanged);
            // 
            // checkBoxPets
            // 
            this.checkBoxPets.AutoSize = true;
            this.checkBoxPets.Location = new System.Drawing.Point(416, 132);
            this.checkBoxPets.Name = "checkBoxPets";
            this.checkBoxPets.Size = new System.Drawing.Size(205, 19);
            this.checkBoxPets.TabIndex = 5;
            this.checkBoxPets.Text = "Disable Combine Pets with Owner";
            this.checkBoxPets.CheckedChanged += new System.EventHandler(this.checkBoxPets_CheckedChanged);
            // 
            // checkBoxRpcap
            // 
            this.checkBoxRpcap.AutoSize = true;
            this.checkBoxRpcap.Location = new System.Drawing.Point(416, 182);
            this.checkBoxRpcap.Name = "checkBoxRpcap";
            this.checkBoxRpcap.Size = new System.Drawing.Size(150, 19);
            this.checkBoxRpcap.TabIndex = 11;
            this.checkBoxRpcap.Text = "(EXPERT) Enable RPCAP";
            this.checkBoxRpcap.CheckedChanged += new System.EventHandler(this.RpcapCheckBox_CheckedChanged);
            // 
            // checkBoxShield
            // 
            this.checkBoxShield.AutoSize = true;
            this.checkBoxShield.Location = new System.Drawing.Point(12, 132);
            this.checkBoxShield.Name = "checkBoxShield";
            this.checkBoxShield.Size = new System.Drawing.Size(199, 19);
            this.checkBoxShield.TabIndex = 4;
            this.checkBoxShield.Text = "Disable Damage Shield Estimates";
            this.checkBoxShield.CheckedChanged += new System.EventHandler(this.checkBoxShield_CheckedChanged);
            // 
            // darkLabel2
            // 
            this.darkLabel2.AutoSize = true;
            this.darkLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel2.Location = new System.Drawing.Point(12, 68);
            this.darkLabel2.Name = "darkLabel2";
            this.darkLabel2.Size = new System.Drawing.Size(67, 15);
            this.darkLabel2.TabIndex = 3;
            this.darkLabel2.Text = "Parse Filter:";
            // 
            // comboBoxFilter
            // 
            this.comboBoxFilter.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.comboBoxFilter.FormattingEnabled = true;
            this.comboBoxFilter.Location = new System.Drawing.Point(172, 65);
            this.comboBoxFilter.Name = "comboBoxFilter";
            this.comboBoxFilter.Size = new System.Drawing.Size(227, 24);
            this.comboBoxFilter.TabIndex = 2;
            // 
            // comboBoxLang
            // 
            this.comboBoxLang.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.comboBoxLang.FormattingEnabled = true;
            this.comboBoxLang.Location = new System.Drawing.Point(172, 35);
            this.comboBoxLang.Name = "comboBoxLang";
            this.comboBoxLang.Size = new System.Drawing.Size(227, 24);
            this.comboBoxLang.TabIndex = 1;
            // 
            // darkLabel1
            // 
            this.darkLabel1.AutoSize = true;
            this.darkLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel1.Location = new System.Drawing.Point(12, 38);
            this.darkLabel1.Name = "darkLabel1";
            this.darkLabel1.Size = new System.Drawing.Size(96, 15);
            this.darkLabel1.TabIndex = 0;
            this.darkLabel1.Text = "Game Language:";
            // 
            // rpcapSectionPanel
            // 
            this.rpcapSectionPanel.Controls.Add(this.darkLabel12);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel11);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel10);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel9);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel8);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel7);
            this.rpcapSectionPanel.Controls.Add(this.textBoxPassword);
            this.rpcapSectionPanel.Controls.Add(this.textBoxUsername);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel6);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel5);
            this.rpcapSectionPanel.Controls.Add(this.textBoxPort);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel4);
            this.rpcapSectionPanel.Controls.Add(this.textBoxHost);
            this.rpcapSectionPanel.Controls.Add(this.darkLabel3);
            this.rpcapSectionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.rpcapSectionPanel.Location = new System.Drawing.Point(0, 220);
            this.rpcapSectionPanel.Name = "rpcapSectionPanel";
            this.rpcapSectionPanel.SectionHeader = "RPCAP";
            this.rpcapSectionPanel.Size = new System.Drawing.Size(803, 200);
            this.rpcapSectionPanel.TabIndex = 3;
            // 
            // darkLabel12
            // 
            this.darkLabel12.AutoSize = true;
            this.darkLabel12.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel12.Location = new System.Drawing.Point(12, 187);
            this.darkLabel12.Name = "darkLabel12";
            this.darkLabel12.Size = new System.Drawing.Size(55, 15);
            this.darkLabel12.TabIndex = 15;
            this.darkLabel12.Text = "Warning:";
            // 
            // darkLabel11
            // 
            this.darkLabel11.AutoSize = true;
            this.darkLabel11.ForeColor = System.Drawing.Color.LightCoral;
            this.darkLabel11.Location = new System.Drawing.Point(79, 187);
            this.darkLabel11.Name = "darkLabel11";
            this.darkLabel11.Size = new System.Drawing.Size(515, 30);
            this.darkLabel11.TabIndex = 14;
            this.darkLabel11.Text = "The username and password are sent over the network to the capture server ***IN C" +
    "LEAR TEXT***\r\nBecause of this credentials are also stored unenecrypted for now.";
            // 
            // darkLabel10
            // 
            this.darkLabel10.AutoSize = true;
            this.darkLabel10.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel10.Location = new System.Drawing.Point(315, 155);
            this.darkLabel10.Name = "darkLabel10";
            this.darkLabel10.Size = new System.Drawing.Size(457, 15);
            this.darkLabel10.TabIndex = 13;
            this.darkLabel10.Text = "Specifies the password that has to be used on the remote machine for authenticati" +
    "on.";
            // 
            // darkLabel9
            // 
            this.darkLabel9.AutoSize = true;
            this.darkLabel9.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel9.Location = new System.Drawing.Point(315, 126);
            this.darkLabel9.Name = "darkLabel9";
            this.darkLabel9.Size = new System.Drawing.Size(459, 15);
            this.darkLabel9.TabIndex = 12;
            this.darkLabel9.Text = "Specifies the username that has to be used on the remote machine for authenticati" +
    "on.";
            // 
            // darkLabel8
            // 
            this.darkLabel8.AutoSize = true;
            this.darkLabel8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel8.Location = new System.Drawing.Point(12, 155);
            this.darkLabel8.Name = "darkLabel8";
            this.darkLabel8.Size = new System.Drawing.Size(60, 15);
            this.darkLabel8.TabIndex = 10;
            this.darkLabel8.Text = "Password:";
            // 
            // darkLabel7
            // 
            this.darkLabel7.AutoSize = true;
            this.darkLabel7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel7.Location = new System.Drawing.Point(12, 126);
            this.darkLabel7.Name = "darkLabel7";
            this.darkLabel7.Size = new System.Drawing.Size(63, 15);
            this.darkLabel7.TabIndex = 9;
            this.darkLabel7.Text = "Username:";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.textBoxPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.textBoxPassword.Location = new System.Drawing.Point(82, 153);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(227, 23);
            this.textBoxPassword.TabIndex = 8;
            this.textBoxPassword.UseSystemPasswordChar = true;
            this.textBoxPassword.TextChanged += new System.EventHandler(this.TextBoxPassword_TextChanged);
            // 
            // textBoxUsername
            // 
            this.textBoxUsername.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.textBoxUsername.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.textBoxUsername.Location = new System.Drawing.Point(82, 124);
            this.textBoxUsername.Name = "textBoxUsername";
            this.textBoxUsername.Size = new System.Drawing.Size(227, 23);
            this.textBoxUsername.TabIndex = 7;
            this.textBoxUsername.TextChanged += new System.EventHandler(this.TextBoxUsername_TextChanged);
            // 
            // darkLabel6
            // 
            this.darkLabel6.AutoSize = true;
            this.darkLabel6.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel6.Location = new System.Drawing.Point(315, 97);
            this.darkLabel6.Name = "darkLabel6";
            this.darkLabel6.Size = new System.Drawing.Size(422, 15);
            this.darkLabel6.TabIndex = 6;
            this.darkLabel6.Text = "Specifies the network port (e.g. \"2002\") we want to use for the RPCAP protocol.";
            // 
            // darkLabel5
            // 
            this.darkLabel5.AutoSize = true;
            this.darkLabel5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel5.Location = new System.Drawing.Point(12, 97);
            this.darkLabel5.Name = "darkLabel5";
            this.darkLabel5.Size = new System.Drawing.Size(32, 15);
            this.darkLabel5.TabIndex = 5;
            this.darkLabel5.Text = "Port:";
            // 
            // textBoxPort
            // 
            this.textBoxPort.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.textBoxPort.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPort.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.textBoxPort.Location = new System.Drawing.Point(82, 95);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(227, 23);
            this.textBoxPort.TabIndex = 4;
            this.textBoxPort.TextChanged += new System.EventHandler(this.TextBoxPort_TextChanged);
            // 
            // darkLabel4
            // 
            this.darkLabel4.AutoSize = true;
            this.darkLabel4.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel4.Location = new System.Drawing.Point(315, 68);
            this.darkLabel4.Name = "darkLabel4";
            this.darkLabel4.Size = new System.Drawing.Size(334, 15);
            this.darkLabel4.TabIndex = 3;
            this.darkLabel4.Text = "Specifies the host (e.g. \"foo.bar.com\") we want to connect to. \r\n";
            // 
            // textBoxHost
            // 
            this.textBoxHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.textBoxHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxHost.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.textBoxHost.Location = new System.Drawing.Point(82, 66);
            this.textBoxHost.Name = "textBoxHost";
            this.textBoxHost.Size = new System.Drawing.Size(227, 23);
            this.textBoxHost.TabIndex = 2;
            this.textBoxHost.TextChanged += new System.EventHandler(this.TextBoxHost_TextChanged);
            // 
            // darkLabel3
            // 
            this.darkLabel3.AutoSize = true;
            this.darkLabel3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel3.Location = new System.Drawing.Point(12, 68);
            this.darkLabel3.Name = "darkLabel3";
            this.darkLabel3.Size = new System.Drawing.Size(35, 15);
            this.darkLabel3.TabIndex = 1;
            this.darkLabel3.Text = "Host:";
            // 
            // darkSectionPanel4
            // 
            this.darkSectionPanel4.Controls.Add(this.debugBox);
            this.darkSectionPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.darkSectionPanel4.Location = new System.Drawing.Point(0, 528);
            this.darkSectionPanel4.Name = "darkSectionPanel4";
            this.darkSectionPanel4.SectionHeader = "Debug Log";
            this.darkSectionPanel4.Size = new System.Drawing.Size(803, 178);
            this.darkSectionPanel4.TabIndex = 5;
            // 
            // debugBox
            // 
            this.debugBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.debugBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.debugBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.debugBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.debugBox.Location = new System.Drawing.Point(1, 25);
            this.debugBox.Multiline = true;
            this.debugBox.Name = "debugBox";
            this.debugBox.ReadOnly = true;
            this.debugBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.debugBox.Size = new System.Drawing.Size(801, 152);
            this.debugBox.TabIndex = 0;
            // 
            // opPanel
            // 
            this.opPanel.Controls.Add(this.opLabel);
            this.opPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.opPanel.Location = new System.Drawing.Point(0, 450);
            this.opPanel.Name = "opPanel";
            this.opPanel.Size = new System.Drawing.Size(803, 108);
            this.opPanel.TabIndex = 6;
            // 
            // opLabel
            // 
            this.opLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.opLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.opLabel.Location = new System.Drawing.Point(569, 34);
            this.opLabel.Name = "opLabel";
            this.opLabel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.opLabel.Size = new System.Drawing.Size(222, 21);
            this.opLabel.TabIndex = 0;
            this.opLabel.Text = "...Searching for game";
            this.opLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // logFileButton
            //
            this.logFileButton.Name = "logFileButton";
            this.logFileButton.Text = "Log Directory";
            this.logFileButton.Size = new System.Drawing.Size(227, 24);
            this.logFileButton.Location = new System.Drawing.Point(172, 95);
            this.logFileButton.TabIndex = 1;
            // 
            // logFileLabel
            // 
            this.logFileLabel.AutoSize = true;
            this.logFileLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.logFileLabel.Location = new System.Drawing.Point(12, 98);
            this.logFileLabel.Name = "logFileLabel";
            this.logFileLabel.Size = new System.Drawing.Size(96, 15);
            this.logFileLabel.TabIndex = 0;
            this.logFileLabel.Text = "Logfile Location:";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(803, 691);
            this.Controls.Add(this.darkSectionPanel4);
            this.Controls.Add(this.opPanel);
            this.Controls.Add(this.rpcapSectionPanel);
            this.Controls.Add(this.darkSectionPanel1);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(819, 220);
            this.Name = "SettingsForm";
            this.Text = "IINACT";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.darkSectionPanel1.ResumeLayout(false);
            this.darkSectionPanel1.PerformLayout();
            this.rpcapSectionPanel.ResumeLayout(false);
            this.rpcapSectionPanel.PerformLayout();
            this.darkSectionPanel4.ResumeLayout(false);
            this.darkSectionPanel4.PerformLayout();
            this.opPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label1;
        private DarkSectionPanel darkSectionPanel1;
        private DarkCheckBox checkBoxDebug;
        private DarkCheckBox checkBoxDotTick;
        private DarkCheckBox checkBoxDotCrit;
        private DarkCheckBox checkBoxPets;
        private DarkCheckBox checkBoxShield;
        private DarkLabel darkLabel2;
        private DarkComboBox comboBoxFilter;
        private DarkComboBox comboBoxLang;
        private DarkLabel darkLabel1;
        private DarkSectionPanel rpcapSectionPanel;
        private DarkLabel darkLabel6;
        private DarkLabel darkLabel5;
        private DarkTextBox textBoxPort;
        private DarkLabel darkLabel4;
        private DarkTextBox textBoxHost;
        private DarkLabel darkLabel3;
        private DarkSectionPanel darkSectionPanel4;
        private DarkLabel darkLabel12;
        private DarkLabel darkLabel11;
        private DarkLabel darkLabel10;
        private DarkLabel darkLabel9;
        private DarkCheckBox checkBoxRpcap;
        private DarkLabel darkLabel8;
        private DarkLabel darkLabel7;
        private DarkTextBox textBoxPassword;
        private DarkTextBox textBoxUsername;
        private DarkTextBox debugBox;
        private DarkLabel darkLabel14;
        private Panel opPanel;
        private DarkLabel opLabel;
        private DarkLabel logFileLabel;
        private DarkButton logFileButton;
        private FolderBrowserDialog logFolderBrowserDialog;
    }
}