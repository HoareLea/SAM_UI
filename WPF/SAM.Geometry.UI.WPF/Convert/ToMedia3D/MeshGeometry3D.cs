using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static MeshGeometry3D ToMedia3D(this Face3D face3D, bool doubleSided = false)
        {
            return ToMedia3D(Spatial.Create.Mesh3D(face3D), doubleSided);
        }

        public static MeshGeometry3D ToMedia3D(this Mesh3D mesh3D, bool doubleSided = false)
        {
            if(mesh3D == null)
            {
                return null;
            }

            MeshGeometry3D result = new MeshGeometry3D();

            List<Spatial.Point3D> point3Ds = mesh3D.GetPoints();
            if(point3Ds != null)
            {
                foreach(Spatial.Point3D point3D in point3Ds)
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

                    Spatial.Vector3D vector3D = mesh3D.GetNormal(i);
                    if (vector3D == null)
                    {
                        continue;
                    }

                    vector3D.Normalize();

                    result.TriangleIndices.Add(tuple.Item1);
                    result.TriangleIndices.Add(tuple.Item2);
                    result.TriangleIndices.Add(tuple.Item3);
                    result.Normals.Add(vector3D.ToMedia3D());

                    if(doubleSided)
                    {
                        result.TriangleIndices.Add(tuple.Item3);
                        result.TriangleIndices.Add(tuple.Item2);
                        result.TriangleIndices.Add(tuple.Item1);
                        result.Normals.Add(vector3D.ToMedia3D());
                    }
                }
            }

            return result;
        }

        public static MeshGeometry3D ToMedia3D(this Shell shell, bool doubleSided = false)
        {
            return ToMedia3D(shell.Mesh3D(), doubleSided);
        }

        public static MeshGeometry3D ToMedia3D(this Sphere sphere, bool doubleSided = false)
        {
            if(sphere == null)
            {
                return null;
            }

            Mesh3D mesh3D = Spatial.Create.Mesh3D(sphere, 0.01);
            if(mesh3D == null)
            {
                return null;
            }

            return ToMedia3D(mesh3D, doubleSided);
        }

        public static MeshGeometry3D ToMedia3D(this ISegmentable3D segmentable3D, double thickness = Core.Tolerance.MacroDistance)
        {
            return ToMedia3D(new ISegmentable3D[] { segmentable3D }, thickness);
        }

        public static MeshGeometry3D ToMedia3D(this Spatial.Point3D point3D, bool doubleSided = false, double thickness = Core.Tolerance.MacroDistance)
        {
            if(point3D == null)
            {
                return null;
            }

            Sphere sphere = new Sphere(point3D, thickness);
            if (sphere == null)
            {
                return null;
            }


            return ToMedia3D(sphere, doubleSided);
        }

        public static MeshGeometry3D ToMedia3D<T>(this IEnumerable<T> segmentable3Ds, double thickness = Core.Tolerance.MacroDistance) where T: ISegmentable3D
        {
            if(segmentable3Ds == null)
            {
                return null;
            }

            HelixToolkit.Wpf.MeshBuilder meshBuilder = new HelixToolkit.Wpf.MeshBuilder();
            foreach (T segmentable3D in segmentable3Ds)
            {
                List<Segment3D> segment3Ds = segmentable3D?.GetSegments();
                if (segment3Ds == null)
                {
                    continue; ;
                }

                foreach(Segment3D segment3D in segment3Ds)
                {
                    if (segment3D == null)
                    {
                        continue;
                    }

                    meshBuilder.AddCylinder(segment3D[0].ToMedia3D(), segment3D[1].ToMedia3D(), thickness);
                    meshBuilder.AddSphere(segment3D[0].ToMedia3D(), thickness);
                    meshBuilder.AddSphere(segment3D[1].ToMedia3D(), thickness);
                }
            }


            return meshBuilder.ToMesh();
        }
    }
}
