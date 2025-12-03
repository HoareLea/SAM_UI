using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureConstructionWindow.xaml
    /// </summary>
    public partial class CreateCaseByApertureConstructionWindow : System.Windows.Window
    {
        public CreateCaseByApertureConstructionWindow()
        {
            InitializeComponent();
        }

        public IEnumerable<ApertureConstructionCase>? ApertureConstructionCases
        {
            get
            {
                return CreateCaseByApertureConstructionControl_Main.ApertureConstructionCases;
            }

            set
            {
                CreateCaseByApertureConstructionControl_Main.ApertureConstructionCases = value;
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
