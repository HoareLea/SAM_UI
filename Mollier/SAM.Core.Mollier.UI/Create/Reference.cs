// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static IReference Reference(this UIMollierPoint uIMollierPoint)
        {
            if (uIMollierPoint == null)
            {
                return null;
            }
            
            if(uIMollierPoint is UIMollierProcessPoint)
            {
                return ((UIMollierProcessPoint)uIMollierPoint).Reference;
            }


            return Geometry.Mollier.Create.Reference(uIMollierPoint);
        }
    }
}
