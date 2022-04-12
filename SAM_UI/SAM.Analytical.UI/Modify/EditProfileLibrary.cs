﻿using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditProfileLibrary(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return;
            }

            ProfileLibrary profileLibrary = uIAnalyticalModel.JSAMObject.ProfileLibrary;

            using (Windows.Forms.ProfileLibraryForm profileLibraryForm = new Windows.Forms.ProfileLibraryForm(profileLibrary))
            {
                if (profileLibraryForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                profileLibrary = profileLibraryForm.ProfileLibrary;
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.JSAMObject.AdjacencyCluster, uIAnalyticalModel.JSAMObject.MaterialLibrary, profileLibrary);
        }
    }
}