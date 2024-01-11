using SAM.Architectural;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalTwoDimensionalViewSettingsControl.xaml
    /// </summary>
    public partial class AnalyticalTwoDimensionalViewSettingsControl : UserControl
    {
        private TwoDimensionalViewSettings twoDimensionalViewSettings;

        public AnalyticalTwoDimensionalViewSettingsControl()
        {
            InitializeComponent();

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
            elevationControl.ValueChanged += ElevationControl_ValueChanged;
            spaceAppearanceSettingsControl.ValueChanged += SpaceAppearanceSettingsControl_ValueChanged;
        }

        public AnalyticalTwoDimensionalViewSettingsControl(TwoDimensionalViewSettings twoDimensionalViewSettings, AnalyticalModel analyticalModel)
        {
            InitializeComponent();

            SetAnalyticalModel(analyticalModel);

            SetAnalyticalTwoDimensionalViewSettings(twoDimensionalViewSettings);

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

        public TwoDimensionalViewSettings TwoDimensionalViewSettings
        {
            get
            {
                return GetTwoDimensionalViewSettings();
            }

            set
            {
                SetAnalyticalTwoDimensionalViewSettings(value);
            }
        }

        private void SetAnalyticalModel(AnalyticalModel analyticalModel)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            spaceAppearanceSettingsControl.AdjacencyCluster = adjacencyCluster;

            List<Level> levels = Analytical.Create.Levels(adjacencyCluster, false);
            levels?.Sort((x, y) => x.Elevation.CompareTo(y.Elevation));

            elevationControl.Levels = levels;
            if (levels != null && levels.Count != 0)
            {
                elevationControl.SelectedLevel = levels[0];
            }

            comboBox_Group.Items.Clear();

            List<ViewSettings> viewSettingsList = analyticalModel.ViewSettings<ViewSettings>();
            if(viewSettingsList != null && viewSettingsList.Count != 0)
            {
                HashSet<string> groups = new HashSet<string>();
                foreach(ViewSettings viewSettings in viewSettingsList)
                {
                    if(viewSettings.TryGetValue(ViewSettingsParameter.Group, out string group) && !string.IsNullOrWhiteSpace(group))
                    {
                        groups.Add(group);
                    }
                }

                List<string> groups_Sorted = new List<string>(groups);
                groups_Sorted.Sort();

                foreach(string group in groups_Sorted)
                {
                    comboBox_Group.Items.Add(group);
                }
            }
        }

        private void SetAnalyticalTwoDimensionalViewSettings(TwoDimensionalViewSettings twoDimensionalViewSettings)
        {
            this.twoDimensionalViewSettings = twoDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Aperture));

            TextAppearance textAppearance = twoDimensionalViewSettings.TextAppearance;
            if(textAppearance == null)
            {
                textAppearance = Geometry.UI.Query.DefaultTextAppearance();
            }

            checkBox_TextVisibility.IsChecked = textAppearance.Opacity != 0;

            textBox_TextSize.Text = textAppearance.Height.ToString();

            spaceAppearanceSettingsControl.SpaceAppearanceSettings = twoDimensionalViewSettings.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();

            textBox_Name.Text = twoDimensionalViewSettings.Name;

            elevationControl.Elevation = twoDimensionalViewSettings.Plane.Origin.Z;

            checkBox_UseDefaultName.IsChecked = true;
            if(twoDimensionalViewSettings.TryGetValue(ViewSettingsParameter.UseDefaultName, out bool useDefaultName))
            {
                checkBox_UseDefaultName.IsChecked = useDefaultName;
            }

            comboBox_Group.Text = string.Empty;
            if(twoDimensionalViewSettings.TryGetValue(ViewSettingsParameter.Group, out string group))
            {
                comboBox_Group.Text = group;
            }
        }

        private TwoDimensionalViewSettings GetTwoDimensionalViewSettings()
        {
            TwoDimensionalViewSettings result = new TwoDimensionalViewSettings(textBox_Name.Text, twoDimensionalViewSettings);

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

            TextAppearance textAppearance = Geometry.UI.Query.DefaultTextAppearance();
            textAppearance.Opacity = checkBox_TextVisibility.IsChecked != null && checkBox_TextVisibility.IsChecked.HasValue && checkBox_TextVisibility.IsChecked.Value ? 1 : 0;

            if (Core.Query.TryConvert(textBox_TextSize.Text, out double textSize))
            {
                textAppearance.Height = textSize;
            }

            result.TextAppearance = textAppearance;

            if (spaceAppearanceSettingsControl.SpaceAppearanceSettings == null)
            {
                result.RemoveAppearanceSettings<SpaceAppearanceSettings>();
            }
            else
            {
                result.AddAppearanceSettings(spaceAppearanceSettingsControl.SpaceAppearanceSettings);
            }

            result.Plane = Geometry.Spatial.Create.Plane(elevationControl.Elevation);

            if (checkBox_UseDefaultName.IsChecked != null && checkBox_UseDefaultName.IsChecked.HasValue)
            {
                result.SetValue(ViewSettingsParameter.UseDefaultName, checkBox_UseDefaultName.IsChecked.Value);
            }

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

        private void textBox_TextSize_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }
    }
}
