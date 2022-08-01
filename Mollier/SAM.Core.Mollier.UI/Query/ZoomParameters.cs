using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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
    }
}
