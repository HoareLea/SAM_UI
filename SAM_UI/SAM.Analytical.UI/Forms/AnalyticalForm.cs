using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public partial class AnalyticalForm : Form
    {
        public AnalyticalForm()
        {
            InitializeComponent();
        }

        private void AnalyticalForm_Load(object sender, EventArgs e)
        {
        }

        private void AnalyticalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void Button_LoadLibrary_Click(object sender, EventArgs e)
        {
            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if(string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            List<ISAMLibrary> libraries = null;
            try
            {
                libraries = Core.Convert.ToSAM<ISAMLibrary>(path);
            }
            catch(Exception exception)
            {
                MessageBox.Show("Could not load file");
            }

            if(libraries == null || libraries.Count == 0)
            {
                return;
            }

            ISAMLibrary library = libraries.FirstOrDefault();

            if (library is ConstructionLibrary)
            {
                DialogResult dialogResult = MessageBox.Show(this, "Use default Material Library?", "Material Library", MessageBoxButtons.YesNo);

                MaterialLibrary materialLibrary = null;
                if (dialogResult == DialogResult.Yes)
                {
                    materialLibrary = Query.DefaultMaterialLibrary();
                }
                else if (dialogResult == DialogResult.No)
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                        openFileDialog.FilterIndex = 2;
                        openFileDialog.RestoreDirectory = true;
                        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                        {
                            return;
                        }

                        List<MaterialLibrary> materialLibraries = null;
                        try
                        {
                            materialLibraries = Core.Convert.ToSAM<MaterialLibrary>(openFileDialog.FileName);
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show("Could not load file");
                        }

                        if (materialLibraries == null || materialLibraries.Count == 0)
                        {
                            return;
                        }

                        materialLibrary = materialLibraries.FirstOrDefault();
                    }
                }
                else
                {
                    return;
                }

                using (Windows.Forms.ConstructionLibraryForm constructionLibraryForm = new Windows.Forms.ConstructionLibraryForm(materialLibrary, (ConstructionLibrary)library))
                {
                    if (constructionLibraryForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    library = constructionLibraryForm.ConstructionLibrary;
                }
            }
            else if(library is MaterialLibrary)
            {
                using(Core.Windows.Forms.MaterialLibraryForm materialLibraryForm = new Core.Windows.Forms.MaterialLibraryForm((MaterialLibrary)library, SAM.Core.Query.Enums(typeof(OpaqueMaterialParameter), typeof(TransparentMaterialParameter))))
                {
                    if(materialLibraryForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    library = materialLibraryForm.MaterialLibrary;
                }
            }

            if(library == null)
            {
                return;
            }

            Core.Convert.ToFile(new IJSAMObject[] { library }, path);
        }
    }
}
