// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Rect3D Bounds(this ModelVisual3D modelVisual3D)
        {
            Rect3D result = Rect3D.Empty;

            if (modelVisual3D == null)
            {
                return result;
            }

            Transform3D transform3D = modelVisual3D.Transform;

            foreach (Visual3D visual3D in modelVisual3D.Children)
            {

                Rect3D rect3D_VisualFace3DObject = Bounds(visual3D, transform3D);

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

        public static Rect3D Bounds(this Visual3D visual3D, Transform3D transform3D = null)
        {
            if(visual3D == null)
            {
                return Rect3D.Empty;
            }

            ModelVisual3D modelVisual3D = visual3D as ModelVisual3D;
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

                if (visual3D is ModelVisual3D)
                {
                    transform3D_Temp = (visual3D as ModelVisual3D).Transform;
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
                foreach (Visual3D visual3D_Temp in visual3DCollection)
                {
                    Rect3D rect3D_Temp = Bounds(visual3D_Temp, transform3D);
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

        public static Rect3D Bounds(this IEnumerable<Visual3D> visual3Ds)
        {
            Rect3D rect3D = Rect3D.Empty;
            foreach (Visual3D visual3D in visual3Ds)
            {
                Rect3D rect3D_VisualAnalyticalModel = Bounds(visual3D);

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
