using System.ComponentModel;
using SAM.Core.Attributes;
using SAM.Geometry.Object;

namespace SAM.Geometry.UI
{
    [AssociatedTypes(typeof(GeometryObjectModel)), Description("GeometryObjectModel Parameter")]
    public enum GeometryObjectModelParameter
    {
        [ParameterProperties("View Settings", "View Settings"), SAMObjectParameterValue(typeof(ViewSettings))] ViewSettings,
    }
}