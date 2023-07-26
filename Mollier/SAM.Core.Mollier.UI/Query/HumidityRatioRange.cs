﻿namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Range<double> HumidityRatioRange(this MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings == null)
            {
                return null;
            }

            double humidtyRatio_Min = mollierControlSettings.HumidityRatio_Min;
            if(double.IsNaN(humidtyRatio_Min))
            {
                return null;
            }
            
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            if(double.IsNaN(humidityRatio_Max))
            {
                return null;
            }

            return new Range<double>(humidtyRatio_Min, humidityRatio_Max);

        }
    }
}
