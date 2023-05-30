using HelixToolkit.Wpf;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static ModelVisual3D ToMedia3D(this GeometryObjectModel geometryObjectModel)
        {
            if (geometryObjectModel == null)
            {
                return null;
            }

            ModelVisual3D result = new ModelVisual3D();
            List<ISAMGeometryObject> sAMGeometryObjects = geometryObjectModel?.GetSAMGeometryObjects<ISAMGeometryObject>();
            if (sAMGeometryObjects != null)
            {
                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    if (sAMGeometryObject == null)
                    {
                        continue;
                    }

                    ModelVisual3D modelVisual3D = ToMedia3D(sAMGeometryObject);
                    if(modelVisual3D == null)
                    {
                        continue;
                    }

                    result.Children.Add(modelVisual3D);
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, geometryObjectModel);

            return result;
        }

        public static ModelVisual3D ToMedia3D(this ISegment3DObject segment3DObject)
        {
            if (segment3DObject is Segment3DObject)
            {
                return ToMedia3D((Segment3DObject)segment3DObject);
            }

            return ToMedia3D(segment3DObject.Segment3D, UI.Query.DefaultCurveAppearance().Thickness);
        }

        public static ModelVisual3D ToMedia3D(this ISAMGeometryObject sAMGeometryObject)
        {
            if(sAMGeometryObject == null)
            {
                return null;
            }

            ModelVisual3D result = new ModelVisual3D();
            List<Model3D> model3Ds = new List<Model3D>();
            if (sAMGeometryObject is GeometryObjectCollection)
            {
                foreach(ISAMGeometry3DObject sAMGeometry3DObject in (GeometryObjectCollection)sAMGeometryObject)
                {
                    if(sAMGeometry3DObject is GeometryObjectCollection)
                    {
                        ModelVisual3D modelVisual3D = ToMedia3D(sAMGeometry3DObject);
                        if (modelVisual3D != null)
                        {
                            result.Children.Add(modelVisual3D);
                        }
                    }
                    else if (sAMGeometry3DObject is Face3DObject)
                    {
                        ISegmentable3D segmentable3D = null;
                        LinesVisual3D linesVisual3D = null;

                        segmentable3D = ((Face3DObject)sAMGeometry3DObject).GetExternalEdge3D() as ISegmentable3D;
                        linesVisual3D = segmentable3D.ToMedia3D(1);
                        result.Children.Add(linesVisual3D);

                        List<IClosedPlanar3D> closedPlanar3Ds = ((Face3DObject)sAMGeometry3DObject).GetInternalEdge3Ds();
                        if (closedPlanar3Ds != null)
                        {
                            foreach (IClosedPlanar3D closedPlanar3D in closedPlanar3Ds)
                            {
                                linesVisual3D = (closedPlanar3D as ISegmentable3D)?.ToMedia3D(1);
                                result.Children.Add(linesVisual3D);
                            }
                        }

                        SurfaceAppearance surfaceAppearance = ((Face3DObject)sAMGeometry3DObject).SurfaceAppearance;

                        result.Content = new GeometryModel3D(((Face3DObject)sAMGeometry3DObject).Face3D.ToMedia3D(Query.DoubleSided()), UI.Create.Material(surfaceAppearance.Color, surfaceAppearance.Opacity));

                    }
                    else
                    {
                        Model3D model3D = Create.Model3D(sAMGeometry3DObject);
                        if (model3D != null)
                        {
                            model3Ds.Add(model3D);
                        }
                    }
                }
            }
            else
            {
                Model3D model3D = Create.Model3D(sAMGeometryObject);
                if (model3D != null)
                {
                    model3Ds.Add(model3D);
                    //result.Content = model3D;
                }
            }

            if(model3Ds != null && model3Ds.Count != 0)
            {
                if(model3Ds.Count == 1)
                {
                    result.Content = model3Ds[0];
                }
                else
                {
                    Model3DGroup model3DGroup = new Model3DGroup();
                    model3Ds.ForEach(x => model3DGroup.Children.Add(x));
                    result.Content = model3DGroup;
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, sAMGeometryObject);

            return result;
        }
    }
}
