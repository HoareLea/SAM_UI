// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class OpenJSONForm : Form
    {
        public enum Action { Undefined, Replace, Merge}

        private Action action = Action.Undefined;
        public Action GetAction()
        {
            return action;
        }
        public OpenJSONForm()
        {
            InitializeComponent();
        }
        private void MergeButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            action = Action.Merge;
            Close();
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            action = Action.Replace;
            Close();
        }

        private void OpenJSONForm_Load(object sender, EventArgs e)
        {
            
        }
    }
}
