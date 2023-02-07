using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleValueComboBoxControl.xaml
    /// </summary>
    public partial class MultipleValueComboBoxControl : UserControl
    {
        private enum Option
        {
            Vary
        }

        private object defaultValue = null;

        private Brush background;

        public string VaryText { get; set; } = "<Vary>";

        public Brush UpdatedBackground { get; set; } = Brushes.LightYellow;


        public event TextCompositionEventHandler TextBoxPreviewTextInput
        {
            add
            {
                comboBox.PreviewTextInput += value;
            }
            remove
            {
                comboBox.PreviewTextInput -= value;
            }
        }

        public MultipleValueComboBoxControl()
        {
            InitializeComponent();
        }

        public List<string> Values
        {
            get
            {
                return GetValues();
            }

            set
            {
                SetValues(value);
            }
        }

        private void SetValues(IEnumerable<string> values)
        {
            comboBox.Items.Clear();
            
            if(values != null)
            {
                foreach (string value in values.Distinct())
                {
                    ComboBoxItem comboBoxItem_New = new ComboBoxItem() { Content = value, Tag = value };
                    comboBox.Items.Add(comboBoxItem_New);
                }
            }

            int count = comboBox.Items.Count;

            if(Vary)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem() { Content = VaryText, Tag = Option.Vary };
                comboBox.Items.Add(comboBoxItem);

                comboBox.SelectedItem = comboBoxItem;
            }
            else if(count == 1)
            {
                comboBox.SelectedItem = comboBox.Items[0];
            }
        }

        private List<string> GetValues()
        {
            List<string> result = new List<string>();
            foreach(ComboBoxItem comboBoxItem in comboBox.Items)
            {
                if(comboBoxItem.Tag is Enum)
                {
                    continue;
                }

                result.Add(comboBoxItem.Tag as string);
            }

            return result;
        }

        public bool Vary
        {
            get
            {
                List<string> values = GetValues();

                if (values == null || values.Count <= 1)
                {
                    return false;
                }

                foreach (string value in values)
                {
                    if (values[0] != value)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void SetDefaultValue(IEnumerable<string> values)
        {
            if(values == null || values.Count() == 0)
            {
                defaultValue = null;
                return;
            }

            defaultValue = values.ElementAt(0);
            if (values.Count() == 1)
            {
                return;
            }

            foreach (string value in values)
            {
                if (defaultValue?.ToString() != value)
                {
                    defaultValue = Option.Vary;
                    return;
                }
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetBackground();
        }

        private void SetBackground()
        {
            if(background == null)
            {
                background = Query.Background(comboBox);
            }
            
            Brush brush = background;
            bool vary = defaultValue is Option && (Option)defaultValue == Option.Vary || Vary;
            if(vary)
            {
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                if(comboBoxItem != null)
                {
                    brush = UpdatedBackground;
                    if ((comboBoxItem.Tag is Option) && (Option)comboBoxItem.Tag == Option.Vary)
                    {
                        brush = background;
                    }
                }
            }
            else
            {
                if (defaultValue == null)
                {
                    List<string> values = GetValues();
                    if (values != null && values.Count != 0)
                    {
                        brush = UpdatedBackground;
                        ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                        if (values[0] == null && comboBoxItem.Tag == null)
                        {
                            brush = background;
                        }
                        else if (comboBoxItem != null && comboBoxItem.Tag is string)
                        {
                            if (values[0] == (string)comboBoxItem.Tag)
                            {
                                brush = background;
                            }
                        }
                    }
                }
                else
                {
                    ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                    if (comboBoxItem != null && comboBoxItem.Tag is string)
                    {
                        brush = UpdatedBackground;
                        if (defaultValue.ToString() == (string)comboBoxItem.Tag)
                        {
                            brush = background;
                        }
                    }
                }
            }
            
            Modify.Background(comboBox, brush);
        }

        private void comboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            SetBackground();
        }
    }
}
