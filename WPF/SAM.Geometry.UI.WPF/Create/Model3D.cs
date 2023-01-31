using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static Model3D Model3D(this Face3DObject face3DObject)
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
                    foreach(IClosedPlanar3D edge3D in face3D.GetEdge3Ds())
                    {
                        List<Segment3D> segment3Ds = (edge3D as ISegmentable3D)?.GetSegments();
                        if(segment3Ds != null)
                        {
                            foreach(Segment3D segment3D in segment3Ds)
                            {
                                Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                                model3DGroup.Children.Add(model3D);
                            }
                        }
                    }
                }
            }

            GeometryModel3D geometryModel3D = new GeometryModel3D(face3D.ToMedia3D(Query.DoubleSided()), UI.Create.Material(surfaceAppearance.Color, surfaceAppearance.Opacity));
            if(model3DGroup == null)
            {
                Core.UI.WPF.Modify.SetIJSAMObject(geometryModel3D, face3DObject);
                return geometryModel3D;
            }

            Model3DGroup result = new Model3DGroup();
            result.Children.Add(model3DGroup);
            result.Children.Add(geometryModel3D);

            Core.UI.WPF.Modify.SetIJSAMObject(result, face3DObject);

            return result;
        }

        public static Model3D Model3D(this Mesh3DObject mesh3DObject)
        {
            if (mesh3DObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = mesh3DObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Mesh3D mesh3D = mesh3DObject.Mesh3D;
            if (mesh3D == null)
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
                    List<Segment3D> segment3Ds = mesh3D.GetSegments();
                    if(segment3Ds != null)
                    {
                        foreach (Segment3D segment3D in segment3Ds)
                        {
                            Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                            model3DGroup.Children.Add(model3D);
                        }
                    }
                }
            }

            GeometryModel3D geometryModel3D = new GeometryModel3D(mesh3D.ToMedia3D(Query.DoubleSided()), UI.Create.Material(surfaceAppearance.Color, surfaceAppearance.Opacity));
            if (model3DGroup == null)
            {
                Core.UI.WPF.Modify.SetIJSAMObject(geometryModel3D, mesh3DObject);
                return geometryModel3D;
            }

            Model3DGroup result = new Model3DGroup();
            result.Children.Add(model3DGroup);
            result.Children.Add(geometryModel3D);

            Core.UI.WPF.Modify.SetIJSAMObject(result, mesh3DObject);

            return result;
        }

        public static Model3D Model3D(this ShellObject shellObject)
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

            Model3DGroup result = new Model3DGroup();
            foreach (Face3D face3D in shell.Face3Ds)
            {
                Model3D model3D = Model3D(new Face3DObject(face3D, surfaceAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, shellObject);

            return result;
        }

        public static Model3D Model3D(this Segment3DObject segment3DObject)
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

            Material material = UI.Create.Material(curveAppearance.Color, curveAppearance.Opacity);

            GeometryModel3D result = new GeometryModel3D(segment3D.ToMedia3D(curveAppearance.Thickness), material);
            Core.UI.WPF.Modify.SetIJSAMObject(result, segment3DObject);
            return result;
        }

        public static Model3D Model3D(this Polygon3DObject polygon3DObject)
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

            Model3DGroup result = new Model3DGroup();
            foreach(Segment3D segment3D in segmentable3D.GetSegments())
            {
                Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, polygon3DObject);

            return result;
        }

        public static Model3D Model3D(this Point3DObject point3DObject)
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

            Material material = UI.Create.Material(pointAppearance.Color, pointAppearance.Opacity);

            GeometryModel3D result = new GeometryModel3D(Convert.ToMedia3D(point3D, false, pointAppearance.Thickness), material);
            Core.UI.WPF.Modify.SetIJSAMObject(result, point3DObject);

            return result;
        }

        public static Model3D Model3D(this SAMGeometry3DObjectCollection sAMGeometry3DObjectCollection)
        {
            if (sAMGeometry3DObjectCollection == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjectCollection)
            {
                if (sAMGeometry3DObject is Face3DObject)
                {
                    Model3D model3D = Model3D((Face3DObject)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else if (sAMGeometry3DObject is Segment3DObject)
                {
                    Model3D model3D = Model3D((Segment3DObject)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else if (sAMGeometry3DObject is Polygon3DObject)
                {
                    Model3D model3D = Model3D((Polygon3DObject)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else if (sAMGeometry3DObject is Point3DObject)
                {
                    Model3D model3D = Model3D((Point3DObject)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else if (sAMGeometry3DObject is ShellObject)
                {
                    Model3D model3D = Model3D((ShellObject)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else if (sAMGeometry3DObject is SAMGeometry3DObjectCollection)
                {
                    Model3D model3D = Model3D((SAMGeometry3DObjectCollection)sAMGeometry3DObject);
                    result.Children.Add(model3D);
                }
                else
                {
                    Model3D model3D = Model3D(sAMGeometry3DObject as dynamic);
                    result.Children.Add(model3D as dynamic);
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, sAMGeometry3DObjectCollection);

            return result;
        }

        public static Model3D Model3D(this Text3DObject text3DObject)
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

            SolidColorBrush solidColorBrush = new SolidColorBrush(textAppearance.Color);
            solidColorBrush.Opacity = textAppearance.Opacity;

            GeometryModel3D result = Core.UI.WPF.Create.GeometryModel3D_Text(text3DObject.Text, solidColorBrush, false, textAppearance.Height, plane.Origin.ToMedia3D(), true, plane.AxisX.ToMedia3D(), plane.AxisY.ToMedia3D(), textAppearance.FontFamilyName);

            Core.UI.WPF.Modify.SetIJSAMObject(result, text3DObject);

            return result;
        }
    }
}
