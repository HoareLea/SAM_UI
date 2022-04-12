using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditInternalConditions(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;

            List<InternalCondition> internalConditions = adjacencyCluster.GetInternalConditions(false, true)?.ToList();
            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary(uIAnalyticalModel.JSAMObject.Name);
            internalConditions?.ForEach(x => internalConditionLibrary.Add(x));

            using (Windows.Forms.InternalConditionLibraryForm internalConditionLibraryForm = new Windows.Forms.InternalConditionLibraryForm(internalConditionLibrary, profileLibrary, adjacencyCluster))
            {
                internalConditionLibraryForm.Text = "Internal Conditions";
                if (internalConditionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                internalConditionLibrary = internalConditionLibraryForm.InternalConditionLibrary;
                profileLibrary = internalConditionLibraryForm.ProfileLibrary;
                adjacencyCluster = internalConditionLibraryForm.AdjacencyCluster;
            }

            internalConditions = internalConditionLibrary?.GetInternalConditions();
            if(internalConditions == null || internalConditions.Count == 0)
            {
                return;
            }

            internalConditions.ForEach(x => adjacencyCluster.AddObject(x));

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}