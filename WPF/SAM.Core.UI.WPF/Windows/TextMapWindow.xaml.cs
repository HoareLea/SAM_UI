using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for TextMapWindow.xaml
    /// </summary>
    public partial class TextMapWindow : Window
    {
        public TextMapWindow()
        {
            InitializeComponent();
        }

        public TextMapWindow(TextMap textMap)
        {
            InitializeComponent();

            textMapControl.TextMap = textMap;
        }

        public TextMap TextMap
        {
            get
            {
                return textMapControl.TextMap;
            }

            set
            {
                textMapControl.TextMap = value;
            }
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
