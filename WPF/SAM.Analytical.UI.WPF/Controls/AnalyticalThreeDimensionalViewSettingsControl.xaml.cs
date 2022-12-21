using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalThreeDimensionalViewSettingsControl.xaml
    /// </summary>
    public partial class AnalyticalThreeDimensionalViewSettingsControl : UserControl
    {
        private AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings;

        public AnalyticalThreeDimensionalViewSettingsControl()
        {
            InitializeComponent();

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        public AnalyticalThreeDimensionalViewSettingsControl(AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings, AnalyticalModel analyticalModel)
        {
            InitializeComponent();

            SetAnalyticalModel(analyticalModel);

            SetAnalyticalThreeDimensionalViewSettings(analyticalThreeDimensionalViewSettings);

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        public AnalyticalThreeDimensionalViewSettings AnalyticalThreeDimensionalViewSettings
        {
            get
            {
                return GetAnalyticalThreeDimensionalViewSettings();
            }

            set
            {
                SetAnalyticalThreeDimensionalViewSettings(value);
            }
        }

        private void SetAnalyticalModel(AnalyticalModel analyticalModel)
        {
            spaceAppearanceSettingsControl.AdjacencyCluster = analyticalModel?.AdjacencyCluster;

            comboBox_Group.Items.Clear();

            List<ViewSettings> viewSettingsList = analyticalModel.ViewSettings<ViewSettings>();
            if (viewSettingsList != null && viewSettingsList.Count != 0)
            {
                HashSet<string> groups = new HashSet<string>();
                foreach (ViewSettings viewSettings in viewSettingsList)
                {
                    if (viewSettings.TryGetValue(ViewSettingsParameter.Group, out string group) && !string.IsNullOrWhiteSpace(group))
                    {
                        groups.Add(group);
                    }
                }

                List<string> groups_Sorted = new List<string>(groups);
                groups_Sorted.Sort();

                foreach (string group in groups_Sorted)
                {
                    comboBox_Group.Items.Add(group);
                }
            }
        }

        private void SetAnalyticalThreeDimensionalViewSettings(AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
        {
            this.analyticalThreeDimensionalViewSettings = analyticalThreeDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = analyticalThreeDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = analyticalThreeDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = analyticalThreeDimensionalViewSettings.ContainsType(typeof(Aperture));


            spaceAppearanceSettingsControl.SpaceAppearanceSettings = analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings;

            textBox_Name.Text = analyticalThreeDimensionalViewSettings.Name;

            comboBox_Group.Text = string.Empty;
            if (analyticalThreeDimensionalViewSettings.TryGetValue(ViewSettingsParameter.Group, out string group))
            {
                comboBox_Group.Text = group;
            }
        }

        private AnalyticalThreeDimensionalViewSettings GetAnalyticalThreeDimensionalViewSettings()
        {
            AnalyticalThreeDimensionalViewSettings result = new AnalyticalThreeDimensionalViewSettings(textBox_Name.Text, analyticalThreeDimensionalViewSettings);

            CheckBox checkBox;

            List<Type> types = new List<Type>();

            checkBox = checkBox_Visibilty_Space;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Space));
            }

            checkBox = checkBox_Visibilty_Aperture;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Aperture));
            }

            checkBox = checkBox_Visibilty_Panel;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Panel));
            }

            result.SetTypes(types);

            result.SpaceAppearanceSettings = spaceAppearanceSettingsControl.SpaceAppearanceSettings;

            if (!string.IsNullOrWhiteSpace(comboBox_Group.Text))
            {
                result.SetValue(ViewSettingsParameter.Group, comboBox_Group.Text);
            }

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }
    }
}
