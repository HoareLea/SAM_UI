using System.Windows.Media;

namespace SAM.Geometry.UI
{
    public static partial class Create
    {
        public static System.Windows.Media.Media3D.Material Material(this Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);

            System.Windows.Media.Media3D.DiffuseMaterial result = new System.Windows.Media.Media3D.DiffuseMaterial(brush);
            return result;
        }
    }
}
