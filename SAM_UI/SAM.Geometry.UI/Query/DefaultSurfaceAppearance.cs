namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static SurfaceAppearance DefaultSurfaceAppearance()
        {
            CurveAppearance curveAppearance = DefaultCurveAppearance();

            return new SurfaceAppearance(System.Windows.Media.Color.FromRgb(128, 128, 128), curveAppearance.Color, curveAppearance.Thickness);
        }

    }
}
