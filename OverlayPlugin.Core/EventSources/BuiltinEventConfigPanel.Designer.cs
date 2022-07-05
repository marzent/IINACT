namespace RainbowMage.OverlayPlugin.EventSources
{
    partial class BuiltinEventConfigPanel
    {
        /// <summary> 
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                registry.EventSourcesStarted -= LoadConfig;
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BuiltinEventConfigPanel));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cbEndEncounterOutOfCombat = new System.Windows.Forms.CheckBox();
            this.cbEndEncounterAfterWipe = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textEnmityInterval = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboSortKey = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textUpdateInterval = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkSortDesc = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbUpdateDuringImport = new System.Windows.Forms.CheckBox();
            this.experimentalWarning = new System.Windows.Forms.Label();
            this.lblLogLines = new System.Windows.Forms.Label();
            this.cbLogLines = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.cbEndEncounterOutOfCombat, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.cbEndEncounterAfterWipe, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.textEnmityInterval, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.comboSortKey, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textUpdateInterval, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkSortDesc, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.cbUpdateDuringImport, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.experimentalWarning, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.lblLogLines, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.cbLogLines, 1, 9);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // cbEndEncounterOutOfCombat
            // 
            resources.ApplyResources(this.cbEndEncounterOutOfCombat, "cbEndEncounterOutOfCombat");
            this.cbEndEncounterOutOfCombat.Name = "cbEndEncounterOutOfCombat";
            this.cbEndEncounterOutOfCombat.UseVisualStyleBackColor = true;
            this.cbEndEncounterOutOfCombat.CheckedChanged += new System.EventHandler(this.cbEndEncounterOutOfCombat_CheckedChanged);
            // 
            // cbEndEncounterAfterWipe
            // 
            resources.ApplyResources(this.cbEndEncounterAfterWipe, "cbEndEncounterAfterWipe");
            this.cbEndEncounterAfterWipe.Name = "cbEndEncounterAfterWipe";
            this.cbEndEncounterAfterWipe.UseVisualStyleBackColor = true;
            this.cbEndEncounterAfterWipe.CheckedChanged += new System.EventHandler(this.cbEndEncounterAfterWipe_CheckedChanged);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textEnmityInterval
            // 
            resources.ApplyResources(this.textEnmityInterval, "textEnmityInterval");
            this.textEnmityInterval.Name = "textEnmityInterval";
            this.textEnmityInterval.Leave += new System.EventHandler(this.TextEnmityInterval_Leave);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // comboSortKey
            // 
            resources.ApplyResources(this.comboSortKey, "comboSortKey");
            this.comboSortKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSortKey.FormattingEnabled = true;
            this.comboSortKey.Name = "comboSortKey";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // textUpdateInterval
            // 
            resources.ApplyResources(this.textUpdateInterval, "textUpdateInterval");
            this.textUpdateInterval.Name = "textUpdateInterval";
            this.textUpdateInterval.Leave += new System.EventHandler(this.TextUpdateInterval_Leave);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // checkSortDesc
            // 
            resources.ApplyResources(this.checkSortDesc, "checkSortDesc");
            this.checkSortDesc.Name = "checkSortDesc";
            this.checkSortDesc.UseVisualStyleBackColor = true;
            this.checkSortDesc.CheckedChanged += new System.EventHandler(this.CheckSortDesc_CheckedChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // cbUpdateDuringImport
            // 
            resources.ApplyResources(this.cbUpdateDuringImport, "cbUpdateDuringImport");
            this.cbUpdateDuringImport.Name = "cbUpdateDuringImport";
            this.cbUpdateDuringImport.UseVisualStyleBackColor = true;
            this.cbUpdateDuringImport.CheckedChanged += new System.EventHandler(this.cbUpdateDuringImport_CheckedChanged);
            // 
            // experimentalWarning
            // 
            resources.ApplyResources(this.experimentalWarning, "experimentalWarning");
            this.tableLayoutPanel1.SetColumnSpan(this.experimentalWarning, 2);
            this.experimentalWarning.Name = "experimentalWarning";
            // 
            // lblLogLines
            // 
            resources.ApplyResources(this.lblLogLines, "lblLogLines");
            this.lblLogLines.Name = "lblLogLines";
            // 
            // cbLogLines
            // 
            resources.ApplyResources(this.cbLogLines, "cbLogLines");
            this.cbLogLines.Name = "cbLogLines";
            this.cbLogLines.UseVisualStyleBackColor = true;
            this.cbLogLines.CheckedChanged += new System.EventHandler(this.cbLogLines_CheckedChanged);
            // 
            // BuiltinEventConfigPanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "BuiltinEventConfigPanel";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ComboBox comboSortKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textUpdateInterval;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkSortDesc;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbUpdateDuringImport;
        private System.Windows.Forms.TextBox textEnmityInterval;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox cbEndEncounterOutOfCombat;
        private System.Windows.Forms.CheckBox cbEndEncounterAfterWipe;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label experimentalWarning;
        private System.Windows.Forms.Label lblLogLines;
        private System.Windows.Forms.CheckBox cbLogLines;
    }
}
