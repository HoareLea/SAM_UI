namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Saves the default colors
        /// </summary>
        /// <returns>Returns default dictionary with color theme key and list of components with defualt colors</returns>
        public static VisibilitySettings DefaultVisibilitySettings()
        {

            VisibilitySettings result = new VisibilitySettings();

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, Default.RelativeHumidity_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.MediumLine, ChartDataType.RelativeHumidity, Default.RelativeHumidity_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.RelativeHumidity, Default.RelativeHumidity_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
           
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, Default.Density_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.Gray));
            
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, Default.Enthalpy_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Enthalpy, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SpecificVolume, Default.SpecificVolume_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.WetBulbTemperature, Default.WetBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DiagramTemperature, Default.DryBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DiagramTemperature, System.Drawing.Color.DarkGray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DryBulbTemperature, Default.DryBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature, System.Drawing.Color.DarkGray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RoomProcess, System.Drawing.Color.Gray));


            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.MediumLine, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, System.Drawing.Color.FromArgb(255,216,237,243)));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));


            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RoomProcess, System.Drawing.Color.Gray));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.MediumLine, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Enthalpy, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DryBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature, System.Drawing.Color.Gray));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RoomProcess, System.Drawing.Color.Gray));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.MediumLine, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Enthalpy, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.SpecificVolume, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.SpecificVolume, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.WetBulbTemperature, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.WetBulbTemperature, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));


            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RoomProcess, System.Drawing.Color.Gray));

            return result;
        }
    }
}

