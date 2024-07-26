using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static bool UpdateUKBRFile(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return false;
            }

            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "tplp2021 files (*.tplp2021)|*.tplp2021|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }
                path = openFileDialog.FileName;
            }

            bool result = Tas.Modify.UpdateUKBRFile(analyticalModel, path);

            return result;
        }
    }
}