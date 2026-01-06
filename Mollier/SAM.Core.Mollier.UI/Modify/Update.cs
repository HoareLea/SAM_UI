// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Mollier.UI.Forms;
using System.Windows.Forms;
using System;
using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static bool Update(this IUIMollierObject mollierObject)
        {
            if (mollierObject is UIMollierPoint)
            {
                UIMollierPoint uIMollierPoint = (UIMollierPoint)mollierObject;
                using (UIMollierPointForm customizePointForm = new UIMollierPointForm(uIMollierPoint))
                {
                    if (customizePointForm.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }
                    uIMollierPoint = customizePointForm.UIMollierPoint;
                }
            }
            else if(mollierObject is UIMollierProcess)
            {
                UIMollierProcess uIMollierProcess = (UIMollierProcess)mollierObject;
                using (UIMollierProcessForm_Limited customizeProcessForm = new UIMollierProcessForm_Limited(uIMollierProcess))
                {
                    if (customizeProcessForm.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }
                    uIMollierProcess = customizeProcessForm.UIMollierProcess;
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return true;
        }
    }
}
