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
    }
}
