using SAM.Geometry.Object;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static SurfaceAppearance SelectionSurfaceAppearance()
        {
            return new SurfaceAppearance(System.Drawing.Color.FromArgb(125, 125, 255), System.Drawing.Color.FromArgb(0, 0, 255), 0.01);
        }

    }
}
