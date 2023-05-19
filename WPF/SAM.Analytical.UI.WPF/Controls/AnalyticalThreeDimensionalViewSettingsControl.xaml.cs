using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalThreeDimensionalViewSettingsControl.xaml
    /// </summary>
    public partial class AnalyticalThreeDimensionalViewSettingsControl : UserControl
    {
        private ThreeDimensionalViewSettings threeDimensionalViewSettings;
        private AnalyticalModel analyticalModel;

        public AnalyticalThreeDimensionalViewSettingsControl()
        {
            InitializeComponent();
        }

        public AnalyticalThreeDimensionalViewSettingsControl(ThreeDimensionalViewSettings threeDimensionalViewSettings, AnalyticalModel analyticalModel)
        {
            InitializeComponent();

            SetAnalyticalModel(analyticalModel);

            SetAnalyticalThreeDimensionalViewSettings(threeDimensionalViewSettings);
        }

        public ThreeDimensionalViewSettings ThreeDimensionalViewSettings
        {
            get
            {
                return GetThreeDimensionalViewSettings();
            }

            set
            {
                SetAnalyticalThreeDimensionalViewSettings(value);
            }
        }

        private void SetAnalyticalModel(AnalyticalModel analyticalModel)
        {
            this.analyticalModel = analyticalModel;

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

        private void SetAnalyticalThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
        {
            this.threeDimensionalViewSettings = threeDimensionalViewSettings == null ? null : new ThreeDimensionalViewSettings(threeDimensionalViewSettings);

            checkBox_Visibilty_Space.IsChecked = threeDimensionalViewSettings.ContainsType(typeof(Space));
            button_Color_Space.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;

            checkBox_Visibilty_Panel.IsChecked = threeDimensionalViewSettings.ContainsType(typeof(Panel));
            button_Color_Panel.IsEnabled = checkBox_Visibilty_Panel.IsChecked != null && checkBox_Visibilty_Panel.IsChecked.Value;

            checkBox_Visibilty_Aperture.IsChecked = threeDimensionalViewSettings.ContainsType(typeof(Aperture));
            button_Color_Aperture.IsEnabled = checkBox_Visibilty_Aperture.IsChecked != null && checkBox_Visibilty_Aperture.IsChecked.Value;

            textBox_Name.Text = threeDimensionalViewSettings.Name;

            comboBox_Group.Text = string.Empty;
            if (threeDimensionalViewSettings.TryGetValue(ViewSettingsParameter.Group, out string group))
            {
                comboBox_Group.Text = group;
            }
        }

        private ThreeDimensionalViewSettings GetThreeDimensionalViewSettings()
        {
            ThreeDimensionalViewSettings result = new ThreeDimensionalViewSettings(textBox_Name.Text, threeDimensionalViewSettings);

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

            if (!string.IsNullOrWhiteSpace(comboBox_Group.Text))
            {
                result.SetValue(ViewSettingsParameter.Group, comboBox_Group.Text);
            }

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            button_Color_Space.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }
        private void checkBox_Visibilty_Panel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            button_Color_Panel.IsEnabled = checkBox_Visibilty_Panel.IsChecked != null && checkBox_Visibilty_Panel.IsChecked.Value;
        }

        private void checkBox_Visibilty_Aperture_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            button_Color_Aperture.IsEnabled = checkBox_Visibilty_Aperture.IsChecked != null && checkBox_Visibilty_Aperture.IsChecked.Value;
        }

        private void button_Color_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(analyticalModel == null)
            {
                return;
            }

            SpaceAppearanceSettings spaceAppearanceSettings = threeDimensionalViewSettings.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();

            Windows.SpaceAppearanceSettingsWindow spaceAppearanceSettingsWindow = new Windows.SpaceAppearanceSettingsWindow(analyticalModel?.AdjacencyCluster, spaceAppearanceSettings);
            bool? result = spaceAppearanceSettingsWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            spaceAppearanceSettings = spaceAppearanceSettingsWindow.SpaceAppearanceSettings;

            threeDimensionalViewSettings.RemoveAppearanceSettings<SpaceAppearanceSettings>();

            if(spaceAppearanceSettings != null)
            {
                threeDimensionalViewSettings.AddAppearanceSettings(spaceAppearanceSettings);
            }
        }

        private void button_Color_Panel_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }


    }
}
