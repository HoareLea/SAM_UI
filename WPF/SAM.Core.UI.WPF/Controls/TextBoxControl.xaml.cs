using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextBoxControl.xaml
    /// </summary>
    public partial class TextBoxControl : UserControl
    {
        public TextBoxControl()
        {
            InitializeComponent();

            TextBox_Value.PreviewTextInput += TextBox_Value_PreviewTextInput;
            TextBox_Value.PreviewKeyDown += TextBox_Value_PreviewKeyDown;

        }

        public string? Text
        {
            get
            {
                return Label_Text.Content?.ToString();
            }

            set
            {
                Label_Text.Content = value;
            }
        }

        public Func<string, bool>? Validation { get; set; }
        
        public string? Value
        {
            get
            {
                return TextBox_Value.Text?.ToString();
            }

            set
            {
                TextBox_Value.Text = value;
            }
        }

        public T? GetValue<T>()
        {
            if (!TryGetValue(out T? result))
            {
                return default;
            }

            return result;
        }

        public T? GetValue<T>(T? defaultValue)
        {
            if (!TryGetValue(out T? result))
            {
                return defaultValue;
            }

            return result;
        }

        public bool TryGetValue<T>(out T? value)
        {
            return Core.Query.TryConvert(Value, out value);
        }

        private void TextBox_Value_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (Validation is not null && !Validation.Invoke(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_Value_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow Backspace, Delete, Left, Right, Tab
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Tab)
            {
                e.Handled = false;
            }
        }

        private void TextBox_Value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Validation is not null)
            {
                e.Handled = !Validation.Invoke(e.Text);
            }
        }
    }
}
