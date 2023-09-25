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
    /// Interaction logic for AddMechanicalSystemsWindow.xaml
    /// </summary>
    public partial class AddMechanicalSystemsWindow : System.Windows.Window
    {
        public AddMechanicalSystemsWindow()
        {
            InitializeComponent();
        }

        public string SupplyUnitName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.SupplyUnitName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.SupplyUnitName = value;
            }
        }

        public string ExhaustUnitName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.ExhaustUnitName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.ExhaustUnitName = value;
            }
        }

        public string VentilationRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.VentilationRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.VentilationRiserName = value;
            }
        }

        public string HeatingRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.HeatingRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.HeatingRiserName = value;
            }
        }

        public string CoolingRiserName
        {
            get
            {
                return AddMechanicalSystemsControl_Main.CoolingRiserName;
            }

            set
            {
                AddMechanicalSystemsControl_Main.CoolingRiserName = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
