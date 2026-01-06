// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using System;
using System.Drawing;
namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static IUIMollierObject ToSAM_UI(this IMollierObject mollierObject)
        {
            if(mollierObject == null)
            {
                return null;
            }

            if(mollierObject is IMollierPoint)
            {
                Color color = Geometry.Mollier.Query.Color((IMollierPoint)mollierObject);
                if (mollierObject is MollierPoint)
                {
                    return new UIMollierPoint((MollierPoint)mollierObject, color);
                }
                else if(mollierObject is UIMollierPoint)
                {
                    return (UIMollierPoint)mollierObject;
                }
                return null;
            }
            else if(mollierObject is IMollierProcess)
            {
                //Color color = Mollier.Query.Color((IMollierProcess)mollierObject);
                if (mollierObject is MollierProcess)
                {
                    UIMollierProcess result = new UIMollierProcess((MollierProcess)mollierObject, Color.Empty);
                    result.UIMollierPointAppearance_Start = Create.UIMollierPointAppearance(DisplayPointType.Process, null);
                    result.UIMollierPointAppearance_End = Create.UIMollierPointAppearance(DisplayPointType.Process, null);

                    return result;
                }
                else if (mollierObject is UIMollierProcess)
                {
                    return (UIMollierProcess)mollierObject;
                }
                return null;
            }
            else if(mollierObject is IMollierGroup)
            {
                if (mollierObject is MollierGroup)
                {
                    return new UIMollierGroup((MollierGroup)mollierObject, Color.Empty);
                }
                else if (mollierObject is UIMollierGroup)
                {
                    return (UIMollierGroup)mollierObject;
                }
                return null;
            }
            else if(mollierObject is IMollierZone)
            {
                throw new NotImplementedException();
            }

            return null;
        }

    }
}

