namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Range<double> DryBulbTemperatureRange(this MollierControlSettings mollierControlSettings)
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

            return new Range<double>(dryBulbTemperature_Min, dryBulbTemperature_Max);

        }
    }
}
