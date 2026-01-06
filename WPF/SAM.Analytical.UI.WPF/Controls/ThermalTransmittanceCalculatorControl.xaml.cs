using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ThermalTransmittanceCalculatorControl.xaml
    /// </summary>
    public partial class ThermalTransmittanceCalculatorControl : UserControl
    {
        private ThermalTransmittanceCalculator thermalTransmittanceCalculator;

        public ThermalTransmittanceCalculatorControl()
        {
            InitializeComponent();
        }

        private void SetConstructions(IEnumerable<Construction> constructions)
        {
            ListView_Constructions.Items.Clear();

            if(constructions == null || constructions.Count() == 0)
            {
                return;
            }

            foreach(Construction construction in constructions)
            {
                if(construction == null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(construction.Name) ? "???" : construction.Name;
                
                ListViewItem listViewItem = new ListViewItem() { Content = name, Tag = construction};
                listViewItem.MouseDoubleClick += ListViewItem_MouseDoubleClick;

                ListView_Constructions.Items.Add(listViewItem);
            }

        }

        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object @object = (sender as ListViewItem)?.Tag;

            if(@object is Construction)
            {
                Edit((Construction)@object);
            }
            else if (@object is LayerThicknessCalculationData)
            {
                Edit((LayerThicknessCalculationData)@object);
            }
        }

        private void AddLayerThicknessCalculationData(LayerThicknessCalculationData layerThicknessCalculationData)
        {
            if(layerThicknessCalculationData == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName) ? "???" : layerThicknessCalculationData.ConstructionName;

            foreach(ListViewItem listViewItem in ListView_LayerThicknessCalculationDatas.Items)
            {
                LayerThicknessCalculationData layerThicknessCalculationData_Temp = listViewItem.Tag as LayerThicknessCalculationData;
                if(layerThicknessCalculationData_Temp == null)
                {
                    continue;
                }

                if(layerThicknessCalculationData_Temp.ConstructionName == layerThicknessCalculationData.ConstructionName)
                {
                    listViewItem.Tag = layerThicknessCalculationData;
                    return;
                }
            }

            ListViewItem listViewItem_New = new ListViewItem() { Content = name, Tag = layerThicknessCalculationData };
            listViewItem_New.MouseDoubleClick += ListViewItem_MouseDoubleClick;

            ListView_LayerThicknessCalculationDatas.Items.Add(listViewItem_New);
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return thermalTransmittanceCalculator?.ConstructionManager;
            }

            set
            {
                thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(value);
                SetConstructions(thermalTransmittanceCalculator.ConstructionManager?.Constructions);
            }
        }

        public Construction Construction
        {
            get
            {
                ListViewItem listViewItem = ListView_Constructions.SelectedItem as ListViewItem;
                if(listViewItem == null)
                {
                    return null;
                }

                return listViewItem.Tag as Construction;
            }
        }

        public LayerThicknessCalculationData LayerThicknessCalculationData
        {
            get
            {
                ListViewItem listViewItem = ListView_LayerThicknessCalculationDatas.SelectedItem as ListViewItem;
                if (listViewItem == null)
                {
                    return null;
                }

                return listViewItem.Tag as LayerThicknessCalculationData;
            }
        }

        public LayerThicknessCalculationData GetLayerThicknessCalculationData(string name)
        {
            string name_Temp = string.IsNullOrWhiteSpace(name) ? "???" : name;

            foreach (ListViewItem listViewItem in ListView_LayerThicknessCalculationDatas.Items)
            {
                LayerThicknessCalculationData layerThicknessCalculationData_Temp = listViewItem.Tag as LayerThicknessCalculationData;
                if (layerThicknessCalculationData_Temp == null)
                {
                    continue;
                }

                if (layerThicknessCalculationData_Temp.ConstructionName == name_Temp)
                {
                    return layerThicknessCalculationData_Temp;
                }
            }

            return null;
        }

        private void button_Add_Click(object sender, RoutedEventArgs e)
        {
            Construction construction = Construction;
            if(construction == null)
            {
                return;
            }

            Edit(construction);
        }

        private void Edit(Construction construction)
        {
            if(construction == null)
            {
                return;
            }

            LayerThicknessCalculationData layerThicknessCalculationData = GetLayerThicknessCalculationData(construction.Name);
            if (layerThicknessCalculationData == null)
            {
                layerThicknessCalculationData = Tas.Create.LayerThicknessCalculationData(construction, ConstructionManager?.MaterialLibrary);
                layerThicknessCalculationData.ThermalTransmittance = double.NaN;
            }

            LayerThicknessCalculationDataWindow layerThicknessCalculationDataWindow = new LayerThicknessCalculationDataWindow();
            layerThicknessCalculationDataWindow.ConstructionManager = ConstructionManager;
            layerThicknessCalculationDataWindow.LayerThicknessCalculationData = layerThicknessCalculationData;

            bool? dialogResult = layerThicknessCalculationDataWindow.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }

            AddLayerThicknessCalculationData(layerThicknessCalculationDataWindow.LayerThicknessCalculationData);
        }

        private void Edit(LayerThicknessCalculationData layerThicknessCalculationData)
        {
            if(layerThicknessCalculationData == null)
            {
                return;
            }

            LayerThicknessCalculationDataWindow layerThicknessCalculationDataWindow = new LayerThicknessCalculationDataWindow();
            layerThicknessCalculationDataWindow.ConstructionManager = ConstructionManager;
            layerThicknessCalculationDataWindow.LayerThicknessCalculationData = layerThicknessCalculationData;

            bool? dialogResult = layerThicknessCalculationDataWindow.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }

            AddLayerThicknessCalculationData(layerThicknessCalculationDataWindow.LayerThicknessCalculationData);
        }

        private void button_Remove_Click(object sender, RoutedEventArgs e)
        {
            LayerThicknessCalculationData layerThicknessCalculationData = LayerThicknessCalculationData;
            if(layerThicknessCalculationData == null)
            {
                return;
            }

            ListViewItem listViewItem = ListView_LayerThicknessCalculationDatas.SelectedItem as ListViewItem;
            if(listViewItem == null)
            {
                return;
            }

            if(MessageBox.Show("Are you sure you want to remove item?", "Remove", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            ListView_LayerThicknessCalculationDatas.Items.Remove(listViewItem);

        }

        private void button_Edit_Click(object sender, RoutedEventArgs e)
        {
            LayerThicknessCalculationData layerThicknessCalculationData = LayerThicknessCalculationData;
            if (layerThicknessCalculationData == null)
            {
                return;
            }

            Edit(layerThicknessCalculationData);
        }

        public List<LayerThicknessCalculationData> LayerThicknessCalculationDatas
        {
            get
            {
                List<LayerThicknessCalculationData> result = new List<LayerThicknessCalculationData>();

                foreach(ListViewItem listViewItem in ListView_LayerThicknessCalculationDatas.Items)
                {
                    LayerThicknessCalculationData layerThicknessCalculationData = listViewItem?.Tag as LayerThicknessCalculationData;
                    if(layerThicknessCalculationData == null)
                    {
                        continue;
                    }

                    result.Add(layerThicknessCalculationData);
                }

                return result;
            }
        }
    }
}
