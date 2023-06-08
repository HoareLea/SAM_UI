namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Returns data that should be displayed after hover over the point
        /// </summary>
        /// <param name="mollierPoint"></param>
        /// <param name="chartType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ToolTipText(this MollierPoint mollierPoint, ChartType chartType, string name = null)
        {
            if (mollierPoint == null)
            {
                return null;
            }

            string mask = "t = {3} °C\nx = {1}{2}\nφ = {0} %\nt_wb = {6} °C\nh = {5} kJ/kg\nρ = {8} kg/m³\np = {4} Pa\nv = {7} m³/kg";
            if (name != null && name != "")
            {
                mask = name + "\nt = {3} °C\nx = {1}{2}\nφ = {0} %\nt_wb = {6} °C\nh = {5} kJ/kg\nρ = {8} kg/m³\np = {4} Pa\nv = {7} m³/kg";
            }

            switch (chartType)
            {
                case ChartType.Psychrometric:
                    return string.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.01), Core.Query.Round(mollierPoint.HumidityRatio, Tolerance.MacroDistance), " kg/kg", Core.Query.Round(mollierPoint.DryBulbTemperature, 0.01), mollierPoint.Pressure, Core.Query.Round(mollierPoint.Enthalpy / 1000, 0.01), Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(), 0.01), Core.Query.Round(mollierPoint.Density(), 0.01));

                case ChartType.Mollier:
             
                    return string.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.01), Core.Query.Round(mollierPoint.HumidityRatio * 1000, 0.01), " g/kg", Core.Query.Round(mollierPoint.DryBulbTemperature, 0.01), mollierPoint.Pressure, Core.Query.Round(mollierPoint.Enthalpy / 1000, 0.01), Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(), 0.01), Core.Query.Round(mollierPoint.Density(), 0.01));
            }
            return null;
        }
    }
}
