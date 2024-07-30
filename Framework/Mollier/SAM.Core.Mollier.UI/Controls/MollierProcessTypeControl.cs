using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierProcessTypeControl : UserControl
    {
        private MollierProcessType processType = MollierProcessType.Undefined;
        public MollierProcessTypeControl()
        {
            InitializeComponent();
            MollierProcessType_ComboBox.Text = "Choose Type";
        }

        public MollierProcessType ProcessType
        {
            get
            {
                return processType; 
            }
        }
        private void MollierProcessType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (MollierProcessType_ComboBox.Text)
            {
                case "Heating":
                    processType = MollierProcessType.Heating;
                    break;
                case "Cooling":
                    processType = MollierProcessType.Cooling;
                    break;
                case "Heat Recovery":
                    processType = MollierProcessType.HeatRecovery;
                    break;
                case "Mixing":
                    processType = MollierProcessType.Mixing;
                    break;
                case "Adiabatic Humidification":
                    break;
                case "Isothermal Humidification":
                    break;
            }
        }
    }
}
