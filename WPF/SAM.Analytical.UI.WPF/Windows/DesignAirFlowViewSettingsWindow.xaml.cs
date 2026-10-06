// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// How one saved 2D view presents the Ventilation Design floor-plan overlay.
    /// <para>
    /// Presentation only, matching <see cref="PartFAirflowViewSettingsWindow"/>. No design airflow value is
    /// held here - every rate is re-read from <c>VentilationTerminal.DesignFlowRate_Lps</c> each time the
    /// view is drawn.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The base class is fully qualified for the same reason as <see cref="PartFAirflowViewSettingsWindow"/>:
    /// this namespace sits inside <c>SAM.Analytical</c>, which has its own <c>Window</c>.
    /// </remarks>
    public partial class DesignAirFlowViewSettingsWindow : System.Windows.Window
    {
        private DesignAirFlowViewSettings designAirFlowViewSettings = new();

        public DesignAirFlowViewSettingsWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The settings being edited. Reading builds a new instance from the controls.
        /// </summary>
        public DesignAirFlowViewSettings DesignAirFlowViewSettings
        {
            get
            {
                return new(designAirFlowViewSettings)
                {
                    Enabled = CheckBox_Enabled.IsChecked == true,
                    ShowSupply = CheckBox_Supply.IsChecked == true,
                    ShowExtract = CheckBox_Extract.IsChecked == true,
                    ShowNet = CheckBox_Net.IsChecked == true,
                    ShowTransfer = CheckBox_Transfer.IsChecked == true,
                };
            }

            set
            {
                designAirFlowViewSettings = value is null ? new DesignAirFlowViewSettings() : new DesignAirFlowViewSettings(value);

                CheckBox_Enabled.IsChecked = designAirFlowViewSettings.Enabled;
                CheckBox_Supply.IsChecked = designAirFlowViewSettings.ShowSupply;
                CheckBox_Extract.IsChecked = designAirFlowViewSettings.ShowExtract;
                CheckBox_Net.IsChecked = designAirFlowViewSettings.ShowNet;
                CheckBox_Transfer.IsChecked = designAirFlowViewSettings.ShowTransfer;

                UpdateEnabled();
            }
        }

        private void CheckBox_Enabled_Click(object sender, RoutedEventArgs e)
        {
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            GroupBox_Show.IsEnabled = CheckBox_Enabled.IsChecked == true;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
