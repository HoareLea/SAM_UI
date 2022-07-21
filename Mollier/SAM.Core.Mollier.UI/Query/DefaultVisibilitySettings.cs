namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static VisibilitySettings DefaultVisibilitySettings()
        {

            VisibilitySettings result = new VisibilitySettings();

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
           
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightGreen));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.Gray));
            
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, System.Drawing.Color.LightGray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.Enthalpy, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Enthalpy, System.Drawing.Color.Black));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Enthalpy, System.Drawing.Color.Black));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.SpecificVolume, System.Drawing.Color.LightPink));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.SpecificVolume, System.Drawing.Color.Gray));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.WetBulbTemperature, System.Drawing.Color.LightSalmon));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.WetBulbTemperature, System.Drawing.Color.Gray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.DiagramTemperature, System.Drawing.Color.LightGray));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.BoldLine, ChartDataType.DiagramTemperature, System.Drawing.Color.DarkGray));


            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Unit, ChartDataType.Density, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Label, ChartDataType.Density, System.Drawing.Color.LightBlue));

            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Enthalpy, System.Drawing.Color.LightBlue));
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
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.Gray));
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
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Point, System.Drawing.Color.Blue));

            return result;
        }
    }
}

