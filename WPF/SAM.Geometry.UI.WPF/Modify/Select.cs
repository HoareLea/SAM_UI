using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static void Select(this Model3D model3D, bool select)
        {
            if(model3D == null)
            {
                return;
            }

            if(!select)
            {
                Restore(model3D);
                return;
            }

            if(model3D is Model3DGroup)
            {
                Model3DCollection model3DCollection = ((Model3DGroup)model3D).Children;
                if(model3DCollection != null)
                {
                    for(int i = 0; i < model3DCollection.Count; i++)
                    {
                        Face3DObject face3DObject = Core.UI.WPF.Query.JSAMObject<Face3DObject>(model3DCollection[i]);
                        if(face3DObject != null)
                        {
                            Model3D model3D_Temp = Create.Model3D(new Face3DObject(face3DObject) { SurfaceAppearance = Query.SelectionSurfaceAppearance() });
                            Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, face3DObject);

                            model3DCollection[i] = model3D_Temp;
                        }
                        else
                        {
                            Select(model3DCollection[i], select);
                        }
                    }
                }
            }
        }
    }
}