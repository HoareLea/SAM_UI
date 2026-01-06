// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Create
    {
        public static ModelVisual3D ModelVisual3D_Text(string text, Brush textColor, bool isDoubleSided, double height, Point3D basePoint, bool isBasePointCenterPoint, Vector3D vectorOver, Vector3D vectorUp, string fontFamilyName = "Arial")
        {
            ModelVisual3D result = new ModelVisual3D();
            result.Content = GeometryModel3D_Text(text, textColor, isDoubleSided, height, basePoint, isBasePointCenterPoint, vectorOver, vectorUp, fontFamilyName);
            return result;
        }
    }


}
