﻿using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SAM.Core.Grasshopper.Mollier;
using SAM.Core.Mollier.UI.Grasshopper.Properties;
using SAM.Geometry.Grasshopper.Mollier;
using SAM.Geometry.Mollier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Grasshopper
{
    public class GooMollierChartObject : GH_Goo<MollierChartObject>, IGH_PreviewData, IGH_BakeAwareData
    {
        public GooMollierChartObject()
            : base()
        {
        }

        public GooMollierChartObject(MollierChartObject mollierChartObject)
            : base(mollierChartObject)
        {
        }

        public override bool IsValid => Value != null;

        public override string TypeName
        {
            get
            {
                return "MollierChartObject";
            }
        }

        public override string TypeDescription
        {
            get
            {
                return "MollierChartObject";
            }
        }

        public override string ToString()
        {
            return "MollierChartObject";
        }

        public BoundingBox ClippingBox
        {
            get
            {
                if (Value == null)
                    return BoundingBox.Empty;

                return Value.BoundingBox();
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooMollierChartObject(Value);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null || Value.UIMollierObject == null)
            {
                return;
            }

            System.Drawing.Color color = Value.UIMollierAppearance.Color;
            if (Value.UIMollierObject is UIMollierPoint)
            {
                MollierPoint mollierPoint = (UIMollierPoint)Value.UIMollierObject;
                if (mollierPoint == null)
                {
                    return;
                }

                Point3d point3d = mollierPoint.ToRhino_Point3d(Value.ChartType, Value.Z);

                args.Pipeline.DrawPoint(point3d, color);

                return;
            }

            else if (Value.UIMollierObject is UIMollierProcess)
            {
                Polyline polyline = ((UIMollierProcess)Value.UIMollierObject)?.ToRhino_Polyline(Value.ChartType, Value.Z);
                if (polyline == null)
                {
                    return;
                }

                if (color == System.Drawing.Color.Empty)
                {
                    color = UI.Query.DefaultMollierControlSettings().VisibilitySettings.GetColor(new MollierControlSettings().DefaultTemplateName, ChartParameterType.Line, (UIMollierProcess)Value.UIMollierObject);
                }

                args.Pipeline.DrawPolyline(polyline, color);

                return;
            }

            else if (Value.UIMollierObject is UIMollierCurve)
            {
                Polyline polyline = ((UIMollierCurve)Value.UIMollierObject).ToRhino_Polyline(Value.ChartType, Value.Z);
                if (polyline == null)
                {
                    return;
                }

                args.Pipeline.DrawPolyline(polyline, color);

                return;
            }

            else if (Value.UIMollierObject is UIMollierGroup)
            {
                UIMollierGroup uIMollierGroup = (UIMollierGroup)Value.UIMollierObject;
                List<IMollierProcess> mollierProcesses = uIMollierGroup.GetObjects<IMollierProcess>();
                foreach (IMollierProcess mollierProcess in mollierProcesses)
                {
                    if (mollierProcess != null)
                    {
                        Polyline polyline = mollierProcess.ToRhino_Polyline(Value.ChartType, Value.Z);
                        args.Pipeline.DrawPolyline(polyline, mollierProcess.Color());
                    }
                }

                List<IMollierPoint> mollierPoints = uIMollierGroup.GetObjects<IMollierPoint>();
                foreach (IMollierPoint mollierPoint in mollierPoints)
                {
                    if (mollierPoint != null)
                    {
                        Point3d point3d = mollierPoint.ToRhino_Point3d(Value.ChartType, Value.Z);
                        args.Pipeline.DrawPoint(point3d, mollierPoint.Color());
                    }
                }
            }

            else
            {
                throw new NotImplementedException();
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {

        }

        public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            obj_guid = Guid.Empty;

            if (Value == null)
            {
                return false;
            }

            if (Value.UIMollierObject is UIMollierPoint)
            {
                MollierPoint mollierPoint = (UIMollierPoint)Value.UIMollierObject;
                if (mollierPoint == null)
                {
                    return false;
                }

                obj_guid = doc.Objects.AddPoint(mollierPoint.ToRhino_Point3d(Value.ChartType, Value.Z), att);
                return true;
            }


            if (Value.UIMollierObject is UIMollierProcess)
            {
                Polyline polyline = ((UIMollierProcess)Value.UIMollierObject)?.ToRhino_Polyline(Value.ChartType, Value.Z);
                if (polyline == null)
                {
                    return false;
                }


                obj_guid = doc.Objects.AddCurve(new PolylineCurve(polyline), att);
                return true;
            }

            if (Value.UIMollierObject is UIMollierCurve)
            {
                Polyline polyline = ((UIMollierCurve)Value.UIMollierObject)?.ToRhino_Polyline(Value.ChartType, Value.Z);
                if (polyline == null)
                {
                    return false;
                }


                obj_guid = doc.Objects.AddCurve(new PolylineCurve(polyline), att);
                return true;
            }

            return false;

        }

        public override bool CastFrom(object source)
        {
            if (source is MollierChartObject)
            {
                Value = (MollierChartObject)source;
                return true;
            }

            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (typeof(Y).IsAssignableFrom(typeof(MollierChartObject)))
            {
                target = (Y)(object)Value;

                return true;
            }

            return base.CastTo(ref target);
        }
    }

    public class GooMollierChartObjectParam : GH_PersistentParam<GooMollierChartObject>, IGH_PreviewObject, IGH_BakeAwareObject
    {
        public override Guid ComponentGuid => new Guid("bbf6a119-0bcb-402c-9dd9-b6b45112c169");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        bool IGH_PreviewObject.Hidden { get; set; } = false;

        bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;

        BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();

        public bool IsBakeCapable => true;

        void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }

        void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);
        }

        public GooMollierChartObjectParam()
            : base("MollierChartObject", "MollierChartObject",
              "Mollier Chart points, processes and groups",
              "SAM", "Mollier")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooMollierChartObject> values)
        {
            throw new Exception();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooMollierChartObject value)
        {
            throw new Exception();
        }

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            foreach (var value in VolatileData.AllData(true))
            {
                Guid uuid = default;
                (value as IGH_BakeAwareData)?.BakeGeometry(doc, att, out uuid);
                obj_ids.Add(uuid);
            }
        }


        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Save As...", Menu_SaveAs, VolatileData.AllData(true).Any());

            //Menu_AppendSeparator(menu);

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_SaveAs(object sender, EventArgs e)
        {
            Core.Grasshopper.Query.SaveAs(VolatileData);
        }

        public override bool Write(GH_IWriter writer)
        {
            //liczbe punktow, punkty , kolor
            //writer.SetInt32("points_number", )
            //writer.SetPoint3D();
            //writer.SetDrawingColor("")
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {

            return base.Read(reader);
        }
    }
}