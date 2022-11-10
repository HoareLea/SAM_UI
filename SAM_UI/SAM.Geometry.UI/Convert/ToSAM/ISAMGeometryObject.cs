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
            else if(sAMGeometry3D is Spatial.Point3D)
            {
                return new Point3DObject((Spatial.Point3D)sAMGeometry3D, Query.DefaultPointAppearance());
            }
            else if (sAMGeometry3D is Polygon3D)
            {
                return new Polygon3DObject((Polygon3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }
            else if (sAMGeometry3D is Segment3D)
            {
                return new Segment3DObject((Segment3D)sAMGeometry3D, Query.DefaultCurveAppearance());
            }


            return null;
        }
    }
}
