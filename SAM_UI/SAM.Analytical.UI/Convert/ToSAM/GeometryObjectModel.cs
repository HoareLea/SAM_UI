using SAM.Geometry;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        public static GeometryObjectModel ToSAM_GeometryObjectModel(this AnalyticalModel analyticalModel)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            List<Panel> panels = analyticalModel.GetPanels();

            GeometryObjectModel result = new GeometryObjectModel();
            foreach(Panel panel in panels)
            {
                VisualPanel visualPanel = new VisualPanel(panel);
                visualPanel.Add(new Face3DObject(panel.Face3D, Query.SurfaceAppearance(panel)));

                List<Aperture> apertures = panel.Apertures;
                if(apertures != null && apertures.Count == 0)
                {
                    foreach(Aperture aperture in apertures)
                    {
                        VisualAperture visualAperture = new VisualAperture(aperture);
                        
                        AperturePart aperturePart = AperturePart.Undefined;
                        List<Face3D> face3Ds = null;

                        aperturePart = AperturePart.Frame;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if(face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart))));
                        }

                        aperturePart = AperturePart.Pane;
                        face3Ds = aperture.GetFace3Ds(aperturePart);
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            face3Ds.ForEach(x => visualAperture.Add(new Face3DObject(x, Query.SurfaceAppearance(aperture, aperturePart))));
                        }

                        visualPanel.Add(visualAperture);
                    }
                }

                result.Add(visualPanel);
            }


            return result;
        }
    }
}
