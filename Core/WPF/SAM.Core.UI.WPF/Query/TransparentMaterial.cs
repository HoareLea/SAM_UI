using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static System.Windows.Media.Media3D.Material TransaprentMaterial()
        {
            return new EmissiveMaterial(Brushes.Transparent);
        }
    }
}
