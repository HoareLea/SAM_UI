using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class HeatingProcessControl : UserControl, IMollierProcessControl
    {
        private MollierForm mollierForm;

        public event SelectMollierPointEventHandler SelectMollierPoint;
        public HeatingProcessControl()
        {
            InitializeComponent();

            processCalculateType_ComboBox.Text = processCalculateType_ComboBox.Items[0].ToString();
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
            IMollierProcess mollierProcess = null;
            MollierPoint start = Start;

            switch (processCalculationType)
            {
                case ProcessCalculationType.DryBulbTemperature:
                    double dryBulbTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.DryBulbTemperature);
                    mollierProcess = Mollier.Create.HeatingProcess(start, dryBulbTemperature);
                    break;
                case ProcessCalculationType.EnthalpyDifference:
                    double enthalpyDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.EnthalpyDifference);
                    mollierProcess = Mollier.Create.HeatingProcess_ByEnthalpyDifference(start, enthalpyDifference * 1000);//because input in kJ
                    break;
                case ProcessCalculationType.DryBulbTemperatureDifference:
                    double dryBulbTemperatureDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.DryBulbTemperatureDifference);
                    mollierProcess = Mollier.Create.HeatingProcess_ByTemperatureDifference(start, dryBulbTemperatureDifference);
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
                MollierPointControl_Start.SelectMollierPointVisible = value != null;
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
            if(processParameterTypes == null || processParameterTypes.Count == 0)
            {
                MessageBox.Show("Wrong Heating Data");
                return;
            }
            flowLayoutPanel_Main.Controls?.Clear();
            List<Control> controls = Create.Controls(processParameterTypes);
            foreach(Control control in controls)
            {
                flowLayoutPanel_Main.Controls.Add(control);
            }            
        }

        private void MollierPointControl_Start_Load(object sender, EventArgs e)
        {

        }
    }
}
