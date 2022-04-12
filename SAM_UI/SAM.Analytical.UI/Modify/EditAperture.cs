using SAM.Analytical.Windows.Forms;
using SAM.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditAperture(this UIAnalyticalModel uIAnalyticalModel, Aperture aperture, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

            ApertureConstructionLibrary apertureConstructionLibrary = null;

            List<ApertureConstruction> apertureConstructions = adjacencyCluster?.GetApertureConstructions();
            if (apertureConstructions != null)
            {
                apertureConstructionLibrary = new ApertureConstructionLibrary(analyticalModel.Name);
                apertureConstructions.ForEach(x => apertureConstructionLibrary.Add(x));
            }

            using (ApertureForm apertureForm = new ApertureForm(aperture, materialLibrary, apertureConstructionLibrary, Core.Query.Enums(typeof(Aperture))))
            {
                if (apertureForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                aperture = apertureForm.Aperture;
                apertureConstructionLibrary = apertureForm.ApertureConstructionLibrary;
            }

            Panel panel = adjacencyCluster.GetPanel(aperture);
            if (panel != null)
            {
                panel.RemoveAperture(aperture.Guid);
                panel.AddAperture(aperture);
                adjacencyCluster.AddObject(aperture);
            }

            adjacencyCluster.ReplaceApertureConstructions(apertureConstructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
    }
}