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

        public Visual3DsAction()
        {

        }

        public List<Visual3D> Visual3Ds
        {
            get
            {
                return visual3Ds == null ? null : new List<Visual3D>(visual3Ds);
            }
        }

        public bool Contains(Visual3D visual3D)
        {
            if(visual3Ds == null || visual3D == null)
            {
                return false;
            }

            return visual3Ds.Contains(visual3D);
        }

        public bool Remove(Visual3D visual3D)
        {
            if (visual3Ds == null || visual3D == null)
            {
                return false;
            }

            int index = visual3Ds.IndexOf(visual3D);
            if(index == -1)
            {
                return false;
            }

            visual3Ds.RemoveAt(index);
            return true;
        }

        public bool Add(Visual3D visual3D)
        {
            if(visual3D == null)
            {
                return false;
            }

            if(visual3Ds == null || visual3Ds.Count == 0)
            {
                visual3Ds = new List<Visual3D>() { visual3D };
                return true;
            }

            if(visual3Ds.Contains(visual3D))
            {
                return false;
            }

            visual3Ds.Add(visual3D);
            return true;
        }
    }
}
