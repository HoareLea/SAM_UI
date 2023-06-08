using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class HeatRecoveryProcessControl : UserControl, IMollierProcessControl
    {

        public HeatRecoveryProcessControl()
        {
            InitializeComponent();
            SensibleHeatRecoveryEfficiencyControl.ProcessParameterType = ProcessParameterType.SensibleHeatRecoveryEfficiency;
            SensibleHeatRecoveryEfficiencyControl.Value = 75;
            LatentHeatRecoveryEfficiencyControl.ProcessParameterType = ProcessParameterType.LatentHeatRecoveryEfficiency;
            LatentHeatRecoveryEfficiencyControl.Value = 0;

            MollierPointControl_Return.PressureVisible = false;
        }


        public UIMollierProcess GetUIMollierProcess()
        {
            MollierPointControl_Return.Pressure = MollierPointControl_Supply.Pressure;

            IMollierProcess mollierProcess = null;
            MollierPoint supplyPoint = Supply;
            MollierPoint returnPoint = Return;
            double sensibleHeatRecoveryEfficiency = SensibleHeatRecoveryEfficiencyControl.Value;
            double latentHeatRecoveryEfficiency = LatentHeatRecoveryEfficiencyControl.Value;
            mollierProcess = Mollier.Create.HeatRecoveryProcess(supplyPoint, returnPoint, sensibleHeatRecoveryEfficiency, latentHeatRecoveryEfficiency);
            return new UIMollierProcess(mollierProcess, Color.Empty);
        }
        public MollierPoint Supply
        {
            get
            {
                return MollierPointControl_Supply.MollierPoint;
            }

            set
            {
                MollierPointControl_Supply.MollierPoint = value;
            }
        }
        public MollierPoint Return
        {
            get
            {
                return MollierPointControl_Return.MollierPoint;
            }
            set
            {
                MollierPointControl_Return.MollierPoint = value;
            }
        }

        private void HeatRecoveryProcessControl_Load(object sender, EventArgs e)
        {

        }

        private void MollierPointControl_Supply_Load(object sender, EventArgs e)
        {

        }
    }
}
