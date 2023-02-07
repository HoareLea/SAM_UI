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

        public AnalyticalModel AnalyticalModel { get; set; }

        public InternalConditionControl()
        {
            InitializeComponent();

            multipleValueTextBoxControl_HeatingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.IsEnabled = false;
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
                internalConditionDatas = value?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));
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
                internalConditionDatas = value?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));
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

            List<InternalCondition> internalConditions_Template = internalConditionDatas.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

            List<InternalConditionData> internalConditionDatas_Temp = internalConditionDatas.ToList();

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.Name);
            multipleValueComboBoxControl_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.Name).FindAll(x => x != null));

            multipleValueTextBoxControl_HeatingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Heating));
            multipleValueTextBoxControl_HeatingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Heating)));
            
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.Values = internalConditionDatas_Temp.ConvertAll(x => x.HeatingDesignTemperature)?.Texts();
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.HeatingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));

            multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.AreaPerPerson);
            multipleValueComboBoxControl_AreaPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.AreaPerPerson));

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson));

            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson));

            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea));

            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGain);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGain));

            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour);
            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour));

            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGainPerArea);
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGainPerArea));

            multipleValueComboBoxControl_LightingProfile_LightingGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGain);
            multipleValueComboBoxControl_LightingProfile_LightingGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGain));

            multipleValueComboBoxControl_LightingProfile_LightLevel.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingLevel);
            multipleValueComboBoxControl_LightingProfile_LightLevel.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingLevel));

            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGainPerArea);
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGainPerArea));

            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGain);
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGain));
        }

    }
}
