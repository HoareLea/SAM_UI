using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextBoxWindow.xaml
    /// </summary>
    public partial class TextBoxWindow : Window
    {
        public TextBoxWindow()
        {
            InitializeComponent();
        }

        public TextBoxWindow(string? title, string? text, object? value)
        {
            InitializeComponent();

            Title = title;
            Text = text;
            Value = value?.ToString();
        }

        public TextBoxWindow(string? title, string? text)
        {
            InitializeComponent();

            Title = title;
            Text = text;
        }
        public string? Text
        {
            get
            {
                return TextBoxControl_Main.Text;
            }

            set
            {
                TextBoxControl_Main.Text = value;
            }
        }

        public Func<string, bool>? Validation
        {
            get
            {
                return TextBoxControl_Main.Validation;
            }

            set
            {
                TextBoxControl_Main.Validation = value;
            }
        }

        public string? Value
        {
            get
            {
                return TextBoxControl_Main.Value;
            }

            set
            {
                TextBoxControl_Main.Value = value;
            }
        }

        public T? GetValue<T>()
        {
            return TextBoxControl_Main.GetValue<T>();
        }

        public T? GetValue<T>(T? defaultValue)
        {
            return TextBoxControl_Main.GetValue(defaultValue);
        }

        public bool TryGetValue<T>(out T? value)
        {
            return TextBoxControl_Main.TryGetValue(out value);
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
