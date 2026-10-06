// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The "Start Iteration 2B" confirmation: why it runs, what it starts from, what it may and may not change,
    /// and the settings it will run at. Nothing runs until Start.
    /// <para>
    /// <b>Presentation only.</b> The facts and the pre-filled settings are <see cref="PartOOptimisationStart"/>'s;
    /// the settings rule is <c>PartOOptimisationSettings.IsValid</c>'s. Start is enabled only while the typed
    /// settings are usable, and the reason is shown where they are not.
    /// </para>
    /// </summary>
    public partial class PartOOptimisationStartWindow : System.Windows.Window
    {
        private PartOOptimisationStart? partOOptimisationStart;

        public PartOOptimisationStartWindow()
        {
            InitializeComponent();

            //Never taller than the screen it opens on: the content scrolls and Start / Cancel stay reachable on a
            //short display or with large text scaling - the same cap the Hub and the Prepare Iteration window use.
            MaxHeight = SystemParameters.WorkArea.Height * 0.92;

            textBlock_Purpose.Text = PartOOptimisationStart.Text_Purpose;
            itemsControl_Changes.ItemsSource = PartOOptimisationStart.Changes;
            itemsControl_Keeps.ItemsSource = PartOOptimisationStart.Keeps;

            textBox_AirFlowStep.TextChanged += (s, e) => Validate();
            textBox_MaximumIterations.TextChanged += (s, e) => Validate();
        }

        /// <summary>What to show and pre-fill. Setting it fills the window.</summary>
        public PartOOptimisationStart? Start
        {
            get => partOOptimisationStart;
            set
            {
                partOOptimisationStart = value;

                itemsControl_Facts.ItemsSource = value?.Facts;

                PartOOptimisationSettings partOOptimisationSettings = value?.Settings ?? new PartOOptimisationSettings();

                textBox_AirFlowStep.Text = partOOptimisationSettings.AirFlowStep_Lps.ToString();
                textBox_MaximumIterations.Text = partOOptimisationSettings.MaximumIterations.ToString();
                checkBox_CapacityEnvelope.IsChecked = partOOptimisationSettings.CapacityEnvelope;
                checkBox_WarmStart.IsChecked = partOOptimisationSettings.WarmStart;

                textBlock_SettingsSource.Text = value?.SettingsSource ?? string.Empty;

                Validate();
            }
        }

        /// <summary>The settings as currently typed, or null where they cannot be used.</summary>
        public PartOOptimisationSettings? Settings => Parse(out string? _);

        /// <summary>Why the settings as typed cannot be used, or null.</summary>
        public string? Refusal
        {
            get
            {
                Parse(out string? refusal);

                return refusal;
            }
        }

        /// <summary>The settings Start was pressed with; null until then, and after Cancel.</summary>
        public PartOOptimisationSettings? ConfirmedSettings { get; private set; }

        /// <summary>Test seam: the step exactly as typed.</summary>
        internal string AirFlowStepText
        {
            get => textBox_AirFlowStep.Text;
            set => textBox_AirFlowStep.Text = value;
        }

        /// <summary>Test seam: the round limit exactly as typed.</summary>
        internal string MaximumIterationsText
        {
            get => textBox_MaximumIterations.Text;
            set => textBox_MaximumIterations.Text = value;
        }

        internal bool CanStart => button_Start.IsEnabled;

        private PartOOptimisationSettings? Parse(out string? refusal)
        {
            PartOOptimisationStart start = partOOptimisationStart ?? PartOOptimisationStart.Create(null!);

            return start.Parse(textBox_AirFlowStep.Text, textBox_MaximumIterations.Text, checkBox_CapacityEnvelope.IsChecked ?? false, checkBox_WarmStart.IsChecked ?? false, out refusal);
        }

        private void Validate()
        {
            string? refusal = Refusal;

            button_Start.IsEnabled = refusal is null;
            button_Start.ToolTip = refusal;

            textBlock_Refusal.Text = refusal ?? string.Empty;
            textBlock_Refusal.Visibility = refusal is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            PartOOptimisationSettings? partOOptimisationSettings = Settings;
            if (partOOptimisationSettings is null)
            {
                return;
            }

            ConfirmedSettings = partOOptimisationSettings;

            DialogResult = true;
        }
    }
}
