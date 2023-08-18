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

            Form form = control.FindForm();
            if(form == null)
            {
                return new SAM.Geometry.Planar.Vector2D(1, 1);
            }

            System.Drawing.Rectangle defaultResolution = new System.Drawing.Rectangle(0, 0, 2560, 1440);
            System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;

            double screenWidth = Screen.GetWorkingArea(form).Width;
            double screenHeight = Screen.GetWorkingArea(form).Height;

            double widthFactor = (form.ClientSize.Width / screenWidth) * ((double)defaultResolution.Width / resolution.Width);
            double heightFactor = (form.ClientSize.Height / screenHeight) * ((double)defaultResolution.Height / resolution.Height);


            MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();

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
            yFactor_Temp *= heightFactor;
            
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
