using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static MeshGeometry3D ToMedia3D(this Face3D face3D)
        {
            return ToMedia3D(Geometry.Spatial.Create.Mesh3D(face3D));
        }

        public static MeshGeometry3D ToMedia3D(this Mesh3D mesh3D)
        {
            if(mesh3D == null)
            {
                return null;
            }

            MeshGeometry3D result = new MeshGeometry3D();

            List<Geometry.Spatial.Point3D> point3Ds = mesh3D.GetPoints();
            if(point3Ds != null)
            {
                foreach(Geometry.Spatial.Point3D point3D in point3Ds)
                {
                    if(point3D == null || !point3D.IsValid())
                    {
                        continue;
                    }

                    result.Positions.Add(point3D.ToMedia3D());
                }
            }

            int count = mesh3D.TrianglesCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    Tuple<int, int, int> tuple = mesh3D.GetTriangleIndexes(i);
                    if(tuple == null)
                    {
                        continue;
                    }

                    result.TriangleIndices.Add(tuple.Item1);
                    result.TriangleIndices.Add(tuple.Item2);
                    result.TriangleIndices.Add(tuple.Item3);
                }
            }

            return result;
        }
    }
}
