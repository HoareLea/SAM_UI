// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Windows.Forms;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditMaterial(this UIAnalyticalModel uIAnalyticalModel, IMaterial material, IWin32Window owner = null)
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

            string uniqueId = materialLibrary.GetUniqueId(material);


            using (MaterialForm materialForm = new MaterialForm(material, Core.Query.Enums(typeof(IMaterial))))
            {
                if (materialForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                material = materialForm.Material;
            }

            materialLibrary.Replace(uniqueId, material);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, analyticalModel.AdjacencyCluster, materialLibrary, analyticalModel.ProfileLibrary);
        }
    }
}
