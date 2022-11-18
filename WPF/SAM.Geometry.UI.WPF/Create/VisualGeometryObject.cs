using SAM.Geometry.Spatial;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject VisualGeometryObject(this Face3DObject face3DObject)
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

            Model3DGroup model3DGroup = null;

            CurveAppearance curveAppearance = surfaceAppearance.CurveAppearance;
            if(curveAppearance != null)
            {
                if(curveAppearance.Thickness != 0)
                {
                    model3DGroup = new Model3DGroup();

                    Material material = UI.Create.Material(curveAppearance.Color);

                    foreach(IClosedPlanar3D edge3D in face3D.GetEdge3Ds())
                    {
                        ISegmentable3D segmentable3D = edge3D as ISegmentable3D;
                        if(segmentable3D != null)
                        {
                            segmentable3D.GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, curveAppearance.Thickness), material)));
                        }
                    }
                }
            }

            VisualGeometryObject result = new VisualGeometryObject(face3DObject);

            GeometryModel3D geometryModel3D = new GeometryModel3D(face3D.ToMedia3D(true), UI.Create.Material(surfaceAppearance.Color));
            if(model3DGroup != null && model3DGroup.Children.Count != 0)
            {
                model3DGroup.Children.Add(geometryModel3D);
                result.Content = model3DGroup;
            }
            else
            {
                result.Content = geometryModel3D;
            }

            return result;
        }

        public static VisualGeometryObject VisualGeometryObject(this ShellObject shellObject)
        {
            if (shellObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = shellObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Shell shell = shellObject.Shell;
            if (shell == null)
            {
                return null;
            }

            Model3DGroup model3DGroup = null;

            CurveAppearance curveAppearance = surfaceAppearance.CurveAppearance;
            if (curveAppearance != null)
            {
                if (curveAppearance.Thickness != 0)
                {
                    model3DGroup = new Model3DGroup();

                    Material material = UI.Create.Material(curveAppearance.Color);

                    foreach (IClosedPlanar3D edge3D in shell.GetEdge3Ds())
                    {
                        ISegmentable3D segmentable3D = edge3D as ISegmentable3D;
                        if (segmentable3D != null)
                        {
                            segmentable3D.GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, curveAppearance.Thickness), material)));
                        }
                    }
                }
            }

            VisualGeometryObject result = new VisualGeometryObject(shellObject);

            GeometryModel3D geometryModel3D = new GeometryModel3D(shell.ToMedia3D(true), UI.Create.Material(surfaceAppearance.Color));
            if (model3DGroup != null && model3DGroup.Children.Count != 0)
            {
                model3DGroup.Children.Add(geometryModel3D);
                result.Content = model3DGroup;
            }
            else
            {
                result.Content = geometryModel3D;
            }

            return result;
        }

        public static VisualGeometryObject VisualGeometryObject(this Segment3DObject segment3DObject)
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

            VisualGeometryObject result = new VisualGeometryObject(segment3DObject);
            result.Content = model3DGroup;

            return result;
        }

        public static VisualGeometryObject VisualGeometryObject(this Polygon3DObject polygon3DObject)
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

            VisualGeometryObject result = new VisualGeometryObject(polygon3DObject);
            result.Content = model3DGroup;

            return result;
        }

        public static VisualGeometryObject VisualGeometryObject(this Point3DObject point3DObject)
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

            VisualGeometryObject result = new VisualGeometryObject(point3DObject);
            result.Content = new GeometryModel3D(Convert.ToMedia3D(point3D, true, pointAppearance.Thickness), material);

            return result;
        }

        public static VisualGeometryObject VisualGeometryObject(this SAMGeometry3DObjectCollection sAMGeometry3DObjectCollection)
        {
            if (sAMGeometry3DObjectCollection == null)
            {
                return null;
            }

            VisualGeometryObject result = new VisualGeometryObject(sAMGeometry3DObjectCollection);
            foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjectCollection)
            {
                if (sAMGeometry3DObject is Face3DObject)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((Face3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Segment3DObject)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((Segment3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Polygon3DObject)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((Polygon3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is Point3DObject)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((Point3DObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is ShellObject)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((ShellObject)sAMGeometry3DObject);
                    result.Children.Add(visualGeometryObject);
                }
                else if (sAMGeometry3DObject is SAMGeometry3DObjectCollection)
                {
                    VisualGeometryObject visualGeometryObject = VisualGeometryObject((SAMGeometry3DObjectCollection)sAMGeometry3DObject);
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

        public static VisualGeometryObject VisualGeometryObject(this Text3DObject text3DObject)
        {
            if (text3DObject == null)
            {
                return null;
            }

            TextAppearance textAppearance = text3DObject.TextAppearance;
            if (textAppearance == null)
            {
                return null;
            }


            Plane plane = text3DObject.Plane;
            if(plane == null)
            {
                return null;
            }

            VisualGeometryObject result = new VisualGeometryObject(text3DObject);
            result.Content = Core.UI.WPF.Create.GeometryModel3D_Text(text3DObject.Text, new SolidColorBrush(textAppearance.Color), true, textAppearance.Height, plane.Origin.ToMedia3D(), true, plane.AxisX.ToMedia3D(), plane.AxisY.ToMedia3D(), textAppearance.FontFamilyName);

            return result;
        }
    }
}
