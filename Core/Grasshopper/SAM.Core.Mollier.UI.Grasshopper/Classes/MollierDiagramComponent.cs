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
            Menu_AppendItem(menu, "Open Diagram (Mollier-Psychrometric)", Menu_OpenMollierDiagram);

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
                mollierForm.FormClosing += MollierForm_FormClosing;
                //mollierForm.MollierControlSettings = GetMollierControlSettings();
            }

            mollierForm.Clear();

            mollierForm.Shown += MollierForm_Shown;

            mollierForm.Show();
        }

        private void MollierForm_Shown(object sender, EventArgs e)
        {
            mollierForm.Shown -= MollierForm_Shown;

            IEnumerable<IGH_Param> gH_Params = GetMollierDiagramParameters();
            if (gH_Params == null || gH_Params.Count() == 0)
            {
                return;
            }

            MollierControlSettings mollierControlSettings = mollierForm.MollierControlSettings;

            ChartType? chartType = GetChartType();
            if (chartType != null && chartType.HasValue)
            {

                mollierControlSettings.ChartType = chartType.Value;

            }

            mollierForm.MollierControlSettings = mollierControlSettings;

            List<IMollierObject> mollierObjects = Query.MollierObjects(gH_Params);
            if (mollierObjects != null && mollierObjects.Count != 0)
            {
                HashSet<double> pressures = mollierObjects.Pressures();
                if (pressures != null && pressures.Count != 0)
                {
                    mollierForm.Pressure = pressures.First();
                }

                mollierForm.AddMollierObjects(mollierObjects);
            }
        }

        private void MollierForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mollierForm = null;
        }

        protected virtual ChartType? GetChartType()
        {
            return null;
        }
    }
}
