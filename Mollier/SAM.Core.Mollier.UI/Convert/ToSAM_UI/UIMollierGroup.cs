// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using System.Drawing;
namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static UIMollierGroup ToSAM_UI(this MollierGroup mollierGroup, bool includeNestedObjects = true, MollierControlSettings mollierControlSettings = null)
        {
            UIMollierGroup result = new UIMollierGroup(new MollierGroup(mollierGroup.Name));
            for(int i = mollierGroup.Count - 1; i >= 0; i--)
            {
                IMollierGroupable mollierGroupable = mollierGroup[i];
                if(mollierGroupable == null)
                {
                    continue;
                }
                if(!(mollierGroupable is IUIMollierObject))
                {
                    if(mollierGroupable is MollierPoint)
                    {
                        MollierPoint mollierPoint = (MollierPoint)mollierGroupable;
                        Color color = mollierPoint.Color();
                        result.Add(new UIMollierPoint(mollierPoint, color));
                    }
                    else if(mollierGroupable is MollierProcess)
                    {
                        IMollierGroupable mollierGroupable_Temp = ToSAM_UI(mollierGroupable) as IMollierGroupable;
                        result.Add(mollierGroupable_Temp);
                    }
                    else if(mollierGroupable is MollierZone)
                    {
                        MollierZone mollierZone = (MollierZone)mollierGroupable;
                        Color color = Color.Blue;
                        result.Add(new UIMollierZone(mollierZone, color));
                    }
                    else if(mollierGroupable is MollierGroup && includeNestedObjects)
                    {
                        result.Add(((MollierGroup)mollierGroupable).ToSAM_UI(includeNestedObjects));
                    }
                }
                else
                {
                    result.Add(mollierGroupable);
                }
            }
            return result;
        }

    }
}

