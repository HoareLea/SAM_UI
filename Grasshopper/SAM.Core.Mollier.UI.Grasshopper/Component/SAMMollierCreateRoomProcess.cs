using Grasshopper.Kernel;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using SAM.Core.Grasshopper;
using SAM.Core.Grasshopper.Mollier;
using System.Windows.Forms;
using Grasshopper.Kernel.Data;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateRoomProcess : GH_SAMVariableOutputParameterComponent
    {
        private MollierForm mollierForm = null;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("08c03d2c-7e26-4094-a154-2b81255c7462");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.SAM_Mollier;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "_start", NickName = "_start", Description = "MollierPoint for Start", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_airFlow", NickName = "_airflow", Description = "AirFlow [m3/s]", Access = GH_ParamAccess.item, Optional = false };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_sensibleLoad", NickName = "_sensibleLoad", Description = "Sensible Load [kW]", Access = GH_ParamAccess.item, Optional = false };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_latentLoad", NickName = "_latentLoad", Description = "Latent Load [kW]", Access = GH_ParamAccess.item, Optional = false };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "roomProcess", NickName = "roomProcess", Description = "Room Process", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "end", NickName = "end", Description = "End", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Updates PanelTypes for AdjacencyCluster
        /// </summary>
        public SAMMollierCreateRoomProcess()
          : base("SAMMollier.CreateRoomProcess", "SAMMollier.CreateRoomProcess",
              "Creates Room Process",
              "SAM", "Mollier")
        {
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            index = Params.IndexOfInputParam("_start");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            MollierPoint start = null;
            if (!dataAccess.GetData(index, ref start) || start == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_airFlow");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            double airFlow = double.NaN;
            if (!dataAccess.GetData(index, ref airFlow) || double.IsNaN(airFlow))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_sensibleLoad");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            double sensibleLoad = double.NaN;
            if (!dataAccess.GetData(index, ref sensibleLoad) || double.IsNaN(sensibleLoad))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_latentLoad");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            double latentLoad = double.NaN;
            if (!dataAccess.GetData(index, ref latentLoad) || double.IsNaN(latentLoad))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            UndefinedProcess undefinedProcess = Core.Mollier.Create.UndefinedProcess(start, airFlow, sensibleLoad * 1000, latentLoad * 1000);
            index = Params.IndexOfOutputParam("roomProcess");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierProcess(undefinedProcess, Color.Empty, null, null, null));
            }
            else
            {
                return;
            }

            MollierPoint end = new MollierPoint(undefinedProcess.End);
            index = Params.IndexOfOutputParam("end");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierPoint(end));
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Open Mollier Diagram", Menu_OpenMollierDiagram);

            //Menu_AppendSeparator(menu);

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_OpenMollierDiagram(object sender, EventArgs e)
        {
            if(Params?.Output == null || Params.Output.Count == 0)
            {
                return;
            }

            List<IMollierProcess> mollierProcesses = new List<IMollierProcess>();
            foreach(IGH_Param gH_Param in Params.Output)
            {
                GooMollierProcessParam gooMollierProcessParam = gH_Param as GooMollierProcessParam;
                if(gooMollierProcessParam == null)
                {
                    continue;
                }

                IGH_Structure gH_Structure = gooMollierProcessParam.VolatileData;
                foreach(object @object in gH_Structure.AllData(true))
                {
                    IMollierProcess mollierProcess_Temp = (@object as GooMollierProcess)?.Value;
                    if(mollierProcess_Temp != null)
                    {
                        mollierProcesses.Add(mollierProcess_Temp);
                    }
                }
            }

            if(mollierForm == null)
            {
                mollierForm = new MollierForm();
            }

            mollierForm.Clear();


            if(mollierProcesses != null && mollierProcesses.Count != 0)
            {
                mollierForm.AddProcesses(mollierProcesses, false);
            }

            mollierForm.Show();
        }
    }
}