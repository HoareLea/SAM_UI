using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper.Mollier;
using SAM.Core.Mollier.UI.Forms;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateMollierProcessByTwoPoints : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("f47d7b0a-7a73-4070-8a74-1dfb0fbdf9ff");

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
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "_start", NickName = "_start", Description = "Start Mollier Point for MollierProcess", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "_end", NickName = "_end", Description = "End Mollier Point for MollierProcess", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;
                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_efficiency_", NickName = "_efficiency_", Description = "Efficiency [0 - 1]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(1);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "mollierProcess", NickName = "mollierProcess", Description = "Mollier Process", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// Updates PanelTypes for AdjacencyCluster
        /// </summary>
        public SAMMollierCreateMollierProcessByTwoPoints()
          : base("SAMMollier.CreateMollierProcessByTwoPoints", "SAMMollier.CreateMollierProcessByTwoPoints",
              "Creates mollier process that is detected by start and end points",
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

            index = Params.IndexOfInputParam("_end");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            MollierPoint end = null;
            if (!dataAccess.GetData(index, ref end) || end == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double efficiency = 1;
            index = Params.IndexOfInputParam("_efficiency_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref efficiency))
                {
                    efficiency = 1;
                }
            }

            // Detecting what type of process is it

            MollierProcess mollierProcess = null;

            if (end.HumidityRatio > start.HumidityRatio) // Humidification
            {
                double humidityRatioDifference = end.HumidityRatio - start.HumidityRatio;
                if (Math.Abs(end.Enthalpy - start.Enthalpy) < Tolerance.Distance) // Adiabactic
                {
                    mollierProcess = Mollier.Create.AdiabaticHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference, efficiency);
                }
                else if (Math.Abs(end.DryBulbTemperature - start.DryBulbTemperature) < Tolerance.Distance) // Isothermic
                {
                    mollierProcess = Mollier.Create.IsotermicHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference, efficiency);
                }
                else
                {
                    mollierProcess = Mollier.Create.UndefinedProcess(start, end, efficiency);
                }
            }
            else if (end.DryBulbTemperature > start.DryBulbTemperature) // Heating
            {
                double temperatureDifference = end.DryBulbTemperature - start.DryBulbTemperature;
                mollierProcess = Mollier.Create.HeatingProcess(start, temperatureDifference, efficiency);
            }
            else
            {
                double temperatureDifference = start.DryBulbTemperature - end.DryBulbTemperature;
                mollierProcess = Mollier.Create.CoolingProcess(start, temperatureDifference, efficiency);
            }



            index = Params.IndexOfOutputParam("mollierProcess");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierProcess(mollierProcess, Color.Empty, null, null, null));
            }

        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return Params.Output;
        }
 
    }
}