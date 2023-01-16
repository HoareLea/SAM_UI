using SAM.Core.UI.WPF;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void ConvertToTBD(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "TAS TBD files (*.tbd)|*.tbd|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = analyticalModel.Name;
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                path = saveFileDialog.FileName;
            }

            bool converted = false;

            using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Convert to TBD", "Converting..."))
            {
                try
                {
                    converted = Tas.TM59.Convert.ToTBD(analyticalModel, path, Analytical.Query.DefaultInternalConditionTextMap_TM59());
                }
                catch
                {
                    converted = false;
                }
            }

            if(converted)
            {
                MessageBox.Show("Model successfuly converted.");
            }
            else
            {
                MessageBox.Show("Model could not be converted.");
            }
        }
    }
}