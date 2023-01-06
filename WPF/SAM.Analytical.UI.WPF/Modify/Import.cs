using SAM.Core.UI;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Import(this UIAnalyticalModel uIAnalyticalModel)
        {
            if (uIAnalyticalModel == null)
            {
                return;
            }

            string path;
            using (OpenFileDialog saveFileDialog = new OpenFileDialog())
            {
                saveFileDialog.Filter = "Honeybee json files (*.hbjson)|*.hbjson|TAS TBD files (*.tbd)|*.tbd|gbXML files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                path = saveFileDialog.FileName;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            AnalyticalModel analyticalModel = null;

            string extension = System.IO.Path.GetExtension(path);
            if (extension.ToLower().EndsWith("tbd"))
            {
                analyticalModel = Tas.Convert.ToSAM(path, false);
            }
            else if (extension.ToLower().EndsWith("xml"))
            {
                analyticalModel = gbXML.Create.AnalyticalModel(path);
            }
            else if (extension.ToLower().EndsWith("hbjson"))
            {
                analyticalModel = SAM.Analytical.LadybugTools.Convert.ToSAM(path) as AnalyticalModel;
            }

            if (analyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}