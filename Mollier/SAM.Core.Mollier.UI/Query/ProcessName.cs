namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Saves short process name value
        /// </summary>
        /// <param name="mollierProcess">Mollier process</param>
        /// <returns>Process name</returns>
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
            if(mollierProcess is RoomProcess)
            {
                processName = "ROOM";
            }
            return processName;
        }
    }
}
