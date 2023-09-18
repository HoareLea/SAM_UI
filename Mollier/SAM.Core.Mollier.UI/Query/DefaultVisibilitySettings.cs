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

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RelativeHumidity, Default.RelativeHumidity_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.RelativeHumidity, Default.RelativeHumidity_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
           
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Density, Default.Density_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Density, System.Drawing.Color.Gray));
            
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Enthalpy, Default.Enthalpy_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SpecificVolume, Default.SpecificVolume_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.WetBulbTemperature, Default.WetBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));
            //result.Add("default", new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Green));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DiagramTemperature, Default.DryBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.DarkGray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DryBulbTemperature, Default.DryBulbTemperature_Color));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.DarkGray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.UndefinedProcess, System.Drawing.Color.Gray));


            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Density, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));


            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RoomProcess, System.Drawing.Color.Gray));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Density, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.Gray));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RoomProcess, System.Drawing.Color.Gray));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.RelativeHumidity, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Density, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Density, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.Enthalpy, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.SpecificVolume, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Unit, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Black));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Label, Mollier.ChartDataType.WetBulbTemperature, System.Drawing.Color.Black));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, Mollier.ChartDataType.DryBulbTemperature, System.Drawing.Color.LightBlue));


            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.IsotermicHumidificationProcess, System.Drawing.Color.MediumPurple));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.AdiabaticHumidificationProcess, System.Drawing.Color.Purple));
            result.Add("blue-black", new BuiltInVisibilitySetting(ChartParameterType.Line, Mollier.ChartDataType.RoomProcess, System.Drawing.Color.Gray));

            return result;
        }
    }
}

