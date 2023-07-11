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

            string mask = "t = {3} °C\nx = {1}{2}\nφ = {0} %\nt_wb = {6} °C\nt_tau = {12} °C (DewPoint)\nh = {5} kJ/kg\nρ = {8} kg/m³\nv = {7} m³/kg\np = {4} Pa\npS = {9}Pa\npW = {10}Pa\npL = {11}Pa";
            if (name != null && name != "")
            {
                mask = string.Format("{0}\n{1}", name, mask);
            }

            switch (chartType)
            {
                case ChartType.Psychrometric:
                    return string.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.1), Core.Query.Round(mollierPoint.HumidityRatio*1000, 0.01), " g/kg", Core.Query.Round(mollierPoint.DryBulbTemperature, 0.01), mollierPoint.Pressure, Core.Query.Round(mollierPoint.Enthalpy / 1000, 0.01), Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(), 0.001), Core.Query.Round(mollierPoint.Density(), 0.001), Core.Query.Round(mollierPoint.SaturationVapourPressure(), 0.1), Core.Query.Round(mollierPoint.PartialVapourPressure(), 0.1), Core.Query.Round(mollierPoint.PartialDryAirPressure(), 0.1), Core.Query.Round(mollierPoint.DewPointTemperature(), 0.1));

                case ChartType.Mollier:
                    return string.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.1), Core.Query.Round(mollierPoint.HumidityRatio * 1000, 0.01), " g/kg", Core.Query.Round(mollierPoint.DryBulbTemperature, 0.01), mollierPoint.Pressure, Core.Query.Round(mollierPoint.Enthalpy / 1000, 0.01), Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(), 0.001), Core.Query.Round(mollierPoint.Density(), 0.001), Core.Query.Round(mollierPoint.SaturationVapourPressure(), 0.1), Core.Query.Round(mollierPoint.PartialVapourPressure(), 0.1), Core.Query.Round(mollierPoint.PartialDryAirPressure(), 0.1), Core.Query.Round(mollierPoint.DewPointTemperature(), 0.1));
            }
            return null;
        }

        public static string ToolTipText(MollierPoint start, MollierPoint end, ChartType chartType, string name = null)
        {
            if (start == null || end == null)
            {
                return null;
            }
            string mask = "Δt = {0} °C\nΔx = {1} {3}\nΔh = {2} kJ/kg\nε = {4} kJ/kg\n*ε=Δh/Δx";
            if (name != null && name != "")
            {
                mask = string.Format("{0}\n{1}", name, mask);
            }
            
            switch (chartType)
            {
                case ChartType.Mollier:
                    return string.Format(mask, System.Math.Round(System.Math.Round(end.DryBulbTemperature, 2) - System.Math.Round(start.DryBulbTemperature, 2), 2), System.Math.Round(System.Math.Round(end.HumidityRatio * 1000, 2) - System.Math.Round(start.HumidityRatio * 1000, 2), 2), System.Math.Round(System.Math.Round(end.Enthalpy / 1000, 2) - System.Math.Round(start.Enthalpy / 1000, 2), 2), "g/kg", System.Math.Round(Mollier.Query.Epsilon(start, end), 0));
                
                case ChartType.Psychrometric:
                    return string.Format(mask, System.Math.Round(end.DryBulbTemperature, 2) - System.Math.Round(start.DryBulbTemperature, 2), System.Math.Round((System.Math.Round(end.HumidityRatio, 5) - System.Math.Round(start.HumidityRatio, 5)) * 1000, 2), System.Math.Round(System.Math.Round(end.Enthalpy / 1000, 2) - System.Math.Round(start.Enthalpy / 1000, 2), 2), "g/kg", System.Math.Round(Mollier.Query.Epsilon(start, end), 0));
            }

            return null;
        }
    }
}
