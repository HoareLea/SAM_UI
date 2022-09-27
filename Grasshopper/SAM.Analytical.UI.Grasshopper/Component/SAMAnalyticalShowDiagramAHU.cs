using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper;
using SAM.Analytical.Mollier;
using SAM.Analytical.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.Grasshopper
{
    public class SAMAnalyticalShowDiagramAHU : GH_SAMVariableOutputParameterComponent
    {
        private Core.Mollier.UI.MollierForm mollierForm;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c4b9b0b6-d11f-4f44-a602-61649979e928");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalShowDiagramAHU()
          : base("SAMAnalytical.ShowDiagramAHU", "SAMAnalytical.ShowDiagramAHU",
              "Show AHU Diagram",
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                GooAnalyticalModelParam gooPanelParam = new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(gooPanelParam, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_GenericObject @genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_airHandlingUnit", NickName = "_airHandlingUnit", Description = "SAM Analytical AirHandlingUnit or name", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(@genericObject, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_recalculate", NickName = "_recalculate", Description = "Recalculate Air Handling Unit.", Access = GH_ParamAccess.item, Optional = true };
                @boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;


            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_airHandlingUnit");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            GH_ObjectWrapper objectWrapper = null;
            if (!dataAccess.GetData(index, ref objectWrapper) || objectWrapper == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            object @object = objectWrapper.Value;
            if (@object is IGH_Goo)
            {
                @object = (@object as dynamic).Value;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            AirHandlingUnit airHandlingUnit = null;
            if (@object is string)
            {
                airHandlingUnit = adjacencyCluster?.GetObjects((AirHandlingUnit x) => x.Name == (string)@object).FirstOrDefault();
            }
            else if (@object is AirHandlingUnit)
            {
                airHandlingUnit = adjacencyCluster?.GetObject<AirHandlingUnit>(((AirHandlingUnit)@object).Guid);
            }

            if (airHandlingUnit == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find or create Air Handling Unit");
                return;
            }

            index = Params.IndexOfInputParam("_recalculate");
            bool recalculate = true;
            if (index != -1)
            {
                dataAccess.GetData(index, ref recalculate);
                return;
            }

            MollierGroup mollierGroup = null;

            AirHandlingUnitResult airHandlingUnitResult = null;
            if (recalculate)
            {
                airHandlingUnitResult = Mollier.Create.AirHandlingUnitResult(analyticalModel, airHandlingUnit.Name);
                if(airHandlingUnitResult != null)
                {
                    adjacencyCluster.AddObject(airHandlingUnitResult);
                    adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitResult);
                }
            }

            airHandlingUnitResult = adjacencyCluster?.GetObjects<AirHandlingUnitResult>()?.Find(x => x.Name == airHandlingUnit.Name);
            if(airHandlingUnitResult != null)
            {
                mollierGroup = airHandlingUnitResult.GetValue<MollierGroup>(AirHandlingUnitResultParameter.Processes);
            }


            if (mollierForm == null  || mollierForm.IsDisposed)
            {
                mollierForm = new Core.Mollier.UI.MollierForm() { ReadOnly = true, WindowState = System.Windows.Forms.FormWindowState.Normal };
            }
            else
            {
                mollierForm.Clear();
            }

            List<IMollierProcess> mollierProcesses = mollierGroup?.GetMollierProcesses();

            double pressure = Core.Mollier.UI.Query.DefaultPressure(null, mollierProcesses);

            mollierForm.Name = string.IsNullOrWhiteSpace(airHandlingUnit.Name) ? mollierForm.Name : airHandlingUnit.Name;
            mollierForm.MollierControlSettings = Query.DefaultMollierControlSettings();
            mollierForm.Pressure = pressure;
            mollierForm.AddProcesses(mollierProcesses, false);
            //mollierForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            //mollierProcesses?.ForEach(x => mollierForm.AddProcess(x, false));
            mollierForm.Show();
        }
    }
}