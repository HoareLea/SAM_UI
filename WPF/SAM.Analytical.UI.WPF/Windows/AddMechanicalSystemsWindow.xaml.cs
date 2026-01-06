// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AddMechanicalSystemsWindow.xaml
    /// </summary>
    public partial class AddMechanicalSystemsWindow : System.Windows.Window
    {
        public AddMechanicalSystemsWindow()
        {
            InitializeComponent();
        }

        public string SupplyUnitName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.SupplyUnitName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.SupplyUnitName = value;
            }
        }

        public string ExhaustUnitName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.ExhaustUnitName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.ExhaustUnitName = value;
            }
        }

        public string VentilationRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.VentilationRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.VentilationRiserName = value;
            }
        }

        public string HeatingRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.HeatingRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.HeatingRiserName = value;
            }
        }

        public string CoolingRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.CoolingRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.CoolingRiserName = value;
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
