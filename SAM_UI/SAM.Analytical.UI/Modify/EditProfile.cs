// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditProfile(this UIAnalyticalModel uIAnalyticalModel, Profile profile, IWin32Window owner = null)
        {
            if (uIAnalyticalModel?.JSAMObject == null)
            {
                return;
            }

            ProfileLibrary profileLibrary = uIAnalyticalModel.JSAMObject.ProfileLibrary;

            using (Windows.Forms.ProfileForm profileForm = new Windows.Forms.ProfileForm(profile))
            {
                profileForm.ProfileLibrary = profileLibrary;

                if (profileForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                profileLibrary = profileForm.ProfileLibrary;
                profile = profileForm.Profile;
                profileLibrary?.Add(profile);
            }

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(uIAnalyticalModel.JSAMObject, uIAnalyticalModel.JSAMObject.AdjacencyCluster, uIAnalyticalModel.JSAMObject.MaterialLibrary, profileLibrary);
        }
    }
}
