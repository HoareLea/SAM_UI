using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper.Mollier;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateMollierProcessByTwoPoints : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("f47d7b0a-7a73-4070-8a74-1dfb0fbdf9ff");

        /// <summary>
        /// The latest version of this components
        /// </summary>  
        public override string LatestComponentVersion => "1.0.4";

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

                global::Grasshopper.Kernel.Parameters.Param_Colour param_Colour = null;
                param_Colour = new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "_color_", NickName = "_color_", Description = "Colour RGB", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Colour, ParamVisibility.Voluntary));
                global::Grasshopper.Kernel.Parameters.Param_String param_Label = null;
                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "startLabel_", NickName = "startLabel_", Description = "Start Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "processLabel_", NickName = "processLabel_", Description = "Process Label", Access = GH_ParamAccess.item, Optional = true };
                param_Label.SetPersistentData("Room");
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "endLabel_", NickName = "endLabel_", Description = "End Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "mollierProcess", NickName = "mollierProcess", Description = "Mollier Process", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "sensibleHeatRatio", NickName = "sensibleHeatRatio", Description = "Sensible Heat Ratio [-]", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
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

            Color color = Color.Empty;

            index = Params.IndexOfInputParam("_color_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref color);
            }

            string startLabel = null;
            index = Params.IndexOfInputParam("startLabel_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref startLabel);
            }
            string processLabel = null;
            index = Params.IndexOfInputParam("processLabel_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref processLabel);
            }
            string endLabel = null;
            index = Params.IndexOfInputParam("endLabel_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref endLabel);
            }


            // Detecting what type of process is it

            MollierProcess mollierProcess = null;

            if (end.HumidityRatio > start.HumidityRatio) // Humidification
            {
                double humidityRatioDifference = end.HumidityRatio - start.HumidityRatio;
                if (Math.Abs(Core.Query.Round(end.Enthalpy, 1) - Core.Query.Round(start.Enthalpy, 1) ) == 0) // Adiabactic
                {
                    mollierProcess = Mollier.Create.AdiabaticHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference);
                }
                else if (Math.Abs(end.DryBulbTemperature - start.DryBulbTemperature) < Tolerance.MacroDistance) // Isothermic
                {
                    mollierProcess = Mollier.Create.IsotermicHumidificationProcess_ByHumidityRatioDifference(start, humidityRatioDifference);
                }
                else
                {
                    mollierProcess = Mollier.Create.UndefinedProcess(start, end);
                }
            }
            else if (end.DryBulbTemperature > start.DryBulbTemperature) // Heating
            {
                double humidityRatioDifference = end.HumidityRatio - start.HumidityRatio;
                if(humidityRatioDifference < 0.0001)
                {
                    mollierProcess = Mollier.Create.HeatingProcess(start, end.DryBulbTemperature);
                }
                else
                {
                    mollierProcess = Mollier.Create.UndefinedProcess(start, end);
                }
            }
            else
            {
                double temperatureDifference = start.DryBulbTemperature - end.DryBulbTemperature;
                mollierProcess = Mollier.Create.CoolingProcess(start, temperatureDifference);
            }



            index = Params.IndexOfOutputParam("mollierProcess");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierProcess(mollierProcess, color, startLabel, processLabel, endLabel));
            }

            index = Params.IndexOfOutputParam("sensibleHeatRatio");
            if (index != -1)
            {
                dataAccess.SetData(index, mollierProcess?.SensibleHeatRatio());
            }

        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return Params.Output;
        }
 
    }
}