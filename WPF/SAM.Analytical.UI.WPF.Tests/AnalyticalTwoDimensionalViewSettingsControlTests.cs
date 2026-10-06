// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The View Settings panel's presentation: Part F Airflow and Ventilation Design read as one
    /// conceptual "Overlays" group rather than two disconnected buttons, and - the thing that actually has
    /// to keep working - a saved view's Part F and Ventilation Design settings persist independently of one
    /// another through <see cref="AnalyticalViewSettingsParameter"/>, exactly as before this reorganisation.
    /// See PR #93 presentation polish.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class AnalyticalTwoDimensionalViewSettingsControlTests
    {
        /// <summary>
        /// Both overlay buttons are inside one "Overlays" group box - not two buttons that merely happen to
        /// sit near each other - and that group box is a control distinct from Color Scheme.
        /// </summary>
        [WpfFact]
        public void OverlayButtons_ShareOneOverlaysGroupBox_DistinctFromColorScheme()
        {
            AnalyticalTwoDimensionalViewSettingsControl control = new();

            GroupBox groupBox_Overlays = Field<GroupBox>(control, "groupBox_Overlays");
            GroupBox groupBox_ColorScheme = Field<GroupBox>(control, "groupBox_ColorScheme");
            Button button_PartFAirflow = Field<Button>(control, "button_PartFAirflow");
            Button button_VentilationDesign = Field<Button>(control, "button_VentilationDesign");

            Assert.NotNull(groupBox_Overlays);
            Assert.NotNull(groupBox_ColorScheme);
            Assert.Equal("Overlays", groupBox_Overlays.Header);

            Assert.True(IsDescendantOf(button_PartFAirflow, groupBox_Overlays));
            Assert.True(IsDescendantOf(button_VentilationDesign, groupBox_Overlays));

            //Not the same box as Color Scheme - the two conceptually different control groups the task
            //required kept apart.
            Assert.False(IsDescendantOf(button_PartFAirflow, groupBox_ColorScheme));
            Assert.NotSame(groupBox_Overlays, groupBox_ColorScheme);
        }

        /// <summary>
        /// A view's Part F and Ventilation Design settings are stored under independent parameters on the
        /// same <see cref="TwoDimensionalViewSettings"/> - exactly the mechanism
        /// <c>AnalyticalTwoDimensionalViewSettingsControl.GetTwoDimensionalViewSettings</c> and
        /// <c>SetAnalyticalTwoDimensionalViewSettings</c> read and write - so setting, reading back, and
        /// disabling one leaves the other completely untouched.
        /// </summary>
        [Fact]
        public void ViewSettings_KeepPartFAndDesignAirflowIndependent()
        {
            Plane plane = Geometry.Spatial.Create.Plane(0);

            TwoDimensionalViewSettings twoDimensionalViewSettings = new(
                Guid.NewGuid(), "Level 0", plane, null, [], Geometry.Object.Query.DefaultTextAppearance(), null);

            PartFAirflowViewSettings partFAirflowViewSettings = new() { Enabled = true, ShowSupply = true, AnnotationScale = 75 };
            DesignAirFlowViewSettings designAirFlowViewSettings = new() { Enabled = true, ShowSupply = true, ShowNet = true };

            twoDimensionalViewSettings.SetValue(AnalyticalViewSettingsParameter.PartFAirflow, partFAirflowViewSettings);
            twoDimensionalViewSettings.SetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, designAirFlowViewSettings);

            Assert.True(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings partF_Read));
            Assert.True(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, out DesignAirFlowViewSettings design_Read));

            Assert.True(partF_Read.Enabled);
            Assert.Equal(75, partF_Read.AnnotationScale);
            Assert.True(design_Read.Enabled);
            Assert.True(design_Read.ShowNet);

            //Disabling Part F, in place, must not touch Ventilation Design's own settings.
            twoDimensionalViewSettings.SetValue(AnalyticalViewSettingsParameter.PartFAirflow, new PartFAirflowViewSettings { Enabled = false });

            Assert.True(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings partF_Disabled));
            Assert.True(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, out DesignAirFlowViewSettings design_Unaffected));

            Assert.False(partF_Disabled.Enabled);
            Assert.True(design_Unaffected.Enabled);
            Assert.True(design_Unaffected.ShowNet);
        }

        /// <summary>
        /// A view that has never been told about either overlay carries neither parameter - absence means
        /// off, and a saved view from before this feature existed must reopen exactly as it was rather than
        /// sprouting a default-on overlay.
        /// </summary>
        [Fact]
        public void ViewSettings_WithNeitherOverlaySet_CarriesNeitherParameter()
        {
            Plane plane = Geometry.Spatial.Create.Plane(0);

            TwoDimensionalViewSettings twoDimensionalViewSettings = new(
                Guid.NewGuid(), "Level 0", plane, null, [], Geometry.Object.Query.DefaultTextAppearance(), null);

            Assert.False(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings _));
            Assert.False(twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, out DesignAirFlowViewSettings _));
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
