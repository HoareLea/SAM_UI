using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalTwoDimensionalViewSettingsControl.xaml
    /// </summary>
    public partial class AnalyticalTwoDimensionalViewSettingsControl : UserControl
    {
        private AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings;

        public AnalyticalTwoDimensionalViewSettingsControl()
        {
            InitializeComponent();

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        public AnalyticalTwoDimensionalViewSettingsControl(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings, AdjacencyCluster adjacencyCluster)
        {
            InitializeComponent();

            SetAdjacencyCluster(adjacencyCluster);

            SetAnalyticalTwoDimensionalViewSettings(analyticalTwoDimensionalViewSettings);

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        public AnalyticalTwoDimensionalViewSettings AnalyticalTwoDimensionalViewSettings
        {
            get
            {
                return GetAnalyticalTwoDimensionalViewSettings();
            }

            set
            {
                SetAnalyticalTwoDimensionalViewSettings(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            spaceAppearanceSettingsControl.AdjacencyCluster = adjacencyCluster;
        }

        private void SetAnalyticalTwoDimensionalViewSettings(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
        {
            this.analyticalTwoDimensionalViewSettings = analyticalTwoDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Aperture));

            spaceAppearanceSettingsControl.SpaceAppearanceSettings = analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings;

            textBox_Name.Text = analyticalTwoDimensionalViewSettings.Name;
        }

        private AnalyticalTwoDimensionalViewSettings GetAnalyticalTwoDimensionalViewSettings()
        {
            AnalyticalTwoDimensionalViewSettings result = new AnalyticalTwoDimensionalViewSettings(textBox_Name.Text, analyticalTwoDimensionalViewSettings);

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

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }
    }
}
