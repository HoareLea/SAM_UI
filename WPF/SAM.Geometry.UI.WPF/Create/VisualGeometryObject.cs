using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject<T> VisualGeometryObject<T>(this T sAMGeometry3DObject, Material material, double thickness = 0.001) where T: ISAMGeometry3DObject
        {
            if(sAMGeometry3DObject == null || material == null)
            {
                return null;
            }

            ISAMGeometry3D sAMGeometry3D = Spatial.Query.SAMGeometry3D<ISAMGeometry3D>(sAMGeometry3DObject);

            Model3D model3D = null;
            if(sAMGeometry3D is Face3D)
            {
                model3D = new GeometryModel3D(((Face3D)sAMGeometry3D).ToMedia3D(), material);
            }
            else if(sAMGeometry3D is ISegmentable3D)
            {
                Model3DGroup model3DGroup = new Model3DGroup();
                ((ISegmentable3D)sAMGeometry3D).GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(false, thickness), material)));

                model3D = model3DGroup;
            }
            else if(sAMGeometry3D is Spatial.Point3D)
            {
                model3D = new GeometryModel3D(Convert.ToMedia3D((Spatial.Point3D)sAMGeometry3D, false, thickness), material);
            }

            if(model3D == null)
            {
                return null;
            }

            VisualGeometryObject<T> result = new VisualGeometryObject<T>(sAMGeometry3DObject);
            result.Content = model3D;

            return result;
        }

        public static VisualGeometryObject<Face3DObject> VisualGeometryObject(this Face3DObject face3DObject)
        {
            if(face3DObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = face3DObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Material material = UI.Create.Material(surfaceAppearance.Color);

            return VisualGeometryObject(face3DObject, material);
        }

        public static VisualGeometryObject<Segment3DObject> VisualGeometryObject(this Segment3DObject segment3DObject)
        {
            if (segment3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = segment3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            Material material = UI.Create.Material(curveAppearance.Color);

            return VisualGeometryObject(segment3DObject, material, curveAppearance.Thickness);
        }

        public static VisualGeometryObject<Polygon3DObject> VisualGeometryObject(this Polygon3DObject polygon3DObject)
        {
            if (polygon3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = polygon3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            Material material = UI.Create.Material(curveAppearance.Color);

            return VisualGeometryObject(polygon3DObject, material, curveAppearance.Thickness);
        }

        public static VisualGeometryObject<Point3DObject> VisualGeometryObject(this Point3DObject point3DObject)
        {
            if (point3DObject == null)
            {
                return null;
            }

            PointAppearance pointAppearance = point3DObject.PointAppearance;
            if (pointAppearance == null)
            {
                return null;
            }

            Material material = UI.Create.Material(pointAppearance.Color);

            return VisualGeometryObject(point3DObject, material, pointAppearance.Thickness);
        }
    }
}
