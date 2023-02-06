using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for MultipleValueTextBoxControl.xaml
    /// </summary>
    public partial class MultipleValueTextBoxControl : UserControl
    {
        private System.Windows.Media.Brush foreground;

        private System.Windows.Media.Brush background;

        private List<string> values;

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

        public System.Windows.Media.Brush VaryForeground { get; set; } = System.Windows.Media.Brushes.DarkGray;
        
        public string VaryText { get; set; } = "<Vary>";
        
        public MultipleValueTextBoxControl()
        {
            InitializeComponent();

            foreground = textBox.Foreground;
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
            bool vary = Vary;
            textBox.Text = vary ? VaryText : Value;
            SetVary(vary);
        }

        private void SetVary(bool vary)
        {
            textBox.Foreground = vary ? VaryForeground : foreground;
        }

        private void textBox_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void textBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if(Vary && textBox.Text == VaryText)
            {
                textBox.Text = string.Empty;
                SetVary(false);
            }
        }

        private void textBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Vary && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = VaryText;
                SetVary(true);
            }
        }

        public System.Windows.Media.Brush Foreground
        {
            get
            {
                return foreground;
            }

            set
            {
                foreground = value;
                textBox.Foreground = value;
            }
        }

        public System.Windows.Media.Brush Background
        {
            get
            {
                return background;
            }

            set
            {
                background = value;
                textBox.Background = value;
            }
        }
    }
}
