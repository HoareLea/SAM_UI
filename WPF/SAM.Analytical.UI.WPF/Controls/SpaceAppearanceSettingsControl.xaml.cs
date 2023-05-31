﻿using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SpaceAppearanceSettingsControl.xaml
    /// </summary>
    public partial class SpaceAppearanceSettingsControl : UserControl
    {
        private AdjacencyCluster adjacencyCluster;

        public event EventHandler ValueChanged;

        public SpaceAppearanceSettingsControl()
        {
            InitializeComponent();

            SetZoneTypeVisibility();
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return AdjacencyCluster;
            }

            set
            {
                SetAdjacencyCluster(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
            LoadZoneCategories();
        }

        private void LoadZoneCategories()
        {
            comboBox_ZoneCategory.Items.Clear();
            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);
            comboBox_ZoneCategory.Items.Add(string.Empty);
            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneCategory.Items.Add(zoneCategory);
            }
        }

        private void LoadParameterNames()
        {
            comboBox_ParameterName.Items.Clear();
            comboBox_ParameterName.Items.Add(string.Empty);

            if(adjacencyCluster == null)
            {
                return;
            }

            if (radioButton_Default.IsChecked.HasValue && radioButton_Default.IsChecked.Value)
            {
                comboBox_ParameterName.Visibility = Visibility.Hidden;
                label_ParameterName.Visibility = Visibility.Hidden;

                comboBox_ZoneCategory.Visibility = Visibility.Hidden;
                label_ZoneType.Visibility = Visibility.Hidden;
                return;
            }
            else
            {
                comboBox_ParameterName.Visibility = Visibility.Visible;
                label_ParameterName.Visibility = Visibility.Visible;

                comboBox_ZoneCategory.Visibility = Visibility.Visible;
                label_ZoneType.Visibility = Visibility.Visible;
            }

            Type type = null;

            List<object> @objects = new List<object>();
            if(radioButton_InternalCondition.IsChecked.HasValue && radioButton_InternalCondition.IsChecked.Value)
            {
                IEnumerable<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions();
                if(internalConditions != null)
                {
                    foreach(InternalCondition internalCondition in internalConditions)
                    {
                        if(internalCondition == null)
                        {
                            continue;
                        }

                        objects.Add(internalCondition);
                    }
                }
                type = typeof(InternalCondition);
            }
            else if(radioButton_Space.IsChecked.HasValue && radioButton_Space.IsChecked.Value)
            {
                IEnumerable<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        if (space == null)
                        {
                            continue;
                        }

                        objects.Add(space);
                    }
                }
                type = typeof(Space);
            }
            else if (radioButton_Zone.IsChecked.HasValue && radioButton_Zone.IsChecked.Value)
            {
                IEnumerable<Zone> zones = adjacencyCluster.GetZones();
                if (zones != null)
                {
                    foreach (Zone zone in zones)
                    {
                        if (zone == null)
                        {
                            continue;
                        }

                        objects.Add(zone);
                    }
                }
                type = typeof(Zone);
            }
            else if (radioButton_VentilationSystem.IsChecked.HasValue && radioButton_VentilationSystem.IsChecked.Value)
            {
                IEnumerable<VentilationSystem> ventilationSystems = adjacencyCluster.GetMechanicalSystems<VentilationSystem>();
                if (ventilationSystems != null)
                {
                    foreach (VentilationSystem ventilationSystem in ventilationSystems)
                    {
                        if (ventilationSystem == null)
                        {
                            continue;
                        }

                        objects.Add(ventilationSystem);
                    }
                }
                type = typeof(VentilationSystem);
            }

            Core.UI.WPF.Modify.AddParameterNames(comboBox_ParameterName, objects, type, new string[] { "ToString", "Location", "InternalCondition", "ToJObject", "ParameterSets", "HashCode", "Type" });
        }

        private void SetZoneTypeVisibility()
        {
            if (radioButton_Zone.IsChecked.HasValue && radioButton_Zone.IsChecked.Value)
            {
                label_ZoneType.Visibility = Visibility.Visible;
                comboBox_ZoneCategory.Visibility = Visibility.Visible;
            }
            else
            {
                label_ZoneType.Visibility = Visibility.Hidden;
                comboBox_ZoneCategory.Visibility = Visibility.Hidden;
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            LoadParameterNames();
            SetZoneTypeVisibility();
            ValueChanged?.Invoke(this, new EventArgs());
        }

        public SpaceAppearanceSettings SpaceAppearanceSettings
        {
            get
            {
                return GetSpaceAppearanceSettings();
            }

            set
            {
                SetSpaceAppearanceSettings(value);
            }
        }

        private SpaceAppearanceSettings GetSpaceAppearanceSettings()
        {
            if (radioButton_InternalCondition.IsChecked.HasValue && radioButton_InternalCondition.IsChecked.Value)
            {
                return new SpaceAppearanceSettings(new InternalConditionAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString()));
            }
            else if (radioButton_Space.IsChecked.HasValue && radioButton_Space.IsChecked.Value)
            {
                return new SpaceAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString());
            }
            else if (radioButton_Zone.IsChecked.HasValue && radioButton_Zone.IsChecked.Value)
            {
                return new SpaceAppearanceSettings(new ZoneAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString(), comboBox_ZoneCategory?.SelectedItem?.ToString()));
            }
            else if (radioButton_VentilationSystem.IsChecked.HasValue && radioButton_VentilationSystem.IsChecked.Value)
            {
                return new SpaceAppearanceSettings(new VentilationSystemAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString()));
            }

            return null;
        }

        private void SetSpaceAppearanceSettings(SpaceAppearanceSettings spaceAppearanceSettings)
        {
            Core.UI.IAppearanceSettings appearanceSettings = spaceAppearanceSettings?.GetValueAppearanceSettings<ValueAppearanceSettings>();
            if(appearanceSettings == null)
            {
                radioButton_Default.IsChecked = true;
                LoadParameterNames();
                SetZoneTypeVisibility();
                return;
            }

            string parameterName = null;
            if(appearanceSettings is ParameterAppearanceSettings)
            {
                parameterName = ((ParameterAppearanceSettings)appearanceSettings).ParameterName;
            }
            else if(appearanceSettings is ITypeAppearanceSettings)
            {
                parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;
            }

            if(parameterName == null)
            {
                return;
            }

            if(appearanceSettings is ZoneAppearanceSettings)
            {
                radioButton_Zone.IsChecked = true;
                LoadZoneCategories();
                comboBox_ZoneCategory.SelectedItem = ((ZoneAppearanceSettings)appearanceSettings).ZoneCategory;
            }
            else if(appearanceSettings is InternalConditionAppearanceSettings)
            {
                radioButton_InternalCondition.IsChecked = true;
            }
            else if (appearanceSettings is VentilationSystemAppearanceSettings)
            {
                radioButton_VentilationSystem.IsChecked = true;
            }
            else 
            {
                radioButton_Space.IsChecked = true;
            }

            LoadParameterNames();
            comboBox_ParameterName.SelectedItem = parameterName;

            SetZoneTypeVisibility();
        }

        private void comboBox_ParameterName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }

        private void comboBox_ZoneCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }
    }
}
