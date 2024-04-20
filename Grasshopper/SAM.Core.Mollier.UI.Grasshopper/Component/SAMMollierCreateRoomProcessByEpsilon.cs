using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Geometry.Grasshopper.Mollier;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateRoomProcessByEpsilon : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2ac378eb-deb2-44f4-88b4-5ac1927828f3");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.6";

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
                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "_mollierPoint", NickName = "_mollierPoint", Description = "Mollier Point for MollierProcess", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_start_", NickName = "_start_", Description = "Start or End Point", Access = GH_ParamAccess.item, Optional = true };
                param_Boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;
                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_epsilon", NickName = "_epsilon", Description = "Epsilon [kJ/kg]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(2501);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_humidityRatio", NickName = "_humidityRatio", Description = "Humidity Ratio of second point [g/kg] \n *This is only used to define where line should be extended (above or below Point)", Access = GH_ParamAccess.item};
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Colour param_Colour;
                param_Colour = new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "_color_", NickName = "_color_", Description = "Colour RGB", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Colour, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_String param_Label;
                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "startLabel_", NickName = "startLabel_", Description = "Start Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "processLabel_", NickName = "processLabel_", Description = "Process Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "endLabel_", NickName = "endLabel_", Description = "End Label", Access = GH_ParamAccess.item, Optional = true };
                param_Label.SetPersistentData("ROOM");

                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));
                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "roomProcess", NickName = "roomProcess", Description = "Room Process", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "color", NickName = "color", Description = "Color", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "sensibleHeatRatio", NickName = "sensibleHeatRatio", Description = "Sensible Heat Ratio [-]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "epsilon", NickName = "epsilon", Description = "Epsilon", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Updates PanelTypes for AdjacencyCluster
        /// </summary>
        public SAMMollierCreateRoomProcessByEpsilon()
          : base("SAMMollier.CreateRoomProcessByEpsilon", "SAMMollier.CreateRoomProcessByEpsilon",
              "Creates RoomProcess by given epsilon [kJ/kg] \n provide x to define lenth of line\n constant pressure and temperature 0C (cp) 1010 J/kgK",
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

            MollierPoint mollierPoint = null;
            if (!dataAccess.GetData(index, ref mollierPoint) || mollierPoint == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double epsilon = 2501;
            index = Params.IndexOfInputParam("_epsilon");
            if (index != -1)
            {
                dataAccess.GetData(index, ref epsilon);
            }

            bool start = true;
            index = Params.IndexOfInputParam("_start_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref start);
            }

            double humidityRatio = double.NaN;
            index = Params.IndexOfInputParam("_humidityRatio");
            if (index != -1)
            {
                dataAccess.GetData(index, ref humidityRatio);
            }

            if(double.IsNaN(humidityRatio))
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

            RoomProcess roomProcess = Mollier.Create.RoomProcess_ByEpsilonAndHumidityRatioDifference(mollierPoint, epsilon, (humidityRatio / 1000) - mollierPoint.HumidityRatio, start);


            index = Params.IndexOfOutputParam("roomProcess");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierProcess(roomProcess, color, startLabel, processLabel, endLabel)); ;
            }

            index = Params.IndexOfOutputParam("color");
            if (index != -1)
            {
                dataAccess.SetData(index, color);
            }

            index = Params.IndexOfOutputParam("epsilon");
            if (index != -1)
            {
                dataAccess.SetData(index, roomProcess.Epsilon());
            }

            index = Params.IndexOfOutputParam("sensibleHeatRatio");
            if (index != -1)
            {
                dataAccess.SetData(index, roomProcess.SensibleHeatRatio());
            }
        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return Params.Output;
        }
    }
}