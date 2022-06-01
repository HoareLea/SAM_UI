using SAM.Core;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public interface IVisualSAMObject
    {
        GeometryModel3D GeometryModel3D { get; }

        bool SetHighlight(bool highlight);
    }
}
