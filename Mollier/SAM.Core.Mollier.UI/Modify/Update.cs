using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using SAM.Core.Mollier.UI.Forms;
using System.Windows.Forms;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static bool Update(this IUIMollierObject mollierObject)
        {
            if (mollierObject is UIMollierPoint)
            {
                UIMollierPoint uIMollierPoint = (UIMollierPoint)mollierObject;
                using (CustomizePointForm customizePointForm = new CustomizePointForm(uIMollierPoint))
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
                using (CustomizeProcessForm customizeProcessForm = new CustomizeProcessForm(uIMollierProcess))
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
