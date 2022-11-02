using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Rect3D Bounds(this VisualGeometryObjectModel visualGeometryObjectModel)
        {
            Rect3D result = Rect3D.Empty;

            if (visualGeometryObjectModel == null)
            {
                return result;
            }

            Transform3D transform3D_VisualAnalyticalModel = visualGeometryObjectModel.Transform;

            foreach (IVisualJSAMObject visualJSAMObject in visualGeometryObjectModel.Children)
            {
                Rect3D rect3D_VisualFace3DObject = Bounds(visualJSAMObject, transform3D_VisualAnalyticalModel);

                //Rect3D rect3D_VisualFace3DObject = visualJSAMObject.GeometryModel3D.Bounds;

                //Transform3D transform = (visualJSAMObject as ModelVisual3D).Transform;
                //transform = HelixToolkit.Wpf.Transform3DHelper.CombineTransform(transform, transform3D_VisualAnalyticalModel);
                //rect3D_VisualFace3DObject = transform.TransformBounds(rect3D_VisualFace3DObject);

                if (result == Rect3D.Empty)
                {
                    result = rect3D_VisualFace3DObject;
                }
                else
                {
                    result.Union(rect3D_VisualFace3DObject);
                }
            }

            return result;
        }

        public static Rect3D Bounds(this IVisualJSAMObject visualJSAMObject, Transform3D transform3D = null)
        {
            if(visualJSAMObject == null)
            {
                return Rect3D.Empty;
            }

            ModelVisual3D modelVisual3D = visualJSAMObject as ModelVisual3D;
            if(modelVisual3D == null)
            {
                return Rect3D.Empty;
            }

            Rect3D result = Rect3D.Empty;

            Model3D model3D = modelVisual3D.Content;
            if (model3D != null)
            {
                result = model3D.Bounds;

                Transform3D transform3D_Temp = Transform3D.Identity;

                if (visualJSAMObject is ModelVisual3D)
                {
                    transform3D_Temp = (visualJSAMObject as ModelVisual3D).Transform;
                }

                if(transform3D != null)
                {
                    transform3D_Temp = HelixToolkit.Wpf.Transform3DHelper.CombineTransform(transform3D_Temp, transform3D);
                }

                result = transform3D_Temp.TransformBounds(result);
            }

            Visual3DCollection visual3DCollection = modelVisual3D.Children;
            if (visual3DCollection != null && visual3DCollection.Count != 0)
            {
                foreach (Visual3D visual3D in visual3DCollection)
                {
                    IVisualJSAMObject visualJSAMObject_Temp = visual3D as IVisualJSAMObject;
                    if (visualJSAMObject_Temp == null)
                    {
                        continue;
                    }

                    Rect3D rect3D_Temp = Bounds(visualJSAMObject_Temp, transform3D);
                    if (rect3D_Temp != Rect3D.Empty)
                    {
                        if (result == Rect3D.Empty)
                        {
                            result = rect3D_Temp;
                        }
                        else
                        {
                            result.Union(rect3D_Temp);
                        }
                    }
                }
            }

            return result;
        }

        public static Rect3D Bounds(this IEnumerable<VisualGeometryObjectModel> visualGeometryObjectModels)
        {
            Rect3D rect3D = Rect3D.Empty;
            foreach (VisualGeometryObjectModel visualGeometryObjectModel in visualGeometryObjectModels)
            {
                Rect3D rect3D_VisualAnalyticalModel = Bounds(visualGeometryObjectModel);

                if (rect3D_VisualAnalyticalModel == Rect3D.Empty)
                {
                    rect3D = rect3D_VisualAnalyticalModel;
                }
                else
                {
                    rect3D.Union(rect3D_VisualAnalyticalModel);
                }
            }

            return rect3D;
        }
    }
}
