using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Clear<T>(this Viewport3D viewport3D)
        {
            Visual3DCollection visual3DCollection = viewport3D.Children;
            if (visual3DCollection == null)
            {
                return;
            }
            
            Clear<T>(visual3DCollection);
        }

        public static void Clear<T>(this Visual3DCollection visual3DCollection)
        {
            if (visual3DCollection == null)
            {
                return;
            }

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach (Visual3D visual3D in visual3DCollection)
            {
                if (!(visual3D is T))
                {
                    if (visual3D is ModelVisual3D)
                    {
                        ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                        Clear<T>(modelVisual3D.Children);
                    }

                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            foreach (Visual3D visual3D in visual3Ds)
            {
                visual3DCollection.Remove(visual3D);
            }
        }
    }
}