using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper.Mollier;
using System.Drawing;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateMollierPointByConst20C : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("96ea09b7-7923-407d-ab99-f393fa78dd4a");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

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
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "_mollierPoint", NickName = "_mollierPoint", Description = "Mollier Point", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;
                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "vapourizationLatentHeat_", NickName = "vapourizationLatentHeat_", Description = "The specific heat of water condensation at temperature 20C", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(Const20C.VapourizationLatentHeat);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "specificHeatWaterVapour_", NickName = "specificHeat_WaterVapour_", Description = "Specific Heat of water vapour at constant pressure and temperature 20C (cpw)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(Const20C.SpecificHeat_WaterVapour);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "specificHeatAir_", NickName = "specificHeatAir_", Description = "Specific Heat of air vapour at constant pressure and temperature 20C (cp)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(Const20C.SpecificHeat_Air);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "specificHeatWater_", NickName = "specificHeatWater_", Description = "Specific Heat of water vapour at constant pressure and temperature 20C (cw)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(Const20C.SpecificHeat_Water);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));


                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "mollierPoint", NickName = "mollierPoint", Description = "SAM Core Mollier MollierPoint", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Updates PanelTypes for AdjacencyCluster
        /// </summary>
        public SAMMollierCreateMollierPointByConst20C()
          : base("SAMMollier.CreateMollierPointByConst20C", "SAMMollier.CreateMollierPointByConst20C",
              "Creates MollierPoint using given constants",
              "SAM", "Mollier")
        {
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            index = Params.IndexOfInputParam("_mollierPoint");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }
            IMollierPoint mollierPoint = null;
            if (!dataAccess.GetData(index, ref mollierPoint) || mollierPoint == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double vapourizationLatentHeat = double.NaN;
            index = Params.IndexOfInputParam("vapourizationLatentHeat_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref vapourizationLatentHeat);
            }

            double specificHeat_WaterVapour = double.NaN;
            index = Params.IndexOfInputParam("specificHeatWaterVapour_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref specificHeat_WaterVapour);
            }

            double specificHeat_Air = double.NaN;
            index = Params.IndexOfInputParam("specificHeatAir_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref specificHeat_Air);
            }

            double specificHeat_Water = double.NaN;
            index = Params.IndexOfInputParam("specificHeatWater_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref specificHeat_Water);
            }


            if (mollierPoint is UIMollierPoint)
            {
                UIMollierPoint UIMollierPoint = (UIMollierPoint)mollierPoint;
                MollierPoint mollierPointTemp = new MollierPoint(UIMollierPoint, vapourizationLatentHeat, specificHeat_WaterVapour, specificHeat_Air, specificHeat_Water);
                mollierPoint = new UIMollierPoint(mollierPointTemp, UIMollierPoint.UIMollierAppearance);
            }
            else
            {
                mollierPoint = new MollierPoint((UIMollierPoint)mollierPoint, vapourizationLatentHeat, specificHeat_WaterVapour, specificHeat_Air, specificHeat_Water);
            }
        

            index = Params.IndexOfOutputParam("mollierPoint");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierPoint(mollierPoint));
            }
        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return Params.Output;
        }
    }
}