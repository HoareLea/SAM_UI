using SAM.Core.Mollier.UI.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class CoolingProcessControl : UserControl, IMollierProcessControl
    {
        private MollierControl mollierControl;
        public CoolingProcessControl()
        {
            InitializeComponent();
            processCalculateType_ComboBox.Text = processCalculateType_ComboBox.Items[0].ToString();
        }

        public CoolingProcessControl(MollierControl mollierControl)
        {
            InitializeComponent();
            processCalculateType_ComboBox.Text = processCalculateType_ComboBox.Items[0].ToString();
            this.mollierControl = mollierControl;
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
                    mollierProcess = Mollier.Create.CoolingProcess(start, dryBulbTemperature);
                    break;
                case ProcessCalculationType.EnthalpyDifference:
                    double enthalpyDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.EnthalpyDifference);
                    mollierProcess = Mollier.Create.CoolingProcess_ByEnthalpyDifference(start, enthalpyDifference * 1000);//because input in kJ
                    break;
                case ProcessCalculationType.DryBulbTemperatureDifference:
                    double dryBulbTemperatureDifference = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.DryBulbTemperatureDifference);
                    mollierProcess = Mollier.Create.CoolingProcess_ByTemperatureDifference(start, dryBulbTemperatureDifference);
                    break;
                case ProcessCalculationType.MediumAndEfficiency:
                    double flowTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.FlowTemperature);
                    double returnTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.ReturnTemperature);
                    double efficiency = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.Efficiency);
                    mollierProcess = Mollier.Create.CoolingProcess_ByMedium(Start, flowTemperature, returnTemperature, efficiency/100);
                    break;
                case ProcessCalculationType.MediumAndDryBulbTemperature:
                    flowTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.FlowTemperature);
                    returnTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.ReturnTemperature);
                    dryBulbTemperature = Query.ParameterValue<double>(flowLayoutPanel_Main, ProcessParameterType.DryBulbTemperature);
                    mollierProcess = Mollier.Create.CoolingProcess_ByMediumAndDryBulbTemperature(Start, flowTemperature, returnTemperature, dryBulbTemperature);
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

        private void optionButton_Click(object sender, EventArgs e)
        {

            if(mollierControl == null)
            {
                return;
            }
            using(PointListOptionForm pointListOptionForm = new PointListOptionForm(mollierControl))
            {
                if(pointListOptionForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                MollierPoint mollierPoint_Temp = pointListOptionForm.MollierPoint;
                if(mollierPoint_Temp != null)
                {
                    MollierPointControl_Start.MollierPoint = mollierPoint_Temp;
                }
            }
        }
    }
}
