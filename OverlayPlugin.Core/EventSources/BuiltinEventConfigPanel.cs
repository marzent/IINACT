using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.EventSources {
    internal partial class BuiltinEventConfigPanel : UserControl, IDisposable {
        private readonly TinyIoCContainer container;
        private readonly Registry registry;
        private BuiltinEventConfig config;

        private static readonly List<KeyValuePair<string, string>> sortKeyDict = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("None", ""),
            new KeyValuePair<string, string>("DPS", "encdps"),
            new KeyValuePair<string, string>("HPS", "enchps"),
        };

        public BuiltinEventConfigPanel(TinyIoCContainer container) {
            InitializeComponent();

            this.container = container;
            registry = container.Resolve<Registry>();
            registry.EventSourcesStarted += LoadConfig;
        }

        private void LoadConfig(object sender, EventArgs args) {
            config = container.Resolve<BuiltinEventConfig>();

            SetupControlProperties();
            SetupConfigEventHandlers();
        }

        private void SetupControlProperties() {
            this.textUpdateInterval.Text = "" + config.UpdateInterval;
            this.textEnmityInterval.Text = "" + config.EnmityIntervalMs;

            this.comboSortKey.DisplayMember = "Key";
            this.comboSortKey.ValueMember = "Value";
            this.comboSortKey.DataSource = sortKeyDict;
            this.comboSortKey.SelectedValue = config.SortKey ?? "";
            this.comboSortKey.SelectedIndexChanged += comboSortKey_SelectedIndexChanged;

            this.checkSortDesc.Checked = config.SortDesc;
            this.cbUpdateDuringImport.Checked = config.UpdateDpsDuringImport;

            this.cbEndEncounterAfterWipe.Checked = config.EndEncounterAfterWipe;
            this.cbEndEncounterOutOfCombat.Checked = config.EndEncounterOutOfCombat;
        }

        private void SetupConfigEventHandlers() {
            this.config.UpdateIntervalChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.textUpdateInterval.Text = "" + config.UpdateInterval;
                });
            };

            this.config.EnmityIntervalChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.textEnmityInterval.Text = "" + config.EnmityIntervalMs;
                });
            };

            this.config.SortKeyChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.comboSortKey.SelectedValue = config.SortKey ?? "";
                });
            };

            this.config.SortDescChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.checkSortDesc.Checked = config.SortDesc;
                });
            };

            this.config.UpdateDpsDuringImportChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.cbUpdateDuringImport.Checked = config.UpdateDpsDuringImport;
                });
            };

            this.config.EndEncounterAfterWipeChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.cbEndEncounterAfterWipe.Checked = config.EndEncounterAfterWipe;
                });
            };

            this.config.EndEncounterOutOfCombatChanged += (o, e) => {
                this.InvokeIfRequired(() => {
                    this.cbEndEncounterOutOfCombat.Checked = config.EndEncounterOutOfCombat;
                });
            };
        }

        private void InvokeIfRequired(Action action) {
            if (this.InvokeRequired) {
                this.Invoke(action);
            } else {
                action();
            }
        }

        private void comboSortKey_SelectedIndexChanged(object sender, EventArgs e) {
            this.config.SortKey = (string)this.comboSortKey.SelectedValue;
            if (this.config.SortKey == "") this.config.SortKey = null;
        }

        private void TextUpdateInterval_Leave(object sender, EventArgs e) {
            if (int.TryParse(this.textUpdateInterval.Text, out var value)) {
                this.config.UpdateInterval = value;
            } else {
                this.textUpdateInterval.Text = "" + this.config.UpdateInterval;
            }
        }

        private void CheckSortDesc_CheckedChanged(object sender, EventArgs e) {
            this.config.SortDesc = this.checkSortDesc.Checked;
        }

        private void cbUpdateDuringImport_CheckedChanged(object sender, EventArgs e) {
            this.config.UpdateDpsDuringImport = this.cbUpdateDuringImport.Checked;
        }

        private void cbEndEncounterAfterWipe_CheckedChanged(object sender, EventArgs e) {
            this.config.EndEncounterAfterWipe = this.cbEndEncounterAfterWipe.Checked;
        }

        private void cbEndEncounterOutOfCombat_CheckedChanged(object sender, EventArgs e) {
            this.config.EndEncounterOutOfCombat = this.cbEndEncounterOutOfCombat.Checked;
        }

        private void TextEnmityInterval_Leave(object sender, EventArgs e) {
            if (int.TryParse(this.textEnmityInterval.Text, out var value)) {
                this.config.EnmityIntervalMs = value;
            } else {
                this.textEnmityInterval.Text = "" + this.config.EnmityIntervalMs;
            }
        }

        private void cbLogLines_CheckedChanged(object sender, EventArgs e) {
            this.config.LogLines = this.cbLogLines.Checked;
        }
    }
}
