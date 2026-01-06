using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void SaveAs(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if(uIAnalyticalModel == null)
            {
                return;
            }

            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                
                if(!string.IsNullOrWhiteSpace(uIAnalyticalModel.Path))
                {
                    saveFileDialog.FileName = System.IO.Path.GetFileName(uIAnalyticalModel.Path);
                    saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path);
                }

                if (saveFileDialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                path = saveFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            uIAnalyticalModel.Path = path;

            uIAnalyticalModel.Save();
        }
    }
}