// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Modify
    {

        public static void Opacity(this Visual3D visual3D, double opacity)
        {
            if (visual3D == null)
            {
                return;
            }

            if (visual3D is ModelVisual3D)
            {
                ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;

                Model3D model3D = modelVisual3D.Content;
                if (model3D == null)
                {
                    return;
                }

                ISAMGeometryObject sAMGeometryObject = null;

                if (model3D is Model3DGroup)
                {
                    Model3DGroup model3DGroup = (Model3DGroup)model3D;

                    for (int i = 0; i < model3DGroup.Children.Count; i++)
                    {
                        sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3DGroup.Children[i]);
                        if (sAMGeometryObject != null)
                        {
                            if (sAMGeometryObject is Face3DObject)
                            {
                                SurfaceAppearance surfaceAppearance = ((Face3DObject)sAMGeometryObject).SurfaceAppearance;
                                surfaceAppearance.Opacity = opacity;

                                Model3D model3D_Temp = Create.Model3D(new Face3DObject((Face3DObject)sAMGeometryObject) { SurfaceAppearance = surfaceAppearance });
                                Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                                model3DGroup.Children[i] = model3D_Temp;
                            }
                        }
                    }
                }

                sAMGeometryObject = Core.UI.WPF.Query.JSAMObject<ISAMGeometryObject>(model3D);
                if (sAMGeometryObject != null)
                {
                    if (sAMGeometryObject is Face3DObject)
                    {
                        SurfaceAppearance surfaceAppearance = ((Face3DObject)sAMGeometryObject).SurfaceAppearance;
                        surfaceAppearance.Opacity = opacity;

                        Model3D model3D_Temp = Create.Model3D(new Face3DObject((Face3DObject)sAMGeometryObject) { SurfaceAppearance = surfaceAppearance });
                        Core.UI.WPF.Modify.SetIJSAMObject(model3D_Temp, sAMGeometryObject);

                        modelVisual3D.Content = model3D_Temp;
                    }
                }
            }
        }
    }
}
