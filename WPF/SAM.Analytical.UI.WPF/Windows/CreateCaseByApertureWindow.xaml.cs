using SAM.Analytical.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureWindow.xaml
    /// </summary>
    public partial class CreateCaseByApertureWindow : System.Windows.Window
    {
        public CreateCaseByApertureWindow()
        {
            InitializeComponent();
        }

        public AnalyticalModel? AnalyticalModel
        {
            get
            {
                return CreateCaseByApertureControl_Main.AnalyticalModel;
            }
            set
            {
                CreateCaseByApertureControl_Main.AnalyticalModel = value;
            }
        }

        public IEnumerable<ApertureCase>? ApertureCases
        {
            get
            {
                return CreateCaseByApertureControl_Main.ApertureCases;
            }
            set
            {
                CreateCaseByApertureControl_Main.ApertureCases = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
