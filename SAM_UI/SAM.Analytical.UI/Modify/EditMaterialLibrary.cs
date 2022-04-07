using SAM.Core;
using SAM.Core.Windows.Forms;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditMaterialLibrary(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return;
            }

            MaterialLibrary materialLibrary = uIAnalyticalModel.JSAMObject.MaterialLibrary;

            using (MaterialLibraryForm materialLibraryForm = new MaterialLibraryForm(materialLibrary, Core.Query.Enums(typeof(IMaterial))))
            {
                if (materialLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                materialLibrary = materialLibraryForm.MaterialLibrary;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.JSAMObject.AdjacencyCluster, materialLibrary, uIAnalyticalModel.JSAMObject.ProfileLibrary);
        }
    }
}