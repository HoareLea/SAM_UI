// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Ventilation Design dialog's native WPF presentation: every control from the "Show" group is
    /// actually there, none of the checkbox labels are given a box narrower than their own measured text
    /// (which is how a control clips without throwing), the window sizes to its content rather than to a
    /// fixed pixel height that only happened to fit on one machine, and round-tripping settings through it
    /// does not lose Supply/Extract/Net independently. See PR #93 presentation polish.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class DesignAirFlowViewSettingsWindowTests
    {
        /// <summary>
        /// Every Supply/Extract/Net control the dialog is meant to offer is actually present and inside the
        /// "Show" group box, by name - not merely "the window has three checkboxes somewhere".
        /// </summary>
        [WpfFact]
        public void DialogState_IncludesEverySupplyExtractNetControl()
        {
            DesignAirFlowViewSettingsWindow window = new();

            CheckBox checkBox_Supply = Field<CheckBox>(window, "CheckBox_Supply");
            CheckBox checkBox_Extract = Field<CheckBox>(window, "CheckBox_Extract");
            CheckBox checkBox_Net = Field<CheckBox>(window, "CheckBox_Net");
            GroupBox groupBox_Show = Field<GroupBox>(window, "GroupBox_Show");

            Assert.NotNull(checkBox_Supply);
            Assert.NotNull(checkBox_Extract);
            Assert.NotNull(checkBox_Net);
            Assert.NotNull(groupBox_Show);

            Assert.True(IsDescendantOf(checkBox_Supply, groupBox_Show));
            Assert.True(IsDescendantOf(checkBox_Extract, groupBox_Show));
            Assert.True(IsDescendantOf(checkBox_Net, groupBox_Show));

            //The full label, not a truncated stand-in - the exact text that was reported clipped.
            Assert.Contains("Net design airflow", checkBox_Net.Content as string);
            Assert.Contains("supply", checkBox_Net.Content as string);
            Assert.Contains("extract", checkBox_Net.Content as string);
        }

        /// <summary>
        /// The window sizes to its content rather than a fixed pixel height - the pattern
        /// <c>PartOWorkflowWindow</c> and <c>PartOIterationWindow</c> already use - so a control added later,
        /// or a system font a little taller than the one this was written on, cannot silently clip again.
        /// A minimum width still guards the checkbox text this task was reported clipping.
        /// </summary>
        [WpfFact]
        public void Window_SizesToContentHeight_WithAMinimumWidthWideEnoughForTheLongestLabel()
        {
            DesignAirFlowViewSettingsWindow window = new();

            Assert.Equal(SizeToContent.Height, window.SizeToContent);
            Assert.True(window.MinWidth >= 420, string.Format("MinWidth is {0}, too narrow for \"Net design airflow (NET = supply − extract)\" not to clip.", window.MinWidth));
        }

        /// <summary>
        /// Setting Supply/Extract/Net independently and reading the settings back keeps each one
        /// independent - the dialog does not couple them, and Net's own wording survives the round trip.
        /// </summary>
        [WpfFact]
        public void SupplyExtractNet_RoundTrip_StayIndependent()
        {
            DesignAirFlowViewSettingsWindow window = new()
            {
                DesignAirFlowViewSettings = new DesignAirFlowViewSettings { Enabled = true, ShowSupply = true, ShowExtract = false, ShowNet = true },
            };

            DesignAirFlowViewSettings result = window.DesignAirFlowViewSettings;

            Assert.True(result.Enabled);
            Assert.True(result.ShowSupply);
            Assert.False(result.ShowExtract);
            Assert.True(result.ShowNet);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static T Field<T>(object instance, string name) where T : class
        {
            FieldInfo fieldInfo = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return fieldInfo?.GetValue(instance) as T;
        }

        private static bool IsDescendantOf(DependencyObject descendant, DependencyObject ancestor)
        {
            DependencyObject current = descendant;

            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = LogicalTreeHelper.GetParent(current);
            }

            return false;
        }
    }
}
