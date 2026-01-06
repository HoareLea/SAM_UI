// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureConstructionCalculationResultControl.xaml
    /// </summary>
    public partial class ApertureConstructionCalculationResultControl : UserControl
    {
        private ConstructionManager constructionManager;
        private ApertureConstructionCalculationResult apertureConstructionCalculationResult;

        public ApertureConstructionCalculationResultControl()
        {
            InitializeComponent();
        }

        public ApertureConstructionCalculationResult ApertureConstructionCalculationResult
        {
            get
            {
                return apertureConstructionCalculationResult;
            }

            set
            {
                SetApertureConstructionCalculationResult(value);
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
                SetApertureConstructionCalculationResult(ApertureConstructionCalculationResult);
            }
        }

        private void SetApertureConstructionCalculationResult(ApertureConstructionCalculationResult apertureConstructionCalculationResult)
        {
            this.apertureConstructionCalculationResult = apertureConstructionCalculationResult;
            if (apertureConstructionCalculationResult != null)
            {

                TextBox_PaneThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.PaneThermalTransmittance) ? null : apertureConstructionCalculationResult.PaneThermalTransmittance.ToString();
                TextBox_FrameThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.FrameThermalTransmittance) ? null : apertureConstructionCalculationResult.FrameThermalTransmittance.ToString();

                ApertureConstruction initialApertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, apertureConstructionCalculationResult.InitialApertureConstructionName)?.FirstOrDefault();

                TextBox_InitialConstructionName.Text = apertureConstructionCalculationResult.InitialApertureConstructionName;
                TextBox_InitialConstructionThickness.Text = initialApertureConstruction == null ? null : Core.Query.Round(initialApertureConstruction.GetThickness(), Core.Tolerance.MacroDistance).ToString();
                TextBox_InitialPaneThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.InitialPaneThermalTransmittance) ? null : apertureConstructionCalculationResult.InitialPaneThermalTransmittance.ToString();
                TextBox_InitialFrameThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.InitialFrameThermalTransmittance) ? null : apertureConstructionCalculationResult.InitialFrameThermalTransmittance.ToString();

                ApertureConstruction calculatedApertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, apertureConstructionCalculationResult.ApertureConstructionName)?.FirstOrDefault();

                TextBox_CalculatedConstructionName.Text = apertureConstructionCalculationResult.ApertureConstructionName;
                TextBox_CalculatedConstructionThickness.Text = calculatedApertureConstruction == null ? null : Core.Query.Round(calculatedApertureConstruction.GetThickness(), Core.Tolerance.MacroDistance).ToString();
                TextBox_CalculatedPaneThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.CalculatedPaneThermalTransmittance) ? null : apertureConstructionCalculationResult.CalculatedPaneThermalTransmittance.ToString();
                TextBox_CalculatedFrameThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationResult.CalculatedFrameThermalTransmittance) ? null : apertureConstructionCalculationResult.CalculatedFrameThermalTransmittance.ToString();
            }
        }
    }
}
