using System.ComponentModel;
using SAM.Core;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI.WPF
{
    [AssociatedTypes(typeof(Setting)), Description("Analytical Setting Parameter")]
    public enum AnalyticalSettingParameter
    {
        [ParameterProperties("TBD Conversion Options", "TBD Conversion Options"), SAMObjectParameterValue(typeof(TBDConversionOptions))] TBDConversionOptions,
    }
}