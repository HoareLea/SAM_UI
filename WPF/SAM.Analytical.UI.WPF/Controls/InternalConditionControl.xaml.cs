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

            multipleValueComboBoxControl_Name.IsEnabled = true;
            multipleValueComboBoxControl_Name.IsEditable = false;

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
            multipleValueComboBoxControl_SpaceOccupancy.IsEnabled = false;

            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.IsEnabled = false;
            multipleValueComboBoxControl_HumidificationProfile_Humidity.IsEnabled = false;

            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextChanged += MultipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextChanged += MultipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGain_TextChanged;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextChanged += MultipleValueComboBoxControl_LightingProfile_LightingGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea_TextChanged;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextChanged += MultipleValueComboBoxControl_EquipmentLatentProfile_LatentGain_TextChanged;
            multipleValueComboBoxControl_AreaPerPerson.TextChanged += MultipleValueComboBoxControl_AreaPerPerson_TextChanged;
            multipleValueComboBoxControl_Occupancy.TextChanged += MultipleValueComboBoxControl_Occupancy_TextChanged;

            multipleValueComboBoxControl_AreaPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentLatentProfile_LatentGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_EquipmentSensibleProfile_SensibleGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_HumidificationProfile_Humidity.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_InfiltrationProfile_Infiltration.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightingGain.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_LightingProfile_LightLevel.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_Occupancy.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_OccupancyProfile_LatentGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueComboBoxControl_SpaceOccupancy.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueTextBoxControl_CoolingProfile_DesignTemperature.TextInput += MultipleValueComboBoxControl_Number_TextInput;
            multipleValueTextBoxControl_HeatingProfile_DesignTemperature.TextInput += MultipleValueComboBoxControl_Number_TextInput;

            multipleValueComboBoxControl_Name.TextChanged += MultipleValueComboBoxControl_Name_TextChanged;

            if (background == null)
            {
                background = button_Color.Background;
            }
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

            InternalCondition internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value).FirstOrDefault();
            if (internalCondition == null)
            {
                internalConditionLibrary = new InternalConditionLibrary("Internal Condition Library");
                adjacencyCluster?.GetInternalConditions(true, false)?.ToList().ForEach(x => internalConditionLibrary.Add(x));
                internalCondition = internalConditionLibrary.GetInternalConditions(multipleValueComboBoxControl_Name.Value).FirstOrDefault();
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
            UpdatedCalculatedEquipmentSensibleGain();
            UpdateCalculatedEquipmentLatentGain();
            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
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
            UpdatedCalculatedEquipmentSensibleGain();
            UpdateCalculatedEquipmentLatentGain();
            UpdateCalculatedOccupancyLatentGain();
            UpdateCalculatedOccupancySensibleGainPerPerson();
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
            UpdateCalculatedOccupancyLatentGain();
        }

        private void MultipleValueComboBoxControl_OccupancyProfile_SensibleGainPerPerson_TextChanged(object sender, System.EventArgs e)
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
                    if (!multipleValueComboBoxControl_AreaPerPerson.Vary)
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

                if (checkBox_InfiltrationProfile.IsChecked.HasValue && checkBox_InfiltrationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_InfiltrationProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_InfiltrationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.InfiltrationProfileName, value);
                    }

                    if (!multipleValueComboBoxControl_InfiltrationProfile_Infiltration.Vary)
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
                    if (!multipleValueTextBoxControl_DehumidificationProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_DehumidificationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.DehumidificationProfileName, value);
                    }
                }

                if (checkBox_HumidificationProfile.IsChecked.HasValue && checkBox_HumidificationProfile.IsChecked.Value)
                {
                    if (!multipleValueTextBoxControl_HumidificationProfile_Name.Vary)
                    {
                        string value = multipleValueTextBoxControl_HumidificationProfile_Name.Value;
                        internalCondition?.SetValue(InternalConditionParameter.HumidificationProfileName, value);
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
            multipleValueComboBoxControl_Name.TextChanged -= MultipleValueComboBoxControl_Name_TextChanged;

            multipleValueComboBoxControl_Name.Values = null;

            if (internalConditionDatas == null)
            {
                return;
            }

            SetColor(internalConditionDatas);

            List<InternalCondition> internalConditions_Template = internalConditionDatas?.ToList().ConvertAll(x => x?.GetInternalConditionTemplate());

            List<InternalConditionData> internalConditionDatas_Temp = internalConditionDatas.ToList();

            multipleValueComboBoxControl_Name.Values = internalConditionDatas_Temp?.ConvertAll(x => x?.Name);
            multipleValueComboBoxControl_Name.SetDefaultValue(internalConditions_Template?.ConvertAll(x => x?.Name).FindAll(x => x != null));

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
                multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGainPerArea);
                multipleValueComboBoxControl_LightingProfile_LightingGainPerArea.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGainPerArea));

                multipleValueComboBoxControl_LightingProfile_LightingGain.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingGain);
                multipleValueComboBoxControl_LightingProfile_LightingGain.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingGain));

                multipleValueComboBoxControl_LightingProfile_LightLevel.Values = internalConditionDatas_Temp.Texts(InternalConditionParameter.LightingLevel);
                multipleValueComboBoxControl_LightingProfile_LightLevel.SetDefaultValue(internalConditions_Template?.Texts(InternalConditionParameter.LightingLevel));
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

            if (checkBox_LightingProfile.IsChecked != null && checkBox_LightingProfile.IsChecked.HasValue && checkBox_LightingProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_LightingProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting));
                multipleValueTextBoxControl_LightingProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Lighting)));
            }

            if (checkBox_DehumidificationProfile.IsChecked != null && checkBox_DehumidificationProfile.IsChecked.HasValue && checkBox_DehumidificationProfile.IsChecked.Value)
            {
                multipleValueTextBoxControl_DehumidificationProfile_Name.Values = internalConditionDatas_Temp.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification));
                multipleValueTextBoxControl_DehumidificationProfile_Name.SetDefaultValue(internalConditions_Template.ConvertAll(x => x?.GetProfileName(ProfileType.Dehumidification)));

                multipleValueComboBoxControl_DehumidificationProfile_Dehumidity.Values = internalConditionDatas_Temp.ConvertAll(x => x.Dehumidity)?.Texts();
            }

            multipleValueComboBoxControl_Name.TextChanged += MultipleValueComboBoxControl_Name_TextChanged;
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

            if(internalConditionDatas != null && !multipleValueTextBoxControl_HumidificationProfile_Name.VarySet)
            {
                internalConditionDatas.ForEach(x => x.SetValue(InternalConditionParameter.HumidificationProfileName, multipleValueTextBoxControl_HumidificationProfile_Name.Value));
            }

            multipleValueComboBoxControl_HumidificationProfile_Humidity.Values = internalConditionDatas?.ToList().ConvertAll(x => x.Humidity)?.Texts();
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

            HashSet<Enum> enums = new HashSet<Enum>();
            foreach (InternalCondition internalCondition_Temp in internalConditionLibrary.GetInternalConditions())
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
    }
}
