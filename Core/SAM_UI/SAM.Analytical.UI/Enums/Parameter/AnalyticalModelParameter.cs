using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI
{
    [AssociatedTypes(typeof(AnalyticalModel)), Description("AnalyticalModel Parameter")]
    public enum AnalyticalModelParameter
    {
        [ParameterProperties("UI Geometry Settings", "UI Geometry Settings"), SAMObjectParameterValue(typeof(Geometry.UI.UIGeometrySettings))] UIGeometrySettings,
    }
}