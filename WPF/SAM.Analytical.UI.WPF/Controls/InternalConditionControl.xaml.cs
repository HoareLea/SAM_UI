using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for InternalConditionControl.xaml
    /// </summary>
    public partial class InternalConditionControl : UserControl
    {
        private List<InternalConditionData> internalConditionDatas;

        public InternalConditionControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void multipleValueComboBoxControl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }

        public List<Space> Spaces
        {
            get
            {
                return internalConditionDatas?.ConvertAll(x => x?.Space);
            }
            set
            {
                internalConditionDatas = value?.ConvertAll(x => new InternalConditionData(x));
                LoadInternalConditionDatas(internalConditionDatas);
            }
        }

        public List<InternalCondition> InternalConditions
        {
            get
            {
                return internalConditionDatas?.ConvertAll(x => x?.InternalCondition);
            }

            set
            {
                internalConditionDatas = value?.ConvertAll(x => new InternalConditionData(x));
                LoadInternalConditionDatas(internalConditionDatas);
            }
        }

        public List<InternalConditionData> InternalConditionDatas
        {
            get
            {
                return internalConditionDatas;
            }

            set
            {
                internalConditionDatas = value;
                LoadInternalConditionDatas(value);
            }
        }

        private void LoadInternalConditionDatas(IEnumerable<InternalConditionData> internalConditionDatas)
        {
            multipleValueComboBoxControl_Name.Values = null;

            if (internalConditionDatas == null)
            {
                return;
            }

            List<InternalConditionData> internalConditionDatas_Temp = internalConditionDatas.ToList();

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x.Name);
            multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.AreaPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGain);
        }

    }
}
