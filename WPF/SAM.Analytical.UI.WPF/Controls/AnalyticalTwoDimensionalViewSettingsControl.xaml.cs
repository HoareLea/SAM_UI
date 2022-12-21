using SAM.Architectural;
using SAM.Geometry.UI;
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
            elevationControl.ValueChanged += ElevationControl_ValueChanged;
            spaceAppearanceSettingsControl.ValueChanged += SpaceAppearanceSettingsControl_ValueChanged;
        }

        public AnalyticalTwoDimensionalViewSettingsControl(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings, AdjacencyCluster adjacencyCluster)
        {
            InitializeComponent();

            SetAdjacencyCluster(adjacencyCluster);

            SetAnalyticalTwoDimensionalViewSettings(analyticalTwoDimensionalViewSettings);

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;

            elevationControl.ValueChanged += ElevationControl_ValueChanged;
            spaceAppearanceSettingsControl.ValueChanged += SpaceAppearanceSettingsControl_ValueChanged;
        }

        private void ElevationControl_ValueChanged(object sender, EventArgs e)
        {
            UpdateName();
        }

        private void SpaceAppearanceSettingsControl_ValueChanged(object sender, EventArgs e)
        {
            UpdateName();
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

            List<Level> levels = Create.Levels(adjacencyCluster, false);
            levels?.Sort((x, y) => x.Elevation.CompareTo(y.Elevation));

            elevationControl.Levels = levels;
            if(levels != null && levels.Count != 0)
            {
                elevationControl.SelectedLevel = levels[0];
            }
        }

        private void SetAnalyticalTwoDimensionalViewSettings(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
        {
            this.analyticalTwoDimensionalViewSettings = analyticalTwoDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = analyticalTwoDimensionalViewSettings.ContainsType(typeof(Aperture));

            spaceAppearanceSettingsControl.SpaceAppearanceSettings = analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings;

            textBox_Name.Text = analyticalTwoDimensionalViewSettings.Name;

            elevationControl.Elevation = analyticalTwoDimensionalViewSettings.Plane.Origin.Z;

            checkBox_UseDefaultName.IsChecked = true;
            if(analyticalTwoDimensionalViewSettings.TryGetValue(ViewSettingsParameter.UseDefaultName, out bool useDefaultName))
            {
                checkBox_UseDefaultName.IsChecked = useDefaultName;
            }
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

            result.Plane = Geometry.Spatial.Create.Plane(elevationControl.Elevation);

            if(checkBox_UseDefaultName.IsChecked != null && checkBox_UseDefaultName.IsChecked.HasValue)
            {
                result.SetValue(ViewSettingsParameter.UseDefaultName, checkBox_UseDefaultName.IsChecked.Value);
            }

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        private void checkBox_UseDefaultName_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateName();
        }

        private void UpdateName()
        {
            bool @checked = checkBox_UseDefaultName.IsChecked != null && checkBox_UseDefaultName.IsChecked.HasValue && checkBox_UseDefaultName.IsChecked.Value;

            textBox_Name.IsEnabled = !@checked;

            if (@checked)
            {
                textBox_Name.Text = Query.DefaultName(elevationControl.SelectedLevel, elevationControl.Elevation, spaceAppearanceSettingsControl.SpaceAppearanceSettings);
            }
        }
    }
}
