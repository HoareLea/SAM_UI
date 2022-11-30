using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class VisualGeometryObject : Core.UI.WPF.VisualJSAMObject<ISAMGeometryObject>, IVisualGeometryObject
    {
        protected ISAMGeometryObject sAMGeometryObject;

        public VisualGeometryObject(ISAMGeometryObject sAMGeometryObject)
            :base(sAMGeometryObject)
        {
            this.sAMGeometryObject = sAMGeometryObject;
        }

        public ISAMGeometryObject SAMGeometryObject
        {
            get
            {
                return sAMGeometryObject;
            }
        }

        public GeometryModel3D GeometryModel3D
        {
            get
            {
                return Content as GeometryModel3D;
            }
        }

        public override bool SetSelected(bool selected)
        {
            Modify.Select(Content, selected);
            return true;
            //Model3DGroup model3DGroup = Content as Model3DGroup;
            //if(model3DGroup != null)
            //{
            //    for(int i =0; i < model3DGroup.Children.Count; i++)
            //    {
            //        Face3DObject face3DObject = Core.UI.WPF.Query.JSAMObject<Face3DObject>(model3DGroup.Children[i]);
            //        if(face3DObject != null)
            //        {
            //            Model3D model3D = Create.Model3D(new Face3DObject(face3DObject) { SurfaceAppearance = Query.SelectionSurfaceAppearance() });
            //            Core.UI.WPF.Modify.SetIJSAMObject(model3D, face3DObject);

            //            model3DGroup.Children[i] = model3D;
            //        }
            //    }
            //}

            //return base.SetSelected(selected);
        }

        public override bool SetHighlight(bool highlight)
        {
            Modify.Highlight(Content, highlight);
            return true;

            //Model3DGroup model3DGroup = Content as Model3DGroup;
            //if (model3DGroup != null)
            //{
            //    for (int i = 0; i < model3DGroup.Children.Count; i++)
            //    {
            //        Face3DObject face3DObject = Core.UI.WPF.Query.JSAMObject<Face3DObject>(model3DGroup.Children[i]);
            //        if (face3DObject != null)
            //        {
            //            Model3D model3D = model3DGroup.Children[i];
            //            if(model3D is Model3DGroup)
            //            {
            //                for (int j = 0; j < ((Model3DGroup)model3D).Children.Count; j++)
            //                {
            //                    Model3D model3D_Temp = ((Model3DGroup)model3D).Children[j];
            //                }
            //            }

            //            model3D = Create.Model3D(new Face3DObject(face3DObject) { SurfaceAppearance = Query.HighlightSurfaceAppearance(face3DObject.SurfaceAppearance) });
            //            Core.UI.WPF.Modify.SetIJSAMObject(model3D, face3DObject);

            //            model3DGroup.Children[i] = model3D;
            //        }
            //    }

            //    return true;
            //}

            //return base.SetHighlight(highlight);
        }
    }
}
