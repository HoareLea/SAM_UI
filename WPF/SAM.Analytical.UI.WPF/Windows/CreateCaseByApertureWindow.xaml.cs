// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureWindow.xaml
    /// </summary>
    public partial class CreateCaseByApertureWindow : System.Windows.Window
    {
        public CreateCaseByApertureWindow()
        {
            InitializeComponent();
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return CreateCaseByApertureControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCaseByApertureControl_Main.AnalyticalModel = value;
            }
        }

        public IEnumerable<ApertureCase>? ApertureCases
        {
            get
            {
                return CreateCaseByApertureControl_Main.ApertureCases;
            }
            set
            {
                CreateCaseByApertureControl_Main.ApertureCases = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
