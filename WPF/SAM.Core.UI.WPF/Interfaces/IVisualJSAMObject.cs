using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public interface IVisualJSAMObject
    {
        GeometryModel3D GeometryModel3D { get; }

        bool SetHighlight(bool highlight);

        double Opacity { get; set; }

        bool SetSelected(bool selected);

        bool Similar(IJSAMObject jSAMObject);
    }
}
