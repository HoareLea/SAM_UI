using Grasshopper.Kernel;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public abstract class MollierDiagramComponent : GH_SAMVariableOutputParameterComponent
    {
        private MollierForm mollierForm = null;

        public MollierDiagramComponent(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
        }

        protected abstract IEnumerable<IGH_Param> GetMollierDiagramParameters();

        protected virtual MollierControlSettings GetMollierControlSettings()
        {
            return new MollierControlSettings();
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Open Mollier Diagram", Menu_OpenMollierDiagram);

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_OpenMollierDiagram(object sender, EventArgs e)
        {
            IEnumerable<IGH_Param> gH_Params = GetMollierDiagramParameters();
            if (gH_Params == null || gH_Params.Count() == 0)
            {
                return;
            }

            if (mollierForm == null)
            {
                mollierForm = new MollierForm();
                mollierForm.MollierControlSettings = GetMollierControlSettings();
            }

            mollierForm.Clear();


            List<IMollierProcess> mollierProcesses = Query.MollierProcesses<IMollierProcess>(gH_Params);
            if (mollierProcesses != null && mollierProcesses.Count != 0)
            {
                mollierForm.AddProcesses(mollierProcesses, false);
            }

            List<MollierPoint> mollierPoints = Query.MollierPoints(gH_Params);
            if (mollierPoints != null && mollierPoints.Count != 0)
            {
                mollierForm.AddPoints(mollierPoints, false);
            }

            mollierForm.Show();
        }
    }
}
