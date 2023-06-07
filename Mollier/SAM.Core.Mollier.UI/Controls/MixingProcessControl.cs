using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MixingProcessControl : UserControl
    {

        public MixingProcessControl()
        {
            InitializeComponent();
            FirstAirflowControl.ProcessParameterType = ProcessParameterType.Airflow;
            SecondAirflowControl.ProcessParameterType = ProcessParameterType.Airflow;
            MollierPointControl_SecondPoint.PressureVisible = false;
        }


        public UIMollierProcess CreateMixingProcess()
        {
            IMollierProcess mollierProcess = null;

            MollierPointControl_SecondPoint.Pressure = MollierPointControl_FirstPoint.Pressure;
            MollierPoint firstPoint = FirstPoint;
            MollierPoint secondPoint = SecondPoint;
            double airflow_1 = FirstAirflowControl.Value;
            double airflow_2 = SecondAirflowControl.Value;
            mollierProcess = Mollier.Create.MixingProcess(firstPoint, secondPoint, airflow_1, airflow_2);  
            return new UIMollierProcess(mollierProcess, Color.Empty);
        }
        public MollierPoint FirstPoint
        {
            get
            {
                return MollierPointControl_FirstPoint.MollierPoint;
            }

            set
            {
                MollierPointControl_FirstPoint.MollierPoint = value;
            }
        }
        public MollierPoint SecondPoint
        {
            get
            {
                return MollierPointControl_SecondPoint.MollierPoint;
            }
            set
            {
                MollierPointControl_SecondPoint.MollierPoint = value;
            }
        }

    }
}
