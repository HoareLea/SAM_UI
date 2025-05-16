using SAM.Geometry.Planar;
using System;
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

            List<string> values = new List<string>();
            values.Add(Core.Query.Description(chartDataType));
            values.Add(string.Format("[{0}]", constantValueCurve.Value.ToString()));
            values.Add(Units.Query.Abbreviation(Query.DefaultUnitType(chartDataType)));
            values.Add(Guid.NewGuid().ToString());
            if(constantValueCurve is ConstantTemperatureCurve)
            {
                values.Add(Core.Query.Description(((ConstantTemperatureCurve)constantValueCurve).Phase));
            }
            else if (constantValueCurve is ConstantEnthalpyCurve)
            {
                values.Add(Core.Query.Description(((ConstantEnthalpyCurve)constantValueCurve).Phase));
            }

            string name = string.Join(" ", values);
            Series result = chart.Series.Add(name);
            result.IsVisibleInLegend = false;
            result.Tag = constantValueCurve;
            result.BorderWidth = 1;

            ChartParameterType chartParameterType = Mollier.Query.ChartParameterType(chartDataType, constantValueCurve.Value); 

            switch (chartDataType)
            {
                case ChartDataType.RelativeHumidity:
                    result.ChartType = SeriesChartType.Line; //SeriesChartType.Spline;

                    if (constantValueCurve.Value == 50)
                    {
                        result.BorderWidth = 1;
                    }
                    else if (constantValueCurve.Value == 100)
                    {
                        result.BorderWidth = 2;
                    }
                    else
                    {
                        result.BorderWidth = 1;
                    }
                    break;

                case ChartDataType.DiagramTemperature:
                    result.ChartType = SeriesChartType.Line;
                    result.BorderWidth = chartParameterType == ChartParameterType.Line ? 1 : 2;
                    break;

                case ChartDataType.DryBulbTemperature:
                    result.ChartType = SeriesChartType.Line;
                    result.BorderWidth = chartParameterType == ChartParameterType.Line ? 1 : 2;
                    break;

                case ChartDataType.Enthalpy:
                    result.ChartType = SeriesChartType.Line;
                    result.BorderWidth = 1;
                    break;

                default:
                    result.ChartType = SeriesChartType.Line;
                    break;
            }

            
            result.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, chartParameterType, chartDataType);

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

                Point2D point2D = ToSAM(new MollierPoint(temperature, humidityRatio, mollierControlSettings.Pressure), chartType);
                if(point2D == null)
                {
                    continue;
                }
                index = result.Points.AddXY(point2D.X, point2D.Y);

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

                    double factor = result.BorderWidth == 1 ? 3 : 4;//extend all enthalpy line in UI

                    Vector2D vector2D = new Vector2D(point2D_1, point2D_2);
                    point2D_2 = point2D_2.GetMoved(vector2D.Unit * factor);
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

