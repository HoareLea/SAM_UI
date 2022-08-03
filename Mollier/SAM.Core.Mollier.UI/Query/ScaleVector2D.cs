using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Geometry.Planar.Vector2D ScaleVector2D(Control control, MollierControlSettings mollierControlSettings)
        {
            double xFactor = 1896, yFactor = 944;

            MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();
            double xFactor_Temp = control.Size.Width / xFactor * (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) / mollierControlSettings_Default.HumidityRatio_Max;
            double yFactor_Temp = control.Size.Height / yFactor * (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / mollierControlSettings_Default.Temperature_Max;

            return new Geometry.Planar.Vector2D(xFactor_Temp, yFactor_Temp);
        }
    }
}
