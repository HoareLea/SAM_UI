using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Rhino;
using SAM.Core.Grasshopper;
using SAM.Core.Grasshopper.Mollier;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    /// <summary>
    /// Represents the SAMMollierGeometry class.
    /// </summary>
    public class SAMMollierGeometry : MollierDiagramComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("a0b93650-1575-4985-b0f2-640afce6aa13");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.4";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Mollier;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                result.Add(new GH_SAMParam(new GooMollierGeometryParam() { Name = "MollierGeometry Lines", NickName = "MollierGeometry Lines", Description = "MollierGeometry Lines, Base of your Chart - select lines you want to display on your chart,\n *output from .CreateMollierDiagram ", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                // global::Grasshopper.Kernel.Parameters.Param_Curve curves = null;
                //curves = new global::Grasshopper.Kernel.Parameters.Param_Curve() { Name = "Mollier Chart", NickName = "Inspect Mollier Lines", Description = "Base of Chart - output from InspectMollierDiagram output ", Access = GH_ParamAccess.list, Optional = true };
                // result.Add(new GH_SAMParam(curves, ParamVisibility.Binding));

                Param_Boolean param_Bool = null;
                param_Bool = new Param_Boolean() { Name = "_chartType_", NickName = "_chartType_", Description = "Type of the chart: true - Mollier Chart, false - Psychrometric Chart", Access = GH_ParamAccess.item, Optional = true };
                param_Bool.SetPersistentData(true);
                result.Add(new GH_SAMParam(param_Bool, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooMollierPointParam() { Name = "Mollier Points", NickName = "Mollier Points", Description = "Mollier Points", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooMollierProcessParam() { Name = "Mollier Processes", NickName = "Mollier Processes", Description = "Mollier Processes", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                param_Bool = new Param_Boolean() { Name = "_coolingLineRealistic_", NickName = "_coolingLineRealistic_", Description = "This option will represent Cooling with dehumidification process in 'more' realistic curve - estimation", Access = GH_ParamAccess.item, Optional = true };
                param_Bool.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Bool, ParamVisibility.Voluntary));


                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {

                List<GH_SAMParam> result = new List<GH_SAMParam>();

                result.Add(new GH_SAMParam(new GooMollierGeometryParam() { Name = "MollierGeometry", NickName = "MollierGeometry", Description = "Capable to contain chart lines, points and processes connected as geometry", Access = GH_ParamAccess.tree }, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        public SAMMollierGeometry()
          : base("SAMMollier.Geometry ", "SAMGeometry ",
              "Displays diagram lines together with provided Mollier points and processes \n *Right click to open diagram in UI",
              "SAM", "Mollier")
        {
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {

            int index = Params.IndexOfInputParam("MollierGeometry Lines");
            List<GooMollierGeometry> curves = new List<GooMollierGeometry>();
            if (index != -1)
            {
                dataAccess.GetDataList(index, curves);
            }

            index = Params.IndexOfInputParam("Mollier Points");
            List<MollierPoint> points = new List<MollierPoint>();
            if (index != -1)
            {
                dataAccess.GetDataList(index, points);
            }

            index = Params.IndexOfInputParam("Mollier Processes");
            List<GooMollierProcess> processes = new List<GooMollierProcess>();
            if (index != -1)
            {
                dataAccess.GetDataList(index, processes);
            }

            index = Params.IndexOfInputParam("_chartType_");
            bool chartType_input = true;
            if (index != -1)
            {
                dataAccess.GetData(index, ref chartType_input);
            }

            ChartType chartType = chartType_input == true ? ChartType.Mollier : ChartType.Psychrometric;

            index = Params.IndexOfInputParam("_coolingLineRealistic_");
            bool coolingLineRealistic = false;
            if (index != -1)
            {
                dataAccess.GetData(index, ref coolingLineRealistic);
            }

            List<GooMollierGeometry> processesLines = new List<GooMollierGeometry>();
            foreach (GooMollierProcess process in processes)
            {
                if (process == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                    return;
                }

                MollierProcess mollierProcess = process.Value is UIMollierProcess ? ((UIMollierProcess)process.Value).MollierProcess : process.Value as MollierProcess;
                if(mollierProcess == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                    return;
                }

                System.Drawing.Color color = Mollier.Query.Color(process.Value);

                GH_MollierGeometry gH_MollierGeometry = null;

                if (coolingLineRealistic && mollierProcess is CoolingProcess)
                {
                    List<MollierPoint> mollierPoints = Mollier.Query.ProcessMollierPoints((CoolingProcess)mollierProcess);
                    if (mollierPoints != null && mollierPoints.Count != 0)
                    {
                        gH_MollierGeometry = Core.Grasshopper.Mollier.Create.GH_MollierGeometry(mollierPoints, color, chartType, 0);
                    }
                }

                if(gH_MollierGeometry == null)
                {
                    gH_MollierGeometry = Core.Grasshopper.Mollier.Create.GH_MollierGeometry(mollierProcess, color, chartType, 0);
                }

                if (gH_MollierGeometry != null)
                {
                    processesLines.Add(new GooMollierGeometry(gH_MollierGeometry));
                }
            }

            List<GooMollierGeometry> points_list = new List<GooMollierGeometry>();
            foreach (MollierPoint mollierPoint in points)
            {
                if (mollierPoint == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                    return;
                }
                System.Drawing.Color color = System.Drawing.Color.Black;
                double X = chartType == ChartType.Mollier ? mollierPoint.HumidityRatio * 1000 : mollierPoint.DryBulbTemperature; ;
                double Y = chartType == ChartType.Mollier ? mollierPoint.DryBulbTemperature : mollierPoint.HumidityRatio * 1000;
                List<Rhino.Geometry.Point3d> point = new List<Rhino.Geometry.Point3d>();
                point.Add(new Rhino.Geometry.Point3d(X, Y, 0));
                points_list.Add(new GooMollierGeometry(new GH_MollierGeometry(point, color)));
            }


            index = Params.IndexOfOutputParam("MollierGeometry");

            if (index != -1)
            {
                dataAccess.SetDataList(index, processesLines);
                dataAccess.SetDataList(index, points_list);
                dataAccess.SetDataList(index, curves);
            }
        }

        protected override IEnumerable<IGH_Param> GetMollierDiagramParameters()
        {
            List<IGH_Param> result = new List<IGH_Param>();
            
            IGH_Param gH_Param = null;

            gH_Param = Params.Input?.Find(x => x.Name == "Mollier Processes");
            if(gH_Param != null)
            {
                result.Add(gH_Param);
            }

            gH_Param = Params.Input?.Find(x => x.Name == "Mollier Points");
            if (gH_Param != null)
            {
                result.Add(gH_Param);
            }

            return result;
        }

        protected override MollierControlSettings GetMollierControlSettings()
        {
            MollierControlSettings result = base.GetMollierControlSettings();
            if(result == null)
            {
                return null;
            }

            IGH_Param gH_Param = Params.Input.Find(x => x.Name == "_chartType_");
            if(gH_Param != null)
            {
                if(gH_Param.VolatileData.AllData(true).First().CastTo(out bool chartType))
                {
                    result.ChartType = chartType ? ChartType.Mollier : ChartType.Psychrometric;
                }
            }

            return result;
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Bake By Mollier", Menu_BakeByMollier, true);
            Menu_AppendItem(menu, "Bake By Psychrometric", Menu_BakeByPsychrometric, true);

            base.AppendAdditionalComponentMenuItems(menu);
        }

        private void Menu_BakeByMollier(object sender, EventArgs e)
        {
            Bake(ChartType.Mollier);
        }

        private void Menu_BakeByPsychrometric(object sender, EventArgs e)
        {
            Bake(ChartType.Psychrometric);
        }

        private void Bake(ChartType chartType)
        {
            IGH_Param gHParam = null;

            List<IMollierProcess> mollierProcesses = null;

            gHParam = Params?.Input?.Find(x => x.Name == "Mollier Processes");
            if (gHParam != null)
            {
                mollierProcesses = Query.MollierProcesses<IMollierProcess>(new IGH_Param[] { gHParam });
            }

            List<IMollierPoint> mollierPoints = null;

            gHParam = Params?.Input?.Find(x => x.Name == "Mollier Points");
            if (gHParam != null)
            {
                mollierPoints = Query.MollierPoints(new IGH_Param[] { gHParam });
            }

            Core.Grasshopper.Mollier.Modify.BakeGeometry(RhinoDoc.ActiveDoc, chartType, mollierProcesses, mollierPoints);

        }
    }
}
