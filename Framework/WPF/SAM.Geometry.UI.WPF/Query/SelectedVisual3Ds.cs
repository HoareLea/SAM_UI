using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static List<Visual3D> SelectedVisual3Ds(this ActionManager actionManager)
        {
            if(actionManager == null)
            {
                return null;
            }

            IEnumerable<SelectAction> selectActions = actionManager.GetActions<SelectAction>();
            if(selectActions == null || selectActions.Count() == 0)
            {
                return null;
            }

            List<Visual3D> result = new List<Visual3D>();
            foreach(SelectAction selectAction in selectActions)
            {
                List<Visual3D> visual3Ds = selectAction?.Visual3Ds;
                if(visual3Ds == null || visual3Ds.Count == 0)
                {
                    continue;
                }

                foreach(Visual3D visual in visual3Ds)
                {
                    if(!result.Contains(visual))
                    {
                        result.Add(visual);
                    }
                }
            }

            return result;
        }
    }
}
