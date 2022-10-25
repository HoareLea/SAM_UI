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

            System.Drawing.Color color = Analytical.Query.Color(panel);

            return Geometry.UI.WPF.Query.Material(Geometry.UI.WPF.Convert.ToMedia(color));
        }

        public static System.Windows.Media.Media3D.Material Material(this Aperture aperture)
        {
            if (aperture == null)
            {
                return null;
            }

            System.Drawing.Color color = Analytical.Query.Color(aperture.ApertureType);

            return Geometry.UI.WPF.Query.Material(Geometry.UI.WPF.Convert.ToMedia(color));
        }

    }
}
