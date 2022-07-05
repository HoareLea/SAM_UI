namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static Geometry.Planar.Point2D ToSAM(this System.Windows.Point point)
        {
            if(point == null)
            {
                return new Geometry.Planar.Point2D(double.NaN, double.NaN);
            }

            return new Geometry.Planar.Point2D(point.X, point.Y);
        }
    }
}
