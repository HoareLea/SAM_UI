using SAM.Analytical.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditSpaces(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            ProfileLibrary profileLibrary = analyticalModel.ProfileLibrary;


            IEnumerable<Space> spaces = null;
            using (SpacesForm spacesForm = new SpacesForm(adjacencyCluster.GetSpaces(), analyticalModel.AdjacencyCluster, analyticalModel.ProfileLibrary))
            {
                spacesForm.StartPosition = FormStartPosition.CenterParent;
                if (spacesForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                spaces = spacesForm.Spaces;
                adjacencyCluster = spacesForm.AdjacencyCluster;
                profileLibrary = spacesForm.ProfileLibrary;
            }

            if(spaces != null && spaces.Count() != 0 && adjacencyCluster != null)
            {
                foreach(Space space in spaces)
                {
                    adjacencyCluster.AddObject(space);
                }
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, profileLibrary);
        }
    }
}