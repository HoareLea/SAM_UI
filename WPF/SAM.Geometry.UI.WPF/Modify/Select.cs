using SAM.Geometry.Object;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static void Select(this Visual3D visual3D,  bool select)
        {
            if (visual3D == null)
            {
                return;
            }

            if(visual3D is ModelVisual3D)
            {
                ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                if (!select)
                {
                    Restore(modelVisual3D);
                    return;
                }

                Model3D model3D = modelVisual3D.Content;
                if(model3D == null)
                {
                    return;
                }

                ISAMGeometryObject sAMGeometryObject = null;

                if (model3D is Model3DGroup)
                {
                    Model3DGroup model3DGroup = (Model3DGroup)model3D;

                    for(int i =0; i < model3DGroup.Children.Count; i++)
                    {
                        sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3DGroup.Children[i]);
                        if (sAMGeometryObject != null)
                        {
                            if (sAMGeometryObject is Face3DObject)
                            {
                                Model3D model3D_Temp = Create.Model3D(new Face3DObject((Face3DObject)sAMGeometryObject) { SurfaceAppearance = Query.SelectionSurfaceAppearance() });
                                Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                                model3DGroup.Children[i] = model3D_Temp;
                            }
                            else if (sAMGeometryObject is Segment3DObject)
                            {
                                Model3D model3D_Temp = Create.Model3D(new Segment3DObject((Segment3DObject)sAMGeometryObject) { CurveAppearance = Query.SelectionSurfaceAppearance().CurveAppearance });
                                Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                                model3DGroup.Children[i] = model3D_Temp;
                            }
                        }
                    }
                }

                sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3D);
                if(sAMGeometryObject  != null)
                {
                    if (sAMGeometryObject is Face3DObject)
                    {
                        Model3D model3D_Temp = Create.Model3D(new Face3DObject((Face3DObject)sAMGeometryObject) { SurfaceAppearance = Query.SelectionSurfaceAppearance() });
                        Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                        modelVisual3D.Content = model3D_Temp;
                    }
                    else if (sAMGeometryObject is Segment3DObject)
                    {
                        Model3D model3D_Temp = Create.Model3D(new Segment3DObject((Segment3DObject)sAMGeometryObject) { CurveAppearance = Query.SelectionSurfaceAppearance().CurveAppearance });
                        Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                        modelVisual3D.Content = model3D_Temp;
                    }
                }
            }
        }
    }
}