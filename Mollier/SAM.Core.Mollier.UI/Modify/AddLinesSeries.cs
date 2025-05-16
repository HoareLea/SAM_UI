using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            if(chart == null || mollierControlSettings == null)
            {
                return null;
            }

            ChartDataType[] chartDataTypes = new ChartDataType[] { ChartDataType.DryBulbTemperature, ChartDataType.RelativeHumidity, ChartDataType.Density, ChartDataType.Enthalpy, ChartDataType.SpecificVolume, ChartDataType.WetBulbTemperature };
            List<List<ConstantValueCurve>> constantValueCurvesList = Enumerable.Repeat<List<ConstantValueCurve>>(null, chartDataTypes.Length).ToList();

            Parallel.For(0, chartDataTypes.Length, (int i) => 
            {
                constantValueCurvesList[i] = Query.ConstantValueCurves(chartDataTypes[i], mollierControlSettings);
            });

            List<Series> result = new List<Series>();
            foreach (List<ConstantValueCurve> constantValueCurves in constantValueCurvesList)
            {
                if(constantValueCurves == null)
                {
                    continue;
                }

                List<Series> series = Convert.ToChart(constantValueCurves, chart, mollierControlSettings);
                if(series == null)
                {
                    continue;
                }

                result.AddRange(series);
            }

            return result;
        }
    }
}
