// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureConstructionWindow.xaml
    /// </summary>
    public partial class CreateCaseByApertureConstructionWindow : System.Windows.Window
    {
        public CreateCaseByApertureConstructionWindow()
        {
            InitializeComponent();
        }

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                return CreateCaseByApertureConstructionControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCaseByApertureConstructionControl_Main.AnalyticalModel = value;
            }
        }

        public IEnumerable<ApertureConstructionCase>? ApertureConstructionCases
        {
            get
            {
                return CreateCaseByApertureConstructionControl_Main.ApertureConstructionCases;
            }

            set
            {
                CreateCaseByApertureConstructionControl_Main.ApertureConstructionCases = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
