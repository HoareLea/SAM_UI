using Microsoft.Win32;
using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows;


namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        public static List<AnalyticalModel>? AnalyticalModels(this AnalyticalModel? analyticalModel)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            CreateCaseByWindowSizeWindow createCaseByWindowSizeWindow = new()
            {
                WindowSizeCases = [ new WindowSizeCase(0.8, null)]
            };

            bool? dialogResult = null;

            dialogResult = createCaseByWindowSizeWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
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

            List<AnalyticalModel> result = [];

            int index = 1;
            foreach(WindowSizeCase windowSizeCase in createCaseByWindowSizeWindow.WindowSizeCases)
            {
                AnalyticalModel analyticalModel_Temp = Analytical.Create.AnalyticalModel_ByWindowSize(analyticalModel, windowSizeCase.ApertureScaleFactor);
                if(analyticalModel_Temp == null)
                {
                    continue;
                }

                string name = index.ToString();

                if(analyticalModel_Temp.TryGetValue("CaseDescription", out string caseDescription) && !string.IsNullOrWhiteSpace(caseDescription))
                {
                    name += string.Format(" {0}", caseDescription);
                }

                string path = System.IO.Path.Combine(directory, string.Format("{0}.json", name));

                Core.Convert.ToFile(analyticalModel_Temp, path);
                result.Add(analyticalModel_Temp);
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