// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void ReplaceNameSpecialCharacters(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            List<string> names = ActiveManager.GetSpecialCharacterMapNames();
            if (names == null || names.Count == 0)
            {
                return;
            }

            string name = null;

            using (ComboBoxForm<string> comboBoxForm = new ("Select language", names))
            {
                comboBoxForm.SelectedItem = names.Find(x => x == "ISO");

                if(comboBoxForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return; 
                }

                name = comboBoxForm.SelectedItem;
            }

            if(string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            adjacencyCluster.ReplaceNameSpecialCharacters(name);

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}