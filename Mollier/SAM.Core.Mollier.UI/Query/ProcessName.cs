using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static string ProcessName(IMollierProcess mollierProcess)
        {
            if(mollierProcess is UIMollierProcess)
            {
                mollierProcess = ((UIMollierProcess)mollierProcess).MollierProcess;
            }
            string processName = "";
            if (mollierProcess is HeatingProcess)
            {
                processName = "HTG";
            }
            if (mollierProcess is CoolingProcess)
            {
                processName = "CLG";
            }
            if (mollierProcess is MixingProcess)
            {
                processName = "MX";
            }
            if (mollierProcess is SteamHumidificationProcess || mollierProcess is AdiabaticHumidificationProcess || mollierProcess is IsotermicHumidificationProcess)
            {
                processName = "HUM";
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                processName = "HR";
            }
            return processName;
        }
    }
}
