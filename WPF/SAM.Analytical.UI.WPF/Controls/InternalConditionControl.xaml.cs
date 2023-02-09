using SAM.Analytical.Windows.Forms;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for InternalConditionControl.xaml
    /// </summary>
    public partial class InternalConditionControl : System.Windows.Controls.UserControl
    {
        private List<InternalConditionData> internalConditionDatas;

        public AnalyticalModel AnalyticalModel { get; set; }

        public InternalConditionControl()
        {
            InitializeComponent();

            checkBox_CoolingProfile.IsChecked = true;
            checkBox_DehumidificationProfile.IsChecked = true;
            checkBox_EquipmentLatentProfile.IsChecked = true;
            checkBox_EquipmentSensibleProfile.IsChecked = true;
            checkBox_HeatingProfile.IsChecked = true;
            checkBox_HumidificationProfile.IsChecked = true;
            checkBox_InfiltrationProfile.IsChecked = true;
            checkBox_LightingProfile.IsChecked = true;
            checkBox_OccupancyProfile.IsChecked = true;

            multipleValueComboBoxControl_Name.IsEnabled = false;
            multipleValueTextBoxControl_HeatingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.IsEnabled = false;
            multipleValueTextBoxControl_OccupancyProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_EquipmentSensibleProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_HumidificationProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_InfiltrationProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_CoolingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.IsEnabled = false;
            multipleValueTextBoxControl_LightingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_EquipmentLatentProfile_Name.IsEnabled = false;

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged;
        }

        private void UpdateCalculatedLightingGain()
        {
            textBox_LightingProfile_CalculatedLightingGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.LightingGain);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_LightingProfile_CalculatedLightingGain.Text = multipleValueComboBoxControl_LightingProfile_LightingGain.VaryText;
                return;
            }

            textBox_LightingProfile_CalculatedLightingGain.Text = double.IsNaN(values[0]) ? null : values[0].ToString();
        }

        private void UpdatedCalculatedEquipmentSensibleGain()
        {
            textBox_EquipmentSensibleProfile_CalculatedSensibleGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.EquipmentSensibleGain);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_EquipmentSensibleProfile_CalculatedSensibleGain.Text = multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.VaryText;
                return;
            }

            textBox_EquipmentSensibleProfile_CalculatedSensibleGain.Text = double.IsNaN(values[0]) ? null : values[0].ToString();
        }

        private void UpdateCalculatedEquipmentLatentGain()
        {
            textBox_EquipmentLatentProfile_CalculatedLatentGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.EquipmentLatentGain);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_EquipmentLatentProfile_CalculatedLatentGain.Text = multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.VaryText;
                return;
            }

            textBox_EquipmentLatentProfile_CalculatedLatentGain.Text = double.IsNaN(values[0]) ? null : values[0].ToString();
        }

        private void MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged(object sender, System.EventArgs e)
        {
            UpdateCalculatedEquipmentLatentGain();
        }

        private void MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged(object sender, System.EventArgs e)
        {
            UpdateCalculatedEquipmentLatentGain();
        }

        private void MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged(object sender, System.EventArgs e)
        {
            UpdateCalculatedLightingGain();
        }

        private void MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged(object sender, System.EventArgs e)
        {
            UpdateCalculatedLightingGain();
        }

        private void MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged(object sender, System.EventArgs e)
        {
            UpdatedCalculatedEquipmentSensibleGain();
        }

        private void MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged(object sender, System.EventArgs e)
        {
            UpdatedCalculatedEquipmentSensibleGain();
        }

        private void MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged(object sender, System.EventArgs e)
        {
            textBox_OccupancyProfile_CalculatedLatentGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.OccupancyLatentGain);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_OccupancyProfile_CalculatedLatentGain.Text = multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.VaryText;
                return;
            }

            textBox_OccupancyProfile_CalculatedLatentGain.Text = double.IsNaN(values[0]) ? null : values[0].ToString();
        }

        private void MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged(object sender, System.EventArgs e)
        {
            textBox_OccupancyProfile_CalculatedSensibleGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if(internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.OccupancySensibleGain);
            if(values == null || values.Count == 0)
            {
                return;
            }

            if(Core.UI.Query.Vary(values))
            {
                textBox_OccupancyProfile_CalculatedSensibleGain.Text = multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.VaryText;
                return;
            }

            textBox_OccupancyProfile_CalculatedSensibleGain.Text = double.IsNaN(values[0]) ? null : values[0].ToString();
        }

        public List<InternalConditionData> GetInternalConditionDatas(bool updated = true)
        {
            if (!updated)
            {
                return internalConditionDatas;
            }

            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return internalConditionDatas;
            }

            List<InternalConditionData> result = new List<InternalConditionData>();
            foreach (InternalConditionData internalConditionData in internalConditionDatas)
            {
                InternalCondition internalCondition = internalConditionData?.InternalCondition;
                if (internalCondition == null)
                {
                    internalCondition = new InternalCondition(string.Empty);
                }

                if (internalCondition != null)
                {
                    internalCondition = new InternalCondition(internalCondition);
                }

                if(!multipleValueComboBoxControl_Name.VarySet)
                {
                    if(internalCondition.Name != multipleValueComboBoxControl_Name.Value)
                    {
                        InternalCondition internalCondition_Temp = AnalyticalModel.AdjacencyCluster.GetInternalConditions(false, true)?.ToList()?.Find(x => x.Name == multipleValueComboBoxControl_Name.Value);
                        if(internalCondition_Temp != null)
                        {
                            internalCondition = new InternalCondition(internalCondition_Temp);
                        }
                    }
                }

                Space space = internalConditionData.Space;
                if (space != null)
                {
                    space = new Space(space);
                }

                if (checkBox_HeatingProfile.IsChecked.HasValue && checkBox_HeatingProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_HeatingProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_HeatingProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.HeatingProfileName, value);

                    }
                }

                if (checkBox_OccupancyProfile.IsChecked.HasValue && checkBox_OccupancyProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_OccupancyProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_OccupancyProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.OccupancyProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Vary)
                    {
                        string value = multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.OccupancySensibleGainPerPerson);
                        }
                    }

                    if (!multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Vary)
                    {
                        string value = multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.OccupancyLatentGainPerPerson);
                        }
                    }
                }

                if (checkBox_EquipmentSensibleProfile.IsChecked.HasValue && checkBox_EquipmentSensibleProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Vary)
                    {
                        string value = multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.EquipmentSensibleGain, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.EquipmentSensibleGain);
                        }
                    }

                    if (!multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Vary)
                    {
                        string value = multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.EquipmentSensibleGainPerArea);
                        }
                    }
                }

                if (checkBox_LightingProfile.IsChecked.HasValue && checkBox_LightingProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_LightingProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_LightingProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.LightingProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Vary)
                    {
                        string value = multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.LightingGainPerArea, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.LightingGainPerArea);
                        }
                    }

                    if (!multipleValueComboBoxControl_LightingProfile_LightingGain.Vary)
                    {
                        string value = multipleValueComboBoxControl_LightingProfile_LightingGain.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.LightingGain, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.LightingGain);
                        }
                    }
                }

                if (checkBox_EquipmentLatentProfile.IsChecked.HasValue && checkBox_EquipmentLatentProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_EquipmentLatentProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_EquipmentLatentProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.EquipmentLatentProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.Vary)
                    {
                        string value = multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.EquipmentLatentGainPerArea, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.EquipmentLatentGainPerArea);
                        }
                    }

                    if (!multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.Vary)
                    {
                        string value = multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.EquipmentLatentGain, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.EquipmentLatentGain);
                        }
                    }
                }

                if (space != null)
                {
                    space.InternalCondition = internalCondition;
                    result.Add(new InternalConditionData(AnalyticalModel, space));
                    continue;
                }

                result.Add(new InternalConditionData(AnalyticalModel, internalCondition));
            }

            return result;
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
                return GetInternalConditionDatas(true);
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

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x == null ? double.NaN : x.Occupancy).Texts();

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.Name);
            multipleValueComboBoxControl_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.Name).FindAll(x => x != null));

            multipleValueTextBoxControl_HeatingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Heating));
            multipleValueTextBoxControl_HeatingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Heating)));

            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.Values = internalConditionDatas_Temp.ConvertAll(x => x.HeatingDesignTemperature)?.Texts();
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.HeatingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));

            multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.AreaPerPerson);
            multipleValueComboBoxControl_AreaPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.AreaPerPerson));

            multipleValueTextBoxControl_OccupancyProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Occupancy));
            multipleValueTextBoxControl_OccupancyProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Occupancy)));

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson));

            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson);
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson));

            multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentSensible));
            multipleValueTextBoxControl_EquipmentSensibleProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentSensible)));

            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea));

            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentSensibleGain);
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGain));

            multipleValueTextBoxControl_InfiltrationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Infiltration));
            multipleValueTextBoxControl_InfiltrationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Infiltration)));

            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour);
            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour));

            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGainPerArea);
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGainPerArea));

            multipleValueComboBoxControl_LightingProfile_LightingGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGain);
            multipleValueComboBoxControl_LightingProfile_LightingGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGain));

            multipleValueComboBoxControl_LightingProfile_LightLevel.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingLevel);
            multipleValueComboBoxControl_LightingProfile_LightLevel.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingLevel));

            multipleValueTextBoxControl_EquipmentLatentProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentLatent));
            multipleValueTextBoxControl_EquipmentLatentProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentLatent)));

            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGainPerArea);
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGainPerArea));

            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGain);
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGain));

            multipleValueTextBoxControl_HumidificationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Humidification));
            multipleValueTextBoxControl_HumidificationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Humidification)));

            multipleValueComboBoxControl_HumidificationProfile_Humidity.Values = internalConditionDatas_Temp.ConvertAll(x => x.Humidity)?.Texts();

            multipleValueTextBoxControl_CoolingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Cooling));
            multipleValueTextBoxControl_CoolingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Cooling)));

            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.Values = internalConditionDatas_Temp.ConvertAll(x => x.CoolingDesignTemperature)?.Texts();
            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.CoolingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));

            multipleValueTextBoxControl_LightingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting));
            multipleValueTextBoxControl_LightingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting)));


            multipleValueTextBoxControl_DehumidificationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification));
            multipleValueTextBoxControl_DehumidificationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification)));

            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.Values = internalConditionDatas_Temp.ConvertAll(x => x.Dehumidity)?.Texts();
        }

        private void ViewProfile(ProfileType profileType)
        {
            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            List<string> names = internalConditionDatas.ConvertAll(x => x.GetProfileName(profileType));
            
            if (names == null || names.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(names))
            {
                return;
            }



            Profile profile = AnalyticalModel.ProfileLibrary.GetProfile(names[0], profileType);
            if (profile == null)
            {
                return;
            }

            using (ProfileForm profileForm = new ProfileForm(profile, false))
            {
                if (profileForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }
        }

        private void button_ViewHeatingProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Heating);
        }

        private void button_ViewOccupancyProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Occupancy);
        }

        private void button_ViewEquipmentSensibleProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.EquipmentSensible);
        }

        private void button_ViewHumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Humidification);
        }

        private void button_ViewInfiltrationProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Infiltration);
        }

        private void button_ViewCoolingProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Cooling);
        }

        private void button_ViewLightingProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Lighting);
        }

        private void button_ViewEquipmentLatentProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.EquipmentLatent);
        }

        private void button_ViewDehumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Dehumidification);
        }

        private void SetProfile(MultipleValueTextBoxControl multipleValueTextBoxControl, ProfileType profileType)
        {
            if (profileType == ProfileType.Undefined || multipleValueTextBoxControl == null)
            {
                return;
            }

            ProfileLibrary profileLibrary = AnalyticalModel?.ProfileLibrary;
            if (profileLibrary == null)
            {
                return;
            }

            Profile profile = Analytical.Windows.Modify.SelectProfile(profileLibrary, profileType);
            if (profile == null)
            {
                return;
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, AnalyticalModel.AdjacencyCluster, AnalyticalModel.MaterialLibrary, profileLibrary);

            multipleValueTextBoxControl.Value = profile.Name;
        }

        private void button_SelectHeatingProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_HeatingProfile_Name, ProfileType.Heating);
        }

        private void button_SelectOccupancyProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_OccupancyProfile_Name, ProfileType.Occupancy);
        }

        private void button_SelectEquipmentSensibleProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_EquipmentSensibleProfile_Name, ProfileType.EquipmentSensible);
        }

        private void button_SelectHumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_HumidificationProfile_Name, ProfileType.Humidification);
        }

        private void button_SelectInfiltrationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_InfiltrationProfile_Name, ProfileType.Infiltration);
        }

        private void button_SelectCoolingProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_CoolingProfile_Name, ProfileType.Cooling);
        }

        private void button_SelectLightingProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_LightingProfile_Name, ProfileType.Lighting);
        }

        private void button_SelectEquipmentLatentProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_EquipmentLatentProfile_Name, ProfileType.EquipmentLatent);
        }

        private void button_SelectDehumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_DehumidificationProfile_Name, ProfileType.Dehumidification);
        }

        private void button_Select_Click(object sender, RoutedEventArgs e)
        {
            if(internalConditionDatas == null)
            {
                return;
            }
            
            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            ProfileLibrary profileLibrary = AnalyticalModel.ProfileLibrary;

            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
            adjacencyCluster?.GetInternalConditions(false, true)?.ToList().ForEach(x => internalConditionLibrary.Add(x));

            InternalCondition internalCondition = null;
            if(!multipleValueComboBoxControl_Name.VarySet)
            {
                internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value).FirstOrDefault();
            }

            using (InternalConditionLibraryForm internalConditionForm = new InternalConditionLibraryForm(internalConditionLibrary, profileLibrary, adjacencyCluster, internalCondition))
            {
                if (internalConditionForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                profileLibrary = internalConditionForm.ProfileLibrary;
                adjacencyCluster = internalConditionForm.AdjacencyCluster;
                internalCondition = internalConditionForm.GetInternalConditions(true)?.FirstOrDefault();
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster, AnalyticalModel.MaterialLibrary, profileLibrary);

            foreach (InternalConditionData internalConditionData in internalConditionDatas)
            {
                internalConditionData.InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }

        private void button_Create_Click(object sender, RoutedEventArgs e)
        {
            if (internalConditionDatas == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
            adjacencyCluster?.GetInternalConditions(true, true)?.ToList().ForEach(x => internalConditionLibrary.Add(x));

            InternalCondition internalCondition = GetInternalConditionDatas(true)?.Find(x => x?.InternalCondition != null)?.InternalCondition;

            string name = internalCondition?.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = "New Internal Condition";
            }

            int index = 1;
            string name_Temp = name;

            while(internalConditionLibrary.GetInternalConditions(name_Temp).FirstOrDefault() != null)
            {
                name_Temp = string.Format("{0} {1}", name, index);
                index++;
            }

            using (Core.Windows.Forms.TextBoxForm<string> textBoxForm = new Core.Windows.Forms.TextBoxForm<string>("Internal Condition Name", "Name"))
            {
                textBoxForm.Value = name_Temp;
                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                internalCondition = internalCondition == null ? new InternalCondition(textBoxForm.Value) : new InternalCondition(textBoxForm.Value, internalCondition);
            }

            adjacencyCluster.AddObject(internalCondition);

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);

            for(int i =0; i < internalConditionDatas.Count; i++)
            {
                internalConditionDatas[i] = new InternalConditionData(AnalyticalModel, internalConditionDatas[i]);
                internalConditionDatas[i].InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }
    }
}
