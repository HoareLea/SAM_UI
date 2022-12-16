using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class SelectAction : Visual3DsAction
    {
        public SelectAction()
        {

        }
        
        public SelectAction(Visual3D visual3D)
            :base(visual3D)
        {

        }

        public SelectAction(IEnumerable<Visual3D> visual3Ds)
            : base(visual3Ds)
        {

        }

        public void Select(bool select)
        {
            List<Visual3D> visual3Ds = Visual3Ds;
            if (visual3Ds != null)
            {
                visual3Ds.ForEach(x => Modify.Select(x, select));
            }
        }
    }
}
