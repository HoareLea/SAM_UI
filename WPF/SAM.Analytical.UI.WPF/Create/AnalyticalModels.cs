// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Microsoft.Win32;
using SAM.Analytical.Classes;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;


namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        public static List<AnalyticalModel>? AnalyticalModels<TSAMObject>(this AnalyticalModel? analyticalModel, IEnumerable<TSAMObject>? sAMObjects_Selected = null) where TSAMObject : ISAMObject
        {
            if (analyticalModel is null)
            {
                return null;
            }

            bool? dialogResult;

            CreateCasesWindow createCasesWindow = new()
            {
                AnalyticalModel = analyticalModel,
                SelectedSAMObjects = sAMObjects_Selected?.Cast<SAMObject>()?.ToList() ?? null
            };

            dialogResult = createCasesWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return null;
            }

            List<Cases> cases = createCasesWindow.Cases;
            if (cases is null)
            {
                return null;
            }

            List<AnalyticalModel> result = UI.Create.AnalyticalModels(analyticalModel, cases);
            if(result == null || result.Count == 0)
            {
                return null;
            }


            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return null;
            }

            string directory = openFolderDialog.FolderName;

            int index = 1;
            foreach(AnalyticalModel analyticalModel_Temp in result)
            {
                string name = index.ToString();

                if(analyticalModel_Temp.TryGetValue("CaseDescription", out string caseDescription) && !string.IsNullOrWhiteSpace(caseDescription))
                {
                    name += string.Format(" {0}", caseDescription);
                }

                string path = System.IO.Path.Combine(directory, string.Format("{0}.json", name));

                Core.Convert.ToFile(analyticalModel_Temp, path);
                //result.Add(analyticalModel_Temp);
                index++;
            }

            if (result.Count > 0)
            {
                MessageBox.Show("Analytical Model created successfuly");
            }
            else
            {
                MessageBox.Show("Failed to create Analytical Model");
            }

            return result;
        }
    }
}
