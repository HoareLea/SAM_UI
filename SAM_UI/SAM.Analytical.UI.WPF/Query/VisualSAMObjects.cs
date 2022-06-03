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

            return VisualSAMObjects<T>(viewport3D.Children);
        }

        public static List<T> VisualSAMObjects<T>(this Visual3DCollection visual3DCollection) where T : IVisualSAMObject
        {
            if (visual3DCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (object @object in visual3DCollection)
            {
                if (!(@object is T))
                {
                    if(@object is ModelVisual3D)
                    {
                        ModelVisual3D modelVisual3D = (ModelVisual3D)@object;
                        List<T> visualSAMObjects = VisualSAMObjects<T>(modelVisual3D.Children);
                        if(visualSAMObjects != null)
                        {
                            result.AddRange(visualSAMObjects);
                        }
                    }
                    
                    continue;
                }

                result.Add((T)@object);
            }

            return result;
        }
    }
}
