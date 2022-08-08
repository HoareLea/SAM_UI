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

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
           
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Density, System.Drawing.Color.LightGreen));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Density, System.Drawing.Color.Gray));
            
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Enthalpy, System.Drawing.Color.LightGray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Enthalpy, System.Drawing.Color.Black));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SpecificVolume, System.Drawing.Color.LightPink));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightSalmon));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));
            //result.Add("default", new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Green));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.LightGray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.DarkGray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.UndefinedProcess, System.Drawing.Color.Gray));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Density, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.SpecificVolume, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.UndefinedProcess, System.Drawing.Color.Gray));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Density, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.Enthalpy, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Unit, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Label, UI.ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, UI.ChartDataType.DiagramTemperature, System.Drawing.Color.Gray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.CoolingProcess, System.Drawing.Color.Blue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatingProcess, System.Drawing.Color.Red));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.HeatRecoveryProcess, System.Drawing.Color.IndianRed));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.MixingProcess, System.Drawing.Color.Green));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.SteamHumidificationProcess, System.Drawing.Color.LightBlue));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, UI.ChartDataType.UndefinedProcess, System.Drawing.Color.Gray));

            return result;
        }
    }
}

