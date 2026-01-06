// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Geometry.Grasshopper.Mollier;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateFanProcess : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("9cd99bf3-eb0b-4289-9c4b-5a97f2ecbd92");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

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

                global::Grasshopper.Kernel.Parameters.Param_Number number = null;

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_sfp_", NickName = "_sfp_", Description = "Specific Fan Power [W/l/s]", Access = GH_ParamAccess.item, Optional = true };
                number.SetPersistentData(1.2);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_density_", NickName = "_density_", Description = "Moist Air Density ρ [kg_MoistAir/m3]", Access = GH_ParamAccess.item, Optional = true };
                number.SetPersistentData(1.2);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_specificHeatCapacity_", NickName = "_specificHeatCapacity_", Description = "Specific Heat Capacity of Air [kJ/kgK]", Access = GH_ParamAccess.item, Optional = true };
                number.SetPersistentData(1.005);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;

                global::Grasshopper.Kernel.Parameters.Param_Colour param_Colour = null;
                param_Colour = new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "_color_", NickName = "_color_", Description = "Colour RGB", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Colour, ParamVisibility.Voluntary));
                global::Grasshopper.Kernel.Parameters.Param_String param_Label = null;
                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "startLabel_", NickName = "startLabel_", Description = "Start Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "processLabel_", NickName = "processLabel_", Description = "Process Label", Access = GH_ParamAccess.item, Optional = true };
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
                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "fanProcess", NickName = "fanProcess", Description = "Fan Process", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "end", NickName = "end", Description = "End", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "color", NickName = "color", Description = "Color", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "sensibleHeatRatio", NickName = "sensibleHeatRatio", Description = "Sensible Heat Ratio [-]", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "epsilon", NickName = "epsilon", Description = "Epsilon", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Updates PanelTypes for AdjacencyCluster
        /// </summary>
        public SAMMollierCreateFanProcess()
          : base("SAMMollier.CreateFanProcess", "SAMMollier.CreateFanProcess",
              "Creates FanProcess",
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
            MollierPoint mollierPoint = null;
            if (!dataAccess.GetData(index, ref mollierPoint) || mollierPoint == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double sfp = 1.2;
            index = Params.IndexOfInputParam("_sfp_");
            if(index != -1)
            {
                dataAccess.GetData(index, ref sfp);
            }

            if (double.IsNaN(sfp))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double density = double.NaN;
            index = Params.IndexOfInputParam("_density_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref density);
            }

            double specificHeatCapacity = double.NaN;
            index = Params.IndexOfInputParam("_specificHeatCapacity_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref specificHeatCapacity);
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

            if(double.IsNaN(density))
            {
                density = mollierPoint.Density();
            }

            if (double.IsNaN(specificHeatCapacity))
            {
                specificHeatCapacity = mollierPoint.SpecificHeatCapacity_Air();
            }

            FanProcess fanProcess = Mollier.Create.FanProcess(mollierPoint, sfp, density, specificHeatCapacity);


            index = Params.IndexOfOutputParam("fanProcess");
            if (index != -1)
            {
                if(fanProcess != null)
                {
                    dataAccess.SetData(index, new GooMollierProcess(fanProcess, color, startLabel, processLabel, endLabel));
                }
                else
                {
                    dataAccess.SetData(index, null);
                }
            }
            else
            {
                return;
            }

            index = Params.IndexOfOutputParam("end");
            if (index != -1)
            {
                if (fanProcess == null)
                {
                    dataAccess.SetData(index, mollierPoint);
                }
                else
                {
                    MollierPoint end = new MollierPoint(fanProcess.End);
                    dataAccess.SetData(index, new GooMollierPoint(end));
                }

                
            }
            index = Params.IndexOfOutputParam("color");
            if (index != -1)
            {
                dataAccess.SetData(index, color);
            }

            index = Params.IndexOfOutputParam("epsilon");
            if (index != -1)
            {
                if (fanProcess == null)
                {
                    dataAccess.SetData(index, null);
                }
                else
                {
                    dataAccess.SetData(index, fanProcess.Epsilon());
                }
            }

            index = Params.IndexOfOutputParam("sensibleHeatRatio");
            if (index != -1)
            {
                dataAccess.SetData(index, fanProcess?.SensibleHeatRatio());
            }
        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return new IGH_Param[] { Params.Output.Find(x => x.Name == "fanProcess") };
        }
    }
}
