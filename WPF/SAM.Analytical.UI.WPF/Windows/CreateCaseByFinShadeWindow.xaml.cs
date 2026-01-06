// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByFinShadeWindow.xaml
    /// </summary>
    public partial class CreateCaseByFinShadeWindow : System.Windows.Window
    {
        public CreateCaseByFinShadeWindow()
        {
            InitializeComponent();
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return CreateCaseByFinShadeControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCaseByFinShadeControl_Main.AnalyticalModel = value;
            }
        }

        public IEnumerable<FinShadeCase>? FinShadeCases
        {
            get
            {
                return CreateCaseByFinShadeControl_Main.FinShadeCases;
            }

            set
            {
                CreateCaseByFinShadeControl_Main.FinShadeCases = value;
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
