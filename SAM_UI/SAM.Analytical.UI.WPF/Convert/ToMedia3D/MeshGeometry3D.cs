using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static MeshGeometry3D ToMedia3D(this Face3D face3D, bool doubleSided = false)
        {
            return ToMedia3D(Geometry.Spatial.Create.Mesh3D(face3D), doubleSided);
        }

        public static MeshGeometry3D ToMedia3D(this Mesh3D mesh3D, bool doubleSided = false)
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

                    if(doubleSided)
                    {
                        result.TriangleIndices.Add(tuple.Item3);
                        result.TriangleIndices.Add(tuple.Item2);
                        result.TriangleIndices.Add(tuple.Item1);
                    }
                }
            }

            return result;
        }

        public static MeshGeometry3D ToMedia3D(this ISegmentable3D segmentable3D, bool doubleSided = false, double thickness = Core.Tolerance.MacroDistance)
        {
            return ToMedia3D(new ISegmentable3D[] { segmentable3D }, doubleSided, thickness);
        }

        public static MeshGeometry3D ToMedia3D<T>(this IEnumerable<T> segmentable3Ds, bool doubleSided = false, double thickness = Core.Tolerance.MacroDistance) where T: ISegmentable3D
        {
            if(segmentable3Ds == null)
            {
                return null;
            }

            MeshGeometry3D result = new MeshGeometry3D();

            foreach (T segmentable3D in segmentable3Ds)
            {
                List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                if (segment3Ds == null)
                {
                    continue; ;
                }

                foreach (Segment3D segment3D in segment3Ds)
                {
                    if (segment3D == null)
                    {
                        continue;
                    }

                    Plane plane = new Plane(segment3D.GetStart(), segment3D.Direction);
                    if (plane == null)
                    {
                        continue;
                    }

                    Geometry.Spatial.Vector3D vector3D_X = plane.AxisX;
                    vector3D_X.Scale(thickness);

                    Geometry.Spatial.Vector3D vector3D_Y = plane.AxisY;
                    vector3D_Y.Scale(thickness);

                    Geometry.Spatial.Point3D point_1 = segment3D.GetStart();
                    Geometry.Spatial.Point3D point_2 = segment3D.GetEnd();

                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_1.GetMoved(vector3D_X)));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_1.GetMoved(vector3D_X.GetNegated())));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_2.GetMoved(vector3D_X)));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_2.GetMoved(vector3D_X.GetNegated())));

                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_1.GetMoved(vector3D_Y)));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_1.GetMoved(vector3D_Y.GetNegated())));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_2.GetMoved(vector3D_Y)));
                    result.Positions.Add(ToMedia3D((Geometry.Spatial.Point3D)point_2.GetMoved(vector3D_Y.GetNegated())));

                    result.TriangleIndices.Add(0);
                    result.TriangleIndices.Add(1);
                    result.TriangleIndices.Add(2);

                    result.TriangleIndices.Add(3);
                    result.TriangleIndices.Add(0);
                    result.TriangleIndices.Add(2);

                    result.TriangleIndices.Add(4);
                    result.TriangleIndices.Add(5);
                    result.TriangleIndices.Add(6);

                    result.TriangleIndices.Add(7);
                    result.TriangleIndices.Add(4);
                    result.TriangleIndices.Add(6);

                    if (doubleSided)
                    {
                        result.TriangleIndices.Add(2);
                        result.TriangleIndices.Add(1);
                        result.TriangleIndices.Add(0);

                        result.TriangleIndices.Add(2);
                        result.TriangleIndices.Add(0);
                        result.TriangleIndices.Add(3);

                        result.TriangleIndices.Add(6);
                        result.TriangleIndices.Add(5);
                        result.TriangleIndices.Add(4);

                        result.TriangleIndices.Add(6);
                        result.TriangleIndices.Add(4);
                        result.TriangleIndices.Add(7);
                    }
                }
            }

            return result;
        }
    }
}
