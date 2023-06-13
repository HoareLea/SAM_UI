using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculates the axis scales from the initial, default values
        /// </summary>
        /// <param name="control">Control</param>
        /// <param name="mollierControlSettings">Mollier Control Settings</param>
        /// <returns>Returns Vector which contains the scale</returns>
        public static Geometry.Planar.Vector2D ScaleVector2D(Control control, MollierControlSettings mollierControlSettings)
        {
            //double widthFactor = 1500, heightFactor = 725; dpi 96
            //double xFactor_2 = 1339 yFactor = 907 120

            double widthFactor = 1501 / (double)control.Size.Width;
            double heigthFactor = 723 / (double)control.Size.Height;



            MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();
            //double xFactor_Temp = control.Size.Width / xFactor * (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / mollierControlSettings_Default.HumidityRatio_Max;
            //double yFactor_Temp = control.Size.Height / yFactor * (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / mollierControlSettings_Default.Temperature_Max;
            double humidityRatioProportion = (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / (mollierControlSettings_Default.HumidityRatio_Max - mollierControlSettings_Default.HumidityRatio_Min);
            double temperatureProportion = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings_Default.Temperature_Max - mollierControlSettings_Default.Temperature_Min);


            double xFactor_Temp = humidityRatioProportion;
            double yFactor_Temp = temperatureProportion;
            if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                double temp = xFactor_Temp;
                xFactor_Temp = yFactor_Temp;
                yFactor_Temp = temp;
            }

            xFactor_Temp *= widthFactor;
            yFactor_Temp *= heigthFactor;
            
            if(control.DeviceDpi == 120)
            {
                Math.LinearEquation linearEquation = Math.Create.LinearEquation(96.0, 1.0, 120.0, 1.2);

                double constant =  linearEquation.Evaluate(control.DeviceDpi);
                xFactor_Temp *= constant;
                yFactor_Temp *= constant;
            }

            //return new Geometry.Planar.Vector2D(1, 1);
            return new Geometry.Planar.Vector2D(xFactor_Temp, yFactor_Temp);
        }
    }
}
