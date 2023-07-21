using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static void AddLinesSeries(this Chart chart, MollierControlSettings mollierControlSettings)
        {
            List<Series> seriesList = null;

            //Temperature
            if(mollierControlSettings.ChartType == ChartType.Mollier)
            {
                seriesList = Convert.ToChart(ChartDataType.DiagramTemperature, chart, mollierControlSettings);
            }
            else
            {
                seriesList = Convert.ToChart(ChartDataType.DryBulbTemperature, chart, mollierControlSettings);
            }

            //Relative Humidity
            seriesList = Convert.ToChart(ChartDataType.RelativeHumidity, chart, mollierControlSettings);
            if (seriesList != null && seriesList.Count != 0)
            {
                foreach (Series series in seriesList)
                {
                    AddLabel_RelativeHumidity(series, mollierControlSettings, 5);
                }
            }

            //Density
            seriesList = Convert.ToChart(ChartDataType.Density, chart, mollierControlSettings);
            if (seriesList != null && seriesList.Count != 0)
            {
                foreach (Series series in seriesList)
                {
                    AddLabel_Unit(chart, series, mollierControlSettings);
                }

                double offset_X = mollierControlSettings.ChartType == ChartType.Mollier ? 2 : -0.5;
                double offset_Y = mollierControlSettings.ChartType == ChartType.Mollier ? -0.5 : 0.0005;

                AddLabel_Label(chart, seriesList[seriesList.Count / 2], mollierControlSettings, "Density ρ [kg/m³]", offset_X, offset_Y);
            }

            //Enthalpy
            seriesList = Convert.ToChart(ChartDataType.Enthalpy, chart, mollierControlSettings);
            if (seriesList != null && seriesList.Count != 0)
            {
                foreach (Series series in seriesList)
                {
                    AddLabel_Unit(chart, series, mollierControlSettings);
                }

                double offset_X = mollierControlSettings.ChartType == ChartType.Mollier ? -1.2 : 4.5;
                double offset_Y = mollierControlSettings.ChartType == ChartType.Mollier ? 3.2 : -0.0018;

                Series series_Temp = seriesList[seriesList.Count / 2];

                AddLabel_Label(chart, series_Temp, mollierControlSettings, "Enthalpy h [kJ/kg]", offset_X, offset_Y, series_Temp.Points.Count / 2);
            }

            //Wet Bulb Temperature
            seriesList = Convert.ToChart(ChartDataType.WetBulbTemperature, chart, mollierControlSettings);
            if (seriesList != null && seriesList.Count != 0)
            {
                foreach (Series series in seriesList)
                {
                    AddLabel_Unit(chart, series, mollierControlSettings);
                }

                double offset_X = mollierControlSettings.ChartType == ChartType.Mollier ? -1.2 : 4.5;
                double offset_Y = mollierControlSettings.ChartType == ChartType.Mollier ? 3.2 : -0.0018;

                Series series_Temp = seriesList[seriesList.Count / 2];

                AddLabel_Label(chart, series_Temp, mollierControlSettings, "Wet Bulb Temperature t_wb [°C]", offset_X, offset_Y, series_Temp.Points.Count - 1);
            }

            //Specific Volume
            seriesList = Convert.ToChart(ChartDataType.SpecificVolume, chart, mollierControlSettings);
            if (seriesList != null && seriesList.Count != 0)
            {
                foreach (Series series in seriesList)
                {
                    Modify.AddLabel_Unit(chart, series, mollierControlSettings);
                }

                double offset_X = mollierControlSettings.ChartType == ChartType.Mollier ? -3 : 4.5;
                double offset_Y = mollierControlSettings.ChartType == ChartType.Mollier ? 0 : -0.0018;

                Series series_Temp = seriesList[seriesList.Count / 2];

                AddLabel_Label(chart, series_Temp, mollierControlSettings, "Specific volume v [m³/kg]", offset_X, offset_Y, series_Temp.Points.Count / 2);
            }
        }
    }
}
