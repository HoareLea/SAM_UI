using System.ComponentModel;

namespace SAM.Core.Mollier.UI
{
    public enum DisplayPointType
    {
        [Description("Undefined")] Undefined,
        [Description("Default Point")] Default,
        [Description("Process Point")] Process,
        [Description("Cooling Saturation Point")] CoolingSaturation,
        [Description("Dew Point")] Dew,
        [Description("Room Point")] Room,

    }
}
