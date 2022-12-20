using SAM.Architectural;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ElevationControl.xaml
    /// </summary>
    public partial class ElevationControl : UserControl
    {
        public event EventHandler ValueChanged;

        public ElevationControl()
        {
            InitializeComponent();
        }

        public List<Level> Levels
        {
            get
            {
                return GetLevels();
            }
            set
            {
                SetLevels(value);
            }
        }

        private List<Level> GetLevels()
        {
            List<Level> result = new List<Level>();
            foreach(object @object in comboBox_Level.Items)
            {
                if(@object is Level)
                {
                    result.Add((Level)@object);
                }
            }

            return result;
        }

        private void SetLevels(IEnumerable<Level> levels)
        {
            Level level_Selected = comboBox_Level.SelectedItem as Level;
            
            comboBox_Level.DisplayMemberPath = "Name";
            comboBox_Level.Items.Clear();
            if(levels == null || levels.Count() == 0)
            {
                return;
            }


            Level level_Selected_New = null;
            foreach(Level level in levels)
            {
                if(level == null)
                {
                    continue;
                }

                comboBox_Level.Items.Add(level);
                if(level_Selected != null && level.Guid == level_Selected.Guid)
                {
                    level_Selected_New = level;
                }
            }

            comboBox_Level.SelectedItem = level_Selected_New;
        }

        public Level SelectedLevel
        {
            get
            {
                return comboBox_Level.SelectedItem as Level;
            }

            set
            {
                if(value == null)
                {
                    comboBox_Level.SelectedItem = null;
                }

                foreach(object @object in comboBox_Level.Items)
                {
                    Level level = @object as Level;
                    if (level != null && level.Guid == value.Guid)
                    {
                        comboBox_Level.SelectedItem = @object;
                        break;
                    }
                }
            }
        }

        public double Elevation
        {
            get
            {
                if(!Core.Query.TryConvert(textBox_Elevation.Text, out double elevation))
                {
                    return double.NaN;
                }

                return elevation;
            }
            set
            {
                textBox_Elevation.Text = double.IsNaN(value) ? (0).ToString() : value.ToString();
            }
        }

        private void textBox_Elevation_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
            
            ValueChanged?.Invoke(this, new EventArgs());
        }

        private void comboBox_Level_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Level level = comboBox_Level.SelectedItem as Level;
            if(level != null)
            {
                Elevation = level.Elevation;
            }

            ValueChanged?.Invoke(this, new EventArgs());
        }
    }
}
