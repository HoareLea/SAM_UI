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

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach (Visual3D visual3D in visual3DCollection)
            {
                if (!(visual3D is T))
                {
                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            foreach (Visual3D visual3D in visual3Ds)
            {
                viewport3D.Children.Remove(visual3D);
            }
        }
    }
}