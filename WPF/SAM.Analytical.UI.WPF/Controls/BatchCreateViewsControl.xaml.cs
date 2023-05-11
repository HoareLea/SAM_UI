using SAM.Architectural;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for BatchCreateViewsControl.xaml
    /// </summary>
    public partial class BatchCreateViewsControl : UserControl
    {
        public BatchCreateViewsControl()
        {
            InitializeComponent();

            textBox_Offset.PreviewTextInput += TextBox_Offset_PreviewTextInput;
        }

        private void TextBox_Offset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return spaceAppearanceSettingsControl.AdjacencyCluster;
            }

            set
            {
                SetAdjacencyCluster(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            spaceAppearanceSettingsControl.AdjacencyCluster = adjacencyCluster;

            spaceAppearanceSettingsControl.SpaceAppearanceSettings = new SpaceAppearanceSettings("Color");

            checkBox_Visibilty_Aperture.IsChecked = true;
            checkBox_Visibilty_Panel.IsChecked = true;
            checkBox_Visibilty_Space.IsChecked = true;

            List<Level> levels = Analytical.Create.Levels(adjacencyCluster, false);
            levels?.Sort((x, y) => x.Elevation.CompareTo(y.Elevation));

            listBox_Levels.Items.Clear();

            foreach(Level level in levels)
            {
                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal};
                stackPanel.Children.Add(new Label() { Content = level.Name });
                stackPanel.Tag = level;

                listBox_Levels.Items.Add(stackPanel);
                listBox_Levels.SelectedItems.Add(stackPanel);
            }

            if(listBox_Levels.Items.Count > 1)
            {
                listBox_Levels.SelectedItems.Remove(listBox_Levels.SelectedItems[listBox_Levels.SelectedItems.Count - 1]);
            }
        }

        public List<Level> SelectedLevels
        {
            get
            {
                return GetLevels(true);
            }
        }

        public double Offset
        {
            get
            {
                return GetOffset();
            }
        }

        private double GetOffset()
        {
            if(!Core.Query.TryConvert(textBox_Offset.Text, out double result))
            {
                return double.NaN;
            }

            return result;
        }

        private List<Level> GetLevels(bool selected = false)
        {
            System.Collections.IList list = selected ? listBox_Levels.SelectedItems : listBox_Levels.Items;

            List <Level> result = new List<Level>();
            foreach(StackPanel stackPanel in list)
            {
                Level level = stackPanel?.Tag as Level;
                if(level != null)
                {
                    result.Add(level);
                }
            }

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, RoutedEventArgs e)
        {
            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        public List<AnalyticalTwoDimensionalViewSettings> AnalyticalTwoDimensionalViewSettingsList
        {
            get
            {
                return GetAnalyticalTwoDimensionalViewSettingsList();
            }
        }

        private List<AnalyticalTwoDimensionalViewSettings> GetAnalyticalTwoDimensionalViewSettingsList()
        {
            List<Level> levels = GetLevels(true);
            if(levels == null || levels.Count == 0)
            {
                return null;
            }

            double offset = GetOffset();

            List<Type> types = new List<Type>();

            CheckBox checkBox;

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

            string group = Query.DefaultGroup(spaceAppearanceSettingsControl.SpaceAppearanceSettings);

            List<AnalyticalTwoDimensionalViewSettings> result = new List<AnalyticalTwoDimensionalViewSettings>();
            foreach (Level level in levels)
            {
                double elevation = level.Elevation + offset;

                string name = Query.DefaultName(level, elevation, spaceAppearanceSettingsControl.SpaceAppearanceSettings);

                AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings = new AnalyticalTwoDimensionalViewSettings(Guid.NewGuid(), name, Geometry.Spatial.Create.Plane(elevation), null, types, Geometry.UI.Query.DefaultTextAppearance());
                analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings = spaceAppearanceSettingsControl.SpaceAppearanceSettings;
                analyticalTwoDimensionalViewSettings.SetValue(ViewSettingsParameter.UseDefaultName, true);
                analyticalTwoDimensionalViewSettings.SetValue(ViewSettingsParameter.Group, group);

                result.Add(analyticalTwoDimensionalViewSettings);
            }


            return result;
        }
    }
}
