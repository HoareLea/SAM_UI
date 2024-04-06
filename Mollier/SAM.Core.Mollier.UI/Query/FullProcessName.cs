namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Saves full process name
        /// </summary>
        /// <param name="mollierProcess">Mollier process</param>
        /// <returns>Full process name</returns>
        public static string FullProcessName(this IMollierProcess mollierProcess)
        {
            if(mollierProcess is UIMollierProcess)
            {
                mollierProcess = ((UIMollierProcess)mollierProcess).MollierProcess;
            }
            string processName = "";
            if (mollierProcess is HeatingProcess)
            {
                processName = "Heating Process";
            }
            if(mollierProcess is FanProcess)
            {
                processName = "Fan Process";
            }
            if (mollierProcess is CoolingProcess)
            {
                processName = "Cooling Process";
            }
            if (mollierProcess is MixingProcess)
            {
                processName = "Mixing Process";
            }
            if (mollierProcess is SteamHumidificationProcess || mollierProcess is AdiabaticHumidificationProcess || mollierProcess is IsothermalHumidificationProcess)
            {
                processName = "Humidification Process";
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                processName = "Heat Recovery Process";
            }
            if(mollierProcess is RoomProcess)
            {
                processName = "Room Process";
            }
            
            return processName;
        }
    }
}
