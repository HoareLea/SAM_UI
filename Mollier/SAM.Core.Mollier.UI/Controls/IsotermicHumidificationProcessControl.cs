using SAM.Geometry.Mollier;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class IsothermalHumidificationProcessControl : UserControl, IMollierProcessControl
    {
        private MollierForm mollierForm;

        public event SelectMollierPointEventHandler SelectMollierPoint;

        public IsothermalHumidificationProcessControl()
        {
            InitializeComponent();
            processCalculateType_ComboBox.Text = processCalculateType_ComboBox.Items[1].ToString();

            MollierPointControl_Start.SelectMollierPoint += MollierPointControl_Start_SelectMollierPoint; ;
        }

        private void MollierPointControl_Start_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            SelectMollierPoint?.Invoke(this, e);

            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected += MollierForm_MollierPointSelected; ;
            }
        }

        private void MollierForm_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected -= MollierForm_MollierPointSelected;
            }

            Start = e.MollierPoint;
        }

        public UIMollierProcess GetUIMollierProcess()
        {
            ProcessCalculationType processCalculationType = Core.Query.Enum<ProcessCalculationType>(processCalculateType_ComboBox.Text);
            MollierProcess mollierProcess = null;
            MollierPoint start = Start;

            switch (processCalculationType)
            {
                case ProcessCalculationType.HumidityRatioDifference:
                    double humidityRatioDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.HumidityRatioDifference);
                    mollierProcess = Mollier.Create.IsothermalHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference/1000);
                    break;
                case ProcessCalculationType.RelativeHumidity:
                    double relativeHumidity = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.RelativeHumidity);
                    mollierProcess = Mollier.Create.IsothermalHumidificationProcess_ByRelativeHumidity(start, relativeHumidity);
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

        public MollierForm MollierForm
        {
            get
            {
                return mollierForm;
            }

            set
            {
                mollierForm = value;
                MollierPointControl_Start.SelectMollierPointVisible = value != null;
                MollierPointControl_Start.PressureEnabled = value == null;
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
