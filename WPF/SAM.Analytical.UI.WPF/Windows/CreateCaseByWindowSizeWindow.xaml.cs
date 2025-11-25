using System.Windows;

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

        public double ApertureScaleFactor
        {
            get
            {
                return CreateCaseByWindowSizeUserControl_Main.ApertureScaleFactor;
            }

            set
            {
                CreateCaseByWindowSizeUserControl_Main.ApertureScaleFactor = value;
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
