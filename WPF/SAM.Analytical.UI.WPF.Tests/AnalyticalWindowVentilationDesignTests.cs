// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Windows;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <c>AnalyticalWindow.DesignAnnotationScale</c> - a private static method reflected into directly
    /// rather than exercised through the whole window, matching the style of testing a small pure helper
    /// this codebase already uses elsewhere for reflection-based structural checks.
    /// <para>
    /// <b>The behaviour being pinned.</b> Ventilation Design's tags must be sized at the SAME physical
    /// scale as the view's Part F settings say, whether or not Part F's own overlay happens to be switched
    /// on at the moment - so turning Part F off does not silently change how big an unrelated overlay's
    /// text is. Only a view that has never carried Part F settings at all falls back to the shared default.
    /// </para>
    /// </summary>
    public class AnalyticalWindowVentilationDesignTests
    {
        /// <summary>
        /// A view whose Part F overlay is currently DISABLED still hands its own annotation scale to
        /// Ventilation Design - the scale is a property of the drawing, not of that checkbox.
        /// </summary>
        [Fact]
        public void DesignAnnotationScale_ReadsPartFsScale_EvenWhenPartFIsDisabledOnThatView()
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = ViewSettings();

            twoDimensionalViewSettings.SetValue(AnalyticalViewSettingsParameter.PartFAirflow, new PartFAirflowViewSettings { Enabled = false, AnnotationScale = 100 });

            double result = Invoke(twoDimensionalViewSettings);

            Assert.Equal(100, result);
        }

        /// <summary>The same, with Part F actually enabled - the ordinary case.</summary>
        [Fact]
        public void DesignAnnotationScale_ReadsPartFsScale_WhenPartFIsEnabled()
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = ViewSettings();

            twoDimensionalViewSettings.SetValue(AnalyticalViewSettingsParameter.PartFAirflow, new PartFAirflowViewSettings { Enabled = true, AnnotationScale = 75 });

            double result = Invoke(twoDimensionalViewSettings);

            Assert.Equal(75, result);
        }

        /// <summary>
        /// A view that has never carried Part F settings at all - never opened the Part F dialog on it -
        /// falls back to the shared default rather than throwing or reading a stale scale.
        /// </summary>
        [Fact]
        public void DesignAnnotationScale_FallsBackToTheSharedDefault_WhenTheViewHasNeverHadPartFSettings()
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = ViewSettings();

            double result = Invoke(twoDimensionalViewSettings);

            Assert.Equal(PartFTagPlacement.DefaultAnnotationScale, result);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static TwoDimensionalViewSettings ViewSettings()
        {
            Plane plane = Geometry.Spatial.Create.Plane(0);

            return new TwoDimensionalViewSettings(
                Guid.NewGuid(), "Level 0", plane, null, [], Geometry.Object.Query.DefaultTextAppearance(), null);
        }

        private static double Invoke(IViewSettings viewSettings)
        {
            MethodInfo methodInfo = typeof(AnalyticalWindow).GetMethod("DesignAnnotationScale", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(methodInfo);

            return (double)methodInfo.Invoke(null, [viewSettings]);
        }
    }
}
