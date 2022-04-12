using SAM.Analytical.Windows.Forms;
using SAM.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditPanel(this UIAnalyticalModel uIAnalyticalModel, Panel panel, IWin32Window owner = null)
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

            ConstructionLibrary constructionLibrary = null;

            List<Construction> constructions = adjacencyCluster?.GetConstructions();
            if (constructions != null)
            {
                constructionLibrary = new ConstructionLibrary(analyticalModel.Name);
                constructions.ForEach(x => constructionLibrary.Add(x));
            }

            using (PanelForm panelForm = new PanelForm(panel, materialLibrary, constructionLibrary, Core.Query.Enums(typeof(Panel))))
            {
                if (panelForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                panel = panelForm.Panel;
                constructionLibrary = panelForm.ConstructionLibrary;
            }

            adjacencyCluster.AddObject(panel);

            adjacencyCluster.ReplaceConstructions(constructionLibrary);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
    }
}