using Microsoft.Win32;
using System.Windows;


namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        public static AnalyticalModel? AnalyticalModel_ByWindowSize(this AnalyticalModel? analyticalModel, string fileName)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            if(string.IsNullOrEmpty(fileName))
            {

            }

            CreateCaseByWindowSizeWindow createCaseByWindowSizeWindow = new()
            {
                ApertureScaleFactor = 0.8
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

            AnalyticalModel result = Analytical.Create.AnalyticalModel_ByWindowSize(analyticalModel, createCaseByWindowSizeWindow.ApertureScaleFactor);

            string path = System.IO.Path.Combine(directory, fileName);

            bool success = Core.Convert.ToFile(result, path);
            if (success)
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