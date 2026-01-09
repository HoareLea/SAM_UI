// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWeatherDataWindow.xaml
    /// </summary>
    public partial class CreateCaseByWeatherDataWindow : System.Windows.Window
    {
        public CreateCaseByWeatherDataWindow()
        {
            InitializeComponent();
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

        public IEnumerable<WeatherDataCase> WeatherDataCases
        {
            get
            {
                return CreateCaseByWeatherDataControl_Main.WeatherDataCases;
            }

            set
            {
                CreateCaseByWeatherDataControl_Main.WeatherDataCases = value;
            }
        }


    }
}
