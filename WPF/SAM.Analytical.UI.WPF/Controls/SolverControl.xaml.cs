using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SolverControl.xaml
    /// </summary>
    public partial class SolverControl : System.Windows.Controls.UserControl
    {
        private List<double> levels = [];

        public SolverControl()
        {
            InitializeComponent();

            Load();
        }

        private void Load()
        {
            SetFilter();

            foreach (PanelType panelType in System.Enum.GetValues(typeof(PanelType)))
            {
                if (panelType == PanelType.Undefined)
                {
                    continue;
                }

                string item = Core.Query.Description(panelType);

                int index = listBox_ExcludedPanelTypes.Items.Add(item);

                PanelGroup panelGroup = Analytical.Query.PanelGroup(panelType);

                if (panelGroup == PanelGroup.Floor || panelGroup == PanelGroup.Roof)
                {
                    listBox_ExcludedPanelTypes.SelectedItems.Add(listBox_ExcludedPanelTypes.Items[index]);
                }
            }
        }

        private void SetFilter()
        {
            textBox_MinArea.IsEnabled = false;
            textBox_MinThinnessRatio.IsEnabled = false;

            if (checkBox_Filter.IsChecked.HasValue && checkBox_Filter.IsChecked.Value)
            {
                textBox_MinArea.IsEnabled = true;
                textBox_MinThinnessRatio.IsEnabled = true;
            }
        }

        private void checkBox_Filter_Click(object sender, RoutedEventArgs e)
        {
            SetFilter();
        }

        private void button_SelectLevels_Click(object sender, RoutedEventArgs e)
        {
            SelectLevelsWindow selectLevelsWindow = new SelectLevelsWindow();
            selectLevelsWindow.SetLevels(levels);
            if(selectLevelsWindow.ShowDialog() != true)
            {
                return;
            }

            levels = selectLevelsWindow.GetLevels();
        }

        private void button_ExcludedPanels_Click(object sender, RoutedEventArgs e)
        {

        }

        public List<PanelType> ExcludedPanelTypes
        {
            get
            {
                List<PanelType> excludedPanelTypes = [];
                foreach (var selectedItem in listBox_ExcludedPanelTypes.SelectedItems)
                {
                    string description = selectedItem.ToString() ?? string.Empty;
                    PanelType panelType = Core.Query.Enum<PanelType>(description);
                    excludedPanelTypes.Add(panelType);
                }
                return excludedPanelTypes;
            }
        }

        public bool RemovePanelInternalEdges
        {
            get
            {
                return checkBox_RemovePanelInternalEdges.IsChecked.HasValue && checkBox_RemovePanelInternalEdges.IsChecked.Value;
            }
        }

        public bool FilterPanels
        {
            get
            {
                return checkBox_Filter.IsChecked.HasValue && checkBox_Filter.IsChecked.Value;
            }
        }

        public double MinArea
        {
            get
            {
                if (!FilterPanels || !double.TryParse(textBox_MinArea.Text, out double result))
                {
                    result = double.NaN;
                }

                return result;
            }
        }

        public double MinThinnessRatio
        {
            get
            {
                if (!FilterPanels || !double.TryParse(textBox_MinThinnessRatio.Text, out double result))
                {
                    result = double.NaN;
                }

                return result;
            }
        }

        public double BucketSize_SingleLevel
        {
            get
            {
                if (!double.TryParse(textBox_BucketSize_SingleLevel.Text, out double result))
                {
                    result = double.NaN;
                }
                return result;

            }
        }


        public double BucketSize_MultipleLevel
        {
            get
            {
                if (!double.TryParse(textBox_BucketSize_MultipleLevel.Text, out double result))
                {
                    result = double.NaN;
                }
                return result;
            }
        }

        public double Weight
        {
            get
            {
                if (!double.TryParse(textBox_Weight.Text, out double result))
                {
                    result = double.NaN;
                }
                return result;
            }

        }

        public double MaxExtension
        {
            get
            {
                if (!double.TryParse(textBox_MaxExtension.Text, out double result))
                {
                    result = double.NaN;
                }
                return result;
            }
        }

        public double LevelOffset
        {
            get
            {
                if (!double.TryParse(textBox_LevelOffset.Text, out double result))
                {
                    result = double.NaN;
                }
                return result;
            }
        }

        public List<double> Levels
        {
            get
            {
                return levels;
            }

            set
            {
                levels = value;
            }
        }
    }
}
