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
        private AdjacencyCluster adjacencyCluster;
        private AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings;

        public AnalyticalTwoDimensionalViewSettingsControl()
        {
            InitializeComponent();
        }

        public AnalyticalTwoDimensionalViewSettingsControl(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings, AdjacencyCluster adjacencyCluster)
        {
            InitializeComponent();

            SetAdjacencyCluster(adjacencyCluster);

            SetAnalyticalTwoDimensionalViewSettings(analyticalTwoDimensionalViewSettings);

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
            this.adjacencyCluster = adjacencyCluster;
            
            comboBox_ZoneType.Items.Clear();
            List<string> zoneCategories = Query.ZoneCategories(adjacencyCluster);
            comboBox_ZoneType.Items.Add(string.Empty);
            foreach (string zoneCategory in zoneCategories)
            {
                comboBox_ZoneType.Items.Add(zoneCategory);
            }
        }

        private void SetAnalyticalTwoDimensionalViewSettings(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
        {
            this.analyticalTwoDimensionalViewSettings = analyticalTwoDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Aperture));

            if(analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings != null)
            {
                Geometry.UI.ParameterAppearanceSettings parameterAppearanceSettings = analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings.ParameterAppearanceSettings<Geometry.UI.ParameterAppearanceSettings>();
                
                if(parameterAppearanceSettings is ZoneAppearanceSettings)
                {
                    comboBox_ZoneType.Text = ((ZoneAppearanceSettings)parameterAppearanceSettings).ZoneCategory;
                }
            }
        }

        private AnalyticalTwoDimensionalViewSettings GetAnalyticalTwoDimensionalViewSettings()
        {
            AnalyticalTwoDimensionalViewSettings result = new AnalyticalTwoDimensionalViewSettings(analyticalTwoDimensionalViewSettings);

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

            if(!string.IsNullOrWhiteSpace(comboBox_ZoneType.Text))
            {
                result.SpaceAppearanceSettings = new SpaceAppearanceSettings(new ZoneAppearanceSettings(comboBox_ZoneType.Text));
            }

            return result;
        }

    }
}
