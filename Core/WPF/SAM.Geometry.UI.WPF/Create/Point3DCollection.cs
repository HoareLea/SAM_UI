using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static Point3DCollection Point3DCollection(this Rect3D rect3D)
        {
            Point3DCollection result = new Point3DCollection();
            result.Add(new Point3D(rect3D.X, rect3D.Y, rect3D.Z));
            result.Add(new Point3D(rect3D.X + rect3D.SizeX, rect3D.Y, rect3D.Z));
            result.Add(new Point3D(rect3D.X, rect3D.Y + rect3D.SizeY, rect3D.Z));
            result.Add(new Point3D(rect3D.X, rect3D.Y, rect3D.Z + rect3D.SizeZ));
            result.Add(new Point3D(rect3D.X + rect3D.SizeX, rect3D.Y + rect3D.SizeY, rect3D.Z));
            result.Add(new Point3D(rect3D.X + rect3D.SizeX, rect3D.Y + rect3D.SizeY, rect3D.Z + rect3D.SizeZ));
            result.Add(new Point3D(rect3D.X, rect3D.Y + rect3D.SizeY, rect3D.Z + rect3D.SizeZ));
            result.Add(new Point3D(rect3D.X + rect3D.SizeX, rect3D.Y + rect3D.SizeY, rect3D.Z + rect3D.SizeZ));
            return result;
        }
    }
}
