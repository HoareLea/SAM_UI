using SAM.Analytical.Tas;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for GlazingCalculationResultControl.xaml
    /// </summary>
    public partial class GlazingCalculationResultControl : UserControl
    {
        private ConstructionManager constructionManager;
        private GlazingCalculationData glazingCalculationData;

        private List<GlazingCalculationResult> glazingCalculationResults;

        public GlazingCalculationResultControl()
        {
            InitializeComponent();
        }

        private void SetGlazingCalculationResults(IEnumerable<GlazingCalculationResult> glazingCalculationResults)
        {
            this.glazingCalculationResults = null;

            DataGrid_Main.ItemsSource = null;
            DataGrid_Current.ItemsSource = null;

            if (glazingCalculationResults == null)
            {
                return;
            }

            this.glazingCalculationResults = glazingCalculationResults.ToList();

            string reference = glazingCalculationData.ConstructionGuid.ToString();

            List<DisplayGlazingCalculationResult> displayGlazingCalculationResults_Main = new List<DisplayGlazingCalculationResult>();
            List<DisplayGlazingCalculationResult> displayGlazingCalculationResults_Current = new List<DisplayGlazingCalculationResult>();

            foreach (GlazingCalculationResult glazingCalculationResult in glazingCalculationResults)
            {
                if (glazingCalculationResult == null)
                {
                    continue;
                }

                DisplayGlazingCalculationResult displayGlazingCalculationResult = new DisplayGlazingCalculationResult(glazingCalculationResult) { ConstructionManager = constructionManager, GlazingCalculationData = glazingCalculationData };

                if (glazingCalculationResult.Reference == reference)
                {
                    displayGlazingCalculationResults_Current = new List<DisplayGlazingCalculationResult>() { displayGlazingCalculationResult };
                }
                else
                {
                    displayGlazingCalculationResults_Main.Add(displayGlazingCalculationResult);
                }
            }

            if(displayGlazingCalculationResults_Current != null && displayGlazingCalculationResults_Current.Count != 0)
            {
                if (CheckBox_ShowAllTypes.IsChecked == null || !CheckBox_ShowAllTypes.IsChecked.HasValue || !CheckBox_ShowAllTypes.IsChecked.Value)
                {
                    DisplayGlazingCalculationResult displayGlazingCalculationResult = displayGlazingCalculationResults_Current[0];
                    displayGlazingCalculationResults_Main.RemoveAll(x => x.PanelGroup != displayGlazingCalculationResult?.PanelGroup);
                }
            }

            displayGlazingCalculationResults_Main.AssignIndexes();
            displayGlazingCalculationResults_Main.SortByIndex();

            DataGrid_Main.ItemsSource = displayGlazingCalculationResults_Main;
            DataGrid_Current.ItemsSource = displayGlazingCalculationResults_Current;

            if(DataGrid_Main.ItemsSource != null)
            {
                foreach(DisplayGlazingCalculationResult displayGlazingCalculationResult in DataGrid_Main.ItemsSource)
                {
                    DataGrid_Main.SelectedItem = displayGlazingCalculationResult;
                    DataGrid_Main.Focus();
                    break;
                }
            }
        } 

        private List<GlazingCalculationResult> GetGlazingCalculationResults()
        {
            List<GlazingCalculationResult> result = new List<GlazingCalculationResult>();

            foreach (object @object in DataGrid_Current.ItemsSource)
            {
                if (@object is DisplayGlazingCalculationResult)
                {
                    result.Add(((DisplayGlazingCalculationResult)@object).GlazingCalculationResult);
                }
            }

            foreach (object @object in DataGrid_Main.ItemsSource)
            {
                if (@object is DisplayGlazingCalculationResult)
                {
                     result.Add(((DisplayGlazingCalculationResult)@object).GlazingCalculationResult);
                }
            }

            return result;
        }

        private void SetConstructionManager(ConstructionManager constructionManager)
        {
            this.constructionManager = constructionManager;

            if(DataGrid_Current?.ItemsSource != null)
            {
                foreach(DisplayGlazingCalculationResult displayGlazingCalculationResult in DataGrid_Current.ItemsSource)
                {
                    displayGlazingCalculationResult.ConstructionManager = constructionManager;
                }
            }

            if(DataGrid_Main?.ItemsSource != null)
            {
                foreach (DisplayGlazingCalculationResult displayGlazingCalculationResult in DataGrid_Main.ItemsSource)
                {
                    displayGlazingCalculationResult.ConstructionManager = constructionManager;
                }
            }
        }

        private void SetGlazingCalculationData(GlazingCalculationData glazingCalculationData)
        {
            this.glazingCalculationData = glazingCalculationData;

            if (DataGrid_Current?.ItemsSource != null)
            {
                foreach (DisplayGlazingCalculationResult displayGlazingCalculationResult in DataGrid_Current.ItemsSource)
                {
                    displayGlazingCalculationResult.GlazingCalculationData = glazingCalculationData;
                }
            }

            if(DataGrid_Main.ItemsSource != null)
            {
                foreach (DisplayGlazingCalculationResult displayGlazingCalculationResult in DataGrid_Main.ItemsSource)
                {
                    displayGlazingCalculationResult.GlazingCalculationData = glazingCalculationData;
                }
            }
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return constructionManager;
            }

            set
            {
                SetConstructionManager(value);

            }
        }

        public List<GlazingCalculationResult> GlazingCalculationResults
        {
            get
            {
                return GetGlazingCalculationResults();
            }

            set
            {
                SetGlazingCalculationResults(value);
            }
        }

        public GlazingCalculationData GlazingCalculationData
        {
            get
            {
                return glazingCalculationData;
            }

            set
            {
                SetGlazingCalculationData(value);
            }
        }

        public GlazingCalculationResult GlazingCalculationResult
        {
            get
            {
                return (DataGrid_Main.SelectedItem as DisplayGlazingCalculationResult)?.GlazingCalculationResult;
            }
        }

        private void CheckBox_ShowAllTypes_Checked(object sender, RoutedEventArgs e)
        {
            SetGlazingCalculationResults(glazingCalculationResults);
        }
    }
}
