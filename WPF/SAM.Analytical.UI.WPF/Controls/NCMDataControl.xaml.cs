using SAM.Core.UI.WPF;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for NCMDataControl.xaml
    /// </summary>
    public partial class NCMDataControl : UserControl
    {
        private string vary = "<Vary>";

        private NCMNameCollection nCMNameCollection;

        private List<NCMData> nCMDatas;

        public NCMDataControl()
        {
            InitializeComponent();

            //MultipleValueComboBoxControl_SystemType.SetDefaultValue();
            //MultipleValueComboBoxControl_LightingOccupancyControls.SetDefaultValue(Core.Query.Descriptions(LightingOccupancyControls.Undefined));
            //MultipleValueComboBoxControl_LightingPhotoelectricControls.SetDefaultValue(Core.Query.Descriptions(LightingPhotoelectricControls.Undefined));
        }

        private void Button_Name_Click(object sender, RoutedEventArgs e)
        {
            NCMNameCollection nCMNameCollection_Temp = nCMNameCollection;
            if (nCMNameCollection_Temp == null)
            {
                nCMNameCollection_Temp = Analytical.Query.DefaultNCMNameCollection();
            }

            NCMNameCollectionWindow nCMNameCollectionWindow = new NCMNameCollectionWindow(nCMNameCollection_Temp, new NCMNameCollectionOptions() { Editable = false });
            nCMNameCollectionWindow.NCMNameDoubleClicked += NCMNameCollectionWindow_NCMNameDoubleClicked;
            if (nCMNameCollectionWindow.ShowDialog() != true)
            {
                return;
            }

            NCMName nCMName = nCMNameCollectionWindow.SelectedNCMName;

            //Button_Name.Content = string.IsNullOrEmpty(nCMName?.FullName) ? "???" : nCMName.FullName.Replace("_", "__");
            //Button_Name.Tag = nCMName;
            MultipleValueComboBoxControl_Name.Value = string.IsNullOrEmpty(nCMName?.FullName) ? "???" : nCMName.FullName;
        }

        private void NCMNameCollectionWindow_NCMNameDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            NCMNameCollectionWindow nCMNameCollectionWindow = sender as NCMNameCollectionWindow;
            if(nCMNameCollectionWindow == null)
            {
                return;
            }

            NCMName nCMName = nCMNameCollectionWindow.SelectedNCMName;
            if(nCMName == null || string.IsNullOrWhiteSpace(nCMName))
            {
                return;
            }

            nCMNameCollectionWindow.DialogResult = true;
        }

        private List<NCMData> GetNCMDatas()
        {
            string @string = null;

            if(!MultipleValueComboBoxControl_Name.VarySet)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].NCMName = MultipleValueComboBoxControl_Name.Value;
                }
            }

            //NCMName nCMName = Button_Name.Tag as NCMName;
            //if (nCMName != null)
            //{
            //    for (int i = 0; i < nCMDatas.Count; i++)
            //    {
            //        if (nCMDatas[i] == null)
            //        {
            //            nCMDatas[i] = new NCMData();
            //        }

            //        nCMDatas[i].NCMName = nCMName;
            //    }
            //}

            //ComboBox_SystemType
            @string = ComboBox_SystemType.SelectedItem as string;
            if (@string != vary)
            {
                if (Core.Query.TryGetEnum(@string, out NCMSystemType nCMSystemType))
                {
                    for (int i = 0; i < nCMDatas.Count; i++)
                    {
                        if (nCMDatas[i] == null)
                        {
                            nCMDatas[i] = new NCMData();
                        }

                        nCMDatas[i].SystemType = nCMSystemType;
                    }
                }
            }

            //ComboBox_LightingOccupancyControls
            @string = ComboBox_LightingOccupancyControls.SelectedItem as string;
            if (@string != vary)
            {
                if (Core.Query.TryGetEnum(@string, out LightingOccupancyControls lightingOccupancyControls))
                {
                    for (int i = 0; i < nCMDatas.Count; i++)
                    {
                        if (nCMDatas[i] == null)
                        {
                            nCMDatas[i] = new NCMData();
                        }

                        nCMDatas[i].LightingOccupancyControls = lightingOccupancyControls;
                    }
                }
            }

            //ComboBox_LightingPhotoelectricControls
            @string = ComboBox_LightingPhotoelectricControls.SelectedItem as string;
            if (@string != vary)
            {
                if (Core.Query.TryGetEnum(@string, out LightingPhotoelectricControls lightingPhotoelectricControls))
                {
                    for (int i = 0; i < nCMDatas.Count; i++)
                    {
                        if (nCMDatas[i] == null)
                        {
                            nCMDatas[i] = new NCMData();
                        }

                        nCMDatas[i].LightingPhotoelectricControls = lightingPhotoelectricControls;
                    }
                }
            }

            bool? @bool = null;

            //CheckBox_LightingPhotoelectricBackSpaceSensor
            @bool = CheckBox_LightingPhotoelectricBackSpaceSensor.IsChecked;
            if (@bool != null && @bool.HasValue)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].LightingPhotoelectricBackSpaceSensor = @bool.Value;
                }
            }

            //CheckBox_LightingPhotoelectricControlsTimeSwitch
            @bool = CheckBox_LightingPhotoelectricControlsTimeSwitch.IsChecked;
            if (@bool != null && @bool.HasValue)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].LightingPhotoelectricControlsTimeSwitch = @bool.Value;
                }
            }

            //CheckBox_LightingDaylightFactorMethod
            @bool = CheckBox_LightingDaylightFactorMethod.IsChecked;
            if (@bool != null && @bool.HasValue)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].LightingDaylightFactorMethod = @bool.Value;
                }
            }

            //CheckBox_IsMainsGasAvailable
            @bool = CheckBox_IsMainsGasAvailable.IsChecked;
            if (@bool != null && @bool.HasValue)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].IsMainsGasAvailable = @bool.Value;
                }
            }

            double @double = double.NaN;

            //MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower
            if (!MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower.VarySet)
            {
                if (Core.Query.TryConvert(MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower.Value, out @double) && !double.IsNaN(@double))
                {
                    for (int i = 0; i < nCMDatas.Count; i++)
                    {
                        if (nCMDatas[i] == null)
                        {
                            nCMDatas[i] = new NCMData();
                        }

                        nCMDatas[i].LightingPhotoelectricParasiticPower = @double;
                    }
                }
            }

            //MultipleValueTextBoxControl_AirPermeability
            if (!MultipleValueTextBoxControl_AirPermeability.VarySet)
            {
                if (Core.Query.TryConvert(MultipleValueTextBoxControl_AirPermeability.Value, out @double) && !double.IsNaN(@double))
                {
                    for (int i = 0; i < nCMDatas.Count; i++)
                    {
                        if (nCMDatas[i] == null)
                        {
                            nCMDatas[i] = new NCMData();
                        }

                        nCMDatas[i].AirPermeability = @double;
                    }
                }
            }

            //MultipleValueTextBoxControl_Description
            if (!MultipleValueTextBoxControl_Description.VarySet)
            {
                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].Description = MultipleValueTextBoxControl_AirPermeability.Value;
                }
            }

            return nCMDatas;
        }

        private void SetNCMDatas(IEnumerable<NCMData> nCMDatas)
        {
            this.nCMDatas = nCMDatas == null ? null : new List<NCMData>(nCMDatas);

            List<string> strings = null;
            List<bool?> bools = null;

            //MultipleValueComboBoxControl_Name
            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : x.NCMName?.FullName);
            strings = strings?.Distinct().ToList();
            if(strings != null)
            {
                if(MultipleValueComboBoxControl_Name.Values == null || MultipleValueComboBoxControl_Name.Values.Count == 0)
                {
                    MultipleValueComboBoxControl_Name.Values = strings;
                }

                MultipleValueComboBoxControl_Name.SetDefaultValue(strings);
            }

            //if (strings != null && strings.Count > 1)
            //{
            //    ComboBox_SystemType.Items.Insert(0, vary);
            //    Button_Name.Content = vary;
            //}
            //else
            //{
            //    Button_Name.Content = strings[0];
            //}

            //ComboBox_SystemType
            ComboBox_SystemType.Items.Clear();
            Core.Query.Descriptions(NCMSystemType.Undefined).ForEach(x => ComboBox_SystemType.Items.Add(x));

            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : Core.Query.Description(x.SystemType));
            strings = strings.Distinct().ToList();

            if (strings != null && strings.Count > 1)
            {
                ComboBox_SystemType.Items.Insert(0, vary);
                ComboBox_SystemType.SelectedItem = vary;
            }
            else
            {
                ComboBox_SystemType.SelectedItem = strings[0];
            }

            //ComboBox_LightingOccupancyControls
            ComboBox_LightingOccupancyControls.Items.Clear();
            Core.Query.Descriptions(LightingOccupancyControls.Undefined).ForEach(x => ComboBox_LightingOccupancyControls.Items.Add(x));

            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : Core.Query.Description(x.LightingOccupancyControls));
            strings = strings.Distinct().ToList();

            if (strings != null && strings.Count > 1)
            {
                ComboBox_LightingOccupancyControls.Items.Insert(0, vary);
                ComboBox_LightingOccupancyControls.SelectedItem = vary;
            }
            else
            {
                ComboBox_LightingOccupancyControls.SelectedItem = strings[0];
            }

            //ComboBox_LightingPhotoelectricControls
            ComboBox_LightingPhotoelectricControls.Items.Clear();
            Core.Query.Descriptions(LightingPhotoelectricControls.Undefined).ForEach(x => ComboBox_LightingPhotoelectricControls.Items.Add(x));

            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : Core.Query.Description(x.LightingPhotoelectricControls));
            strings = strings.Distinct().ToList();

            if (strings != null && strings.Count > 1)
            {
                ComboBox_LightingPhotoelectricControls.Items.Insert(0, vary);
                ComboBox_LightingPhotoelectricControls.SelectedItem = vary;
            }
            else
            {
                ComboBox_LightingPhotoelectricControls.SelectedItem = strings[0];
            }

            //CheckBox_LightingPhotoelectricBackSpaceSensor
            bools = this.nCMDatas?.ConvertAll(x => x == null ? null as bool? : x.LightingPhotoelectricBackSpaceSensor);
            bools = bools.Distinct().ToList();

            if (bools != null && bools.Count > 1)
            {
                CheckBox_LightingPhotoelectricBackSpaceSensor.IsChecked = null;
            }
            else
            {
                CheckBox_LightingPhotoelectricBackSpaceSensor.IsChecked = bools[0];
            }

            //CheckBox_LightingPhotoelectricControlsTimeSwitch
            bools = this.nCMDatas?.ConvertAll(x => x == null ? null as bool? : x.LightingPhotoelectricControlsTimeSwitch);
            bools = bools.Distinct().ToList();

            if (bools != null && bools.Count > 1)
            {
                CheckBox_LightingPhotoelectricControlsTimeSwitch.IsChecked = null;
            }
            else
            {
                CheckBox_LightingPhotoelectricControlsTimeSwitch.IsChecked = bools[0];
            }

            //CheckBox_LightingDaylightFactorMethod
            bools = this.nCMDatas?.ConvertAll(x => x == null ? null as bool? : x.LightingDaylightFactorMethod);
            bools = bools.Distinct().ToList();

            if (bools != null && bools.Count > 1)
            {
                CheckBox_LightingDaylightFactorMethod.IsChecked = null;
            }
            else
            {
                CheckBox_LightingDaylightFactorMethod.IsChecked = bools[0];
            }

            //CheckBox_IsMainsGasAvailable
            bools = this.nCMDatas?.ConvertAll(x => x == null ? null as bool? : x.IsMainsGasAvailable);
            bools = bools.Distinct().ToList();

            if (bools != null && bools.Count > 1)
            {
                CheckBox_IsMainsGasAvailable.IsChecked = null;
            }
            else
            {
                CheckBox_IsMainsGasAvailable.IsChecked = bools[0];
            }


            //MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower
            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : x.LightingPhotoelectricParasiticPower.ToString());
            strings = strings.Distinct().ToList();

            MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower.Values = strings;
            MultipleValueTextBoxControl_LightingPhotoelectricParasiticPower.SetDefaultValue(strings);

            //MultipleValueTextBoxControl_AirPermeability
            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : x.AirPermeability.ToString());
            strings = strings.Distinct().ToList();

            MultipleValueTextBoxControl_AirPermeability.Values = strings;
            MultipleValueTextBoxControl_AirPermeability.SetDefaultValue(strings);

            //MultipleValueTextBoxControl_Description
            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : x.Description);
            strings = strings.Distinct().ToList();

            MultipleValueTextBoxControl_Description.Values = strings;
            MultipleValueTextBoxControl_Description.SetDefaultValue(strings);
        }

        public List<NCMData> NCMDatas
        {
            get
            {
                return GetNCMDatas();
            }

            set
            {
                SetNCMDatas(value);
            }
        }

        public NCMNameCollection NCMDataCollection
        {
            get
            {
                return nCMNameCollection;
            }

            set
            {
                nCMNameCollection = value;
            }
        }

        public List<string> AvailableNCMNames
        {
            set
            {
                MultipleValueComboBoxControl_Name.Values = value;

            }

            get
            {
                return MultipleValueComboBoxControl_Name.Values;
            }
        }
    }
}
