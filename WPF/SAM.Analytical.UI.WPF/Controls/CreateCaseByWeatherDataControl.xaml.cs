// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWeatherDataControl.xaml
    /// </summary>
    public partial class CreateCaseByWeatherDataControl : UserControl
    {
        public CreateCaseByWeatherDataControl()
        {
            InitializeComponent();

            Add();
        }

        public IEnumerable<WeatherDataCase>? WeatherDataCases
        {
            get
            {
                List<WeatherDataCase> result = [];
                foreach (object children in StackPanel_Main.Children)
                {
                    if (children is not SelectSAMObjectComboBoxControl selectSAMObjectComboBoxControl)
                    {
                        continue;
                    }

                    if (selectSAMObjectComboBoxControl.GetJSAMObject<WeatherData>() is WeatherData weatherData)
                    {
                        result.Add(new WeatherDataCase(weatherData));
                    }
                }

                return result;
            }

            set
            {
                StackPanel_Main.Children.Clear();

                if (value == null)
                {
                    return;
                }

                foreach(WeatherDataCase weatherDataCase in value)
                {
                    if(weatherDataCase.WeatherData is not WeatherData weatherData)
                    {
                        continue;
                    }

                    SelectSAMObjectComboBoxControl? selectSAMObjectComboBoxControl = Add();
                    if(selectSAMObjectComboBoxControl is null)
                    {
                        continue;
                    }

                    int index = selectSAMObjectComboBoxControl.Add(weatherDataCase.WeatherData?.Name, weatherDataCase.WeatherData);
                    selectSAMObjectComboBoxControl.SelectedText = weatherDataCase.WeatherData?.Name;
                }
            }
        }

        private SelectSAMObjectComboBoxControl? Add()
        {
            SelectSAMObjectComboBoxControl result = new()
            {
                Margin = new Thickness(0, 5, 0, 5),
                Width = 200
            };

            result.ValidateFunc = new Func<IJSAMObject, bool>(x => x is WeatherData);
            result.ReadFunc = new Func<string, IJSAMObject>(x =>
            {
                if (!UI.Query.TryGetWeatherData(x, out WeatherData weatherData_Temp) || weatherData_Temp == null)
                {
                    return null;
                }

                return weatherData_Temp;
            });

            result.DialogFilter = "epw files (*.epw)|*.epw|TAS TBD files (*.tbd)|*.tbd|TAS TSD files (*.tsd)|*.tsd|TAS TWD files (*.twd)|*.twd|All files (*.*)|*.*"; ;
            result.DialogFilterIndex = 1;

            result.SelectionChanged += SelectSAMObjectComboBoxControl_SelectionChanged;

            StackPanel_Main.Children.Add(result);

            return result;
        }

        private void SelectSAMObjectComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectSAMObjectComboBoxControl? selectSAMObjectComboBoxControl = sender as SelectSAMObjectComboBoxControl;
            if (selectSAMObjectComboBoxControl == null)
            {
                return;
            }

            // Check if this is the last ComboBox in the panel
            if (StackPanel_Main.Children[StackPanel_Main.Children.Count - 1] == selectSAMObjectComboBoxControl)
            {
                // Only add if a value is selected
                if (selectSAMObjectComboBoxControl.GetJSAMObject<WeatherData>() != null)
                {
                    Add();
                }
            }
        }
    }


}
