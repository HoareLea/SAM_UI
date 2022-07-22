namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static ChartDataType ChartDataType(this IMollierProcess mollierProcess)
        {
            ChartDataType process = UI.ChartDataType.Undefined;

            if (mollierProcess is CoolingProcess)
            {
                process = UI.ChartDataType.CoolingProcess;
            }
            if (mollierProcess is HeatingProcess)
            {
                process = UI.ChartDataType.HeatingProcess;
            }
            if (mollierProcess is HeatRecoveryProcess)
            {
                process = UI.ChartDataType.HeatRecoveryProcess;
            }
            if (mollierProcess is MixingProcess)
            {
                process = UI.ChartDataType.MixingProcess;
            }
            if (mollierProcess is SteamHumidificationProcess)
            {
                process = UI.ChartDataType.SteamHumidificationProcess;
            }
            return process;
        }
    }
}
