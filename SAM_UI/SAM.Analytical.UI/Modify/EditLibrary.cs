using SAM.Core;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditLibrary(IWin32Window owner = null)
        {
            string path = null;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM", "resources", "Analytical");
                if (System.IO.Directory.Exists(directory))
                {
                    openFileDialog.InitialDirectory = directory;
                }

                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            List<ISAMLibrary> libraries = null;
            try
            {
                libraries = Core.Convert.ToSAM<ISAMLibrary>(path);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Could not load file");
            }

            if (libraries == null || libraries.Count == 0)
            {
                return;
            }

            ISAMLibrary library = libraries.FirstOrDefault();

            if (library is ConstructionLibrary)
            {
                DialogResult dialogResult = MessageBox.Show(owner, "Use default Material Library?", "Material Library", MessageBoxButtons.YesNo);

                MaterialLibrary materialLibrary = null;
                if (dialogResult == DialogResult.Yes)
                {
                    materialLibrary = Analytical.Query.DefaultMaterialLibrary();
                }
                else if (dialogResult == DialogResult.No)
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAM");
                        if (System.IO.Directory.Exists(directory))
                        {
                            openFileDialog.InitialDirectory = directory;
                        }

                        openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                        openFileDialog.FilterIndex = 2;
                        openFileDialog.RestoreDirectory = true;
                        if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
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
                    if (constructionLibraryForm.ShowDialog(owner) != DialogResult.OK)
                    {
                        return;
                    }

                    library = constructionLibraryForm.ConstructionLibrary;
                }
            }
            else if (library is MaterialLibrary)
            {
                using (MaterialLibraryForm materialLibraryForm = new MaterialLibraryForm((MaterialLibrary)library, Core.Query.Enums(typeof(IMaterial))))
                {
                    if (materialLibraryForm.ShowDialog(owner) != DialogResult.OK)
                    {
                        return;
                    }

                    library = materialLibraryForm.MaterialLibrary;
                }
            }

            if (library == null)
            {
                MessageBox.Show("Library type is not supported. Please select different type of library.");
                return;
            }

            Core.Convert.ToFile(new IJSAMObject[] { library }, path);
        }
    }
}