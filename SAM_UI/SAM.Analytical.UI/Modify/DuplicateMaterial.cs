using SAM.Core;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void DuplicateMaterial(this UIAnalyticalModel uIAnalyticalModel, IMaterial material, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;
            if (materialLibrary == null)
            {
                return;
            }

            material = Core.Windows.Modify.Duplicate(materialLibrary, material, owner, Core.Query.Enums(typeof(IMaterial)));
            if (material == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, analyticalModel.AdjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);
        }
    }
}