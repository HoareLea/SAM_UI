using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSizeWindow.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeWindow : System.Windows.Window
    {
        public CreateCaseByWindowSizeWindow()
        {
            InitializeComponent();
        }

        public IEnumerable<WindowSizeCase>? WindowSizeCases
        {
            get
            {
                return CreateCaseByWindowSizeControl_Main.WindowSizeCases;
            }

            set
            {
                CreateCaseByWindowSizeControl_Main.WindowSizeCases = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
