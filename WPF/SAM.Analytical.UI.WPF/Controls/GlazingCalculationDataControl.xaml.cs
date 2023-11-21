using SAM.Analytical.Tas;
using SAM.Core;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for GlazingCalculationControl.xaml
    /// </summary>
    public partial class GlazingCalculationDataControl : UserControl
    {
        private ConstructionManager constructionManager;

        public GlazingCalculationDataControl()
        {
            InitializeComponent();

            SingleSelectionTreeViewControl_Main.GettingCategory += SingleSelectionTreeViewControl_Main_GettingCategory;
            SingleSelectionTreeViewControl_Main.GettingText += SingleSelectionTreeViewControl_Main_GettingText;
            SingleSelectionTreeViewControl_Main.CompareObjects += SingleSelectionTreeViewControl_Main_CompareObjects;

            TextBox_LightTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_LightTransmittance_ToleranceMax.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_LightTransmittance_ToleranceMin.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;

            TextBox_TotalSolarEnergyTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_TotalSolarEnergyTransmittance_ToleranceMax.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_TotalSolarEnergyTransmittance_ToleranceMin.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;

            TextBox_MaxThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_MinThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
        }

        private void SingleSelectionTreeViewControl_Main_CompareObjects(object sender, Core.UI.WPF.CompareObjectsEventArgs e)
        {
            if(e.Object_1 == e.Object_2)
            {
                e.Equals = true;
                return;
            }

            if(e.Object_1 is SAMObject && e.Object_2 is SAMObject)
            {
                if(((SAMObject)e.Object_1).Guid == ((SAMObject)e.Object_2).Guid)
                {
                    e.Equals = true;
                    return;
                }
            }
        }

        private void SingleSelectionTreeViewControl_Main_GettingText(object sender, Core.UI.WPF.GettingTextEventArgs e)
        {
            object @object = e?.Object;
            if (@object == null)
            {
                return;
            }

            e.Text = @object is SAMObject ? ((SAMObject)@object).Name : @object.ToString();
        }

        private void SingleSelectionTreeViewControl_Main_GettingCategory(object sender, Core.UI.WPF.GettingCategoryEventArgs e)
        {
            object @object = e?.Object;
            if (@object == null)
            {
                return;
            }

            if(@object is Construction)
            {
                e.Category = new Category("Constructions");
                return;
            }

            if (@object is ApertureConstruction)
            {
                ApertureConstruction apertureConstruction = (ApertureConstruction)@object;

                e.Category = new Category(apertureConstruction.ApertureType == ApertureType.Window ? "Windows" : "Doors");
                return;
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
                constructionManager = value;
                SetConstructionManager(value);
            }
        }

        public GlazingCalculationData GlazingCalculationData
        {
            get
            {
                return GetGlazingCalculationData();
            }

            set
            {
                SetGlazingCalculationData(value);
            }
        }

        private GlazingCalculationData GetGlazingCalculationData()
        {
            GlazingCalculationData result = new GlazingCalculationData()
            {
                ConstructionGuid = System.Guid.Empty,
                TotalSolarEnergyTransmittance = double.NaN,
                TotalSolarEnergyTransmittanceRange = null,
                LightTransmittance = double.NaN,
                LightTransmittanceRange = null,
                ThicknessRange = null
            };

            SAMObject sAMObject = SingleSelectionTreeViewControl_Main.GetSlecledObject<SAMObject>();
            if(sAMObject != null)
            {
                result.ConstructionGuid = sAMObject.Guid;
            }

            double value = double.NaN;
            double min = double.NaN;
            double max = double.NaN;

            if(Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance.Text, out value))
            {
                result.TotalSolarEnergyTransmittance = value;
            }

            if (Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance_ToleranceMax.Text, out max) && Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance_ToleranceMin.Text, out min))
            {
                result.TotalSolarEnergyTransmittanceRange = new Range<double>(-System.Math.Abs(min), max);
            }

            if (Core.Query.TryConvert(TextBox_LightTransmittance.Text, out value))
            {
                result.LightTransmittance = value;
            }

            if (Core.Query.TryConvert(TextBox_LightTransmittance_ToleranceMax.Text, out max) && Core.Query.TryConvert(TextBox_LightTransmittance_ToleranceMin.Text, out min))
            {
                result.LightTransmittanceRange = new Range<double>(-System.Math.Abs(min), max);
            }

            if (Core.Query.TryConvert(TextBox_MaxThickness.Text, out max) && Core.Query.TryConvert(TextBox_MinThickness.Text, out min))
            {
                result.ThicknessRange = new Range<double>(-System.Math.Abs(min), max);
            }

            return result;
        }

        private void SetGlazingCalculationData(GlazingCalculationData glazingCalculationData)
        {
            if(glazingCalculationData == null)
            {
                return;
            }

            IAnalyticalObject analyticalObject = constructionManager.Constructions?.Find(x => x.Guid == glazingCalculationData.ConstructionGuid);
            if(analyticalObject == null)
            {
                analyticalObject = constructionManager.ApertureConstructions?.Find(x => x.Guid == glazingCalculationData.ConstructionGuid);
            }

            if(analyticalObject == null)
            {
                return;
            }

            SingleSelectionTreeViewControl_Main.SetSelectedObject(analyticalObject);

            TextBox_TotalSolarEnergyTransmittance.Text = double.IsNaN(glazingCalculationData.TotalSolarEnergyTransmittance) ? null : glazingCalculationData.TotalSolarEnergyTransmittance.ToString();

            if (glazingCalculationData.TotalSolarEnergyTransmittanceRange != null)
            {
                TextBox_TotalSolarEnergyTransmittance_ToleranceMax.Text = glazingCalculationData.TotalSolarEnergyTransmittanceRange.Max.ToString();
                TextBox_TotalSolarEnergyTransmittance_ToleranceMin.Text = System.Math.Abs(glazingCalculationData.TotalSolarEnergyTransmittanceRange.Min).ToString();
            }

            TextBox_LightTransmittance.Text = double.IsNaN(glazingCalculationData.LightTransmittance) ? null : glazingCalculationData.LightTransmittance.ToString();

            if(glazingCalculationData.LightTransmittanceRange != null)
            {
                TextBox_LightTransmittance_ToleranceMax.Text = glazingCalculationData.LightTransmittanceRange.Max.ToString();
                TextBox_LightTransmittance_ToleranceMin.Text = System.Math.Abs(glazingCalculationData.LightTransmittanceRange.Min).ToString();
            }

            if(glazingCalculationData.ThicknessRange != null)
            {
                TextBox_MaxThickness.Text = glazingCalculationData.ThicknessRange.Max.ToString();
                TextBox_MinThickness.Text = System.Math.Abs(glazingCalculationData.ThicknessRange.Min).ToString();
            }
        }

        private void SetConstructionManager(ConstructionManager constructionManager)
        {
            this.constructionManager = constructionManager;

            List<IAnalyticalObject> analyticalObjects = new List<IAnalyticalObject>();

            if (constructionManager != null)
            {
                constructionManager.Constructions?.ForEach(x => analyticalObjects.Add(x));
                constructionManager.ApertureConstructions?.ForEach(x => analyticalObjects.Add(x));
            }

            SingleSelectionTreeViewControl_Main.SetObjects(analyticalObjects);
            SingleSelectionTreeViewControl_Main.ExpandAll();
        }

        private void TextBox_PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }
    }
}
