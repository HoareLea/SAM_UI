using SAM.Core;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void RemoveMaterial(this UIAnalyticalModel uIAnalyticalModel, IMaterial material)
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

            materialLibrary.Remove(material);
            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, analyticalModel.AdjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);
        }
    }
}