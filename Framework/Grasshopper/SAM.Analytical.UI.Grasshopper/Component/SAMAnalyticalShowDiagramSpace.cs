using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper;
using SAM.Analytical.Mollier;
using SAM.Analytical.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.Grasshopper
{
    public class SAMAnalyticalShowDiagramSpace : GH_SAMVariableOutputParameterComponent
    {
        private Core.Mollier.UI.MollierForm mollierForm;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("77e2ab94-5f34-466d-99eb-2095247fbe5b");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalShowDiagramSpace()
          : base("SAMAnalytical.ShowDiagramSpace", "SAMAnalytical.ShowDiagramSpace",
              "Show Space Diagram",
              "SAM", "UI")
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

                GooSpaceParam gooSpaceParam = new GooSpaceParam() { Name = "_space", NickName = "_space", Description = "SAM Analytical Space", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(gooSpaceParam, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = null;

                @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_calculationMethod_", NickName = "_calculationMethod_", Description = "Air Handling Unit Calculation Method", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(AirHandlingUnitCalculationMethod.FixedSupplyTemperature);
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

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

            Space space = null;
            index = Params.IndexOfInputParam("_space");
            if (index == -1 || !dataAccess.GetData(index, ref space) || space == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AirHandlingUnitCalculationMethod airHandlingUnitCalculationMethod = AirHandlingUnitCalculationMethod.FixedSupplyTemperature;
            index = Params.IndexOfInputParam("_calculationMethod_");
            string airHandlingUnitCalculationMethodString = null;
            dataAccess.GetData(index, ref airHandlingUnitCalculationMethodString);
            if (index != -1 && !string.IsNullOrWhiteSpace(airHandlingUnitCalculationMethodString))
            {
                if (!Enum.TryParse(airHandlingUnitCalculationMethodString, out airHandlingUnitCalculationMethod))
                {
                    airHandlingUnitCalculationMethod = AirHandlingUnitCalculationMethod.FixedSupplyTemperature;
                }
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            space = adjacencyCluster?.GetObject<Space>(space.Guid);
            if (space == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            space.SetValue(Mollier.SpaceParameter.AirHandlingUnitCalculationMethod, airHandlingUnitCalculationMethod);
            adjacencyCluster.AddObject(space);

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            List<VentilationSystem> ventilationSystems = adjacencyCluster.Systems<VentilationSystem>(space);
            if (ventilationSystems != null && ventilationSystems.Count != 0)
            {
                VentilationSystem ventilationSystem = ventilationSystems.Find(x => x != null);

                string unitName = null;
                if (!ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out unitName) || string.IsNullOrWhiteSpace(unitName))
                {
                    unitName = null;
                }

                if(!string.IsNullOrEmpty(unitName))
                {
                    AirHandlingUnit airHandlingUnit = adjacencyCluster?.GetObjects<AirHandlingUnit>()?.Find(x => x.Name == unitName);
                    if (airHandlingUnit != null)
                    {
                        AirHandlingUnitResult airHandlingUnitResult = Mollier.Create.AirHandlingUnitResult(analyticalModel, airHandlingUnit.Name, space);
                        if (airHandlingUnitResult != null)
                        {
                            MollierGroup  mollierGroup = airHandlingUnitResult.GetValue<MollierGroup>(AirHandlingUnitResultParameter.Processes);

                            List<IMollierProcess> mollierProcesses = mollierGroup?.GetObjects<IMollierProcess>();

                            double pressure = Standard.Pressure;

                            bool regenerate = true;
                            if (mollierForm == null || mollierForm.IsDisposed)
                            {
                                mollierForm = new Core.Mollier.UI.MollierForm() { ReadOnly = true, WindowState = System.Windows.Forms.FormWindowState.Normal };
                                regenerate = false;
                            }
                            else
                            {
                                mollierForm.Clear();
                            }
                            pressure = Core.Mollier.UI.Query.DefaultPressure(null, mollierProcesses);

                            mollierForm.Name = string.IsNullOrWhiteSpace(space.Name) ? mollierForm.Name : space.Name;
                            mollierForm.MollierControlSettings = Core.Mollier.UI.Query.DefaultMollierControlSettings();
                            mollierForm.Pressure = pressure;

                            mollierForm.AddMollierObjects(mollierProcesses, false, regenerate);
                            mollierForm.Show(!regenerate);
                        }
                    }
                }


            }

        }
    }
}