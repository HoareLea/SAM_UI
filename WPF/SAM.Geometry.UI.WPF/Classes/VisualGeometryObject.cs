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
            if (sAMGeometryObject is SAMGeometry3DObjectCollection)
            {
                SAMGeometry3DObjectCollection sAMGeometry3DObjects = (SAMGeometry3DObjectCollection)sAMGeometryObject;
                int i = 0;
                foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjects)
                {
                    if(sAMGeometry3DObject is Face3DObject)
                    {
                        Face3DObject face3DObject = new Face3DObject((Face3DObject)sAMGeometry3DObject);
                        face3DObject.SurfaceAppearance = new SurfaceAppearance(face3DObject.SurfaceAppearance.Color, System.Windows.Media.Color.FromRgb(0, 0, 255), 0.02);
                        Model3DGroup model = Content as Model3DGroup;
                        if(model != null)
                        {
                            model.Children[i] = Create.Model3D(face3DObject);
                        }
                    }

                    i++;
                }
            }

            return base.SetSelected(selected);
        }
    }
}
