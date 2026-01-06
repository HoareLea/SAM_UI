// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static TextBox EditableTextBox(this ComboBox comboBox)
        {
            if(comboBox == null)
            {
                return null;
            }

            return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
        }
    }
}
