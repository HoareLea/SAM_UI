// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSizeWindow.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeWindow : System.Windows.Window
    {
        public CreateCaseByWindowSizeWindow()
        {
            InitializeComponent();
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return CreateCaseByWindowSizeControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCaseByWindowSizeControl_Main.AnalyticalModel = value;
            }
        }

        public IEnumerable<WindowSizeCase>? WindowSizeCases
        {
            get
            {
                return CreateCaseByWindowSizeControl_Main.WindowSizeCases;
            }

            set
            {
                CreateCaseByWindowSizeControl_Main.WindowSizeCases = value;
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
