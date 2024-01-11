using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static Model3D Model3D(this ISAMGeometryObject sAMGeometryObject)
        {
            if(sAMGeometryObject == null)
            {
                return null;
            }

            //SAMGeometryObjectCollection
            if (sAMGeometryObject is SAMGeometryObjectCollection)
            {
                return Model3D((SAMGeometryObjectCollection)sAMGeometryObject);
            }

            //SAMGeometry3DObjectCollection
            if (sAMGeometryObject is SAMGeometry3DObjectCollection)
            {
                return Model3D((SAMGeometry3DObjectCollection)sAMGeometryObject);
            }

            //Text3DObject
            if (sAMGeometryObject is Text3DObject)
            {
                return Model3D((Text3DObject)sAMGeometryObject);
            }

            //IFace3DObject
            if (sAMGeometryObject is IFace3DObject)
            {
                IFace3DObject value = (IFace3DObject)sAMGeometryObject;

                if(value is Face3DObject)
                {
                    return Model3D((Face3DObject)sAMGeometryObject);
                }

                return Model3D(new Face3DObject(value.Face3D, UI.Query.DefaultSurfaceAppearance()));
            }

            //IMesh3DObject
            if (sAMGeometryObject is IMesh3DObject)
            {
                IMesh3DObject value = (IMesh3DObject)sAMGeometryObject;

                if (value is Mesh3DObject)
                {
                    return Model3D((Mesh3DObject)sAMGeometryObject);
                }

                return Model3D(new Mesh3DObject(value.Mesh3D, UI.Query.DefaultSurfaceAppearance()));
            }

            //IShellObject
            if (sAMGeometryObject is IShellObject)
            {
                IShellObject value = (IShellObject)sAMGeometryObject;

                if (value is ShellObject)
                {
                    return Model3D((ShellObject)sAMGeometryObject);
                }

                return Model3D(new ShellObject(value.Shell, UI.Query.DefaultSurfaceAppearance()));
            }

            //ISegment3DObject
            if (sAMGeometryObject is ISegment3DObject)
            {
                ISegment3DObject value = (ISegment3DObject)sAMGeometryObject;

                if (value is Segment3DObject)
                {
                    return Model3D((Segment3DObject)sAMGeometryObject);
                }

                return Model3D(new Segment3DObject(value.Segment3D, UI.Query.DefaultCurveAppearance()));
            }

            //IPolygon3DObject
            if (sAMGeometryObject is IPolygon3DObject)
            {
                IPolygon3DObject value = (IPolygon3DObject)sAMGeometryObject;

                if (value is Polygon3DObject)
                {
                    return Model3D((Polygon3DObject)sAMGeometryObject);
                }

                return Model3D(new Polygon3DObject(value.Polygon3D, UI.Query.DefaultCurveAppearance()));
            }

            //IPoint3DObject
            if (sAMGeometryObject is IPoint3DObject)
            {
                IPoint3DObject value = (IPoint3DObject)sAMGeometryObject;

                if (value is Point3DObject)
                {
                    return Model3D((Point3DObject)sAMGeometryObject);
                }

                return Model3D(new Point3DObject(value.Point3D, UI.Query.DefaultPointAppearance()));
            }

            //IPolyline3DObject
            if (sAMGeometryObject is IPolyline3DObject)
            {
                IPolyline3DObject value = (IPolyline3DObject)sAMGeometryObject;

                if (value is Polyline3DObject)
                {
                    return Model3D((Polyline3DObject)sAMGeometryObject);
                }

                return Model3D(new Polyline3DObject(value.Polyline3D, UI.Query.DefaultCurveAppearance()));
            }

            //IBoundingBox3DObject
            if (sAMGeometryObject is IBoundingBox3DObject)
            {
                IBoundingBox3DObject value = (IBoundingBox3DObject)sAMGeometryObject;

                if (value is BoundingBox3DObject)
                {
                    return Model3D((BoundingBox3DObject)sAMGeometryObject);
                }

                return Model3D(new BoundingBox3DObject(value.BoundingBox3D, UI.Query.DefaultCurveAppearance()));
            }

            //IExtrusionObject
            if (sAMGeometryObject is IExtrusionObject)
            {
                IExtrusionObject value = (IExtrusionObject)sAMGeometryObject;

                if (value is ExtrusionObject)
                {
                    return Model3D((ExtrusionObject)sAMGeometryObject);
                }

                return Model3D(new ExtrusionObject(value.Extrusion, UI.Query.DefaultSurfaceAppearance()));
            }

            //IRectangle3DObject
            if (sAMGeometryObject is IRectangle3DObject)
            {
                IRectangle3DObject value = (IRectangle3DObject)sAMGeometryObject;

                if (value is Rectangle3DObject)
                {
                    return Model3D((Rectangle3DObject)sAMGeometryObject);
                }

                return Model3D(new Rectangle3DObject(value.Rectangle3D, UI.Query.DefaultCurveAppearance()));
            }

            //ISphereObject
            if (sAMGeometryObject is ISphereObject)
            {
                ISphereObject value = (ISphereObject)sAMGeometryObject;

                if (value is SphereObject)
                {
                    return Model3D((SphereObject)sAMGeometryObject);
                }

                return Model3D(new SphereObject(value.Sphere, UI.Query.DefaultSurfaceAppearance()));
            }

            //ITriangle3DObject
            if (sAMGeometryObject is ITriangle3DObject)
            {
                ITriangle3DObject value = (ITriangle3DObject)sAMGeometryObject;

                if (value is Triangle3DObject)
                {
                    return Model3D((Triangle3DObject)sAMGeometryObject);
                }

                return Model3D(new Triangle3DObject(value.Triangle3D, UI.Query.DefaultCurveAppearance()));
            }

            //ISAMGeometry3DGroup
            if (sAMGeometryObject is ISAMGeometry3DGroupObject)
            {
                ISAMGeometry3DGroupObject value = (ISAMGeometry3DGroupObject)sAMGeometryObject;

                if (value is SAMGeometry3DGroupObject)
                {
                    return Model3D((SAMGeometry3DGroupObject)sAMGeometryObject);
                }

                return Model3D(new SAMGeometry3DGroupObject(value.SAMGeometry3DGroup, UI.Query.DefaultPointAppearance(), UI.Query.DefaultCurveAppearance(), UI.Query.DefaultSurfaceAppearance()));
            }

            return null;
        }
        
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

        public static Model3D Model3D(this SphereObject sphereObject)
        {
            if (sphereObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = sphereObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Sphere sphere = sphereObject.Sphere;
            if (sphere == null)
            {
                return null;
            }

            List<Face3D> face3Ds = sphere.Mesh3D(1)?.GetTriangles()?.ConvertAll(x => new Face3D(x));
            if(face3Ds == null || face3Ds.Count == 0)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Face3D face3D in face3Ds)
            {
                Model3D model3D = Model3D(new Face3DObject(face3D, surfaceAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, sphereObject);

            return result;
        }

        public static Model3D Model3D(this ExtrusionObject extrusionObject)
        {
            if (extrusionObject == null)
            {
                return null;
            }

            SurfaceAppearance surfaceAppearance = extrusionObject.SurfaceAppearance;

            if (surfaceAppearance == null)
            {
                return null;
            }

            Extrusion extrusion = extrusionObject.Extrusion;
            if (extrusion == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Face3D face3D in extrusion.Face3Ds())
            {
                Model3D model3D = Model3D(new Face3DObject(face3D, surfaceAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, extrusionObject);

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

        public static Model3D Model3D(this BoundingBox3DObject boundingBox3DObject)
        {
            if (boundingBox3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = boundingBox3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            BoundingBox3D boundingBox3D = boundingBox3DObject.BoundingBox3D;
            if (boundingBox3D == null)
            {
                return null;
            }

            Material material = UI.Create.Material(curveAppearance.Color, curveAppearance.Opacity);

            GeometryModel3D result = new GeometryModel3D(boundingBox3D.ToMedia3D(curveAppearance.Thickness), material);
            Core.UI.WPF.Modify.SetIJSAMObject(result, boundingBox3DObject);
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

        public static Model3D Model3D(this Triangle3DObject triangle3DObject)
        {
            if (triangle3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = triangle3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            ISegmentable3D segmentable3D = triangle3DObject.Triangle3D;
            if (segmentable3D == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Segment3D segment3D in segmentable3D.GetSegments())
            {
                Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, triangle3DObject);

            return result;
        }

        public static Model3D Model3D(this Polyline3DObject polyline3DObject)
        {
            if (polyline3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = polyline3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            ISegmentable3D segmentable3D = polyline3DObject.Polyline3D;
            if (segmentable3D == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Segment3D segment3D in segmentable3D.GetSegments())
            {
                Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, polyline3DObject);

            return result;
        }

        public static Model3D Model3D(this Rectangle3DObject rectangle3DObject)
        {
            if (rectangle3DObject == null)
            {
                return null;
            }

            CurveAppearance curveAppearance = rectangle3DObject.CurveAppearance;

            if (curveAppearance == null)
            {
                return null;
            }

            ISegmentable3D segmentable3D = rectangle3DObject.Rectangle3D;
            if (segmentable3D == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Segment3D segment3D in segmentable3D.GetSegments())
            {
                Model3D model3D = Model3D(new Segment3DObject(segment3D, curveAppearance));
                result.Children.Add(model3D);
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, rectangle3DObject);

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
                Model3D model3D = Model3D(sAMGeometry3DObject);
                if(model3D != null)
                {
                    result.Children.Add(model3D);
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, sAMGeometry3DObjectCollection);

            return result;
        }

        public static Model3D Model3D(this SAMGeometryObjectCollection sAMGeometryObjectCollection)
        {
            if (sAMGeometryObjectCollection == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjectCollection)
            {
                Model3D model3D = Model3D(sAMGeometryObject);
                if (model3D != null)
                {
                    result.Children.Add(model3D);
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, sAMGeometryObjectCollection);

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

            SolidColorBrush solidColorBrush = new SolidColorBrush(textAppearance.Color.ToMedia());
            solidColorBrush.Opacity = textAppearance.Opacity;

            GeometryModel3D result = Core.UI.WPF.Create.GeometryModel3D_Text(text3DObject.Text, solidColorBrush, false, textAppearance.Height, plane.Origin.ToMedia3D(), true, plane.AxisX.ToMedia3D(), plane.AxisY.ToMedia3D(), textAppearance.FontFamilyName);

            Core.UI.WPF.Modify.SetIJSAMObject(result, text3DObject);

            return result;
        }

        public static Model3D Model3D(this SAMGeometry3DGroupObject sAMGeometry3DGroupObject)
        {
            if(sAMGeometry3DGroupObject == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (ISAMGeometry3D sAMGeometry3D in sAMGeometry3DGroupObject)
            {
                ISAMGeometryObject sAMGeometryObject = UI.Convert.ToSAM_ISAMGeometryObject(sAMGeometry3D, sAMGeometry3DGroupObject.PointAppearance, sAMGeometry3DGroupObject.CurveAppearance, sAMGeometry3DGroupObject.SurfaceAppearance);
                if(sAMGeometryObject == null)
                {
                    continue;
                }

                Model3D model3D = Model3D(sAMGeometryObject);
                if(model3D == null)
                {
                    continue;
                }

                result.Children.Add(model3D);
            }
            
            Core.UI.WPF.Modify.SetIJSAMObject(result, sAMGeometry3DGroupObject);
            
            return result;
        }
    }
}
