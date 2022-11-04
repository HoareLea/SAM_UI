using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Geometry.UI
{
    [AssociatedTypes(typeof(GeometryObjectModel)), Description("GeometryObjectModel Parameter")]
    public enum GeometryObjectModelParameter
    {
        [ParameterProperties("Section Plane", "Section Plane"), SAMObjectParameterValue(typeof(Spatial.Plane))] SectionPlane,
    }
}