using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class RoomProcessControl : UserControl, IMollierProcessControl
    {
        private MollierForm mollierForm;

        public event SelectMollierPointEventHandler SelectMollierPoint;

        public RoomProcessControl()
        {
            InitializeComponent();

            ParameterControl_HumidityRatio_Start.ProcessParameterType = ProcessParameterType.HumidityRatio;
            ParameterControl_HumidityRatio_Start.Enabled = false;

            ParameterControl_HumidityRatio_Room.ProcessParameterType = ProcessParameterType.HumidityRatio;
            ParameterControl_HumidityRatio_Room.Enabled = false;

            ParameterControl_LatentLoad.Name = "Q Latent";
            ParameterControl_LatentLoad.UnitType = Units.UnitType.Kilowatt;
            ParameterControl_LatentLoad.ValueHanged += ParameterControl_LatentLoad_ValueHanged;

            ParameterControl_SensibleLoad.Name = "Q Sensible";
            ParameterControl_SensibleLoad.UnitType = Units.UnitType.Kilowatt;
            ParameterControl_SensibleLoad.ValueHanged += ParameterControl_SensibleLoad_ValueHanged;

            ParameterControl_SensibleLoadRatio.Name = "Sensible Heat Ratio";
            ParameterControl_SensibleLoadRatio.UnitType = Units.UnitType.Unitless;
            ParameterControl_SensibleLoadRatio.Enabled = false;

            ParameterControl_Airflow.ProcessParameterType = ProcessParameterType.Airflow;
            ParameterControl_Airflow.Value = 0.1;
            ParameterControl_Airflow.ValueHanged += ParameterControl_Airflow_ValueHanged;

            mollierPointControl_Room.Enabled = false;

            MollierPointControl_Start.ValueHanged += MollierPointControl_Start_ValueHanged;
            MollierPointControl_Start.SelectMollierPoint += MollierPointControl_Start_SelectMollierPoint;
            MollierPointControl_Start.SelectMollierPointVisible = false;

            ParameterControl_Epsilon.Name = "Epsilon";
            ParameterControl_Epsilon.UnitType = Units.UnitType.KilojulePerKilogram;
            ParameterControl_Epsilon.Enabled = false;
        }

        private void MollierPointControl_Start_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            SelectMollierPoint?.Invoke(this, e);

            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected += MollierForm_MollierPointSelected;
            }
        }

        private void ParameterControl_Airflow_ValueHanged(object sender, System.EventArgs e)
        {
            CalculateRoomMollierPoint();
        }

        private void ParameterControl_LatentLoad_ValueHanged(object sender, System.EventArgs e)
        {
            CalculateRoomMollierPoint();
            SetSensibleHeatRatio();
        }

        private void ParameterControl_SensibleLoad_ValueHanged(object sender, System.EventArgs e)
        {
            CalculateRoomMollierPoint();
            SetSensibleHeatRatio();
        }

        private void SetSensibleHeatRatio()
        {
            ParameterControl_SensibleLoadRatio.Value = 0;

            double sensible = ParameterControl_SensibleLoad.Value;
            if (double.IsNaN(sensible))
            {
                return;
            }

            double latent = ParameterControl_LatentLoad.Value;
            if (double.IsNaN(latent))
            {
                return;
            }

            if (sensible == 0 && latent == 0)
            {
                return;
            }

            ParameterControl_SensibleLoadRatio.Value = Core.Query.Round(Mollier.Query.SensibleHeatRatio(sensible, latent), Tolerance.MacroDistance);

            ParameterControl_Epsilon.Value = Core.Query.Round(Mollier.Query.Epsilon_BySensibleAndLatentGain(sensible, latent), 0);

            //if (latent < 0)
            //{
            //    sensible = -sensible;
            //}

            //ParameterControl_SensibleLoadRatio.Value = Core.Query.Round(sensible / (System.Math.Abs(sensible) + System.Math.Abs(latent)), Tolerance.MacroDistance);
        }

        private void MollierPointControl_Start_ValueHanged(object sender, System.EventArgs e)
        {
            CalculateRoomMollierPoint();

            ParameterControl_HumidityRatio_Start.Value = MollierPointControl_Start.MollierPoint == null ? 0 : Core.Query.Round(MollierPointControl_Start.MollierPoint.HumidityRatio * 1000, Tolerance.MacroDistance);
        }

        public UIMollierProcess GetUIMollierProcess()
        {
            RoomProcess RoomProcess = GetRoomProcess();
            if (RoomProcess == null)
            {
                return null;
            }

            return new UIMollierProcess(RoomProcess, Color.Empty);
        }

        public RoomProcess GetRoomProcess()
        {
            mollierPointControl_Room.MollierPoint = null;

            MollierPoint start = MollierPointControl_Start.MollierPoint;
            if (start == null)
            {
                return null;
            }

            double airMassFlow = ParameterControl_Airflow.Value;
            if (double.IsNaN(airMassFlow))
            {
                return null;
            }

            airMassFlow *= start.Density();

            double latentHeat = ParameterControl_LatentLoad.Value;
            if (double.IsNaN(latentHeat))
            {
                return null;
            }

            double sensibleLoad = ParameterControl_SensibleLoad.Value;
            if (double.IsNaN(sensibleLoad))
            {
                return null;
            }

            return Mollier.Create.RoomProcess(start, airMassFlow, sensibleLoad * 1000, latentHeat * 1000);
        }

        public void CalculateRoomMollierPoint()
        {
            MollierPoint mollierPoint = GetRoomProcess()?.End;

            mollierPointControl_Room.MollierPoint = mollierPoint;
            ParameterControl_HumidityRatio_Room.Value = mollierPoint == null ? double.NaN : Core.Query.Round(mollierPoint.HumidityRatio * 1000, Tolerance.MacroDistance);
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

        private void MollierForm_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            if (MollierForm != null)
            {
                MollierForm.MollierPointSelected -= MollierForm_MollierPointSelected;
            }

            StartMollierPoint = e.MollierPoint;
        }

        public MollierPoint StartMollierPoint
        {
            get
            {
                return MollierPointControl_Start.MollierPoint;
            }

            set
            {
                MollierPointControl_Start.MollierPoint = value;
                ParameterControl_HumidityRatio_Start.Value = MollierPointControl_Start.MollierPoint == null ? 0 : Core.Query.Round(MollierPointControl_Start.MollierPoint.HumidityRatio * 1000, Tolerance.MacroDistance);
            }
        }

        public MollierPoint RoomMollierPoint
        {
            get
            {
                return mollierPointControl_Room.MollierPoint;
            }

            set
            {
                mollierPointControl_Room.MollierPoint = value;
            }
        }
    }
}