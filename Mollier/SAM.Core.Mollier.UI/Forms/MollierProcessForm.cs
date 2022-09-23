using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm : Form
    {
        private ProcessType processType = ProcessType.Undefined;
        public MollierProcessForm()
        {
            InitializeComponent();
        }
        public MollierProcess GetMollierProcess(double pressure)
        {
            MollierProcess mollierProcess = null;
            if (!Core.Query.TryConvert(TextBox_Temperature.Text, out double temperature))
            {
                return null;
            }

            if (!Core.Query.TryConvert(TextBox_HumidityRatio.Text, out double humidtyRatio))
            {
                return null;
            }
            if(!Core.Query.TryConvert(ComboBox_ChooseProcess.Text, out double process))
            {
                return null;
            }
            //mollierProcess.Start = new MollierPoint(temperature, humidtyRatio, pressure);
            return mollierProcess;
        }

        private void ComboBox_ChooseProcess_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            processType = ProcessType.Undefined;
            if(ComboBox_ChooseProcess.Text == "Heat")
            {
                processType = ProcessType.Heating;
            }
            if (ComboBox_ChooseProcess.Text == "Cool")
            {
                processType = ProcessType.Cooling;
            }
            if (ComboBox_ChooseProcess.Text == "Heat Recovery")
            {
                processType = ProcessType.HeatRecovery;
            }
            if (ComboBox_ChooseProcess.Text == "Mixing")
            {
                processType = ProcessType.Mixing;
            }
            if(ComboBox_ChooseProcess.Text == "Humidify")
            {
                processType = ProcessType.Humidification;
            }
            return;
        }
    }
}
