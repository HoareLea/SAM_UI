namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Caluclates dew point for cooling process
        /// </summary>
        /// <param name="coolingProcess">Cooling process</param>
        /// <returns>Dew point</returns>
        public static MollierPoint DewPoint(this CoolingProcess coolingProcess)
        {
            if(coolingProcess == null)
            {
                return null;
            }

            MollierPoint start = coolingProcess.Start;

            if(coolingProcess.Efficiency == 1)
            {
                return Mollier.Query.DewPoint(start);
            }

            MollierPoint end = coolingProcess.End;

            double relativeHumidity = end.RelativeHumidity;
            double dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByHumidityRatio(start.HumidityRatio, end.RelativeHumidity, start.Pressure);

            return Mollier.Create.MollierPoint_ByRelativeHumidity(dryBulbTemperature, relativeHumidity, end.Pressure);
        }
    }
}
