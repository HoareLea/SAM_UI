using SAM.Core;
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

            TextMap textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();

            using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Convert to TBD", "Converting..."))
            {
                converted = Tas.Convert.ToTBD(analyticalModel, path, null, null, null, true);
                if (converted)
                {
                    if (Tas.TM59.Modify.TryCreatePath(path, out string path_TM59))
                    {
                        Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, new TM59Manager(textMap));
                    }
                    string zoneCategory = "Flat";

                    if (string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        converted = false;
                    }
                    else
                    {
                        if (!Tas.SAP.Modify.TryCreatePath(path, out string path_SAP))
                        {
                            converted = false;
                        }
                        else
                        {
                            converted = Tas.SAP.Convert.ToFile(analyticalModel, path_SAP, zoneCategory, textMap);
                        }

                    }
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