namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns default mollier control settings values
        /// </summary>
        /// <returns>Default mollier control settings</returns>
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

