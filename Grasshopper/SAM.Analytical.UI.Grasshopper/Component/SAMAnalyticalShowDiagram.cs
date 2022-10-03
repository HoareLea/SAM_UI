using Grasshopper.Kernel;
using SAM.Analytical.UI.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Grasshopper.Mollier;
using SAM.Core.Mollier;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.Grasshopper
{
    public class SAMAnalyticalShowDiagram : GH_SAMVariableOutputParameterComponent
    {
        private Core.Mollier.UI.MollierForm mollierForm;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3c6e715a-3cbd-4c61-b033-83936bf07e97");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalShowDiagram()
          : base("SAMAnalytical.ShowDiagram", "SAMAnalytical.ShowDiagram",
              "Show Diagram",
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
                GooMollierPointParam gooMollierPointParam = new GooMollierPointParam() { Name = "_mollierPoints", NickName = "_mollierPoints", Description = "SAM Core MollierPoints", Optional = true, Access = GH_ParamAccess.list };
                result.Add(new GH_SAMParam(gooMollierPointParam, ParamVisibility.Binding));

                GooMollierProcessParam gooMollierProcesses = new GooMollierProcessParam() { Name = "_mollierProcesses", NickName = "_mollierProcesses", Description = "SAM Core MollierProcesses", Optional = true, Access = GH_ParamAccess.list };
                result.Add(new GH_SAMParam(gooMollierProcesses, ParamVisibility.Binding));


                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
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

            List<MollierPoint> mollierPoints = new List<MollierPoint>(); ;
            index = Params.IndexOfInputParam("_mollierPoints");
            if (index != -1)
            {
                dataAccess.GetDataList(index, mollierPoints);
            }

            List<IMollierProcess> mollierProcesses = new List<IMollierProcess>();
            index = Params.IndexOfInputParam("_mollierProcesses");
            if (index != -1)
            {
                dataAccess.GetDataList(index, mollierProcesses);
            }

            if (mollierForm == null || mollierForm.IsDisposed)
            {
                mollierForm = new Core.Mollier.UI.MollierForm() { ReadOnly = true, WindowState = System.Windows.Forms.FormWindowState.Normal };
            }
            else
            {
                mollierForm.Clear();
            }

            //CreateDefault Grasshopper visibilitySettings

            double pressure = Core.Mollier.UI.Query.DefaultPressure(mollierPoints, mollierProcesses);
            mollierForm.Name = "Mollier Diagram";
            mollierForm.MollierControlSettings = Core.Mollier.UI.Query.DefaultMollierControlSettings();
            mollierForm.default_chart(mollierForm.MollierControlSettings);
            mollierForm.Pressure = double.IsNaN(pressure) ? Standard.Pressure : pressure;
            //mollierForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            //mollierForm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            //mollierForm.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;

            //mollierForm.ClientSize = new System.Drawing.Size(723, 1501);

            //mollierForm.AutoScaleDimensions = new System.Drawing.SizeF(6, 15);

            //mollierForm.Bounds = new System.Drawing.Rectangle(23, 64, 1501, 723);

            //mollierForm.ClientSize = new System.Drawing.Size(723, 1501);




            //mollierProcesses?.ForEach(x => mollierForm.AddProcess(x, false));
            mollierForm.AddProcesses(mollierProcesses, false);
            mollierForm.AddPoints(mollierPoints, false);

            mollierForm.Show();
        }
    }
}