using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static Series ToChart(this ConstantValueCurve constantValueCurve, Chart chart, MollierControlSettings mollierControlSettings)
        {
            if(constantValueCurve == null || chart == null || mollierControlSettings == null)
            {
                return null;
            }

            if(chart.Series == null)
            {
                return null;
            }

            List<MollierPoint> mollierPoints = constantValueCurve.MollierPoints;
            if (mollierPoints == null || mollierPoints.Count == 0)
            {
                return null;
            }
            
            ChartDataType chartDataType = constantValueCurve.ChartDataType;

            string name = string.Format("{0} {1} [{2}]", Core.Query.Description(chartDataType), constantValueCurve.Value, Units.Query.Abbreviation(Query.DefaultUnitType(chartDataType)));
            Series result = chart.Series.Add(name);
            result.IsVisibleInLegend = false;
            result.Tag = constantValueCurve;
            result.BorderWidth = 1;

            ChartParameterType chartParameterType = ChartParameterType.Line;  

            switch (chartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    result.ChartType = SeriesChartType.Spline;
                    chartParameterType = constantValueCurve.Value != 100 ? ChartParameterType.Line : ChartParameterType.BoldLine;
                    result.BorderWidth = chartParameterType == ChartParameterType.Line ? 1 : 2;
                    break;

                case ChartDataType.DiagramTemperature:
                    result.ChartType = SeriesChartType.Line;
                    chartParameterType = constantValueCurve.Value % 10 != 0 ? ChartParameterType.Line : ChartParameterType.BoldLine;
                    result.BorderWidth = chartParameterType == ChartParameterType.Line ? 1 : 2;
                    break;

                case ChartDataType.Enthalpy:
                    result.ChartType = SeriesChartType.Line;
                    chartParameterType = constantValueCurve.Value % 10000 != 0 ? ChartParameterType.Line : ChartParameterType.BoldLine;
                    result.BorderWidth = chartParameterType == ChartParameterType.Line ? 1 : 2;
                    break;

                default:
                    result.ChartType = SeriesChartType.Line;
                    break;
            }

            
            result.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, chartParameterType, chartDataType);

            ChartType chartType = mollierControlSettings.ChartType;

            List<DataPoint> dataPoints = new List<DataPoint>();
            foreach (MollierPoint mollierPoint in mollierPoints)
            {
                double temperature = mollierPoint.DryBulbTemperature;
                if (double.IsNaN(temperature) || double.IsInfinity(temperature))
                {
                    continue;
                }

                double humidityRatio = mollierPoint.HumidityRatio;
                if (double.IsNaN(humidityRatio) || double.IsInfinity(humidityRatio))
                {
                    continue;
                }

                int index = -1;
                if (chartType == ChartType.Mollier)
                {
                    if(chartDataType != ChartDataType.DiagramTemperature && chartDataType != ChartDataType.Enthalpy)
                    {
                        temperature = Mollier.Query.DiagramTemperature(mollierPoint);
                        if (double.IsNaN(temperature) || double.IsInfinity(temperature))
                        {
                            continue;
                        }
                    }

                    humidityRatio = humidityRatio * 1000;
                    index = result.Points.AddXY(humidityRatio, temperature);
                }
                else
                {
                    index = result.Points.AddXY(temperature, humidityRatio);
                }

                if(index != -1)
                {
                    dataPoints.Add(result.Points[index]);
                }
            }

            if(chartDataType == ChartDataType.Enthalpy)
            {
                if (dataPoints.Count > 1)
                {
                    Point2D point2D_1 = dataPoints[0].ToSAM();
                    Point2D point2D_2 = dataPoints[dataPoints.Count - 1].ToSAM();

                    double factor = result.BorderWidth == 1 ? 2 : 3;

                    Vector2D vector2D = new Vector2D(point2D_1, point2D_2);
                    point2D_2 = point2D_2 + (vector2D.Unit * factor);
                    result.Points.Add(point2D_2.ToChart());
                }
            }

            return result;
        }
    
        public static List<Series> ToChart(this IEnumerable<ConstantValueCurve> constantValueCurves, Chart chart, MollierControlSettings mollierControlSettings)
        {
            if(constantValueCurves == null || constantValueCurves.Count() == 0 || chart == null ||  mollierControlSettings == null)
            {
                return null;
            }

            List<Series> result = new List<Series>();
            foreach (ConstantValueCurve constantValueCurve in constantValueCurves)
            {
                Series series = constantValueCurve.ToChart(chart, mollierControlSettings);
                if (series == null)
                {
                    continue;
                }

                result.Add(series);
            }

            return result;
        }

        public static List<Series> ToChart(ChartDataType chartDataType, Chart chart, MollierControlSettings mollierControlSettings)
        {
            if(chartDataType == ChartDataType.Undefined || chart == null || mollierControlSettings == null)
            {
                return null;
            }

            List<ConstantValueCurve> constantValueCurves = Query.ConstantValueCurves(chartDataType, mollierControlSettings);
            if(constantValueCurves == null || constantValueCurves.Count == 0)
            {
                return null;
            }

            return ToChart(constantValueCurves, chart, mollierControlSettings);
        }

    }
}

