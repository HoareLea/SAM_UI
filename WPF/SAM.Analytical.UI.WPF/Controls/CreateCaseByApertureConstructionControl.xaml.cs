using SAM.Analytical.Classes;
using SAM.Core;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByApertureConstructionControl.xaml
    /// </summary>
    public partial class CreateCaseByApertureConstructionControl : UserControl
    {
        public CreateCaseByApertureConstructionControl()
        {
            InitializeComponent();

            Add();
        }

        public IEnumerable<ApertureConstructionCase>? ApertureConstructionCases
        {
            get
            {
                List<ApertureConstructionCase> result = [];
                foreach (object children in StackPanel_Main.Children)
                {
                    if (children is not SelectSAMObjectComboBoxControl selectSAMObjectComboBoxControl)
                    {
                        continue;
                    }

                    if (selectSAMObjectComboBoxControl.GetJSAMObject<ApertureConstruction>() is ApertureConstruction apertureConstruction)
                    {
                        result.Add(new ApertureConstructionCase(apertureConstruction, null));
                    }
                }

                return result;
            }

            set
            {
                StackPanel_Main.Children.Clear();

                if (value == null)
                {
                    return;
                }

                foreach (ApertureConstructionCase apertureConstructionCase in value)
                {
                    if (apertureConstructionCase.ApertureConstruction is not ApertureConstruction apertureConstruction)
                    {
                        continue;
                    }

                    SelectSAMObjectComboBoxControl selectSAMObjectComboBoxControl = Add();
                    if (selectSAMObjectComboBoxControl is null)
                    {
                        continue;
                    }

                    selectSAMObjectComboBoxControl.Add(apertureConstructionCase.ApertureConstruction?.Name, apertureConstructionCase.ApertureConstruction);
                    selectSAMObjectComboBoxControl.SelectedText = apertureConstructionCase.ApertureConstruction?.Name;
                }
            }
        }

        private SelectSAMObjectComboBoxControl Add()
        {
            SelectSAMObjectComboBoxControl result = new()
            {
                Margin = new Thickness(0, 5, 0, 5),
                Width = 200
            };

            result.ValidateFunc = new Func<IJSAMObject, bool>(x => x is ApertureConstruction);
            result.ReadFunc = new Func<string, IJSAMObject>(x =>
            {
                if(string.IsNullOrWhiteSpace(x))
                {
                    return null;
                }

                string json = System.IO.File.ReadAllText(x);
                if(string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                List<IJSAMObject> jSAMObjects = Core.Create.IJSAMObjects<IJSAMObject>(json);
                if(jSAMObjects == null || !jSAMObjects.Any())
                {
                    return null;
                }

                List<ApertureConstruction> apertureConstructions = [];
                foreach (IJSAMObject jSAMObject in jSAMObjects)
                {
                    if(jSAMObject is ApertureConstruction apertureConstruction)
                    {
                        apertureConstructions.Add(apertureConstruction);
                    }
                    else if(jSAMObject is AnalyticalModel analyticalModel)
                    {
                        analyticalModel.AdjacencyCluster?.ApertureConstructions()?.ForEach(x => apertureConstructions.Add(x));
                    }
                    else if(jSAMObject is ApertureConstructionLibrary apertureConstructionLibrary)
                    {
                        apertureConstructionLibrary.GetApertureConstructions()?.ForEach(x => apertureConstructions.Add(x));

                    }
                }

                if(apertureConstructions.Count == 0)
                {
                    return null;
                }

                if(apertureConstructions.Count == 1)
                {
                    return apertureConstructions[0];
                }

                Core.Windows.Forms.SearchForm<ApertureConstruction> searchForm = new Core.Windows.Forms.SearchForm<ApertureConstruction>("Aperture Constructions", apertureConstructions, x => x?.Name, false);
                if(searchForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }

                return searchForm.SelectedItems?.FirstOrDefault();

            });

            result.SelectionChanged += SelectSAMObjectComboBoxControl_SelectionChanged;

            StackPanel_Main.Children.Add(result);

            return result;
        }

        private void SelectSAMObjectComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.UI.WPF.SelectSAMObjectComboBoxControl? selectSAMObjectComboBoxControl = sender as SelectSAMObjectComboBoxControl;
            if (selectSAMObjectComboBoxControl == null)
            {
                return;
            }

            // Check if this is the last ComboBox in the panel
            if (StackPanel_Main.Children[StackPanel_Main.Children.Count - 1] == selectSAMObjectComboBoxControl)
            {
                // Only add if a value is selected
                if (selectSAMObjectComboBoxControl.GetJSAMObject<ApertureConstruction>() != null)
                {
                    Add();
                }
            }
        }
    }
}
