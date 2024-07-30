using SAM.Analytical.Windows.Forms;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for InternalConditionControl.xaml
    /// </summary>
    public partial class InternalConditionControl : System.Windows.Controls.UserControl
    {
        private Brush background = null;

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
            checkBox_Occupancy.IsChecked = true;
            checkBox_PollutantProfile.IsChecked = true;
            checkBox_VentilationProfile.IsChecked = true;

            multipleValueComboBoxControl_Name.IsEnabled = true;
            multipleValueComboBoxControl_Name.IsEditable = false;

            multipleValueTextBoxControl_HeatingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.IsEnabled = false;
            multipleValueTextBoxControl_OccupancyProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_EquipmentSensibleProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_HumidificationProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_DehumidificationProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_InfiltrationProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_CoolingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.IsEnabled = false;
            multipleValueTextBoxControl_LightingProfile_Name.IsEnabled = false;
            multipleValueTextBoxControl_EquipmentLatentProfile_Name.IsEnabled = false;
            multipleValueComboBoxControl_SpaceOccupancy.IsEnabled = false;

            multipleValueTextBoxControl_VentilationProfile_Name.IsEnabled = false;

            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.IsEnabled = false;
            multipleValueComboBoxControl_HumidificationProfile_Humidity.IsEnabled = false;
            textBox_VentilationProfile_CalculatedSupplyAirFlowPerArea.IsEnabled = false;
            textBox_VentilationProfile_CalculatedSupplyAirFlow.IsEnabled = false;
            textBox_VentilationProfile_CalculatedSupplyAirChangesPerHour.IsEnabled = false;
            textBox_VentilationProfile_CalculatedExhaustAirFlowPerPerson.IsEnabled = false;
            textBox_VentilationProfile_CalculatedExhaustAirFlowPerArea.IsEnabled = false;
            textBox_VentilationProfile_CalculatedExhaustAirFlow.IsEnabled = false;
            textBox_VentilationProfile_CalculatedExhaustAirChangesPerHour.IsEnabled = false;
            textBox_VentilationProfile_CalculatedSupplyAirFlowPerPerson.IsEnabled = false;

            multipleValueComboBoxControl_AreaPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_HumidificationProfile_Humidity.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightLevel.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_Occupancy.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_SpaceOccupancy.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;

            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.TextInput += MultipleValueComboBoxControl_Number_TextInput;

            multipleValueComboBoxControl_Name.TextChanged += MultipleValueComboBoxControl_Name_TextChanged;

            if (background == null)
            {
                background = button_Color.Background;
            }
        }

        private void MultipleValueComboBoxControl_PollutantProfile_GenerationPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdatePollution();
        }

        private void MultipleValueComboBoxControl_PollutantProfile_GenerationPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdatePollution();
        }

        private void MultipleValueComboBoxControl_LightingProfile_LightingGainPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedLightingGain();
        }

        private void MultipleValueComboBoxControl_Name_TextChanged(object sender, EventArgs e)
        {
            if (internalConditionDatas == null)
            {
                return;
            }

            if (multipleValueComboBoxControl_Name.VarySet)
            {
                LoadInternalConditionDatas(internalConditionDatas);
                return;
            }

            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            ProfileLibrary profileLibrary = AnalyticalModel.ProfileLibrary;

            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
            adjacencyCluster?.GetInternalConditions(false, true)?.ToList().ForEach(x => internalConditionLibrary.Add(x));

            InternalCondition internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value)?.FirstOrDefault();
            if (internalCondition == null)
            {
                internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
                adjacencyCluster?.GetInternalConditions(true, false)?.ToList().ForEach(x => internalConditionLibrary.Add(x));
                internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value)?.FirstOrDefault();
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster, AnalyticalModel.MaterialLibrary, profileLibrary);

            for (int i = 0; i < internalConditionDatas.Count; i++)
            {
                internalConditionDatas[i] = new InternalConditionData(AnalyticalModel, internalConditionDatas[i]);
                internalConditionDatas[i].InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }

        private void MultipleValueComboBoxControl_Number_TextInput(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }

        private void MultipleValueComboBoxControl_Occupancy_TextChanged(object sender, EventArgs e)
        {
            multipleValueComboBoxControl_AreaPerPerson.TextChanged -= MultipleValueComboBoxControl_AreaPerPerson_TextChanged;

            if (multipleValueComboBoxControl_Occupancy.VarySet)
            {
                multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas.Texts(InternalConditionParameter.AreaPerPerson);
            }
            else
            {
                if (Core.Query.TryConvert(multipleValueComboBoxControl_Occupancy.Value, out double occupancy) && !double.IsNaN(occupancy))
                {
                    internalConditionDatas?.ForEach(x => x.Occupancy = occupancy);
                }
                else
                {
                    internalConditionDatas?.ForEach(x => x.Occupancy = double.NaN);
                }

                multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas?.Texts(InternalConditionParameter.AreaPerPerson);
            }

            multipleValueComboBoxControl_SpaceOccupancy.Values = internalConditionDatas?.Texts(SpaceParameter.Occupancy);

            multipleValueComboBoxControl_AreaPerPerson.TextChanged += MultipleValueComboBoxControl_AreaPerPerson_TextChanged;

            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedEquipmentSensibleGain();
            UpdateCalculatedEquipmentLatentGain();
            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedLightingGain();
            UpdatePollution();
        }

        private void MultipleValueComboBoxControl_AreaPerPerson_TextChanged(object sender, EventArgs e)
        {
            multipleValueComboBoxControl_Occupancy.TextChanged -= MultipleValueComboBoxControl_Occupancy_TextChanged;
            if (multipleValueComboBoxControl_AreaPerPerson.VarySet)
            {
                multipleValueComboBoxControl_Occupancy.Values = internalConditionDatas.ConvertAll(x => x == null || double.IsNaN(x.Occupancy) ? null : x.Occupancy.ToString());

            }
            else
            {
                if (Core.Query.TryConvert(multipleValueComboBoxControl_AreaPerPerson.Value, out double areaPerPerson) && !double.IsNaN(areaPerPerson))
                {
                    internalConditionDatas?.ForEach(x => x.AreaPerPerson = areaPerPerson);
                }
                else
                {
                    internalConditionDatas?.ForEach(x => x.AreaPerPerson = double.NaN);
                }

                multipleValueComboBoxControl_Occupancy.Values = internalConditionDatas.ConvertAll(x => x == null || double.IsNaN(x.Occupancy) ? null : Core.Query.Round(x.Occupancy, 0.01).ToString());
            }

            multipleValueComboBoxControl_SpaceOccupancy.Values = internalConditionDatas?.Texts(SpaceParameter.Occupancy);

            multipleValueComboBoxControl_Occupancy.TextChanged += MultipleValueComboBoxControl_Occupancy_TextChanged;

            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedEquipmentSensibleGain();
            UpdateCalculatedEquipmentLatentGain();
            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedLightingGain();
            UpdatePollution();
        }

        private void UpdatePollution()
        {
            textBox_PollutantProfile_CalculatedPollutantGeneration.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.PollutantGeneration);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_PollutantProfile_CalculatedPollutantGeneration.Text = multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.VaryText;
                return;
            }

            textBox_PollutantProfile_CalculatedPollutantGeneration.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
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

            textBox_LightingProfile_CalculatedLightingGain.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
        }

        private void UpdateCalculatedEquipmentSensibleGain()
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

            textBox_EquipmentSensibleProfile_CalculatedSensibleGain.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
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

            textBox_EquipmentLatentProfile_CalculatedLatentGain.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
        }

        private void UpdateCalculatedOccupancyLatentGain()
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

            textBox_OccupancyProfile_CalculatedLatentGain.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
        }

        private void UpdateCalculatedOccupancySensibleGainPerPerson()
        {
            textBox_OccupancyProfile_CalculatedSensibleGain.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = internalConditionDatas.ConvertAll(x => x.OccupancySensibleGain);
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(values))
            {
                textBox_OccupancyProfile_CalculatedSensibleGain.Text = multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.VaryText;
                return;
            }

            textBox_OccupancyProfile_CalculatedSensibleGain.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
        }

        private void MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedEquipmentLatentGain();
        }

        private void MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedEquipmentLatentGain();
        }

        private void MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedLightingGain();
        }

        private void MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedLightingGain();
        }

        private void MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedEquipmentSensibleGain();
        }

        private void MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedEquipmentSensibleGain();
        }

        private void MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedEquipmentSensibleGain();
        }

        private void MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedOccupancyLatentGain();
        }

        private void MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateCalculatedOccupancySensibleGainPerPerson();
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

                if (!multipleValueComboBoxControl_Name.VarySet)
                {
                    if (internalCondition.Name != multipleValueComboBoxControl_Name.Value)
                    {
                        InternalCondition internalCondition_Temp = AnalyticalModel.AdjacencyCluster.GetInternalConditions(false, true)?.ToList()?.Find(x => x.Name == multipleValueComboBoxControl_Name.Value);
                        if (internalCondition_Temp != null)
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

                System.Drawing.Color color = GetColor(out bool vary);
                if (!vary)
                {
                    if (color == System.Drawing.Color.Empty)
                    {
                        internalCondition?.RemoveValue(InternalConditionParameter.Color);
                    }
                    else
                    {
                        internalCondition?.SetValue(InternalConditionParameter.Color, color);
                    }
                }

                if (checkBox_Occupancy.IsChecked.HasValue && checkBox_Occupancy.IsChecked.Value)
                {
                    if (!multipleValueComboBoxControl_AreaPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_AreaPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.AreaPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.AreaPerPerson);
                        }
                    }
                }

                if (!multipleValueTextBoxControl_Description.VarySet)
                {
                    string value = multipleValueTextBoxControl_Description.NewValue;
                    if(string.IsNullOrEmpty(value))
                    {
                        internalCondition?.RemoveValue(InternalConditionParameter.Description);
                    }
                    else
                    {
                        internalCondition?.SetValue(InternalConditionParameter.Description, value);
                    }
                }

                if (!multipleValueComboBoxControl_SpaceOccupancy.VarySet)
                {
                    string value = multipleValueComboBoxControl_SpaceOccupancy.Value;
                    if (Core.Query.TryConvert(value, out double value_Temp))
                    {
                        space?.SetValue(SpaceParameter.Occupancy, value_Temp);
                    }
                    else
                    {
                        space?.RemoveValue(SpaceParameter.Occupancy);
                    }
                }

                if (checkBox_HeatingProfile.IsChecked.HasValue && checkBox_HeatingProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_HeatingProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_HeatingProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.HeatingProfileName, value);
                    }
                }

                if (checkBox_OccupancyProfile.IsChecked.HasValue && checkBox_OccupancyProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_OccupancyProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_OccupancyProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.OccupancyProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.VarySet)
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

                    if (!multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.VarySet)
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
                    if (!multipleValueTextBoxControl_EquipmentSensibleProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.VarySet)
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

                    if (!multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.VarySet)
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

                    if (!multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.EquipmentSensibleGainPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.EquipmentSensibleGainPerPerson);
                        }
                    }
                }

                if (checkBox_LightingProfile.IsChecked.HasValue && checkBox_LightingProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_LightingProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_LightingProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.LightingProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.VarySet)
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

                    if (!multipleValueComboBoxControl_LightingProfile_LightingGain.VarySet)
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

                    if (!multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.LightingGainPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.LightingGainPerPerson);
                        }
                    }

                    if (!multipleValueComboBoxControl_LightingProfile_LightLevel.VarySet)
                    {
                        string value = multipleValueComboBoxControl_LightingProfile_LightLevel.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.LightingLevel, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.LightingLevel);
                        }
                    }

                    if (!multipleValueComboBoxControl_LightingProfile_LightingControlFunction.VarySet)
                    {
                        string value = multipleValueComboBoxControl_LightingProfile_LightingControlFunction.Value;
                        if(string.IsNullOrEmpty(value))
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.LightingControlFunction);
                        }
                        else
                        {
                            internalCondition?.SetValue(InternalConditionParameter.LightingControlFunction, value);
                        }

                    }
                }

                if (checkBox_EquipmentLatentProfile.IsChecked.HasValue && checkBox_EquipmentLatentProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_EquipmentLatentProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_EquipmentLatentProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.EquipmentLatentProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.VarySet)
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

                    if (!multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.VarySet)
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

                if (checkBox_InfiltrationProfile.IsChecked.HasValue && checkBox_InfiltrationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_InfiltrationProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_InfiltrationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.InfiltrationProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_InfiltrationProfile_Infiltration.VarySet)
                    {
                        string value = multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.InfiltrationAirChangesPerHour);
                        }
                    }
                }

                if (checkBox_DehumidificationProfile.IsChecked.HasValue && checkBox_DehumidificationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_DehumidificationProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_DehumidificationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.DehumidificationProfileName, value);
                    }
                }

                if (checkBox_HumidificationProfile.IsChecked.HasValue && checkBox_HumidificationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_HumidificationProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_HumidificationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.HumidificationProfileName, value);
                    }
                }

                if (checkBox_PollutantProfile.IsChecked.HasValue && checkBox_PollutantProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_PollutantProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_PollutantProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.PollutantProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.VarySet)
                    {
                        string value = multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.PollutantGenerationPerArea, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.PollutantGenerationPerArea);
                        }
                    }

                    if (!multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.PollutantGenerationPerPerson, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.PollutantGenerationPerPerson);
                        }
                    }
                }

                if (checkBox_VentilationProfile.IsChecked.HasValue && checkBox_VentilationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_VentilationProfile_Name.VarySet)
                    {
                        string value = multipleValueTextBoxControl_VentilationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.VentilationProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.SupplyAirFlowPerPerson, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.SupplyAirFlowPerPerson);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.SupplyAirFlowPerArea);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.SupplyAirFlow, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.SupplyAirFlow);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.SupplyAirChangesPerHour, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.SupplyAirChangesPerHour);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.ExhaustAirFlowPerPerson, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.ExhaustAirFlowPerPerson);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.ExhaustAirFlowPerArea, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.ExhaustAirFlowPerArea);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.ExhaustAirFlow, value_Temp / 1000);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.ExhaustAirFlow);
                        }
                    }

                    if (!multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.VarySet)
                    {
                        string value = multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.Value;
                        if (Core.Query.TryConvert(value, out double value_Temp))
                        {
                            internalCondition?.SetValue(InternalConditionParameter.ExhaustAirChangesPerHour, value_Temp);
                        }
                        else
                        {
                            internalCondition?.RemoveValue(InternalConditionParameter.ExhaustAirChangesPerHour);
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
            bool internalConditionOnly = InternalConditionOnly();
            SetInternalCondlitionOnly(internalConditionOnly);

            multipleValueComboBoxControl_Name.TextChanged -= MultipleValueComboBoxControl_Name_TextChanged;

            multipleValueComboBoxControl_Name.Values = null;

            if (internalConditionDatas == null)
            {
                return;
            }

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextChanged -= MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextChanged -= MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextChanged -= MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.TextChanged -= MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextChanged -= MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextChanged -= MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextChanged -= MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.TextChanged -= MultipleValueComboBoxControl_LightingProfile_LightingGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextChanged -= MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextChanged -= MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged;
            multipleValueComboBoxControl_AreaPerPerson.TextChanged -= MultipleValueComboBoxControl_AreaPerPerson_TextChanged;
            multipleValueComboBoxControl_Occupancy.TextChanged -= MultipleValueComboBoxControl_Occupancy_TextChanged;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.TextChanged -= MultipleValueComboBoxControl_PollutantProfile_GenerationPerArea_TextChanged;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.TextChanged -= MultipleValueComboBoxControl_PollutantProfile_GenerationPerPerson_TextChanged;
            
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.TextChanged -= multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.TextChanged -= multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.TextChanged -= multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.TextChanged -= multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.TextChanged -= multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.TextChanged -= multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.TextChanged -= multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.TextChanged -= multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour_TextChanged;

            SetColor(internalConditionDatas);

            List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

            List<InternalConditionData> internalConditionDatas_Temp = internalConditionDatas.ToList();

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp?.ConvertAll(x => x?.Name);
            multipleValueComboBoxControl_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.Name).FindAll(x => x != null));

            multipleValueTextBoxControl_Description.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.Description);
            multipleValueTextBoxControl_Description.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.Description));

            if (checkBox_Occupancy.IsChecked != null && checkBox_Occupancy.IsChecked.HasValue && checkBox_Occupancy.IsChecked.Value)
            {
                multipleValueComboBoxControl_AreaPerPerson.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.AreaPerPerson);
                multipleValueComboBoxControl_AreaPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.AreaPerPerson));

                multipleValueComboBoxControl_Occupancy.Values = internalConditionDatas_Temp?.ConvertAll(x => x == null || double.IsNaN(x.Occupancy) ? null : x.Occupancy.ToString());

                multipleValueComboBoxControl_SpaceOccupancy.Values = internalConditionDatas_Temp?.ConvertAll(x => x == null || double.IsNaN(x.SpaceOccupancy) ? null : Core.Query.Round(x.SpaceOccupancy, 0.01).ToString());
            }

            if (checkBox_HeatingProfile.IsChecked != null && checkBox_HeatingProfile.IsChecked.HasValue && checkBox_HeatingProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_HeatingProfile_Name.Values = internalConditionDatas_Temp?.ConvertAll(x => x?.GetProfileName(ProfileType.Heating));
                multipleValueTextBoxControl_HeatingProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Heating)));

                multipleValueTextBoxControl_HeatingProfile_DesignTemperature.Values = internalConditionDatas_Temp?.ConvertAll(x => x.HeatingDesignTemperature)?.Texts();
                multipleValueTextBoxControl_HeatingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.HeatingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));
            }

            if (checkBox_OccupancyProfile.IsChecked != null && checkBox_OccupancyProfile.IsChecked.HasValue && checkBox_OccupancyProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_OccupancyProfile_Name.Values = internalConditionDatas_Temp?.ConvertAll(x => x?.GetProfileName(ProfileType.Occupancy));
                multipleValueTextBoxControl_OccupancyProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Occupancy)));

                multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson);
                multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson));

                multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson);
                multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson));
            }

            if (checkBox_EquipmentSensibleProfile.IsChecked != null && checkBox_EquipmentSensibleProfile.IsChecked.HasValue && checkBox_EquipmentSensibleProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Values = internalConditionDatas_Temp?.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentSensible));
                multipleValueTextBoxControl_EquipmentSensibleProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentSensible)));

                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea);
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea));
                
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.EquipmentSensibleGainPerPerson);
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGainPerPerson));

                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.Values = internalConditionDatas_Temp?.Texts(InternalConditionParameter.EquipmentSensibleGain);
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGain));
            }

            if (checkBox_InfiltrationProfile.IsChecked != null && checkBox_InfiltrationProfile.IsChecked.HasValue && checkBox_InfiltrationProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_InfiltrationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Infiltration));
                multipleValueTextBoxControl_InfiltrationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Infiltration)));

                multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour);
                multipleValueComboBoxControl_InfiltrationProfile_Infiltration.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour));
            }

            if (checkBox_LightingProfile.IsChecked != null && checkBox_LightingProfile.IsChecked.HasValue && checkBox_LightingProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_LightingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting));
                multipleValueTextBoxControl_LightingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting)));

                multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGainPerArea);
                multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGainPerArea));

                multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGainPerPerson);
                multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGainPerPerson));

                multipleValueComboBoxControl_LightingProfile_LightingGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGain);
                multipleValueComboBoxControl_LightingProfile_LightingGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGain));

                multipleValueComboBoxControl_LightingProfile_LightLevel.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingLevel);
                multipleValueComboBoxControl_LightingProfile_LightLevel.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingLevel));

                multipleValueComboBoxControl_LightingProfile_LightingControlFunction.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingControlFunction);
                multipleValueComboBoxControl_LightingProfile_LightingControlFunction.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingControlFunction));
            }

            if (checkBox_EquipmentLatentProfile.IsChecked != null && checkBox_EquipmentLatentProfile.IsChecked.HasValue && checkBox_EquipmentLatentProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_EquipmentLatentProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentLatent));
                multipleValueTextBoxControl_EquipmentLatentProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentLatent)));

                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGainPerArea);
                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGainPerArea));

                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.EquipmentLatentGain);
                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGain));
            }

            if (checkBox_HumidificationProfile.IsChecked != null && checkBox_HumidificationProfile.IsChecked.HasValue && checkBox_HumidificationProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_HumidificationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Humidification));
                multipleValueTextBoxControl_HumidificationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Humidification)));

                multipleValueComboBoxControl_HumidificationProfile_Humidity.Values = internalConditionDatas_Temp.ConvertAll(x => x.Humidity)?.Texts();
            }

            if (checkBox_CoolingProfile.IsChecked != null && checkBox_CoolingProfile.IsChecked.HasValue && checkBox_CoolingProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_CoolingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Cooling));
                multipleValueTextBoxControl_CoolingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Cooling)));

                multipleValueTextBoxControl_CoolingProfile_DesignTemperature.Values = internalConditionDatas_Temp.ConvertAll(x => x.CoolingDesignTemperature)?.Texts();
                multipleValueTextBoxControl_CoolingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.CoolingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));
            }

            if (checkBox_DehumidificationProfile.IsChecked != null && checkBox_DehumidificationProfile.IsChecked.HasValue && checkBox_DehumidificationProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_DehumidificationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification));
                multipleValueTextBoxControl_DehumidificationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification)));

                multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.Values = internalConditionDatas_Temp.ConvertAll(x => x.Dehumidity)?.Texts();
            }

            if (checkBox_PollutantProfile.IsChecked != null && checkBox_PollutantProfile.IsChecked.HasValue && checkBox_PollutantProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_PollutantProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Pollutant));

                multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.PollutantGenerationPerArea);
                multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.PollutantGenerationPerPerson);
            }

            if (checkBox_VentilationProfile.IsChecked != null && checkBox_VentilationProfile.IsChecked.HasValue && checkBox_VentilationProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_VentilationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Ventilation));
                multipleValueTextBoxControl_VentilationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Ventilation)));

                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.SupplyAirFlowPerPerson, 1000);
                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlowPerPerson, 1000));

                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.SupplyAirFlowPerArea, 1000);
                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlowPerArea, 1000));

                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.SupplyAirFlow, 1000);
                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlow, 1000));

                multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.SupplyAirChangesPerHour, 1000);
                multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirChangesPerHour, 1000));

                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.ExhaustAirFlowPerPerson, 1000);
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlowPerPerson, 1000));

                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.ExhaustAirFlowPerArea, 1000);
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlowPerArea, 1000));

                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.ExhaustAirFlow, 1000);
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlow, 1000));

                multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.ExhaustAirChangesPerHour, 1000);
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirChangesPerHour, 1000));
            }

            multipleValueTextBoxControl_VentilationSystem_Name.Values = internalConditionDatas_Temp.Texts(MechanicalSystemCategory.Ventilation);
            multipleValueTextBoxControl_HeatingSystem_Name.Values = internalConditionDatas_Temp.Texts(MechanicalSystemCategory.Heating);
            multipleValueTextBoxControl_CoolingSystem_Name.Values = internalConditionDatas_Temp.Texts(MechanicalSystemCategory.Cooling);
            multipleValueTextBoxControl_SupplyUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.SupplyUnitName);
            multipleValueTextBoxControl_ExhaustUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.ExhaustUnitName);

            multipleValueComboBoxControl_Name.TextChanged += MultipleValueComboBoxControl_Name_TextChanged;

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerPerson.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged;
            multipleValueComboBoxControl_AreaPerPerson.TextChanged += MultipleValueComboBoxControl_AreaPerPerson_TextChanged;
            multipleValueComboBoxControl_Occupancy.TextChanged += MultipleValueComboBoxControl_Occupancy_TextChanged;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerArea.TextChanged += MultipleValueComboBoxControl_PollutantProfile_GenerationPerArea_TextChanged;
            multipleValueComboBoxControl_PollutantProfile_GenerationPerPerson.TextChanged += MultipleValueComboBoxControl_PollutantProfile_GenerationPerPerson_TextChanged;

            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.TextChanged += multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.TextChanged += multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.TextChanged += multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.TextChanged += multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.TextChanged += multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.TextChanged += multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.TextChanged += multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow_TextChanged;
            multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.TextChanged += multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour_TextChanged;

            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedEquipmentSensibleGain();
            UpdateCalculatedEquipmentLatentGain();
            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
            UpdateCalculatedLightingGain();
            UpdatePollution();
            UpdateSupplyAirFlow();
            UpdateExhaustAirFlow();
        }

        private void SetColor(IEnumerable<InternalConditionData> internalConditionDatas)
        {
            button_Color.Content = string.Empty;

            if (internalConditionDatas == null || internalConditionDatas.Count() == 0)
            {
                button_Color.Background = background;
            }

            List<System.Drawing.Color> colors = internalConditionDatas?.Colors();
            if (colors == null || colors.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(colors))
            {
                button_Color.Content = multipleValueComboBoxControl_Name.VaryText;
                button_Color.Background = background;
                return;
            }

            colors.Remove(System.Drawing.Color.Empty);
            if (colors == null || colors.Count == 0)
            {
                button_Color.Background = background;
                return;
            }

            System.Drawing.Color color = colors[0];

            button_Color.Background = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private System.Drawing.Color GetColor(out bool vary)
        {
            vary = false;
            if (!string.IsNullOrWhiteSpace(button_Color?.Content?.ToString()))
            {
                vary = true;
            }

            SolidColorBrush solidColorBrush = button_Color.Background as SolidColorBrush;
            if (solidColorBrush == null)
            {
                return System.Drawing.Color.Empty;
            }

            SolidColorBrush solidColorBrush_Background = background as SolidColorBrush;
            if (solidColorBrush_Background == null)
            {
                return System.Drawing.Color.Empty;
            }

            if (solidColorBrush.Color == solidColorBrush_Background.Color)
            {
                return System.Drawing.Color.Empty;
            }

            return System.Drawing.Color.FromArgb(solidColorBrush.Color.A, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B);
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



            Profile profile = AnalyticalModel.ProfileLibrary.GetProfile(names[0], profileType, true);
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
        
        private void button_ViewVentilationProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewProfile(ProfileType.Ventilation);
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

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, AnalyticalModel.AdjacencyCluster, AnalyticalModel.MaterialLibrary, profileLibrary);

            if (profile != null)
            {
                multipleValueTextBoxControl.Value = profile.Name;
            }
        }

        private void button_SelectVentilationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_VentilationProfile_Name, ProfileType.Ventilation);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_OccupancyProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.VentilationProfileName, multipleValueTextBoxControl_VentilationProfile_Name.Value));
            }
        }

        private void button_SelectHeatingProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_HeatingProfile_Name, ProfileType.Heating);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_HeatingProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.HeatingProfileName, multipleValueTextBoxControl_HeatingProfile_Name.Value));
            }

            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.Values = internalConditionDatas?.ToList().ConvertAll(x => x.HeatingDesignTemperature)?.Texts();
        }

        private void button_SelectOccupancyProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_OccupancyProfile_Name, ProfileType.Occupancy);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_OccupancyProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.OccupancyProfileName, multipleValueTextBoxControl_OccupancyProfile_Name.Value));
            }

            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
        }

        private void button_SelectEquipmentSensibleProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_EquipmentSensibleProfile_Name, ProfileType.EquipmentSensible);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_EquipmentSensibleProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.EquipmentSensibleProfileName, multipleValueTextBoxControl_EquipmentSensibleProfile_Name.Value));
            }

            UpdateCalculatedEquipmentSensibleGain();
        }

        private void button_SelectHumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_HumidificationProfile_Name, ProfileType.Humidification);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_HumidificationProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.HumidificationProfileName, multipleValueTextBoxControl_HumidificationProfile_Name.Value));
            }

            multipleValueComboBoxControl_HumidificationProfile_Humidity.Values = internalConditionDatas?.ToList().ConvertAll(x => x.Humidity)?.Texts();
        }

        private void button_SelectInfiltrationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_InfiltrationProfile_Name, ProfileType.Infiltration);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_InfiltrationProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.InfiltrationProfileName, multipleValueTextBoxControl_InfiltrationProfile_Name.Value));
            }

            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Values = internalConditionDatas?.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour);
        }

        private void button_SelectCoolingProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_CoolingProfile_Name, ProfileType.Cooling);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_CoolingProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.CoolingProfileName, multipleValueTextBoxControl_CoolingProfile_Name.Value));
            }

            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.Values = internalConditionDatas?.ToList().ConvertAll(x => x.CoolingDesignTemperature)?.Texts();
        }

        private void button_SelectLightingProfile_Click(object sender, RoutedEventArgs e)
        {
            
            SetProfile(multipleValueTextBoxControl_LightingProfile_Name, ProfileType.Lighting);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_LightingProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.LightingProfileName, multipleValueTextBoxControl_LightingProfile_Name.Value));
            }

            UpdateCalculatedLightingGain();
        }

        private void button_SelectEquipmentLatentProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_EquipmentLatentProfile_Name, ProfileType.EquipmentLatent);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_EquipmentLatentProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.EquipmentLatentProfileName, multipleValueTextBoxControl_EquipmentLatentProfile_Name.Value));
            }

            UpdateCalculatedEquipmentLatentGain();
        }

        private void button_ViewPollutantProfile_Click(object sender, RoutedEventArgs e)
        {

            ViewProfile(ProfileType.Pollutant);
        }

        private void button_SelectPollutantProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_PollutantProfile_Name, ProfileType.Pollutant);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_PollutantProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.PollutantProfileName, multipleValueTextBoxControl_PollutantProfile_Name.Value));
            }

            UpdatePollution();
        }

        private void button_SelectDehumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            SetProfile(multipleValueTextBoxControl_DehumidificationProfile_Name, ProfileType.Dehumidification);

            List<InternalConditionData> internalConditionDatas = InternalConditionDatas;
            if (internalConditionDatas != null && !multipleValueTextBoxControl_DehumidificationProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.DehumidificationProfileName, multipleValueTextBoxControl_DehumidificationProfile_Name.Value));
            }

            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.Values = internalConditionDatas?.ToList().ConvertAll(x => x.Dehumidity)?.Texts();
        }

        private void button_Select_Click(object sender, RoutedEventArgs e)
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

            ProfileLibrary profileLibrary = AnalyticalModel.ProfileLibrary;

            InternalConditionLibrary internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
            adjacencyCluster?.GetInternalConditions(false, true)?.ToList().ForEach(x => internalConditionLibrary.Add(x));

            InternalCondition internalCondition = null;
            if (!multipleValueComboBoxControl_Name.VarySet)
            {
                internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value)?.FirstOrDefault();
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

            for (int i = 0; i < internalConditionDatas.Count; i++)
            {
                internalConditionDatas[i] = new InternalConditionData(AnalyticalModel, internalConditionDatas[i]);
                internalConditionDatas[i].InternalCondition = internalCondition;
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

            while (internalConditionLibrary.GetInternalConditions(name_Temp).FirstOrDefault() != null)
            {
                name_Temp = string.Format("{0} {1}", name, index);
                index++;
            }

            using (TextBoxForm<string> textBoxForm = new TextBoxForm<string>("Internal Condition Name", "Name"))
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

            for (int i = 0; i < internalConditionDatas.Count; i++)
            {
                internalConditionDatas[i] = new InternalConditionData(AnalyticalModel, internalConditionDatas[i]);
                internalConditionDatas[i].InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }

        private void button_Reset_Click(object sender, RoutedEventArgs e)
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
            adjacencyCluster?.GetInternalConditions(false, true)?.ToList().ForEach(x => internalConditionLibrary.Add(x));

            List<InternalCondition> internalConditions = internalConditionLibrary.GetInternalConditions();
            if(internalConditions == null)
            {
                return;
            }

            HashSet<Enum> enums = new HashSet<Enum>();
            foreach (InternalCondition internalCondition_Temp in internalConditions)
            {
                if (internalCondition_Temp == null)
                {
                    continue;
                }

                List<Enum> enums_Temp = Core.Query.Enums(internalCondition_Temp);
                if (enums_Temp == null)
                {
                    continue;
                }

                enums_Temp.ForEach(x => enums.Add(x));
            }

            List<Tuple<Enum, Core.Attributes.ParameterProperties>> tuples = enums.ToList().ConvertAll(x => new Tuple<Enum, Core.Attributes.ParameterProperties>(x, Core.Attributes.ParameterProperties.Get(x)));
            tuples.RemoveAll(x => x == null || x.Item2 == null || string.IsNullOrWhiteSpace(x.Item2.Name));
            tuples.Sort((x, y) => x.Item2.Name.CompareTo(y.Item2.Name));

            using (TreeViewForm<Tuple<Enum, Core.Attributes.ParameterProperties>> treeViewForm = new TreeViewForm<Tuple<Enum, Core.Attributes.ParameterProperties>>("Select Parameters", tuples, x => x.Item2.Name))
            {
                if (treeViewForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                tuples = treeViewForm.SelectedItems;
            }

            if (tuples == null || tuples.Count == 0)
            {
                return;
            }

            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Are you sure you want to reset values", "Reset", MessageBoxButtons.YesNo);
            if (dialogResult != DialogResult.Yes)
            {
                return;
            }

            for (int i = 0; i < internalConditionDatas.Count; i++)
            {
                InternalCondition internalCondition = internalConditionDatas[i].InternalCondition;
                if (internalCondition == null)
                {
                    continue;
                }

                InternalCondition internalCondition_Template = internalConditionLibrary.GetInternalConditions(internalCondition.Name).FirstOrDefault();
                if (internalCondition_Template == null)
                {
                    continue;
                }

                foreach (Tuple<Enum, Core.Attributes.ParameterProperties> tuple in tuples)
                {
                    if (internalCondition_Template.TryGetValue(tuple.Item1, out object value))
                    {
                        internalCondition.SetValue(tuple.Item1, value);
                    }
                    else
                    {
                        internalCondition.RemoveValue(tuple.Item1);
                    }
                }

                internalConditionDatas[i] = new InternalConditionData(AnalyticalModel, internalConditionDatas[i]);
                internalConditionDatas[i].InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }

        private void button_Color_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color color = System.Drawing.Color.Empty;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                Color color_Start = (button_Color.Background as SolidColorBrush).Color;

                colorDialog.Color = System.Drawing.Color.FromArgb(color_Start.A, color_Start.R, color_Start.G, color_Start.B);
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                color = colorDialog.Color;
            }

            for (int i = 0; i < internalConditionDatas.Count; i++)
            {
                InternalCondition internalCondition = internalConditionDatas[i].InternalCondition;
                if (internalCondition == null)
                {
                    continue;
                }

                if (color == System.Drawing.Color.Empty)
                {
                    internalCondition.RemoveValue(InternalConditionParameter.Color);
                }
                else
                {
                    internalCondition.SetValue(InternalConditionParameter.Color, color);
                }

                internalConditionDatas[i].InternalCondition = internalCondition;
            }

            LoadInternalConditionDatas(internalConditionDatas);
        }

        private void checkBox_VentilationProfile_Click(object sender, RoutedEventArgs e)
        {

            if (checkBox_VentilationProfile.IsChecked != null && checkBox_VentilationProfile.IsChecked.HasValue && checkBox_VentilationProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_VentilationProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Ventilation)));

                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlowPerPerson, 1000));
                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlowPerArea, 1000));
                multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirFlow, 1000));
                multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.SupplyAirChangesPerHour));

                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlowPerPerson, 1000));
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlowPerArea, 1000));
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirFlow, 1000));
                multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.ExhaustAirChangesPerHour));
            }
        }

        private void checkBox_HeatingProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_HeatingProfile.IsChecked != null && checkBox_HeatingProfile.IsChecked.HasValue && checkBox_HeatingProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_HeatingProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Heating)));
                multipleValueTextBoxControl_HeatingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.HeatingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));
            }
        }

        private void checkBox_OccupancyProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_OccupancyProfile.IsChecked != null && checkBox_OccupancyProfile.IsChecked.HasValue && checkBox_OccupancyProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_OccupancyProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Occupancy)));
                multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancySensibleGainPerPerson));
                multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.OccupancyLatentGainPerPerson));
            }
        }

        private void checkBox_EquipmentSensibleProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_EquipmentSensibleProfile.IsChecked != null && checkBox_EquipmentSensibleProfile.IsChecked.HasValue && checkBox_EquipmentSensibleProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_EquipmentSensibleProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentSensible)));
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGainPerArea));
                multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentSensibleGain));
            }
        }

        private void checkBox_HumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_HumidificationProfile.IsChecked != null && checkBox_HumidificationProfile.IsChecked.HasValue && checkBox_HumidificationProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_HumidificationProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Humidification)));
            }
        }

        private void checkBox_InfiltrationProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_InfiltrationProfile.IsChecked != null && checkBox_InfiltrationProfile.IsChecked.HasValue && checkBox_InfiltrationProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_InfiltrationProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Infiltration)));
                multipleValueComboBoxControl_InfiltrationProfile_Infiltration.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.InfiltrationAirChangesPerHour));
            }
        }

        private void checkBox_Occupancy_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBox_Occupancy.IsChecked != null && checkBox_Occupancy.IsChecked.HasValue && checkBox_Occupancy.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueComboBoxControl_AreaPerPerson.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.AreaPerPerson));
            }
        }

        private void checkBox_CoolingProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_CoolingProfile.IsChecked != null && checkBox_CoolingProfile.IsChecked.HasValue && checkBox_CoolingProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_CoolingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Cooling)));
                multipleValueTextBoxControl_CoolingProfile_DesignTemperature.SetDefaultValue(Query.Texts(internalConditions_Template?.ConvertAll(x => Analytical.Query.CoolingDesignTemperature(x, AnalyticalModel?.ProfileLibrary))));
            }
        }

        private void checkBox_LightingProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_LightingProfile.IsChecked != null && checkBox_LightingProfile.IsChecked.HasValue && checkBox_LightingProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_LightingProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting)));
            }
        }

        private void checkBox_EquipmentLatentProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_EquipmentLatentProfile.IsChecked != null && checkBox_EquipmentLatentProfile.IsChecked.HasValue && checkBox_EquipmentLatentProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_EquipmentLatentProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.EquipmentLatent)));
                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGainPerArea));
                multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.EquipmentLatentGain));
            }
        }

        private void checkBox_DehumidificationProfile_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_DehumidificationProfile.IsChecked != null && checkBox_DehumidificationProfile.IsChecked.HasValue && checkBox_DehumidificationProfile.IsChecked.Value)
            {
                List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

                multipleValueTextBoxControl_DehumidificationProfile_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification)));
            }
        }

        private bool InternalConditionOnly()
        {
            if(internalConditionDatas == null)
            {
                return false;
            }

            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                if(internalConditionData.Space == null && internalConditionData.InternalCondition != null)
                {
                    return true;
                }
            }

            return false;

        }

        private void SetInternalCondlitionOnly(bool internalConditionOnly)
        {
            Visibility visibility = internalConditionOnly ? Visibility.Hidden : Visibility.Visible;

            multipleValueComboBoxControl_Occupancy.Visibility = visibility;
            label_Occupancy.Visibility = visibility;

            textBox_EquipmentSensibleProfile_CalculatedSensibleGain.Visibility = visibility;
            label_EquipmentSensibleProfile_CalculatedSensibleGain.Visibility = visibility;

            textBox_OccupancyProfile_CalculatedSensibleGain.Visibility = visibility;
            label_OccupancyProfile_CalculatedSensibleGain.Visibility = visibility;

            textBox_OccupancyProfile_CalculatedLatentGain.Visibility = visibility;
            label_OccupancyProfile_CalculatedLatentGain.Visibility = visibility;

            textBox_EquipmentLatentProfile_CalculatedLatentGain.Visibility = visibility;
            label_EquipmentLatentProfile_CalculatedLatentGain.Visibility = visibility;

            textBox_LightingProfile_CalculatedLightingGain.Visibility = visibility;
            label_LightingProfile_CalculatedLightingGain.Visibility = visibility;

        }

        private void multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateSupplyAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdateSupplyAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow_TextChanged(object sender, EventArgs e)
        {
            UpdateSupplyAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour_TextChanged(object sender, EventArgs e)
        {
            UpdateSupplyAirFlow();
        }

        private void UpdateSupplyAirFlow()
        {
            textBox_VentilationProfile_CalculatedSupplyAirFlowPerPerson.Text = null;
            textBox_VentilationProfile_CalculatedSupplyAirFlowPerArea.Text = null;
            textBox_VentilationProfile_CalculatedSupplyAirFlow.Text = null;
            textBox_VentilationProfile_CalculatedSupplyAirChangesPerHour.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = null;

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedSupplyAirFlow, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlow.Text = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlow.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlow.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedSupplyAirFlowPerArea, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlowPerArea.Text = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerArea.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlowPerArea.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedSupplyAirFlowPerPerson, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlowPerPerson.Text = multipleValueComboBoxControl_VentilationProfile_SupplyAirFlowPerPerson.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedSupplyAirFlowPerPerson.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedSupplyAirChangesPerHour, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedSupplyAirChangesPerHour.Text = multipleValueComboBoxControl_VentilationProfile_SupplyAirChangesPerHour.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedSupplyAirChangesPerHour.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
                }
            }
        }

        private void multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson_TextChanged(object sender, EventArgs e)
        {
            UpdateExhaustAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea_TextChanged(object sender, EventArgs e)
        {
            UpdateExhaustAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow_TextChanged(object sender, EventArgs e)
        {
            UpdateExhaustAirFlow();
        }

        private void multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour_TextChanged(object sender, EventArgs e)
        {
            UpdateExhaustAirFlow();
        }

        private void UpdateExhaustAirFlow()
        {
            textBox_VentilationProfile_CalculatedExhaustAirFlowPerPerson.Text = null;
            textBox_VentilationProfile_CalculatedExhaustAirFlowPerArea.Text = null;
            textBox_VentilationProfile_CalculatedExhaustAirFlow.Text = null;
            textBox_VentilationProfile_CalculatedExhaustAirChangesPerHour.Text = null;

            List<InternalConditionData> internalConditionDatas = GetInternalConditionDatas(true);
            if (internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            List<double> values = null;

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedExhaustAirFlow, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlow.Text = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlow.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlow.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedExhaustAirFlowPerArea, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlowPerArea.Text = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerArea.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlowPerArea.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedExhaustAirFlowPerPerson, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlowPerPerson.Text = multipleValueComboBoxControl_VentilationProfile_ExhaustAirFlowPerPerson.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedExhaustAirFlowPerPerson.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0] * 1000, 0.01).ToString();
                }
            }

            values = internalConditionDatas.ConvertAll(x => Core.Query.Round(x.CalculatedExhaustAirChangesPerHour, Core.Tolerance.Distance));
            if (values != null && values.Count > 0)
            {
                if (Core.UI.Query.Vary(values))
                {
                    textBox_VentilationProfile_CalculatedExhaustAirChangesPerHour.Text = multipleValueComboBoxControl_VentilationProfile_ExhaustAirChangesPerHour.VaryText;
                }
                else
                {
                    textBox_VentilationProfile_CalculatedExhaustAirChangesPerHour.Text = double.IsNaN(values[0]) ? null : Core.Query.Round(values[0], 0.01).ToString();
                }
            }
        }

        private void button_SelectVentilationSystem_Click(object sender, RoutedEventArgs e)
        {
            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = Modify.EditMechanicalSystems(adjacencyCluster, Spaces, MechanicalSystemCategory.Ventilation, Spaces);
            if(sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_VentilationSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Ventilation);
            multipleValueTextBoxControl_SupplyUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.SupplyUnitName);
            multipleValueTextBoxControl_ExhaustUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.ExhaustUnitName);
        }

        private void button_SelectHeatingSystem_Click(object sender, RoutedEventArgs e)
        {
            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = Modify.EditMechanicalSystems(adjacencyCluster, Spaces, MechanicalSystemCategory.Heating, Spaces);
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_HeatingSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Heating);
        }

        private void button_SelectCoolingSystem_Click(object sender, RoutedEventArgs e)
        {
            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = Modify.EditMechanicalSystems(adjacencyCluster, Spaces, MechanicalSystemCategory.Heating, Spaces);
            if (sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_CoolingSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Cooling);
        }

        private void button_RemoveVentilationSystem_Click(object sender, RoutedEventArgs e)
        {
            List<Space> spaces = Spaces;
            if(spaces == null || spaces.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            foreach(Space space in spaces)
            {
                List<VentilationSystem> ventilationSystems = adjacencyCluster.GetRelatedObjects<VentilationSystem>(space);
                if(ventilationSystems == null || ventilationSystems.Count == 0)
                {
                    continue;
                }

                foreach(VentilationSystem ventilationSystem in ventilationSystems)
                {
                    adjacencyCluster.RemoveRelation(space, ventilationSystem);
                }
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_VentilationSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Ventilation);
            multipleValueTextBoxControl_SupplyUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.SupplyUnitName);
            multipleValueTextBoxControl_ExhaustUnitName.Values = internalConditionDatas?.Texts(VentilationSystemParameter.ExhaustUnitName);
        }

        private void button_RemoveHeatingSystem_Click(object sender, RoutedEventArgs e)
        {
            List<Space> spaces = Spaces;
            if (spaces == null || spaces.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            foreach (Space space in spaces)
            {
                List<HeatingSystem> heatingSystems = adjacencyCluster.GetRelatedObjects<HeatingSystem>(space);
                if (heatingSystems == null || heatingSystems.Count == 0)
                {
                    continue;
                }

                foreach (HeatingSystem heatingSystem in heatingSystems)
                {
                    adjacencyCluster.RemoveRelation(space, heatingSystem);
                }
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_HeatingSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Heating);
        }

        private void button_RemoveCoolingSystem_Click(object sender, RoutedEventArgs e)
        {
            List<Space> spaces = Spaces;
            if (spaces == null || spaces.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = AnalyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return;
            }

            foreach (Space space in spaces)
            {
                List<CoolingSystem> coolingSystems = adjacencyCluster.GetRelatedObjects<CoolingSystem>(space);
                if (coolingSystems == null || coolingSystems.Count == 0)
                {
                    continue;
                }

                foreach (CoolingSystem coolingSystem in coolingSystems)
                {
                    adjacencyCluster.RemoveRelation(space, coolingSystem);
                }
            }

            AnalyticalModel = new AnalyticalModel(AnalyticalModel, adjacencyCluster);
            internalConditionDatas = internalConditionDatas?.ConvertAll(x => new InternalConditionData(AnalyticalModel, x));

            multipleValueTextBoxControl_CoolingSystem_Name.Values = internalConditionDatas?.Texts(MechanicalSystemCategory.Cooling);
        }
    }
}
