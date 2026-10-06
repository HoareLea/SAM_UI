// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Geometry.Planar;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The persistence spike for the Part F airflow overlay, proven rather than inferred.
    /// <para>
    /// The architecture rests on one assumption: that a SECOND parameter enum can be associated with
    /// <see cref="ViewSettings"/> from the analytical layer, because <c>SAM.Geometry.UI</c> knows nothing
    /// about <c>SAM.Analytical</c> and cannot name a Part F type from its own enum. If that were not
    /// supported, presentation state would have to be stored some other way, and everything built on top
    /// would be built on sand. These tests settle it.
    /// </para>
    /// <para>
    /// They also fix the two properties that matter most for existing work: a saved view that predates
    /// the overlay must reopen with it OFF, and manual label positions must survive a full round trip
    /// through the model in world coordinates.
    /// </para>
    /// </summary>
    public class PartFAirflowViewSettingsTests
    {
        private const double tolerance = 1e-9;

        // ------------------------------------------------------------------
        // The spike: a second parameter enum on ViewSettings
        // ------------------------------------------------------------------

        /// <summary>
        /// The core question. <c>SAM.Geometry.UI</c> already owns a <c>ViewSettingsParameter</c>; this
        /// asserts the analytical layer can associate its own with the same type and set a value through
        /// it. There is precedent - <c>AnalyticalModelParameter</c> exists in both SAM.Analytical and
        /// SAM.Analytical.UI - but precedent is not proof for a different target type.
        /// </summary>
        [Fact]
        public void ASecondParameterEnum_CanBeAssociatedWithViewSettings()
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = View();

            //Fully qualified: SAM.Analytical.UI.WPF has an IsValid extension of its own that would
            //otherwise win overload resolution here.
            Assert.True(Core.Query.IsValid(typeof(TwoDimensionalViewSettings), Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow));

            //And the geometry layer's own enum still works on the same object, so the two coexist.
            Assert.True(Core.Query.IsValid(typeof(TwoDimensionalViewSettings), Geometry.UI.ViewSettingsParameter.Group));

            Assert.True(twoDimensionalViewSettings.SetValue(Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow, new PartFAirflowViewSettings { Enabled = true }));
            Assert.True(twoDimensionalViewSettings.SetValue(Geometry.UI.ViewSettingsParameter.Group, "Part F"));

            Assert.True(twoDimensionalViewSettings.TryGetValue(Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings partFAirflowViewSettings));
            Assert.NotNull(partFAirflowViewSettings);
            Assert.True(partFAirflowViewSettings.Enabled);

            Assert.Equal("Part F", twoDimensionalViewSettings.GetValue<string>(Geometry.UI.ViewSettingsParameter.Group));
        }

        // ------------------------------------------------------------------
        // Round trip through the model
        // ------------------------------------------------------------------

        /// <summary>
        /// The whole chain a saved view actually travels: settings on the view, view in
        /// <c>UIGeometrySettings</c>, settings on the <c>AnalyticalModel</c>, serialised and read back.
        /// Anything that survives this survives save and reopen.
        /// </summary>
        [Fact]
        public void PartFViewSettings_RoundTripThroughTheModel()
        {
            Guid guid_View = Guid.NewGuid();
            Guid guid_Dwelling = Guid.NewGuid();
            Guid guid_Terminal = Guid.NewGuid();

            PartFAirflowViewSettings partFAirflowViewSettings = new()
            {
                Enabled = true,
                OperatingMode = PartFOperatingMode.Setback,
                DwellingFilter = PartFDwellingFilter.SelectedDwelling,
                DwellingGuid = guid_Dwelling,
                //1:100, a real drawing scale - AnnotationScale is the layout scale's denominator,
                //not a text multiplier, and a value that only round-trips is not a value worth asserting.
                AnnotationScale = 100,
                ShowSpaceNetAirflow = true,
                ShowContextGeometry = false,
                ShowOutdoorAndExhaust = true,
            };

            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid_Terminal, PartFAnnotationType.Terminal, new Point2D(12.25, -3.75)));

            AnalyticalModel analyticalModel = Model(guid_View, partFAirflowViewSettings);

            //Save and reopen.
            AnalyticalModel analyticalModel_Reopened = new(analyticalModel.ToJsonObject());

            PartFAirflowViewSettings partFAirflowViewSettings_Reopened = Settings(analyticalModel_Reopened, guid_View);

            Assert.NotNull(partFAirflowViewSettings_Reopened);
            Assert.True(partFAirflowViewSettings_Reopened.Enabled);
            Assert.Equal(PartFOperatingMode.Setback, partFAirflowViewSettings_Reopened.OperatingMode);
            Assert.Equal(PartFDwellingFilter.SelectedDwelling, partFAirflowViewSettings_Reopened.DwellingFilter);
            Assert.Equal(guid_Dwelling, partFAirflowViewSettings_Reopened.DwellingGuid);
            Assert.Equal(100, partFAirflowViewSettings_Reopened.AnnotationScale, tolerance);

            //Toggles both directions, so a false is not just a default being read back.
            Assert.True(partFAirflowViewSettings_Reopened.ShowSpaceNetAirflow);
            Assert.False(partFAirflowViewSettings_Reopened.ShowContextGeometry);
            Assert.True(partFAirflowViewSettings_Reopened.ShowOutdoorAndExhaust);
            Assert.True(partFAirflowViewSettings_Reopened.ShowSupply);
        }

        /// <summary>
        /// A manually placed label survives the round trip in WORLD coordinates, keyed on the terminal's
        /// own guid. Screen pixels would be meaningless after a reopen at a different window size.
        /// </summary>
        [Fact]
        public void ManualLabelPosition_SurvivesTheRoundTripInWorldCoordinates()
        {
            Guid guid_View = Guid.NewGuid();
            Guid guid_Terminal = Guid.NewGuid();

            PartFAirflowViewSettings partFAirflowViewSettings = new() { Enabled = true };

            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid_Terminal, PartFAnnotationType.Terminal, new Point2D(12.25, -3.75)));

            AnalyticalModel analyticalModel = new(Model(guid_View, partFAirflowViewSettings).ToJsonObject());

            PartFAnnotationOverride partFAnnotationOverride = Assert.Single(Settings(analyticalModel, guid_View).AnnotationOverrides);

            Assert.Equal(guid_Terminal, partFAnnotationOverride.ObjectGuid);
            Assert.Equal(PartFAnnotationType.Terminal, partFAnnotationOverride.AnnotationType);
            Assert.Equal(12.25, partFAnnotationOverride.Position2D.X, tolerance);
            Assert.Equal(-3.75, partFAnnotationOverride.Position2D.Y, tolerance);
            Assert.True(partFAnnotationOverride.IsUserPositioned);
        }

        /// <summary>
        /// A terminal's own guid keys the override, not the space plus a role. The assessment allows more
        /// than one terminal of a role in one space, so a space-plus-role key would collide exactly where
        /// the drawing is most crowded.
        /// </summary>
        [Fact]
        public void TwoTerminalsInOneSpace_KeepSeparateLabelPositions()
        {
            Guid guid_View = Guid.NewGuid();
            Guid guid_Terminal_1 = Guid.NewGuid();
            Guid guid_Terminal_2 = Guid.NewGuid();

            PartFAirflowViewSettings partFAirflowViewSettings = new() { Enabled = true };

            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid_Terminal_1, PartFAnnotationType.Terminal, new Point2D(1, 1)));
            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid_Terminal_2, PartFAnnotationType.Terminal, new Point2D(2, 2)));

            AnalyticalModel analyticalModel = new(Model(guid_View, partFAirflowViewSettings).ToJsonObject());

            PartFAirflowViewSettings partFAirflowViewSettings_Reopened = Settings(analyticalModel, guid_View);

            Assert.Equal(2, partFAirflowViewSettings_Reopened.AnnotationOverrides.Count);
            Assert.Equal(1, partFAirflowViewSettings_Reopened.Override(guid_Terminal_1, PartFAnnotationType.Terminal).Position2D.X, tolerance);
            Assert.Equal(2, partFAirflowViewSettings_Reopened.Override(guid_Terminal_2, PartFAnnotationType.Terminal).Position2D.X, tolerance);
        }

        /// <summary>
        /// One object can carry more than one kind of annotation, and their positions stay distinct.
        /// </summary>
        [Fact]
        public void OneObject_KeepsSeparatePositionsPerAnnotationType()
        {
            Guid guid = Guid.NewGuid();

            PartFAirflowViewSettings partFAirflowViewSettings = new();

            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid, PartFAnnotationType.Transfer, new Point2D(1, 1)));
            partFAirflowViewSettings.AnnotationOverrides.Add(new PartFAnnotationOverride(guid, PartFAnnotationType.DoorRequirement, new Point2D(5, 5)));

            Assert.Equal(1, partFAirflowViewSettings.Override(guid, PartFAnnotationType.Transfer).Position2D.X, tolerance);
            Assert.Equal(5, partFAirflowViewSettings.Override(guid, PartFAnnotationType.DoorRequirement).Position2D.X, tolerance);
        }

        // ------------------------------------------------------------------
        // Backward compatibility
        // ------------------------------------------------------------------

        /// <summary>
        /// A view saved before the overlay existed carries no Part F parameter, and must reopen with the
        /// overlay OFF. Reading the absence as "defaults on" would make every existing saved view in every
        /// existing model sprout arrows the first time it was opened.
        /// </summary>
        [Fact]
        public void AViewWithNoPartFSettings_ReopensWithTheOverlayOff()
        {
            Guid guid_View = Guid.NewGuid();

            AnalyticalModel analyticalModel = new(Model(guid_View, null).ToJsonObject());

            IViewSettings viewSettings = Reopened(analyticalModel, guid_View);

            Assert.NotNull(viewSettings);

            Assert.False(((TwoDimensionalViewSettings)viewSettings).TryGetValue(Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings partFAirflowViewSettings));
            Assert.Null(partFAirflowViewSettings);

            //Which is what the overlay reads: no settings means not enabled.
            Assert.False(partFAirflowViewSettings?.Enabled ?? false);
        }

        /// <summary>
        /// A freshly constructed settings object is off, so even an explicitly attached one draws nothing
        /// until somebody turns it on.
        /// </summary>
        [Fact]
        public void NewSettings_AreDisabled()
        {
            Assert.False(new PartFAirflowViewSettings().Enabled);
        }

        /// <summary>
        /// Nothing about the view settings may carry an engineering value. This is asserted rather than
        /// left to review, because the whole separation depends on it and a well-meant "cache the rate so
        /// the drawing is quick" would be easy to add and hard to notice.
        /// </summary>
        [Fact]
        public void ViewSettings_CarryNoEngineeringValues()
        {
            List<string> names = [.. typeof(PartFAirflowViewSettings)
                .GetProperties()
                .Select(x => x.Name)

                //A "Show..." property is a visibility toggle by construction - ShowCompliance decides
                //whether the compliance SYMBOL is drawn, and holds no status of its own.
                .Where(x => !x.StartsWith("Show", StringComparison.Ordinal))
                .Where(x => x.Contains("Lps", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("FlowRate", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("Compliance", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("Terminal", StringComparison.OrdinalIgnoreCase))];

            Assert.True(names.Count == 0, string.Format("PartFAirflowViewSettings is presentation only, but carries: {0}.", string.Join(", ", names)));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static TwoDimensionalViewSettings View()
        {
            return new TwoDimensionalViewSettings(
                Guid.NewGuid(),
                "Level 0 [0m] Part F",
                Geometry.Spatial.Create.Plane(1.2),
                null,
                [typeof(Space), typeof(Panel)],
                Geometry.Object.Query.DefaultTextAppearance(),
                null);
        }

        /// <summary>A model carrying one saved view, with or without Part F settings on it.</summary>
        private static AnalyticalModel Model(Guid guid_View, PartFAirflowViewSettings partFAirflowViewSettings)
        {
            TwoDimensionalViewSettings twoDimensionalViewSettings = new(
                guid_View,
                "Level 0 [0m] Part F",
                Geometry.Spatial.Create.Plane(1.2),
                null,
                [typeof(Space), typeof(Panel)],
                Geometry.Object.Query.DefaultTextAppearance(),
                null);

            if (partFAirflowViewSettings is not null)
            {
                Assert.True(twoDimensionalViewSettings.SetValue(Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow, partFAirflowViewSettings));
            }

            UIGeometrySettings uIGeometrySettings = new();

            Assert.True(uIGeometrySettings.AddViewSettings(twoDimensionalViewSettings));

            AnalyticalModel result = new("Part F spike", null, null, null, new AdjacencyCluster());

            Assert.True(result.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings));

            return result;
        }

        private static IViewSettings Reopened(AnalyticalModel analyticalModel, Guid guid_View)
        {
            Assert.True(analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings));
            Assert.NotNull(uIGeometrySettings);

            return uIGeometrySettings.GetViewSettings(guid_View);
        }

        private static PartFAirflowViewSettings Settings(AnalyticalModel analyticalModel, Guid guid_View)
        {
            IViewSettings viewSettings = Reopened(analyticalModel, guid_View);

            ((TwoDimensionalViewSettings)viewSettings).TryGetValue(Analytical.UI.AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings result);

            return result;
        }
    }
}
