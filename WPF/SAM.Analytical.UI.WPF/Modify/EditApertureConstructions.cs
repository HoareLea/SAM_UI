using SAM.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditApertureConstructions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            ApertureConstructionLibrary apertureConstructionLibrary = new ApertureConstructionLibrary(uIAnalyticalModel.JSAMObject.Name);
            apertureConstructions?.ForEach(x => apertureConstructionLibrary.Add(x));

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;

            using (Analytical.Windows.Forms.ApertureConstructionLibraryForm apertureConstructionLibraryForm = new Analytical.Windows.Forms.ApertureConstructionLibraryForm(materialLibrary, apertureConstructionLibrary))
            {
                apertureConstructionLibraryForm.Text = "Aperture Constructions";
                if (apertureConstructionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                apertureConstructionLibrary = apertureConstructionLibraryForm.ApertureConstructionLibrary;
            }

            adjacencyCluster.ReplaceApertureConstructions(apertureConstructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }
    }
}