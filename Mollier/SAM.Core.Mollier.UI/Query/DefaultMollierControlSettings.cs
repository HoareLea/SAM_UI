namespace SAM.Core.Mollier.UI
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
            mollierControlSettings.DefaultTemplateName = "blue";
            mollierControlSettings.ChartType = ChartType.Mollier;
            return mollierControlSettings;
        }
    }
}

