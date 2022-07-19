namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static VisibilitySettings DefaultVisibilitySettings()
        {

            VisibilitySettings result = new VisibilitySettings();
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("blue", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightBlue));


            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightGray));
            result.Add("gray", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightGray));


            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.RelativeHumidity, System.Drawing.Color.LightBlue));
            result.Add("default", new BuiltInVisibilitySetting(ChartParameterType.Line, ChartDataType.Density, System.Drawing.Color.LightBlue));


            return result;
        }
    }
}

