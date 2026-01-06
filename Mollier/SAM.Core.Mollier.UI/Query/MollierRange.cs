namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculates mollier range for given axes length from settings 
        /// </summary>
        /// <param name="mollierControlSettings"></param>
        /// <returns>Mollier range</returns>
        public static MollierRange MollierRange(this MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings == null)
            {
                return null;
            }

            double dryBulbTemperature_Min = mollierControlSettings.Temperature_Min;
            if(double.IsNaN(dryBulbTemperature_Min))
            {
                return null;
            }
            
            double dryBulbTemperature_Max = mollierControlSettings.Temperature_Max;
            if(double.IsNaN(dryBulbTemperature_Max))
            {
                return null;
            }

            double humidtyRatio_Min = mollierControlSettings.HumidityRatio_Min;
            if (double.IsNaN(humidtyRatio_Min))
            {
                return null;
            }

            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            if (double.IsNaN(humidityRatio_Max))
            {
                return null;
            }

            return new MollierRange(dryBulbTemperature_Min, dryBulbTemperature_Max, humidtyRatio_Min, humidityRatio_Max);
        }
    }
}
