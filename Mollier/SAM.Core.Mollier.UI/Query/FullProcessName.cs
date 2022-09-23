using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static string FullProcessName(IMollierProcess mollierProcess)
        {
            if(mollierProcess is UIMollierProcess)
            {
                mollierProcess = ((UIMollierProcess)mollierProcess).MollierProcess;
            }
            string processName = "";
            if (mollierProcess is HeatingProcess)
            {
                processName = "Heating";
            }
            if (mollierProcess is CoolingProcess)
            {
                processName = "Cooling";
            }
            if (mollierProcess is MixingProcess)
            {
                processName = "Mixing";
            }
            if (mollierProcess is SteamHumidificationProcess || mollierProcess is AdiabaticHumidificationProcess || mollierProcess is IsotermicHumidificationProcess)
            {
                processName = "Humidification";
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                processName = "Heat Recovery";
            }
            
            return processName;
        }
    }
}
