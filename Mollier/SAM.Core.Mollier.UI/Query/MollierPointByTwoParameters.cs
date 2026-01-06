// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculates mollier point by 2 given parameters or dew point parameter
        /// </summary>
        /// <param name="pressure">Pressure</param>
        /// <param name="humidityRatio">Humidity ratio</param>
        /// <param name="dryBulbTemperature">Dry bulb temperature</param>
        /// <param name="relativeHumidity">Relative humidity</param>
        /// <param name="wetBulbTemperature">Wet bulb temperature</param>
        /// <param name="dewPointTemperature">Dew point temperature</param>
        /// <param name="enthalpy">Enthalpy</param>
        /// <returns>Calculated mollier point</returns>
        public static MollierPoint MollierPointByTwoParametersOrDewPoint(double pressure = Standard.Pressure, double humidityRatio = double.NaN, double dryBulbTemperature = double.NaN, double relativeHumidity = double.NaN, double wetBulbTemperature = double.NaN, double dewPointTemperature = double.NaN, double enthalpy = double.NaN)
        {
            //checking if there are good number of data - dew point or 2 of the others

            if (double.IsNaN(dewPointTemperature))
            {
                int dataNumber = 0;
                dataNumber = !double.IsNaN(dryBulbTemperature) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(humidityRatio) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(relativeHumidity) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(wetBulbTemperature) ? dataNumber + 1 : dataNumber;
                dataNumber = !double.IsNaN(enthalpy) ? dataNumber + 1 : dataNumber;
                if (dataNumber != 2)
                {
                    return null;
                }
            }
            else
            {
                if (!double.IsNaN(dryBulbTemperature) || !double.IsNaN(humidityRatio) || !double.IsNaN(relativeHumidity) || !double.IsNaN(wetBulbTemperature) || !double.IsNaN(enthalpy))
                {
                    return null;
                }
            }


            if (!double.IsNaN(dewPointTemperature))
            {
                dryBulbTemperature = dewPointTemperature;
                relativeHumidity = 100;
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(humidityRatio))
            {
                //relativeHumidity = Mollier.Query.RelativeHumidity(dryBulbTemperature, humidityRatio, pressure);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(relativeHumidity))
            {
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
            }
            else if (!double.IsNaN(dryBulbTemperature) && !double.IsNaN(wetBulbTemperature))
            {
                humidityRatio = Mollier.Query.HumidityRatio_ByWetBulbTemperature(dryBulbTemperature, wetBulbTemperature, pressure);
            }
            else if (!double.IsNaN(humidityRatio) && !double.IsNaN(relativeHumidity))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByHumidityRatio(humidityRatio, relativeHumidity, pressure);
            }
            else if (!double.IsNaN(humidityRatio) && !double.IsNaN(wetBulbTemperature))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByWetBulbTemperatureAndHumidityRatio(wetBulbTemperature, humidityRatio, pressure);
            }
            else if (!double.IsNaN(relativeHumidity) && !double.IsNaN(wetBulbTemperature))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature, relativeHumidity, pressure);
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
            }
            else if (!double.IsNaN(enthalpy) && !double.IsNaN(humidityRatio))
            {
                dryBulbTemperature = Mollier.Query.DryBulbTemperature(enthalpy, humidityRatio, pressure);
            }

                MollierPoint result = new MollierPoint(dryBulbTemperature, humidityRatio, pressure);

            return result;
        }
    }
}
