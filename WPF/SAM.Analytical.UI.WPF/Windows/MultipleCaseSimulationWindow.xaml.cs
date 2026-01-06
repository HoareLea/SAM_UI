// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleCaseSimulationWindow.xaml
    /// </summary>
    public partial class MultipleCaseSimulationWindow : System.Windows.Window
    {
        public MultipleCaseSimulationWindow()
        {
            InitializeComponent();
        }

        public string Directory
        {
            get
            {
                return MultipleCaseSimulationControl_Main.Directory;
            }

            set
            {
                MultipleCaseSimulationControl_Main.Directory = value;
            }
        }

        public bool Parallel
        {
            get
            {
                return MultipleCaseSimulationControl_Main.Parallel;
            }
            set
            {
                MultipleCaseSimulationControl_Main.Parallel = value;
            }
        }

        public List<string> Paths
        {
            get
            {
                return MultipleCaseSimulationControl_Main.Paths;
            }

            set
            {
                MultipleCaseSimulationControl_Main.Paths = value;
            }
        }

        public WorkflowSettings WorkflowSettings
        {
            get
            {
                return MultipleCaseSimulationControl_Main.WorkflowSettings;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                MultipleCaseSimulationControl_Main.WorkflowSettings = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(Directory))
            {
                MessageBox.Show("Provide directory");
                return;
            }

            if(Paths == null || Paths.Count == 0)
            {
                MessageBox.Show("Provide files");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
