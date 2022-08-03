using SAM.Core.Mollier.UI;

namespace SAM.Analytical.UI.Grasshopper
{
    public static partial class Query
    {
        public static MollierControlSettings DefaultMollierControlSettings()
        {
            MollierControlSettings mollierControlSettings = new MollierControlSettings();
            mollierControlSettings.Density_line = false;
            mollierControlSettings.Enthalpy_line = true;
            mollierControlSettings.SpecificVolume_line = false;
            mollierControlSettings.WetBulbTemperature_line = false;
            mollierControlSettings.Color = "blue";
            mollierControlSettings.ChartType = Core.Mollier.ChartType.Mollier;
            return mollierControlSettings;
        }
    }
}

