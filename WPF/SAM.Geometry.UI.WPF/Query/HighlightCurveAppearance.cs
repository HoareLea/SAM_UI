namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static CurveAppearance HighlightCurveAppearance(CurveAppearance curveAppearance)
        {
            return new CurveAppearance(curveAppearance.Color, 0.02);
        }

    }
}
