using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Geometry.UI
{
    [AssociatedTypes(typeof(GeometryObjectModel)), Description("GeometryObjectModel Parameter")]
    public enum GeometryObjectModelParameter
    {
        [ParameterProperties("View Settings", "View Settings"), SAMObjectParameterValue(typeof(ViewSettings))] ViewSettings,
    }
}