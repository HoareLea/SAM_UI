using SAM.Geometry.Object;

namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static PointAppearance DefaultPointAppearance()
        {
            return new PointAppearance(System.Drawing.Color.FromArgb(0, 0, 0), 0.001);
        }

    }
}
