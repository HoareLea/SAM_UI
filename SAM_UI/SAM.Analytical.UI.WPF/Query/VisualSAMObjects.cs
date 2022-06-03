using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<T> VisualSAMObjects<T>(this Viewport3D viewport3D) where T: IVisualSAMObject
        {
            if(viewport3D == null)
            {
                return null;
            }

            Visual3DCollection visual3DCollection = viewport3D.Children;
            if (visual3DCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (object @object in visual3DCollection)
            {
                if (!(@object is T))
                {
                    continue;
                }

                result.Add((T)@object);
            }

            return result;

        }
    }
}
