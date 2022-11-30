using System.Windows;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI
{
    public static partial class DependencyProperty
    {
        public static readonly System.Windows.DependencyProperty IJSAMObjectProperty = System.Windows.DependencyProperty.Register( "IJSAMObject", typeof(IJSAMObject), typeof(Model3D), new PropertyMetadata(null));
    }
}
