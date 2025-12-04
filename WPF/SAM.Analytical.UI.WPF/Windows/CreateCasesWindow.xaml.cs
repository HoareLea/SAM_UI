using SAM.Analytical.Classes;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCasesWindow.xaml
    /// </summary>
    public partial class CreateCasesWindow : System.Windows.Window
    {
        public CreateCasesWindow()
        {
            InitializeComponent();
        }

        public List<Cases> Cases
        {
            get
            {
                return CreateCasesControl_Main.Cases;
            }

            set
            {
                CreateCasesControl_Main.Cases = value;
            }
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return CreateCasesControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCasesControl_Main.AnalyticalModel = value;
            }
        }

        private void button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
