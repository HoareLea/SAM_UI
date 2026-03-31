// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;


namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SelectLevelsControl.xaml
    /// </summary>
    public partial class SelectLevelsControl : System.Windows.Controls.UserControl
    {
        public SelectLevelsControl()
        {
            InitializeComponent();

            ListBoxControl_Main.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;
        }

        public List<double> GetLevels(bool selected = false)
        {
            return ListBoxControl_Main.GetValues<double>(selected);
        }

        public void SetLevels(List<double> levels, bool selected = false)
        {
            if (levels is null)
            {
                return;
            }

            if (selected)
            {
                ListBoxControl_Main.SetSelection(levels);
            }
            else
            {
                List<double> levels_Temp = [.. levels];
                levels_Temp.Sort();

                ListBoxControl_Main.SetValues(levels_Temp, x => Core.Query.Round(x, Core.Tolerance.MacroDistance).ToString());
            }
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            TextBoxWindow textBoxWindow = new("Add Level", "Enter level value:", 0);
            if (textBoxWindow.ShowDialog() != true)
            {
                return;
            }

            double value = textBoxWindow.GetValue<double>();
            if(double.IsNaN(value))
            {
                System.Windows.Forms.MessageBox.Show("Invalid value");
                return;
            }

            List<double> levels_Selected = GetLevels(true) ?? [];
            levels_Selected.Add(value);

            List<double> levels_All = GetLevels(false);
            levels_All.Add(value);

            SetLevels(levels_All, false);
            SetLevels(levels_Selected, true);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if(System.Windows.Forms.MessageBox.Show("Are you sure to remove selected levels?", "Levels", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return; 
            }

            List<double> levels_All = GetLevels(false);
            List<double> levels_Selected = GetLevels(true);

            levels_All.RemoveAll(levels_Selected.Contains);

            SetLevels(levels_All);
        }
    }
}
