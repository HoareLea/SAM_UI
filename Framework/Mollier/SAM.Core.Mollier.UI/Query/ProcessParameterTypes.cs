using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Convert process calculation type into process parameter type
        /// </summary>
        /// <param name="processCalculationType">Process calculation type</param>
        /// <returns>Process parameter type</returns>
        public static List<ProcessParameterType> ProcessParameterTypes(this ProcessCalculationType processCalculationType)
        {
            switch(processCalculationType)
            {
                case ProcessCalculationType.DryBulbTemperature:
                    return new List<ProcessParameterType>() { ProcessParameterType.DryBulbTemperature };
                case ProcessCalculationType.DryBulbTemperatureDifference:
                    return new List<ProcessParameterType>() { ProcessParameterType.DryBulbTemperatureDifference };
                case ProcessCalculationType.HumidityRatioDifference:
                    return new List<ProcessParameterType>() { ProcessParameterType.HumidityRatioDifference };
                case ProcessCalculationType.SpecificEnthalpyDifference:
                    return new List<ProcessParameterType>() { ProcessParameterType.SpecificEnthalpyDifference };
                case ProcessCalculationType.MediumAndDryBulbTemperature:
                    return new List<ProcessParameterType>() { ProcessParameterType.FlowTemperature, ProcessParameterType.ReturnTemperature, ProcessParameterType.DryBulbTemperature };
                case ProcessCalculationType.MediumAndEfficiency:
                    return new List<ProcessParameterType>() { ProcessParameterType.FlowTemperature, ProcessParameterType.ReturnTemperature, ProcessParameterType.Efficiency };
                case ProcessCalculationType.RelativeHumidity:
                    return new List<ProcessParameterType>() { ProcessParameterType.RelativeHumidity };
            }
            return null;
        }
    }
}
