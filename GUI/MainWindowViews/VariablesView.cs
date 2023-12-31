﻿using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.GUI.Dialogs;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Models;
using SuchByte.MacroDeck.Variables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuchByte.MacroDeck.GUI.MainWindowContents
{
    public partial class VariablesView : UserControl
    {

        public VariablesView()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            this.UpdateTranslation();
        }

        public void UpdateTranslation()
        {
            this.SuspendLayout();
            this.Name = Language.LanguageManager.Strings.VariablesTitle;
            this.lblName.Text = Language.LanguageManager.Strings.Name;
            this.lblType.Text = Language.LanguageManager.Strings.Type;
            this.lblValue.Text = Language.LanguageManager.Strings.Value;
            this.lblCreator.Text = Language.LanguageManager.Strings.Creator;
            this.btnCreateVariable.Text = Language.LanguageManager.Strings.CreateVariable;
            this.ResumeLayout();
        }

        private void LoadCreators()
        {
            List<string> variableCreators = new List<string>();
            foreach (Variable variable in VariableManager.ListVariables)
            {
                if (!variableCreators.Contains(variable.Creator)) {
                    variableCreators.Add(variable.Creator);
                }
            }

            var filterModel = VariableViewCreatorFilterModel.Deserialize(Properties.Settings.Default.VariableViewSelectedFilter);            

            foreach (string creator in variableCreators)
            {
                if (this.creatorFilter.Controls.OfType<CheckBox>().Where(x => x.Name.Equals(creator)).Count() > 0)
                    continue;

                CheckBox creatorCheckBox = new CheckBox
                {
                    Checked = !filterModel.HiddenCreators.Contains(creator),
                    Text = creator,
                    Name = creator,
                    AutoSize = false,
                    Size = new Size(creatorFilter.Width - 30, 20),
                };
                this.creatorFilter.Controls.Add(creatorCheckBox);
                creatorCheckBox.CheckedChanged += CreatorCheckBox_CheckedChanged;
            }
        }

        private void CreatorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            Parallel.ForEach(this.variablesPanel.Controls.OfType<VariableItem>().Where(x => x.Variable.Creator == checkBox.Name).ToArray(),
                variableItem =>
                {
                    if (this.IsDisposed) return;
                    this.Invoke(new Action(() => variableItem.Visible = checkBox.Checked));
                });

            var filterModel = new VariableViewCreatorFilterModel()
            {
                HiddenCreators = (from creator in creatorFilter.Controls.OfType<CheckBox>()
                                    where !creator.Checked
                                    select creator.Name).ToList()
            };
            Properties.Settings.Default.VariableViewSelectedFilter = filterModel.Serialize();
            Properties.Settings.Default.Save();
        }

        private void VariablesPage_Load(object sender, EventArgs e)
        {
            LoadCreators();
            LoadVariables();
            VariableManager.OnVariableChanged += this.VariableChanged;
            VariableManager.OnVariableRemoved += this.VariableRemoved;
        }

        private void VariableRemoved(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => VariableRemoved(sender, e)));
                return;
            }
            string variableName = sender as string;
            var variableItemView = this.variablesPanel.Controls.OfType<VariableItem>().Where(x => x.Variable.Name.Equals(variableName)).FirstOrDefault();
            if (variableItemView != null)
            {
                this.variablesPanel.Controls.Remove(variableItemView);
            }
        }

        private void VariableChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => VariableChanged(sender, e)));
                return;
            }
            Variable variable = sender as Variable;
            if (this.IsDisposed) return;
            var variableItemView = this.variablesPanel.Controls.OfType<VariableItem>().Where(x => x.Variable.Name.Equals(variable.Name)).FirstOrDefault();
            if (variableItemView == null)
            {
                VariableItem newVariableItem = new VariableItem(variable);
                this.variablesPanel.Controls.Add(newVariableItem);
                this.LoadCreators();
            } else
            {
                variableItemView.Variable = variable;
                variableItemView.Update();
            }                        
        }

        private void LoadVariables()
        {
            this.variablesPanel.Controls.Clear();
            foreach (var variable in VariableManager.ListVariables)
            {
                if (this.IsDisposed) return;
                VariableItem variableItem = new VariableItem(variable)
                {
                    Visible = this.creatorFilter.Controls.OfType<CheckBox>().Where(x => variable.Creator == x.Name).FirstOrDefault().Checked
                };
                this.variablesPanel.Controls.Add(variableItem);
            }
        }

        private void BtnCreateVariable_Click(object sender, EventArgs e)
        {
            using (var variableDialog = new VariableDialog())
            {
                if (variableDialog.ShowDialog() == DialogResult.OK)
                {
                    VariableManager.InsertVariable(variableDialog.Variable);
                }
            }
        }
    }
}
