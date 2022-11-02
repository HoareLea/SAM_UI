using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject<Face3DObject> VisualGeometryObject(this Face3DObject face3DObject)
        {
            if (face3DObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = face3DObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Face3D face3D = face3DObject.Face3D;
            if (face3D == null)
            {
                return null;
            }

            VisualGeometryObject<Face3DObject> result = new VisualGeometryObject<Face3DObject>(face3DObject);
            result.Content = new GeometryModel3D(face3D.ToMedia3D(true), UI.Create.Material(surfaceAppearance.Color));

            return result;
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

            Segment3D segment3D = segment3DObject.Segment3D;
            if(segment3D == null)
            {
                return null;
            }

            Material material = UI.Create.Material(curveAppearance.Color);

            Model3DGroup model3DGroup = new Model3DGroup();
            model3DGroup.Children.Add(new GeometryModel3D(segment3D.ToMedia3D(true, curveAppearance.Thickness), material));

            VisualGeometryObject <Segment3DObject> result = new VisualGeometryObject<Segment3DObject>(segment3DObject);
            result.Content = model3DGroup;

            return result;
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

            ISegmentable3D segmentable3D = polygon3DObject.Polygon3D;
            if (segmentable3D == null)
            {
                return null;
            }

            Material material = UI.Create.Material(curveAppearance.Color);

            Model3DGroup model3DGroup = new Model3DGroup();
            segmentable3D.GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, curveAppearance.Thickness), material)));

            VisualGeometryObject<Polygon3DObject> result = new VisualGeometryObject<Polygon3DObject>(polygon3DObject);
            result.Content = model3DGroup;

            return result;
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

            Spatial.Point3D point3D = point3DObject.Point3D;
            if (point3D == null)
            {
                return null;
            }

            Material material = UI.Create.Material(pointAppearance.Color);

            VisualGeometryObject<Point3DObject> result = new VisualGeometryObject<Point3DObject>(point3DObject);
            result.Content = new GeometryModel3D(Convert.ToMedia3D(point3D, true, pointAppearance.Thickness), material);

            return result;
        }

        public static VisualGeometryObject<SAMGeometry3DObjectCollection> VisualGeometryObject(this SAMGeometry3DObjectCollection sAMGeometry3DObjectCollection)
        {
            if (sAMGeometry3DObjectCollection == null)
            {
                return null;
            }

            VisualGeometryObject<SAMGeometry3DObjectCollection> result = new VisualGeometryObject<SAMGeometry3DObjectCollection>(sAMGeometry3DObjectCollection);
            foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjectCollection)
            {
                if (sAMGeometry3DObject is Face3DObject)
                {
                    VisualGeometryObject<Face3DObject> visualGeometryObject = VisualGeometryObject((Face3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Segment3DObject)
                {
                    VisualGeometryObject<Segment3DObject> visualGeometryObject = VisualGeometryObject((Segment3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Polygon3DObject)
                {
                    VisualGeometryObject<Polygon3DObject> visualGeometryObject = VisualGeometryObject((Polygon3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Point3DObject)
                {
                    VisualGeometryObject<Point3DObject> visualGeometryObject = VisualGeometryObject((Point3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is SAMGeometry3DObjectCollection)
                {
                    VisualGeometryObject<SAMGeometry3DObjectCollection> visualGeometryObject = VisualGeometryObject((SAMGeometry3DObjectCollection)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else
                {
                    IVisualGeometryObject visualGeometryObject = VisualGeometryObject(sAMGeometry3DObject as dynamic);
                    result.Children.Add(visualGeometryObject as dynamic);
                }
            }

            return result;
        }
    }
}
