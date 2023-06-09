using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class HeatRecoveryProcessControl : UserControl, IMollierProcessControl
    {
        private MollierForm mollierForm;

        public event SelectMollierPointEventHandler SelectSupplyMollierPoint;
        public event SelectMollierPointEventHandler SelectReturnMollierPoint;

        public HeatRecoveryProcessControl()
        {
            InitializeComponent();
            SensibleHeatRecoveryEfficiencyControl.ProcessParameterType = ProcessParameterType.SensibleHeatRecoveryEfficiency;
            SensibleHeatRecoveryEfficiencyControl.Value = 75;
            LatentHeatRecoveryEfficiencyControl.ProcessParameterType = ProcessParameterType.LatentHeatRecoveryEfficiency;
            LatentHeatRecoveryEfficiencyControl.Value = 0;

            MollierPointControl_Return.PressureVisible = false;

            MollierPointControl_Supply.SelectMollierPoint += MollierPointControl_Supply_SelectMollierPoint;
            MollierPointControl_Return.SelectMollierPoint += MollierPointControl_Return_SelectMollierPoint;
        }

        private void MollierPointControl_Return_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            SelectReturnMollierPoint?.Invoke(this, e);

            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected += MollierForm_ReturnMollierPointSelected;
            }
        }

        private void MollierForm_ReturnMollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected -= MollierForm_ReturnMollierPointSelected;
            }

            Return = e.MollierPoint;
        }

        private void MollierPointControl_Supply_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            SelectSupplyMollierPoint?.Invoke(this, e);

            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected += MollierForm_SupplyMollierPointSelected; ;
            }
        }

        private void MollierForm_SupplyMollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected -= MollierForm_SupplyMollierPointSelected;
            }

            Supply = e.MollierPoint;
        }

        public UIMollierProcess GetUIMollierProcess()
        {
            MollierPointControl_Return.Pressure = MollierPointControl_Supply.Pressure;

            MollierPoint supplyPoint = Supply;
            MollierPoint returnPoint = Return;

            double sensibleHeatRecoveryEfficiency = SensibleHeatRecoveryEfficiencyControl.Value;
            double latentHeatRecoveryEfficiency = LatentHeatRecoveryEfficiencyControl.Value;

            IMollierProcess mollierProcess = Mollier.Create.HeatRecoveryProcess(supplyPoint, returnPoint, sensibleHeatRecoveryEfficiency, latentHeatRecoveryEfficiency);
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

        public MollierForm MollierForm
        {
            get
            {
                return mollierForm;
            }

            set
            {
                mollierForm = value;
                MollierPointControl_Supply.SelectMollierPointVisible = value != null;
                MollierPointControl_Return.SelectMollierPointVisible = value != null;
                MollierPointControl_Supply.PressureEnabled = value == null;
                MollierPointControl_Return.PressureEnabled = value == null;
            }
        }
    }
}
