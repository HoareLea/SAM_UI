// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The lightweight configuration of an optional screening: which engineering strategies to screen, and whether to
    /// find each dwelling's first permitted passing strategy or populate the whole comparison.
    /// <para>
    /// The list is <c>Query.PartOScreeningStrategies</c> - engineering strategies, with the iteration they correspond
    /// to as secondary detail only. A strategy this build cannot screen is shown, disabled, with the reason, so a later
    /// one (cooling) appears here without changing the window.
    /// </para>
    /// </summary>
    public partial class PartOScreeningWindow : System.Windows.Window
    {
        private readonly Dictionary<PartOScreeningStrategy, CheckBox> checkBoxes = [];

        public PartOScreeningWindow(bool catalogueHasProducts, IEnumerable<PartOScreeningStrategy>? strategies_Last, PartOScreeningMode partOScreeningMode, PartOMixedDesignConstraints? partOMixedDesignConstraints)
        {
            InitializeComponent();

            HashSet<PartOScreeningStrategy> last = [.. strategies_Last ?? []];

            foreach (PartOScreeningStrategy partOScreeningStrategy in UI.Query.PartOScreeningStrategies())
            {
                string? unavailable = UI.Query.PartOScreeningStrategyUnavailable(partOScreeningStrategy, catalogueHasProducts);

                CheckBox checkBox = new()
                {
                    Content = UI.Query.PartOScreeningStrategyLabel(partOScreeningStrategy),
                    IsEnabled = unavailable is null,
                    //First time: every available strategy. Afterwards: the last choice, where still available.
                    IsChecked = unavailable is null && (last.Count == 0 || last.Contains(partOScreeningStrategy)),
                    Margin = new Thickness(0, 4, 0, 0),
                };

                checkBox.Checked += (s, e) => RefreshEstimate();
                checkBox.Unchecked += (s, e) => RefreshEstimate();

                stackPanel_Strategies.Children.Add(checkBox);

                string detail = UI.Query.PartOScreeningStrategyDetail(partOScreeningStrategy) ?? string.Empty;
                if (unavailable is not null)
                {
                    detail = string.Format("{0} {1}", unavailable, detail);
                }
                else if (partOMixedDesignConstraints is not null && !partOMixedDesignConstraints.Allows(partOScreeningStrategy))
                {
                    detail = string.Format("{0} Not permitted by the project constraints: minimum screening will not run it, and it is never suggested.", detail);
                }

                stackPanel_Strategies.Children.Add(new TextBlock
                {
                    Text = detail,
                    Style = (Style)FindResource("PartO.Caption"),
                    Margin = new Thickness(20, 0, 0, 0),
                });

                checkBoxes[partOScreeningStrategy] = checkBox;
            }

            radioButton_Minimum.IsChecked = partOScreeningMode != PartOScreeningMode.FullComparison;
            radioButton_Full.IsChecked = partOScreeningMode == PartOScreeningMode.FullComparison;
            radioButton_Minimum.Checked += (s, e) => RefreshEstimate();
            radioButton_Full.Checked += (s, e) => RefreshEstimate();

            button_Screen.Click += (s, e) =>
            {
                if (Strategies.Count == 0)
                {
                    MessageBox.Show(this, "Choose at least one strategy to screen.", "Part O — Screen dwelling strategies");
                    return;
                }

                DialogResult = true;
            };

            RefreshEstimate();
        }

        /// <summary>The chosen, screenable strategies, in the least-intervention order.</summary>
        public List<PartOScreeningStrategy> Strategies => [.. UI.Query.PartOScreeningStrategies().Where(x => checkBoxes.TryGetValue(x, out CheckBox? checkBox) && checkBox.IsEnabled && checkBox.IsChecked == true)];

        public PartOScreeningMode Mode => radioButton_Full.IsChecked == true ? PartOScreeningMode.FullComparison : PartOScreeningMode.Minimum;

        private void RefreshEstimate()
        {
            int count = Strategies.Count;

            textBlock_Estimate.Text = Mode == PartOScreeningMode.FullComparison
                ? string.Format("{0} of the whole building.", UI.Query.PartOCount(count, "full-year TAS run", "full-year TAS runs"))
                : string.Format("Up to {0} of the whole building - fewer where every dwelling passes early.", UI.Query.PartOCount(count, "full-year TAS run", "full-year TAS runs"));

            button_Screen.IsEnabled = count != 0;
        }
    }
}
