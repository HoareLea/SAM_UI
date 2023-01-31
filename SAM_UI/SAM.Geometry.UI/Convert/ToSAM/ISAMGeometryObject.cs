using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public static partial class Convert
    {
        public static ISAMGeometryObject ToSAM_ISAMGeometryObject(this ISAMGeometry sAMGeometry)
        {
            if(sAMGeometry == null)
            {
                return null;
            }

            ISAMGeometry3D sAMGeometry3D = sAMGeometry as ISAMGeometry3D;
            if(sAMGeometry3D == null)
            {
                if(sAMGeometry is Planar.ISAMGeometry2D)
                {
                    sAMGeometry3D = Plane.WorldXY.Convert((Planar.ISAMGeometry2D)sAMGeometry);
                }
            }

            if(sAMGeometry3D == null)
            {
                return null;
            }

            if(sAMGeometry3D is Face3D)
            {
                Face3DObject result = new Face3DObject((Face3D)sAMGeometry3D, Query.DefaultSurfaceAppearance());
                return result;
            }
            else if(sAMGeometry3D is Point3D)
            {
                return new Point3DObject((Point3D)sAMGeometry3D, Query.DefaultPointAppearance());
            }
            else if (sAMGeometry3D is Polygon3D)
            {
                return new Polygon3DObject((Polygon3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Segment3D)
            {
                return new Segment3DObject((Segment3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Shell)
            {
                ShellObject result = new ShellObject((Shell)sAMGeometry3D, Query.DefaultSurfaceAppearance());
                return result;
            }
            else if (sAMGeometry3D is Mesh3D)
            {
                Mesh3DObject result = new Mesh3DObject((Mesh3D)sAMGeometry3D, Query.DefaultSurfaceAppearance());
                return result;
            }
            else if (sAMGeometry3D is BoundingBox3D)
            {
                return new BoundingBox3DObject((BoundingBox3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Extrusion)
            {
                ExtrusionObject result = new ExtrusionObject((Extrusion)sAMGeometry3D, Query.DefaultSurfaceAppearance());
                return result;
            }
            else if (sAMGeometry3D is Polyline3D)
            {
                return new Polyline3DObject((Polyline3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Rectangle3D)
            {
                return new Rectangle3DObject((Rectangle3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Sphere)
            {
                SphereObject result = new SphereObject((Sphere)sAMGeometry3D, Query.DefaultSurfaceAppearance());
                return result;
            }
            else if (sAMGeometry3D is Triangle3D)
            {
                return new Triangle3DObject((Triangle3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }

            return null;
        }
    }
}
