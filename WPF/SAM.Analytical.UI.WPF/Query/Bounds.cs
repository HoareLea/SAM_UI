using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Rect3D Bounds(this VisualAnalyticalModel visualAnalyticalModel)
        {
            Rect3D result = Rect3D.Empty;

            if (visualAnalyticalModel == null)
            {
                return result;
            }

            Transform3D transform_VisualAnalyticalModel = visualAnalyticalModel.Transform;

            foreach (IVisualJSAMObject visualJSAMObject in visualAnalyticalModel.Children)
            {
                Rect3D rect3D_VisualFace3DObject = visualJSAMObject.GeometryModel3D.Bounds;

                Transform3D transform = (visualJSAMObject as ModelVisual3D).Transform;
                transform = HelixToolkit.Wpf.Transform3DHelper.CombineTransform(transform, transform_VisualAnalyticalModel);
                rect3D_VisualFace3DObject = transform.TransformBounds(rect3D_VisualFace3DObject);

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

        public static Rect3D Bounds(this IEnumerable<VisualAnalyticalModel> visualAnalyticalModels)
        {
            Rect3D rect3D = Rect3D.Empty;
            foreach (VisualAnalyticalModel visualAnalyticalModel in visualAnalyticalModels)
            {
                Rect3D rect3D_VisualAnalyticalModel = Bounds(visualAnalyticalModel);

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
