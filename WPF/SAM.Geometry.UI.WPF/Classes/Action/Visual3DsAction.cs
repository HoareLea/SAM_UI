using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class Visual3DsAction : IAction
    {
        private List<Visual3D> visual3Ds;

        public Visual3DsAction(Visual3D visual3D)
        {
            if(visual3D != null)
            {
                visual3Ds = new List<Visual3D>();
                visual3Ds.Add(visual3D);
            }
        }

        public Visual3DsAction(IEnumerable<Visual3D> visual3Ds)
        {
            if(visual3Ds != null)
            {
                this.visual3Ds = new List<Visual3D>(visual3Ds);
            }
        }

        public List<Visual3D> Visual3Ds
        {
            get
            {
                return visual3Ds == null ? null : new List<Visual3D>(visual3Ds);
            }
        }
    }
}
