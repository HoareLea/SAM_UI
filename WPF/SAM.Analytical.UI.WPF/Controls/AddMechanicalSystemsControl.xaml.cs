// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AddMechanicalSystemsControl.xaml
    /// </summary>
    public partial class AddMechanicalSystemsControl : UserControl
    {
        public AddMechanicalSystemsControl()
        {
            InitializeComponent();
        }

        public string SupplyUnitName
        {
            get
            {
                return TextBox_SupplyUnitName.Text;
            }

            set
            {
                TextBox_SupplyUnitName.Text = value;
            }
        }

        public string ExhaustUnitName
        {
            get
            {
                return TextBox_ExhaustUnitName.Text;
            }

            set
            {
                TextBox_ExhaustUnitName.Text = value;
            }
        }

        public string VentilationRiserName
        {
            get
            {
                return TextBox_VentilationRiserName.Text;
            }

            set
            {
                TextBox_VentilationRiserName.Text = value;
            }
        }

        public string HeatingRiserName
        {
            get
            {
                return TextBox_HeatingRiserName.Text;
            }

            set
            {
                TextBox_HeatingRiserName.Text = value;
            }
        }

        public string CoolingRiserName
        {
            get
            {
                return TextBox_CoolingRiserName.Text;
            }

            set
            {
                TextBox_CoolingRiserName.Text = value;
            }
        }
    }
}
