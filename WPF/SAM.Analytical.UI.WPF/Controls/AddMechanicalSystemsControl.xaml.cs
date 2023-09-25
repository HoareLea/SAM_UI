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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AddMechanicalSystemsControl.xaml
    /// </summary>
    public partial class AddMechanicalSystemsControl : UserControl
    {
        public AddMechanicalSystemsControl()
        {
            InitializeComponent();
        }

        public string SupplyUnitName
        {
            get
            {
                return TextBox_SupplyUnitName.Text;
            }

            set
            {
                TextBox_SupplyUnitName.Text = value;
            }
        }

        public string ExhaustUnitName
        {
            get
            {
                return TextBox_ExhaustUnitName.Text;
            }

            set
            {
                TextBox_ExhaustUnitName.Text = value;
            }
        }

        public string VentilationRiserName
        {
            get
            {
                return TextBox_VentilationRiserName.Text;
            }

            set
            {
                TextBox_VentilationRiserName.Text = value;
            }
        }

        public string HeatingRiserName
        {
            get
            {
                return TextBox_HeatingRiserName.Text;
            }

            set
            {
                TextBox_HeatingRiserName.Text = value;
            }
        }

        public string CoolingRiserName
        {
            get
            {
                return TextBox_CoolingRiserName.Text;
            }

            set
            {
                TextBox_CoolingRiserName.Text = value;
            }
        }
    }
}
