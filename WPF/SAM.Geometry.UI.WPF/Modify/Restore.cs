using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static void Restore(this Model3D model3D)
        {
            if(model3D == null)
            {
                return;
            }

            if(model3D is Model3DGroup)
            {
                Model3DCollection model3DCollection = ((Model3DGroup)model3D).Children;
                if(model3DCollection != null)
                {
                    for(int i = 0; i < model3DCollection.Count; i++)
                    {
                        ISAMGeometry3DObject sAMGeometry3DObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometry3DObject>(model3DCollection[i]);
                        if(sAMGeometry3DObject != null)
                        {
                            Model3D model3D_Temp = Create.Model3D(sAMGeometry3DObject as dynamic);
                            if(model3D_Temp != null)
                            {
                                model3DCollection[i] = model3D_Temp;
                            }
                        }
                        else
                        {
                            Restore(model3DCollection[i]);
                        }
                    }
                }
            }
        }

        public static void Restore(this Visual3D visual3D)
        {
            if (visual3D == null)
            {
                return;
            }

            ISAMGeometryObject sAMGeometryObject = null;

            if (visual3D is ModelVisual3D)
            {
                ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;

                Model3D model3D = modelVisual3D.Content;
                if (model3D == null)
                {
                    return;
                }

                if (model3D is Model3DGroup)
                {
                    Model3DGroup model3DGroup = (Model3DGroup)model3D;

                    for (int i = 0; i < model3DGroup.Children.Count; i++)
                    {
                        sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3DGroup.Children[i]);
                        if (sAMGeometryObject != null)
                        {
                            Model3D model3D_Temp = Create.Model3D(sAMGeometryObject as dynamic);
                            if (model3D_Temp != null)
                            {
                                model3DGroup.Children[i] = model3D_Temp;
                            }
                        }
                    }
                }

                sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3D);
                if (sAMGeometryObject != null)
                {
                    Model3D model3D_Temp = Create.Model3D(sAMGeometryObject as dynamic);
                    if (model3D_Temp != null)
                    {
                        modelVisual3D.Content = model3D_Temp;
                    }
                }
            }
        }
    }
}