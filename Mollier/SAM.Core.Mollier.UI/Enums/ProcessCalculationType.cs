using System.ComponentModel;

namespace SAM.Core.Mollier.UI
{
    public enum ProcessCalculationType
    {
        //description identycznie do calculation type nazwy
        [Description("Undefined")] Undefined,
        [Description("Dry Bulb Temperature")] DryBulbTemperature,
        [Description("Dry Bulb Temperature Difference")] DryBulbTemperatureDifference,
        [Description("Enthalpy Difference")] SpecificEnthalpyDifference,
        [Description("Relative Humidity")] RelativeHumidity,
        [Description("Humidity Ratio Difference")] HumidityRatioDifference,  
        [Description("Medium and Dry Bulb Temperature")] MediumAndDryBulbTemperature,
        [Description("Medium and Efficiency")] MediumAndEfficiency,
    }
}
