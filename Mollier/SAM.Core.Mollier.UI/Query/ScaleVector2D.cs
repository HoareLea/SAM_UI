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
            //double widthFactor = 1500, heightFactor = 725;
            //double xFactor_2 = 1339 yFactor = 775

            double widthFactor = (double)1501 / (double)control.Size.Width;
            double heigthFactor = (double)723 / (double)control.Size.Height;



            MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();
            //double xFactor_Temp = control.Size.Width / xFactor * (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / mollierControlSettings_Default.HumidityRatio_Max;
            //double yFactor_Temp = control.Size.Height / yFactor * (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / mollierControlSettings_Default.Temperature_Max;
            double xFactor_Temp = (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / (mollierControlSettings_Default.HumidityRatio_Max - mollierControlSettings_Default.HumidityRatio_Min);
            double yFactor_Temp = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings_Default.Temperature_Max - mollierControlSettings_Default.Temperature_Min);

            xFactor_Temp *= widthFactor;
            yFactor_Temp *= heigthFactor;
            
            if(mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                double temp = xFactor_Temp;
                xFactor_Temp = yFactor_Temp;
                yFactor_Temp = temp;
            }
            return new Geometry.Planar.Vector2D(xFactor_Temp, yFactor_Temp);
        }
    }
}
