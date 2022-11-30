using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {
        public static void Highlight(this Model3D model3D, bool select)
        {
            if (model3D == null)
            {
                return;
            }

            if (!select)
            {
                Restore(model3D);
                return;
            }

            if (model3D is Model3DGroup)
            {
                Model3DCollection model3DCollection = ((Model3DGroup)model3D).Children;
                if (model3DCollection != null)
                {
                    for (int i = 0; i < model3DCollection.Count; i++)
                    {
                        Segment3DObject segment3DObject = Core.UI.WPF.Query.JSAMObject<Segment3DObject>(model3DCollection[i]);
                        if (segment3DObject != null)
                        {
                            Model3D model3D_Temp = Create.Model3D(new Segment3DObject(segment3DObject) { CurveAppearance = Query.HighlightCurveAppearance(segment3DObject.CurveAppearance) });
                            Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, segment3DObject);

                            model3DCollection[i] = model3D_Temp;
                        }
                        else
                        {
                            Highlight(model3DCollection[i], select);
                        }
                    }
                }
            }
        }
    }
}