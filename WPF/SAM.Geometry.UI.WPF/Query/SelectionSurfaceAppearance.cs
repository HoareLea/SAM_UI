namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static SurfaceAppearance SelectionSurfaceAppearance()
        {
            return new SurfaceAppearance(System.Windows.Media.Color.FromRgb(125, 125, 255), System.Windows.Media.Color.FromRgb(0, 0, 255), 0.01);
        }

    }
}
