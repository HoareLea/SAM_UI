using SAM.Geometry.Object;

namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static CurveAppearance DefaultCurveAppearance()
        {
            return new CurveAppearance(System.Drawing.Color.FromArgb(0, 0, 0), 0.005);
        }

    }
}
