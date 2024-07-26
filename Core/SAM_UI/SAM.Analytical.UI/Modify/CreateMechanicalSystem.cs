using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static MechanicalSystem CreateMechanicalSystem(this UIAnalyticalModel uIAnalyticalModel, MechanicalSystemType mechanicalSystemType = null, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }

            if(mechanicalSystemType == null)
            {
                List<MechanicalSystemType> mechanicalSystemTypes = adjacencyCluster.GetMechanicalSystemTypes<MechanicalSystemType>();
                if(mechanicalSystemTypes == null || mechanicalSystemTypes.Count == 0)
                {
                    mechanicalSystemTypes = Analytical.Query.DefaultSystemTypeLibrary().GetSystemTypes<MechanicalSystemType>();
                }

                if(mechanicalSystemTypes == null || mechanicalSystemTypes.Count == 0)
                {
                    return null;
                }

                using (ComboBoxForm<MechanicalSystemType> comboBoxForm = new ComboBoxForm<MechanicalSystemType>("Mechanical System Type", mechanicalSystemTypes, (MechanicalSystemType x) => x?.Name))
                {
                    if(comboBoxForm.ShowDialog(owner) != DialogResult.OK)
                    {
                        return null;
                    }

                    mechanicalSystemType = comboBoxForm.SelectedItem;
                }
            }

            if(mechanicalSystemType == null)
            {
                return null;
            }

            string id = Analytical.Create.Id(adjacencyCluster, mechanicalSystemType);

            MechanicalSystem mechanicalSystem = Analytical.Create.MechanicalSystem(mechanicalSystemType, id);

            using (Windows.Forms.MechanicalSystemForm mechanicalSystemForm = new Windows.Forms.MechanicalSystemForm(mechanicalSystem, uIAnalyticalModel.JSAMObject.AdjacencyCluster))
            {
                if (mechanicalSystemForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return null;
                }

                adjacencyCluster = mechanicalSystemForm.AdjacencyCluster;
                mechanicalSystem = mechanicalSystemForm.MechanicalSystem;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster);

            return mechanicalSystem;
        }
    }
}