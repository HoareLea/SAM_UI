namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Checks what process is it
        /// </summary>
        /// <param name="mollierProcess">Mollier Process</param>
        /// <returns>Returns type of process</returns>
        public static ChartDataType ChartDataType(this IMollierProcess mollierProcess)
        {
            ChartDataType process = Mollier.ChartDataType.Undefined;

            if (mollierProcess is CoolingProcess)
            {
                process = Mollier.ChartDataType.CoolingProcess;
            }
            if (mollierProcess is HeatingProcess)
            {
                process = Mollier.ChartDataType.HeatingProcess;
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                process = Mollier.ChartDataType.HeatRecoveryProcess;
            }
            if (mollierProcess is MixingProcess)
            {
                process = Mollier.ChartDataType.MixingProcess;
            }
            if (mollierProcess is SteamHumidificationProcess)
            {
                process = Mollier.ChartDataType.SteamHumidificationProcess;
            }
            if(mollierProcess is AdiabaticHumidificationProcess)
            {
                process = Mollier.ChartDataType.AdiabaticHumidificationProcess;
            }
            if(mollierProcess is IsotermicHumidificationProcess)
            {
                process = Mollier.ChartDataType.IsotermicHumidificationProcess;
            }
            return process;
        }
    }
}
