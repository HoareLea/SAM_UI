using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static System.Windows.Media.Media3D.Material Material(this Panel panel)
        {
            if(panel == null)
            {
                return null;
            }

            return Material(Analytical.Query.Color(panel).ToMedia());
        }

        public static System.Windows.Media.Media3D.Material Material(this Aperture aperture)
        {
            if (aperture == null)
            {
                return null;
            }

            return Material(Analytical.Query.Color(aperture.ApertureType).ToMedia());
        }

        public static System.Windows.Media.Media3D.Material Material(this Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);

            System.Windows.Media.Media3D.DiffuseMaterial result = new System.Windows.Media.Media3D.DiffuseMaterial(brush);
            return result;
        }
    }
}
