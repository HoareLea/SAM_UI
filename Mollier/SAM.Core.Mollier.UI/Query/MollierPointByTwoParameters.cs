namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculate point by 2 parameters of 5 possible - (DryBulbTemperature
        /// </summary>
        /// <param name="name_1"></param>
        /// <param name="value_1"></param>
        /// <param name="name_2"></param>
        /// <param name="value_2"></param>
        /// <returns>MollierPoint</returns>
        public static MollierPoint MollierPointByTwoParameters(string name_1, double value_1, string name_2, double value_2)
        {
            if (name_1 == null || name_2 == null || value_1 == double.NaN || value_2 == double.NaN)
            {
                return null;
            }
            MollierPoint mollierPoint = null;
            return mollierPoint;
        }

        public static MollierPoint MollierPointByTwoParameters(double pressure = Standard.Pressure, double humidityRatio = double.NaN, double dryBulbTemperature = double.NaN, double relativeHumidity = double.NaN, double wetBulbTemperature = double.NaN, double dewPointTemperature = double.NaN)
        {
            //checking if there are good number of data - dew point or 2 of the others

            if (double.IsNaN(dewPointTemperature))
            {
                int dataNumber = 0;
                dataNumber = !double.IsNaN(dryBulbTemperature) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(humidityRatio) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(relativeHumidity) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(wetBulbTemperature) ? dataNumber + 1 : dataNumber;
                if(dataNumber != 2)
                {
                    return null;
                }
            }
            else
            {
                if (!double.IsNaN(dryBulbTemperature) || !double.IsNaN(humidityRatio) || !double.IsNaN(relativeHumidity) || !double.IsNaN(wetBulbTemperature))
                {
                    return null;
                }
            }


            if (!double.IsNaN(dewPointTemperature))
            {
                dryBulbTemperature = dewPointTemperature;
                relativeHumidity = 100;
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
                wetBulbTemperature = Mollier.Query.WetBulbTemperature(dryBulbTemperature, relativeHumidity, pressure);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(humidityRatio))
            {
                relativeHumidity = Mollier.Query.RelativeHumidity(dryBulbTemperature, humidityRatio, pressure);
                wetBulbTemperature = Mollier.Query.WetBulbTemperature(dryBulbTemperature, relativeHumidity, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(relativeHumidity))
            {
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
                wetBulbTemperature = Mollier.Query.WetBulbTemperature(dryBulbTemperature, relativeHumidity, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(wetBulbTemperature))
            {
                humidityRatio = Mollier.Query.HumidityRatio_ByWetBulbTemperature(dryBulbTemperature, wetBulbTemperature, pressure);
                relativeHumidity = Mollier.Query.RelativeHumidity(dryBulbTemperature, humidityRatio, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }
            else if (!double.IsNaN(humidityRatio) && !double.IsNaN(relativeHumidity))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByHumidityRatio(humidityRatio, relativeHumidity, pressure);
                wetBulbTemperature = Mollier.Query.WetBulbTemperature(dryBulbTemperature, relativeHumidity, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }
            else if (!double.IsNaN(humidityRatio) && !double.IsNaN(wetBulbTemperature))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByWetBulbTemperatureAndHumidityRatio(wetBulbTemperature, humidityRatio, pressure);
                relativeHumidity = Mollier.Query.RelativeHumidity(dryBulbTemperature, humidityRatio, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }
            else if (!double.IsNaN(relativeHumidity) && !double.IsNaN(wetBulbTemperature))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature, relativeHumidity, pressure);
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
                dewPointTemperature = Mollier.Query.DewPointTemperature(dryBulbTemperature, relativeHumidity);
            }


            //if (double.IsNaN(dryBulbTemperature) || double.IsNaN(humidityRatio) || double.IsNaN(relativeHumidity) || double.IsNaN(wetBulbTemperature) || double.IsNaN(dewPointTemperature))
            //{
            //    return null;
            //}

            MollierPoint result = new MollierPoint(dryBulbTemperature, humidityRatio, pressure);

            return result;
        }
    }
}
