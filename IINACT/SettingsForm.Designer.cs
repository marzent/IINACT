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
            this.checkBoxShield = new DarkUI.Controls.DarkCheckBox();
            this.logFileLabel = new DarkUI.Controls.DarkLabel();
            this.logFileButton = new DarkUI.Controls.DarkButton();
            this.darkLabel2 = new DarkUI.Controls.DarkLabel();
            this.comboBoxFilter = new DarkUI.Controls.DarkComboBox();
            this.comboBoxLang = new DarkUI.Controls.DarkComboBox();
            this.darkLabel1 = new DarkUI.Controls.DarkLabel();
            this.darkSectionPanel4 = new DarkUI.Controls.DarkSectionPanel();
            this.debugBox = new DarkUI.Controls.DarkTextBox();
            this.opPanel = new System.Windows.Forms.Panel();
            this.opLabel = new DarkUI.Controls.DarkLabel();
            this.logFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.darkSectionPanel1.SuspendLayout();
            this.darkSectionPanel4.SuspendLayout();
            this.opPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label1.Location = new System.Drawing.Point(0, 1442);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 32);
            this.label1.TabIndex = 1;
            // 
            // darkSectionPanel1
            // 
            this.darkSectionPanel1.Controls.Add(this.darkLabel14);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDebug);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDotTick);
            this.darkSectionPanel1.Controls.Add(this.checkBoxDotCrit);
            this.darkSectionPanel1.Controls.Add(this.checkBoxPets);
            this.darkSectionPanel1.Controls.Add(this.checkBoxShield);
            this.darkSectionPanel1.Controls.Add(this.logFileLabel);
            this.darkSectionPanel1.Controls.Add(this.logFileButton);
            this.darkSectionPanel1.Controls.Add(this.darkLabel2);
            this.darkSectionPanel1.Controls.Add(this.comboBoxFilter);
            this.darkSectionPanel1.Controls.Add(this.comboBoxLang);
            this.darkSectionPanel1.Controls.Add(this.darkLabel1);
            this.darkSectionPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.darkSectionPanel1.Location = new System.Drawing.Point(0, 0);
            this.darkSectionPanel1.Margin = new System.Windows.Forms.Padding(6);
            this.darkSectionPanel1.Name = "darkSectionPanel1";
            this.darkSectionPanel1.SectionHeader = "Parse Settings";
            this.darkSectionPanel1.Size = new System.Drawing.Size(1491, 469);
            this.darkSectionPanel1.TabIndex = 2;
            // 
            // darkLabel14
            // 
            this.darkLabel14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.darkLabel14.AutoSize = true;
            this.darkLabel14.ForeColor = System.Drawing.Color.Gray;
            this.darkLabel14.Location = new System.Drawing.Point(832, 81);
            this.darkLabel14.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.darkLabel14.Name = "darkLabel14";
            this.darkLabel14.Size = new System.Drawing.Size(648, 32);
            this.darkLabel14.TabIndex = 9;
            this.darkLabel14.Text = "Changing Parse Settings may requires an application restart";
            // 
            // checkBoxDebug
            // 
            this.checkBoxDebug.AutoSize = true;
            this.checkBoxDebug.Location = new System.Drawing.Point(22, 388);
            this.checkBoxDebug.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxDebug.Name = "checkBoxDebug";
            this.checkBoxDebug.Size = new System.Drawing.Size(501, 36);
            this.checkBoxDebug.TabIndex = 8;
            this.checkBoxDebug.Text = "(DEBUG) Forward Debug Fields to Overlays";
            this.checkBoxDebug.CheckedChanged += new System.EventHandler(this.checkBoxDebug_CheckedChanged);
            // 
            // checkBoxDotTick
            // 
            this.checkBoxDotTick.AutoSize = true;
            this.checkBoxDotTick.Location = new System.Drawing.Point(773, 335);
            this.checkBoxDotTick.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxDotTick.Name = "checkBoxDotTick";
            this.checkBoxDotTick.Size = new System.Drawing.Size(425, 36);
            this.checkBoxDotTick.TabIndex = 7;
            this.checkBoxDotTick.Text = "(DEBUG) Also Show \'Real\' DoT Ticks";
            this.checkBoxDotTick.CheckedChanged += new System.EventHandler(this.checkBoxDotTick_CheckedChanged);
            // 
            // checkBoxDotCrit
            // 
            this.checkBoxDotCrit.AutoSize = true;
            this.checkBoxDotCrit.Location = new System.Drawing.Point(22, 335);
            this.checkBoxDotCrit.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxDotCrit.Name = "checkBoxDotCrit";
            this.checkBoxDotCrit.Size = new System.Drawing.Size(451, 36);
            this.checkBoxDotCrit.TabIndex = 6;
            this.checkBoxDotCrit.Text = "(DEBUG) Simulate Individual DoT Crits";
            this.checkBoxDotCrit.CheckedChanged += new System.EventHandler(this.checkBoxDotCrit_CheckedChanged);
            // 
            // checkBoxPets
            // 
            this.checkBoxPets.AutoSize = true;
            this.checkBoxPets.Location = new System.Drawing.Point(773, 282);
            this.checkBoxPets.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxPets.Name = "checkBoxPets";
            this.checkBoxPets.Size = new System.Drawing.Size(407, 36);
            this.checkBoxPets.TabIndex = 5;
            this.checkBoxPets.Text = "Disable Combine Pets with Owner";
            this.checkBoxPets.CheckedChanged += new System.EventHandler(this.checkBoxPets_CheckedChanged);
            // 
            // checkBoxShield
            // 
            this.checkBoxShield.AutoSize = true;
            this.checkBoxShield.Location = new System.Drawing.Point(22, 282);
            this.checkBoxShield.Margin = new System.Windows.Forms.Padding(6);
            this.checkBoxShield.Name = "checkBoxShield";
            this.checkBoxShield.Size = new System.Drawing.Size(400, 36);
            this.checkBoxShield.TabIndex = 4;
            this.checkBoxShield.Text = "Disable Damage Shield Estimates";
            this.checkBoxShield.CheckedChanged += new System.EventHandler(this.checkBoxShield_CheckedChanged);
            // 
            // logFileLabel
            // 
            this.logFileLabel.AutoSize = true;
            this.logFileLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.logFileLabel.Location = new System.Drawing.Point(22, 209);
            this.logFileLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.logFileLabel.Name = "logFileLabel";
            this.logFileLabel.Size = new System.Drawing.Size(188, 32);
            this.logFileLabel.TabIndex = 0;
            this.logFileLabel.Text = "Logfile Location:";
            // 
            // logFileButton
            // 
            this.logFileButton.Location = new System.Drawing.Point(319, 203);
            this.logFileButton.Margin = new System.Windows.Forms.Padding(6);
            this.logFileButton.Name = "logFileButton";
            this.logFileButton.Padding = new System.Windows.Forms.Padding(9, 11, 9, 11);
            this.logFileButton.Size = new System.Drawing.Size(843, 51);
            this.logFileButton.TabIndex = 1;
            this.logFileButton.Text = "Log Directory";
            // 
            // darkLabel2
            // 
            this.darkLabel2.AutoSize = true;
            this.darkLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel2.Location = new System.Drawing.Point(22, 145);
            this.darkLabel2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.darkLabel2.Name = "darkLabel2";
            this.darkLabel2.Size = new System.Drawing.Size(134, 32);
            this.darkLabel2.TabIndex = 3;
            this.darkLabel2.Text = "Parse Filter:";
            // 
            // comboBoxFilter
            // 
            this.comboBoxFilter.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.comboBoxFilter.FormattingEnabled = true;
            this.comboBoxFilter.Location = new System.Drawing.Point(319, 139);
            this.comboBoxFilter.Margin = new System.Windows.Forms.Padding(6);
            this.comboBoxFilter.Name = "comboBoxFilter";
            this.comboBoxFilter.Size = new System.Drawing.Size(418, 40);
            this.comboBoxFilter.TabIndex = 2;
            // 
            // comboBoxLang
            // 
            this.comboBoxLang.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.comboBoxLang.FormattingEnabled = true;
            this.comboBoxLang.Location = new System.Drawing.Point(319, 75);
            this.comboBoxLang.Margin = new System.Windows.Forms.Padding(6);
            this.comboBoxLang.Name = "comboBoxLang";
            this.comboBoxLang.Size = new System.Drawing.Size(418, 40);
            this.comboBoxLang.TabIndex = 1;
            // 
            // darkLabel1
            // 
            this.darkLabel1.AutoSize = true;
            this.darkLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel1.Location = new System.Drawing.Point(22, 81);
            this.darkLabel1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.darkLabel1.Name = "darkLabel1";
            this.darkLabel1.Size = new System.Drawing.Size(192, 32);
            this.darkLabel1.TabIndex = 0;
            this.darkLabel1.Text = "Game Language:";
            // 
            // darkSectionPanel4
            // 
            this.darkSectionPanel4.Controls.Add(this.debugBox);
            this.darkSectionPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.darkSectionPanel4.Location = new System.Drawing.Point(0, 699);
            this.darkSectionPanel4.Margin = new System.Windows.Forms.Padding(6);
            this.darkSectionPanel4.Name = "darkSectionPanel4";
            this.darkSectionPanel4.SectionHeader = "Debug Log";
            this.darkSectionPanel4.Size = new System.Drawing.Size(1491, 743);
            this.darkSectionPanel4.TabIndex = 5;
            // 
            // debugBox
            // 
            this.debugBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.debugBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.debugBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.debugBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.debugBox.Location = new System.Drawing.Point(1, 25);
            this.debugBox.Margin = new System.Windows.Forms.Padding(6);
            this.debugBox.Multiline = true;
            this.debugBox.Name = "debugBox";
            this.debugBox.ReadOnly = true;
            this.debugBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.debugBox.Size = new System.Drawing.Size(1489, 717);
            this.debugBox.TabIndex = 0;
            // 
            // opPanel
            // 
            this.opPanel.Controls.Add(this.opLabel);
            this.opPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.opPanel.Location = new System.Drawing.Point(0, 469);
            this.opPanel.Margin = new System.Windows.Forms.Padding(6);
            this.opPanel.Name = "opPanel";
            this.opPanel.Size = new System.Drawing.Size(1491, 230);
            this.opPanel.TabIndex = 6;
            // 
            // opLabel
            // 
            this.opLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.opLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.opLabel.Location = new System.Drawing.Point(1057, 73);
            this.opLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.opLabel.Name = "opLabel";
            this.opLabel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.opLabel.Size = new System.Drawing.Size(412, 45);
            this.opLabel.TabIndex = 0;
            this.opLabel.Text = "...Searching for game";
            this.opLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1491, 1474);
            this.Controls.Add(this.darkSectionPanel4);
            this.Controls.Add(this.opPanel);
            this.Controls.Add(this.darkSectionPanel1);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MinimumSize = new System.Drawing.Size(1499, 389);
            this.Name = "SettingsForm";
            this.Text = "IINACT";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.darkSectionPanel1.ResumeLayout(false);
            this.darkSectionPanel1.PerformLayout();
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
        private DarkSectionPanel darkSectionPanel4;
        private DarkTextBox debugBox;
        private DarkLabel darkLabel14;
        private Panel opPanel;
        private DarkLabel opLabel;
        private DarkLabel logFileLabel;
        private DarkButton logFileButton;
        private FolderBrowserDialog logFolderBrowserDialog;
    }
}