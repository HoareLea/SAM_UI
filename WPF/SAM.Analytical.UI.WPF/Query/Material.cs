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

            return Geometry.UI.Create.Material(Core.UI.Convert.ToMedia(color));
        }

        public static System.Windows.Media.Media3D.Material Material(this Aperture aperture)
        {
            if (aperture == null)
            {
                return null;
            }

            System.Drawing.Color color = Analytical.Query.Color(aperture.ApertureType);

            return Geometry.UI.Create.Material(Core.UI.Convert.ToMedia(color));
        }

    }
}
