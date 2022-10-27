using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public interface IVisualGeometry : Core.UI.WPF.IVisualJSAMObject
    {
        GeometryModel3D GeometryModel3D { get; }

    }
}
