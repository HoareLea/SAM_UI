using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class VisualGeometry<T> : Core.UI.WPF.VisualJSAMObject<T>, IVisualGeometry where T : ISAMGeometry
    {
        protected T sAMGeometry;

        public VisualGeometry(T sAMGeometry)
            :base(sAMGeometry)
        {
            this.sAMGeometry = sAMGeometry;
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
