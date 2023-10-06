using SAM.Core.UI.WPF;
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
            if (nCMNameCollectionWindow.ShowDialog() != true)
            {
                return;
            }
        }

        private List<NCMData> GetNCMDatas()
        {
            string value = null;

            string fullName = Button_Name.Content as string;
            if (fullName != vary)
            {
                NCMNameCollection nCMNameCollection_Temp = nCMNameCollection;
                if (nCMNameCollection_Temp == null)
                {
                    nCMNameCollection_Temp = Analytical.Query.DefaultNCMNameCollection();
                }
                
                NCMName nCMName = nCMNameCollection_Temp.ToList().Find(x => x.FullName == fullName);
                if(nCMName == null)
                {
                    nCMName = fullName;
                }

                for (int i = 0; i < nCMDatas.Count; i++)
                {
                    if (nCMDatas[i] == null)
                    {
                        nCMDatas[i] = new NCMData();
                    }

                    nCMDatas[i].NCMName = nCMName;
                }
            }

            //ComboBox_SystemType
            value = ComboBox_SystemType.SelectedItem as string;
            if(value != vary)
            {
                if(Core.Query.TryGetEnum(value, out NCMSystemType nCMSystemType))
                {
                    for(int i = 0; i < nCMDatas.Count; i++)
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
            value = ComboBox_LightingOccupancyControls.SelectedItem as string;
            if (value != vary)
            {
                if (Core.Query.TryGetEnum(value, out LightingOccupancyControls lightingOccupancyControls))
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
            value = ComboBox_LightingPhotoelectricControls.SelectedItem as string;
            if (value != vary)
            {
                if (Core.Query.TryGetEnum(value, out LightingPhotoelectricControls lightingPhotoelectricControls))
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

            return nCMDatas;
        }

        private void SetNCMDatas(IEnumerable<NCMData> nCMDatas)
        {
            this.nCMDatas = nCMDatas == null ? null : new List<NCMData>(nCMDatas);

            List<string> strings = null;
            List<bool?> bools = null;

            //Button_Name
            strings = this.nCMDatas?.ConvertAll(x => x == null ? null : x.Name);
            strings = strings.Distinct().ToList();
            if (strings != null && strings.Count > 1)
            {
                ComboBox_SystemType.Items.Insert(0, vary);
                Button_Name.Content = vary;
            }
            else
            {
                Button_Name.Content = strings[0];
            }

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
    }
}
