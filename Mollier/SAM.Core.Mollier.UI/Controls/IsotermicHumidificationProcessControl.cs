using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class IsotermicHumidificationProcessControl : UserControl
    {

        public IsotermicHumidificationProcessControl()
        {
            InitializeComponent();
            processCalculateType_ComboBox.Text = processCalculateType_ComboBox.Items[0].ToString();
        }


        public UIMollierProcess CreateIsotermicHumidificationProcess()
        {
            ProcessCalculationType processCalculationType = Core.Query.Enum<ProcessCalculationType>(processCalculateType_ComboBox.Text);
            IMollierProcess mollierProcess = null;
            MollierPoint start = Start;

            switch (processCalculationType)
            {
                case ProcessCalculationType.HumidityRatioDifference:
                    double humidityRatioDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.HumidityRatioDifference);
                    mollierProcess = Mollier.Create.IsotermicHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference/1000);
                    break;
                case ProcessCalculationType.RelativeHumidity:
                    double relativeHumidity = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.RelativeHumidity);
                    mollierProcess = Mollier.Create.IsotermicHumidificationProcess_ByRelativeHumidity(start, relativeHumidity);
                    break;
            }
            return new UIMollierProcess(mollierProcess, Color.Empty);
        }
        public MollierPoint Start
        {
            get
            {
                return MollierPointControl_Start.MollierPoint;
            }

            set
            {
                MollierPointControl_Start.MollierPoint = value;
            }
        }
        private void processCalculateType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProcessCalculationType processCalculationType = Core.Query.Enum<ProcessCalculationType>(processCalculateType_ComboBox.Text);

            List<ProcessParameterType> processParameterTypes = Query.ProcessParameterTypes(processCalculationType);
            if (processParameterTypes == null || processParameterTypes.Count == 0)
            {
                MessageBox.Show("Wrong Heating Data");
                return;
            }
            flowLayoutPanel_Main.Controls?.Clear();
            List<Control> controls = Create.Controls(processParameterTypes);
            foreach (Control control in controls)
            {
                flowLayoutPanel_Main.Controls.Add(control);
            }
        }

    }
}
