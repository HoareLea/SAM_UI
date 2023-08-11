using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using SAM.Core.Grasshopper;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper.Mollier;
using System.Drawing;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class SAMMollierCreateMollierPoint : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("945e6aa3-8bb9-4a44-81d4-1ec831de47b0");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.5";

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_dryBulbTemperature", NickName = "_dryBulbTemperature", Description = "Dry Bulb Tempearture [°C]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number = null;

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "_relativeHumidity_", NickName = "_relativeHumidity_", Description = "Relative Humidity [%]", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "_humidityRatio_", NickName = "_humidityRatio_", Description = "Humidity Ratio [g_waterVapor/kg_dryAir]", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "_wetBulbTemperature_", NickName = "_wetBulbTemperature_", Description = "Wet Bulb Temperature[°C]", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_pressure_", NickName = "_pressure_", Description = "Pressure [Pa]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(Standard.Pressure);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_elevation_", NickName = "_elevation_", Description = "Elevation [m]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(double.NaN);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Colour param_Colour = null;

                param_Colour = new global::Grasshopper.Kernel.Parameters.Param_Colour() { Name = "_color_", NickName = "_color_", Description = "Colour RGB", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Colour, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_String param_Label = null;

                param_Label = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_label_", NickName = "_label_", Description = "Label", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(param_Label, ParamVisibility.Voluntary));

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
        public SAMMollierCreateMollierPoint()
          : base("SAMMollier.CreateMollierPoint", "SAMMollier.CreateMollierPoint",
              "Creates MollierPoint",
              "SAM", "Mollier")
        {
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            index = Params.IndexOfInputParam("_dryBulbTemperature");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Incomplete data");
                return;
            }
            double dryBulbTemperature = double.NaN;
            if (!dataAccess.GetData(index, ref dryBulbTemperature) || double.IsNaN(dryBulbTemperature))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Incomplete data");
                return;
            }

            double pressure = double.NaN;
            index = Params.IndexOfInputParam("_pressure_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref pressure);
            }


            index = Params.IndexOfInputParam("_elevation_");
            if (index != -1)
            {
                double elevation = double.NaN;
                if (dataAccess.GetData(index, ref elevation) && !double.IsNaN(elevation))
                {
                    pressure = Core.Mollier.Query.Pressure(elevation);
                }
            }

            double nan = 0;

            index = Params.IndexOfInputParam("_humidityRatio_");
            double humidityRatio = double.NaN;
            if (index == -1 || !dataAccess.GetData(index, ref humidityRatio))
            {
                nan++;
            }
            humidityRatio = humidityRatio / 1000;

            index = Params.IndexOfInputParam("_relativeHumidity_");
            double relativeHumidity = double.NaN;
            if (index == -1 || !dataAccess.GetData(index, ref relativeHumidity))
            {
                nan++;
            }
            else
            {
                humidityRatio = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
            }

            index = Params.IndexOfInputParam("_wetBulbTemperature_");
            double wetBulbTemperature = double.NaN;
            if (index == -1 || !dataAccess.GetData(index, ref wetBulbTemperature))
            {
                nan++;
            }
            else
            {
                humidityRatio = Core.Mollier.Query.HumidityRatio_ByWetBulbTemperature(dryBulbTemperature, wetBulbTemperature, pressure);
            }


            if (nan != 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            MollierPoint mollierPoint = new MollierPoint(dryBulbTemperature, humidityRatio, pressure);

            Color color = Color.Empty;

            index = Params.IndexOfInputParam("_color_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref color);
            }

            string label = null;
            index = Params.IndexOfInputParam("_label_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref label);
            }

            UIMollierPoint uIMollierPoint = new UIMollierPoint(mollierPoint, new UIMollierAppearance(color, label));


            index = Params.IndexOfOutputParam("mollierPoint");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooMollierPoint(uIMollierPoint));
            }
        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            return Params.Output;
        }
    }
}