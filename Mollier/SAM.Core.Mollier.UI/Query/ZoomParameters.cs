using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static void ZoomParameters(List<IMollierProcess> mollierProcesses, out double humidityRatio_Min, out double humidityRatio_Max, out double temperature_Min, out double temperature_Max)
        {
            //returns axis lenght after zoom 
            humidityRatio_Min = 1e9;
            humidityRatio_Max = -1e9;
            temperature_Min = 1e9;
            temperature_Max = -1e9;
            foreach (IMollierProcess process in mollierProcesses)
            {
                MollierPoint start = process.Start;
                MollierPoint end = process.End;
                if (humidityRatio_Min > System.Math.Min(start.HumidityRatio * 1000, end.HumidityRatio * 1000))
                {
                    humidityRatio_Min = System.Math.Floor(System.Math.Min(start.HumidityRatio * 1000, end.HumidityRatio * 1000) / 5) * 5;
                    if((humidityRatio_Min == start.HumidityRatio * 1000 || humidityRatio_Min == end.HumidityRatio * 1000) && humidityRatio_Min != 0)
                    {
                        humidityRatio_Min -= 5;
                    }
                }
                if(humidityRatio_Max < System.Math.Max(start.HumidityRatio * 1000, end.HumidityRatio * 1000))
                {
                    humidityRatio_Max = System.Math.Ceiling(System.Math.Max(start.HumidityRatio * 1000, end.HumidityRatio * 1000) / 5) * 5;
                    if (humidityRatio_Max == start.HumidityRatio * 1000 || humidityRatio_Max == end.HumidityRatio * 1000)
                    {
                        humidityRatio_Max += 5;
                    }
                }
                if (temperature_Min > System.Math.Min(start.DryBulbTemperature, end.DryBulbTemperature))
                {
                    temperature_Min = System.Math.Floor(System.Math.Min(start.DryBulbTemperature, end.DryBulbTemperature) / 5) * 5;
                    if (temperature_Min == start.DryBulbTemperature || temperature_Min == end.DryBulbTemperature)
                    {
                        temperature_Min -= 5;
                    }
                }

                if(temperature_Max < System.Math.Max(start.DryBulbTemperature, end.DryBulbTemperature))
                {
                    temperature_Max = System.Math.Ceiling(System.Math.Max(start.DryBulbTemperature, end.DryBulbTemperature) / 5) * 5;
                    if(temperature_Max == start.DryBulbTemperature || temperature_Max == end.DryBulbTemperature)
                    {
                        temperature_Max += 5;
                    }
                }
            }
        }
        public static void ZoomParameters(IEnumerable<Series> series_List, out double x_Min, out double x_Max, out double y_Min, out double y_Max, double x_Factor = 5, double y_Factor = 5)
        {
            List<DataPoint> dataPoints = new List<DataPoint>();
            foreach (Series series in series_List)
            {
                if (!(series.Tag is IMollierProcess) && !(series.Tag is List<MollierPoint>))
                {
                    continue;
                }


                foreach (DataPoint dataPoint in series.Points)
                {
                    dataPoints.Add(dataPoint);
                }
            }
            x_Min = double.MaxValue;
            x_Max = double.MinValue;
            y_Min = double.MaxValue;
            y_Max = double.MinValue;
            foreach (DataPoint dataPoint in dataPoints)
            {
                if (dataPoint.XValue > x_Max)
                {
                    x_Max = dataPoint.XValue;
                }
                if (dataPoint.XValue < x_Min)
                {
                    x_Min = dataPoint.XValue;
                }

                if (dataPoint.YValues[0] > y_Max)
                {
                    y_Max = dataPoint.YValues[0];
                }
                if (dataPoint.YValues[0] < y_Min)
                {
                    y_Min = dataPoint.YValues[0];
                }
            }
            x_Min = x_Min % x_Factor == 0 ? System.Math.Floor(x_Min / x_Factor) * x_Factor - x_Factor : System.Math.Floor(x_Min / x_Factor) * x_Factor;
            x_Max = x_Max % x_Factor == 0 ? System.Math.Ceiling(x_Max / x_Factor) * x_Factor - x_Factor : System.Math.Ceiling(x_Max / x_Factor) * x_Factor;
            y_Min = y_Min % y_Factor == 0 ? System.Math.Floor(y_Min / y_Factor) * y_Factor - y_Factor : System.Math.Floor(y_Min / y_Factor) * y_Factor;
            y_Max = y_Max % y_Factor == 0 ? System.Math.Ceiling(y_Max / y_Factor) * y_Factor - y_Factor : System.Math.Ceiling(y_Max / y_Factor) * y_Factor;
        }
    }
}
