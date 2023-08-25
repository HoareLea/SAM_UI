namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static MollierControlSettings DefaultMollierControlSettings()
        {
            MollierControlSettings mollierControlSettings = new MollierControlSettings();
            mollierControlSettings.Density_Line = false;
            mollierControlSettings.Enthalpy_Line = true;
            mollierControlSettings.SpecificVolume_Line = false;
            mollierControlSettings.WetBulbTemperature_Line = false;
            mollierControlSettings.DefaultTemplateName = "blue";
            mollierControlSettings.ChartType = ChartType.Mollier;
            return mollierControlSettings;
        }
    }
}

