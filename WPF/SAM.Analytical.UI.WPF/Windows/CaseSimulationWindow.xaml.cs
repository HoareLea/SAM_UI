// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CaseSimulationWindow.xaml
    /// </summary>
    public partial class CaseSimulationWindow : System.Windows.Window
    {
        public CaseSimulationWindow()
        {
            InitializeComponent();
        }

        public string? Directory
        {
            get
            {
                return caseSimulationControl_Main.Directory;
            }
            set
            {
                caseSimulationControl_Main.Directory = value;
            }

        }

        public bool Parallel
        {
            get
            {
                return caseSimulationControl_Main.Parallel;
            }
            set
            {
                caseSimulationControl_Main.Parallel = value;
            }
        }

        public WorkflowSettings WorkflowSettings
        {
            get
            {
                return caseSimulationControl_Main.WorkflowSettings;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                caseSimulationControl_Main.WorkflowSettings = value;
            }
        }
        
        private void button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
