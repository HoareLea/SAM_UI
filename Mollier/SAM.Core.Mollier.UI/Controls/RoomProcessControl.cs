using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class RoomProcessControl : UserControl
    {

        public RoomProcessControl()
        {
            InitializeComponent();

            ParameterControl_HumidityRatio_Start.ProcessParameterType = ProcessParameterType.HumidityRatioDifference;

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

            ParameterControl_HumidityRatio_Start.Enabled = false;

            mollierPointControl_Room.Enabled = false;

            MollierPointControl_Start.ValueHanged += MollierPointControl_Start_ValueHanged;
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
            if(double.IsNaN(sensible))
            {
                return;
            }

            double latent = ParameterControl_LatentLoad.Value;
            if (double.IsNaN(latent))
            {
                return;
            }

            if(sensible == 0 && latent == 0)
            {
                return;
            }

            ParameterControl_SensibleLoadRatio.Value = sensible / (sensible + latent);
        }

        private void MollierPointControl_Start_ValueHanged(object sender, System.EventArgs e)
        {
            CalculateRoomMollierPoint();

            ParameterControl_HumidityRatio_Start.Value = MollierPointControl_Start.MollierPoint == null ? 0 : Core.Query.Round(MollierPointControl_Start.MollierPoint.HumidityRatio, Tolerance.Distance);
        }

        public void CalculateRoomMollierPoint()
        {
            mollierPointControl_Room.MollierPoint = null;

            MollierPoint start = MollierPointControl_Start.MollierPoint;
            if(start == null)
            {
                return;
            }

            double airFlow = ParameterControl_Airflow.Value;
            if(double.IsNaN(airFlow))
            {
                return;
            }


            double latentHeat = ParameterControl_LatentLoad.Value;
            if (double.IsNaN(latentHeat))
            {
                return;
            }

            double sensibleLoad = ParameterControl_SensibleLoad.Value;
            if (double.IsNaN(sensibleLoad))
            {
                return;
            }

            UndefinedProcess undefinedProcess = Mollier.Create.UndefinedProcess(start, airFlow, sensibleLoad * 1000, latentHeat * 1000);
            if(undefinedProcess == null)
            {
                return;
            }

            mollierPointControl_Room.MollierPoint = undefinedProcess.End;
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
                ParameterControl_HumidityRatio_Start.Value = MollierPointControl_Start.MollierPoint == null ? 0 : Core.Query.Round(MollierPointControl_Start.MollierPoint.HumidityRatio, Tolerance.Distance);
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
