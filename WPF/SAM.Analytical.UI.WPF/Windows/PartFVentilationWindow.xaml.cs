// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for PartFVentilationWindow.xaml
    /// </summary>
    public partial class PartFVentilationWindow : System.Windows.Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartFVentilationWindow"/> class.
        /// </summary>
        public PartFVentilationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the selected zone category.
        /// </summary>
        public string? SelectedZoneCategory
        {
            get
            {
                return partFVentilationControl.SelectedZoneCategory;
            }
        }

        /// <summary>
        /// Gets or sets the list of zone categories.
        /// </summary>
        public List<string>? ZoneCategories
        {
            get
            {
                return partFVentilationControl.ZoneCategories;
            }
            set
            {
                partFVentilationControl.ZoneCategories = value;
            }
        }

        /// <summary>
        /// Gets or sets the setback operating factor: setback flow rate = continuous design flow rate x
        /// factor. Validated by the control and again by the calculator.
        /// </summary>
        public double SetbackFlowRateFactor
        {
            get
            {
                return partFVentilationControl.SetbackFlowRateFactor;
            }
            set
            {
                partFVentilationControl.SetbackFlowRateFactor = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
