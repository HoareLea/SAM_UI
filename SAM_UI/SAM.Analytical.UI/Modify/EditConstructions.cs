using SAM.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditConstructions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AdjacencyCluster adjacencyCluster = uIAnalyticalModel.JSAMObject.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<Construction> constructions = adjacencyCluster.GetConstructions();

            ConstructionLibrary constructionLibrary = new ConstructionLibrary(uIAnalyticalModel.JSAMObject.Name);
            constructions?.ForEach(x => constructionLibrary.Add(x));

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;

            using (Windows.Forms.ConstructionLibraryForm constructionLibraryForm = new Windows.Forms.ConstructionLibraryForm(materialLibrary, constructionLibrary))
            {
                constructionLibraryForm.Text = "Constructions";
                if (constructionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                constructionLibrary = constructionLibraryForm.ConstructionLibrary;
            }

            adjacencyCluster.ReplaceConstructions(constructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, adjacencyCluster);
        }
    }
}