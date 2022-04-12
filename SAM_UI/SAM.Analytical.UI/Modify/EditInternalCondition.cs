using SAM.Analytical.Windows.Forms;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditInternalCondition(this UIAnalyticalModel uIAnalyticalModel, InternalCondition internalCondition, IWin32Window owner = null)
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

            ProfileLibrary profileLibrary= analyticalModel.ProfileLibrary;


            using (InternalConditionForm internalConditionForm = new InternalConditionForm(internalCondition, profileLibrary, adjacencyCluster))
            {
                if (internalConditionForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                internalCondition = internalConditionForm.InternalCondition;
            }


            if(internalCondition == null)
            {
                return;
            }

            adjacencyCluster.AddObject(internalCondition);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
    }
}