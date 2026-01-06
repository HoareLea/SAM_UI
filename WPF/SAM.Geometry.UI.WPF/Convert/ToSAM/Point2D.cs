namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static Planar.Point2D ToSAM(this System.Windows.Point point)
        {
            if(point == null)
            {
                return new Planar.Point2D(double.NaN, double.NaN);
            }

            return new Planar.Point2D(point.X, point.Y);
        }
    }
}
