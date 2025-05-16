using SAM.Core.UI.WPF;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static bool Export(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return false;
            }

            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Honeybee json files (*.hbjson)|*.hbjson|TAS TBD files (*.tbd)|*.tbd|gbXML files (*.xml)|*.xml|GEM files (*.gem)|*.gem|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = analyticalModel.Name;
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }

                path = saveFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            bool result = false;

            using (ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager("Export", "Exporting"))
            {
                string extension = System.IO.Path.GetExtension(path);
                if (extension.ToLower().EndsWith("tbd"))
                {
                    result = Tas.Convert.ToTBD(analyticalModel, path);
                }
                else if (extension.ToLower().EndsWith("gbxml"))
                {
                    gbXMLSerializer.gbXML gbXML = Analytical.gbXML.Convert.TogbXML(analyticalModel);
                    if (gbXML != null)
                    {
                        result = Core.gbXML.Create.gbXML(gbXML, path);
                    }
                }
                else if (extension.ToLower().EndsWith("hbjson"))
                {
                    HoneybeeSchema.Model model = LadybugTools.Convert.ToLadybugTools(analyticalModel);
                    string json = model.ToJson();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        System.IO.File.WriteAllText(path, json);
                        result = true;
                    }
                }
                else if (extension.ToLower().EndsWith("gem"))
                {
                    string gem = GEM.Convert.ToGEM(analyticalModel);
                    if (!string.IsNullOrWhiteSpace(gem))
                    {
                        System.IO.File.WriteAllText(path, gem);
                        result = true;
                    }
                }
            }

            return result;

        }
    }
}