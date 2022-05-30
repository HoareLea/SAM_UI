using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static void Update(this ModelVisual3D modelVisual3D, AnalyticalModel analyticalModel)
        {
            if(analyticalModel == null || modelVisual3D == null)
            {
                return;
            }

            modelVisual3D.Content = null;

            Model3DGroup model3DGroup = new Model3DGroup();
            List<Panel> panels = analyticalModel.GetPanels();
            if (panels != null && panels.Count != 0)
            {
                foreach (Panel panel in panels)
                {
                    GeometryModel3D geometryModel3D = Convert.ToMedia3D(panel);
                    if (geometryModel3D == null)
                    {
                        continue;
                    }

                    model3DGroup.Children.Add(geometryModel3D);

                }
            }

            modelVisual3D.Content = model3DGroup;
        }
    }
}
