using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Material TransaprentMaterial()
        {
            return new EmissiveMaterial(Brushes.Transparent);
        }
    }
}
