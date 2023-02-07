using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleValueTextBoxControl.xaml
    /// </summary>
    public partial class MultipleValueTextBoxControl : UserControl
    {
        private enum Option
        {
            Vary
        }

        private object defaultValue = null;

        private Brush background;

        private List<string> values;

        public string VaryText { get; set; } = "<Vary>";

        public Brush UpdatedBackground { get; set; } = Brushes.LightYellow;

        public event TextCompositionEventHandler TextBoxPreviewTextInput
        {
            add
            {
                textBox.PreviewTextInput += value;
            }
            remove
            {
                textBox.PreviewTextInput -= value;
            }
        }
        
        public MultipleValueTextBoxControl()
        {
            InitializeComponent();

            background = textBox.Background;
        }

        public List<string> Values
        {
            get
            {
                return values;
            }

            set
            {
                SetValues(value);
            }
        }

        public string Value
        {
            get
            {
                return values == null || values.Count == 0 ? null : values[0];
            }

            set
            {
                SetValues(new List<string>() { value });
                SetText();
            }
        }

        public string NewValue
        {
            get
            {
                return textBox.Text;
            }
        }

        public bool Updated
        {
            get
            {
                if(string.IsNullOrEmpty(Value) && string.IsNullOrEmpty(Value))
                {
                    return false;
                }
                
                return Value != textBox.Text;
            }
        }

        private void SetValues(IEnumerable<string> values)
        {
            this.values = null;

            if (values != null)
            {
                this.values = new List<string>();
                foreach(string value in values)
                {
                    this.values.Add(value);
                }
            }

            SetText();
        }

        public void SetDefaultValue(IEnumerable<string> values)
        {
            if (values == null || values.Count() == 0)
            {
                defaultValue = null;
                SetBackground();
                return;
            }

            defaultValue = values.ElementAt(0);
            if (values.Count() == 1)
            {
                SetBackground();
                return;
            }

            foreach (string value in values)
            {
                if (defaultValue?.ToString() != value)
                {
                    defaultValue = Option.Vary;
                    SetBackground();
                    return;
                }
            }

            SetBackground();
        }

        public bool Vary
        {
            get
            {
                if(values == null || values.Count <= 1)
                {
                    return false;
                }

                foreach(string value in values)
                {
                    if(values[0] != value)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void SetText()
        {
            textBox.Text = Vary ? VaryText : Value;
            SetBackground();
        }

        private void SetBackground()
        {
            if (background == null)
            {
                background = textBox.Background;
            }

            Brush brush = background;
            bool vary = defaultValue is Option && (Option)defaultValue == Option.Vary || Vary;
            if (vary)
            {
                brush = UpdatedBackground;
                if (textBox.Text == VaryText)
                {
                    brush = background;
                }
            }
            else
            {
                if (defaultValue == null)
                {
                    brush = UpdatedBackground;
                    if ((string.IsNullOrEmpty(textBox.Text) && string.IsNullOrEmpty(Value)) || textBox.Text == Value)
                    {
                        brush = background;
                    }
                }
                else
                {
                    brush = UpdatedBackground;
                    if (textBox.Text == defaultValue.ToString())
                    {
                        brush = background;
                    }
                }
            }

            textBox.Background = brush;
        }

        private void textBox_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void textBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if(Vary && textBox.Text == VaryText)
            {
                textBox.Text = string.Empty;
            }
        }

        private void textBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Vary && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = VaryText;
            }
        }

        public new bool IsEnabled
        {
            get
            {
                return base.IsEnabled;
            }

            set
            {
                textBox.IsEnabled = value;
                base.IsEnabled = value;
            }
        }

        private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SetBackground();
        }
    }
}
