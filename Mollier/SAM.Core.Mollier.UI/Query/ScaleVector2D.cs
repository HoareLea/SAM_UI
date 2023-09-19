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
            if(control == null)
            {
                return new Geometry.Planar.Vector2D(1, 1);
            }
            System.Drawing.Rectangle defaultResolution = new System.Drawing.Rectangle(0, 0, 3840, 2160);
            // System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;
            
            Screen myScreen = Screen.FromControl(control);
            System.Drawing.Rectangle resolution = myScreen.Bounds;


            //double screenWidth = Screen.GetWorkingArea(form).Width;
            //double screenHeight = Screen.GetWorkingArea(form).Height;

            //double widthFactor = (form.ClientSize.Width / screenWidth) * ((double)defaultResolution.Width / resolution.Width);
            //double heightFactor = (form.ClientSize.Height / screenHeight) * ((double)defaultResolution.Height / resolution.Height);

            double widthFactor = ((double)defaultResolution.Width / resolution.Width);
            double heightFactor = ((double)defaultResolution.Height / resolution.Height);

            var defaultDPI = 96;
            var programScale = 1.5;
            var currentDPI = (int)Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "LogPixels", 96);
            var currentScale = (double)currentDPI / defaultDPI;
            var scaling = (double)currentScale / programScale;


            // IF GRASHOPPER OPENED HASH THIS
            widthFactor *= scaling;
            heightFactor *= scaling;

            //double widthFactor = System.Math.Max(2525.0 / (double)k.Width, 1);
            //double heightFactor = System.Math.Max(1299.0 / (double)k.Height, 1);

            //widthFactor *= scaling;
            //heightFactor *= scaling;


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

            return new Geometry.Planar.Vector2D(xFactor_Temp, yFactor_Temp);
        }
    }
}
