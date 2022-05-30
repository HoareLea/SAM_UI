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

            SolidColorBrush brush = new SolidColorBrush(Analytical.Query.Color(panel).ToMedia());

            System.Windows.Media.Media3D.DiffuseMaterial result = new System.Windows.Media.Media3D.DiffuseMaterial(brush);
            return result;
        }
    }
}
