using SAM.Core;
using SAM.Core.Windows.Forms;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditAddressAndLocation(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            using (AddressAndLocationForm addressAndLocationForm = new AddressAndLocationForm(analyticalModel.Address, analyticalModel.Location))
            {
                if (addressAndLocationForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                analyticalModel = new AnalyticalModel(analyticalModel.Name, analyticalModel.Description, addressAndLocationForm.Location, addressAndLocationForm.Address, analyticalModel.AdjacencyCluster);
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}