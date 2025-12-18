using SAM.Analytical.Tas;
using SAM.Core.Tas;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static WorkflowSettings DefaultWorkflowSettings()
        {
            return new WorkflowSettings()
            {
                Path_TBD = null,
                Path_gbXML = null,
                WeatherData = null,
                DesignDays_Heating = null,
                DesignDays_Cooling = null,
                SurfaceOutputSpecs = [new SurfaceOutputSpec("Tas.Simulate")
                    {
                        SolarGain = true,
                        Conduction = true,
                        ApertureData = false,
                        Condensation = false,
                        Convection = false,
                        LongWave = false,
                        Temperature = false
                    }],
                UnmetHours = false,
                Simulate = true,
                Sizing = true,
                UpdateZones = true,
                UseWidths = false,
                AddIZAMs = true,
                SimulateFrom = 1,
                SimulateTo = 365,
                RemoveExistingTBD = false,
            };
        }
    }
}
