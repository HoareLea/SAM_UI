using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Creates series for all the chart base lines
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddLinesSeries(this Chart chart, MollierControlSettings mollierControlSettings)
        {
            List<Series> result = new List<Series>();

            List<Series> dryBulbTemperatureSeries = Convert.ToChart(ChartDataType.DryBulbTemperature, chart, mollierControlSettings);
            if(dryBulbTemperatureSeries != null)
            {
                result.AddRange(dryBulbTemperatureSeries);
            }
            List<Series> relativeHumiditySeries = Convert.ToChart(ChartDataType.RelativeHumidity, chart, mollierControlSettings);
            if(relativeHumiditySeries != null)
            {
                result.AddRange(relativeHumiditySeries);
            }
            List<Series> densitySeries = Convert.ToChart(ChartDataType.Density, chart, mollierControlSettings);
            if (densitySeries != null)
            {
                result.AddRange(densitySeries);
            }
            List<Series> enthalpySeries = Convert.ToChart(ChartDataType.Enthalpy, chart, mollierControlSettings);
            if (enthalpySeries != null)
            {
                result.AddRange(enthalpySeries);
            }
            List<Series> specificVolumeSeries = Convert.ToChart(ChartDataType.SpecificVolume, chart, mollierControlSettings);
            if (specificVolumeSeries != null)
            {
                result.AddRange(specificVolumeSeries);
            }
            List<Series> wetBulbTemperatureSeries = Convert.ToChart(ChartDataType.WetBulbTemperature, chart, mollierControlSettings);
            if (wetBulbTemperatureSeries != null)
            {
                result.AddRange(wetBulbTemperatureSeries);
            }

            return result;
        }
    }
}
