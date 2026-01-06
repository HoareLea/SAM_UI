// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {

        public static void Highlight(this Visual3D visual3D, bool highlight)
        {
            if (visual3D == null)
            {
                return;
            }

            if (!highlight)
            {
                Restore(visual3D);
                return;
            }

            if (visual3D is ModelVisual3D)
            {
                ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                Highlight(modelVisual3D.Content, highlight);

                Segment3DObject segment3DObject = Core.UI.WPF.Query.JSAMObject<Segment3DObject>(modelVisual3D.Content);
                if (segment3DObject != null)
                {
                    Model3D model3D_Temp = Create.Model3D(new Segment3DObject(segment3DObject) { CurveAppearance = Query.HighlightCurveAppearance(segment3DObject.CurveAppearance) });
                    Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, segment3DObject);

                    modelVisual3D.Content = model3D_Temp;
                }
            }
        }
        public static void Highlight(this Model3D model3D, bool highlight)
        {
            if (model3D == null)
            {
                return;
            }

            if (!highlight)
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
                            Highlight(model3DCollection[i], highlight);
                        }
                    }
                }
            }
        }
    }
}
