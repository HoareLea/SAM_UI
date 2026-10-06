// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Part F airflow tab: the dwelling's REAL floor plan with the airflow drawn over it.
    /// <para>
    /// <b>This tab is for checking and debugging the assessment, and it is not the drawing interface.</b>
    /// The drawing an engineer issues is a normal saved 2D Section or Floor Plan view with the Part F
    /// annotation switched on. That is why every line of the drawing itself lives in
    /// <see cref="PartFAirflowRenderer"/> and is shared with the saved view: if this tab had a renderer of
    /// its own, the thing being checked here would not be the thing being issued there.
    /// </para>
    /// <para>
    /// So this file does four things and no more: it loads the plan for the selected dwelling and level, it
    /// turns the "Show" checkboxes into the <see cref="PartFAirflowViewSettings"/> the renderer is driven by,
    /// it reports what is selected, and it says so when there is no model to draw. Nothing here computes a
    /// rate, a direction, a status or a position.
    /// </para>
    /// </summary>
    public partial class PartFAssessmentWindow
    {
        /// <summary>Height [m] above a level's floor that the plan is cut at, the usual plan convention.</summary>
        private const double cutHeight_M = 1.2;

        private AnalyticalModel analyticalModel;
        private List<double> elevations = [];

        /// <summary>
        /// The one Part F renderer, shared with the normal saved 2D view. Created once, with the plan it
        /// draws on; it listens to the plan's own view changes and redraws itself.
        /// </summary>
        private PartFAirflowRenderer renderer;

        /// <summary>
        /// The model the plan is drawn from. Without one the window still works - every grid, the report
        /// and the schematic are unaffected - and the airflow tab says so instead of drawing a diagram of
        /// its own devising.
        /// </summary>
        public AnalyticalModel AnalyticalModel
        {
            get
            {
                return analyticalModel;
            }

            set
            {
                analyticalModel = value;
                LoadFloorPlan();
            }
        }

        /// <summary>
        /// The drawing scale the annotation is laid out for, as its denominator: 50 means 1:50. Setting it
        /// lays the tags out again; the camera never does. See
        /// <see cref="PartFAirflowViewSettings.AnnotationScale"/>.
        /// </summary>
        public double AnnotationScale
        {
            get
            {
                return annotationScale;
            }

            set
            {
                double annotationScale_New = value > 0 ? value : PartFTagPlacement.DefaultAnnotationScale;

                if (annotationScale_New == annotationScale)
                {
                    return;
                }

                annotationScale = annotationScale_New;

                ApplyViewSettings();
            }
        }

        private double annotationScale = PartFTagPlacement.DefaultAnnotationScale;

        /// <summary>The level currently shown, or null where the dwelling sits on one level.</summary>
        private double? Elevation
        {
            get
            {
                int index = ComboBox_Level.SelectedIndex;

                return index >= 0 && index < elevations.Count ? elevations[index] : null;
            }
        }

        /// <summary>
        /// Connects the plan. Called once from the constructor.
        /// </summary>
        private void InitialiseFloorPlan()
        {
            renderer = new PartFAirflowRenderer(FloorPlan);

            //Selection comes from the plan's own hit testing, on the real spaces and panels, so clicking a
            //room selects that room rather than whatever the overlay happened to draw on top of it.
            FloorPlan.ObjectSelectionChanged += FloorPlan_ObjectSelectionChanged;
            FloorPlan.MouseLeftButtonUp += FloorPlan_MouseLeftButtonUp;
        }

        /// <summary>
        /// Re-reads the values at the current operating condition without touching a single ANCHOR, and
        /// redraws. The topology is not recomputed - switching between continuous, high, setback and measured
        /// changes what the system is doing, not where the rooms are.
        /// </summary>
        private void Refresh()
        {
            ApplyViewSettings();

            renderer?.Refresh();
        }

        /// <summary>
        /// Hands the "Show" checkboxes and the operating condition to the renderer as view settings.
        /// <para>
        /// The checkboxes ARE the view settings for this tab. A saved 2D view stores the same object, which
        /// is what lets one renderer serve both without either owning a drawing rule.
        /// </para>
        /// </summary>
        private void ApplyViewSettings()
        {
            if (renderer is null)
            {
                return;
            }

            bool terminals = CheckBox_Terminals.IsChecked == true;

            //No DwellingScope: this tab draws the dwelling already selected in the window and hands the
            //renderer that dwelling's assessment directly, so it never asks a scope which dwellings to
            //assess. The scope belongs to a SAVED view, which has to reproduce an assessment from what it
            //stored - see PartFAssessmentCache.
            renderer.ViewSettings = new PartFAirflowViewSettings()
            {
                Enabled = true,
                OperatingMode = OperatingMode,
                AnnotationScale = AnnotationScale,

                //One "Terminal tags" checkbox here, three air types in the settings: the saved view can show
                //supply without extract, and this tab has never needed to.
                ShowSupply = terminals,
                ShowGeneralExtract = terminals,
                ShowLocalKitchenExtract = terminals,

                ShowTransfer = CheckBox_Transfer.IsChecked == true,
                ShowUnresolved = CheckBox_Unresolved.IsChecked == true,
                ShowValues = CheckBox_Values.IsChecked == true,
                ShowCompliance = CheckBox_Compliance.IsChecked == true,
                ShowDoorRequirements = CheckBox_DoorData.IsChecked == true,
                ShowContextGeometry = CheckBox_Context.IsChecked == true,
            };
        }

        // ------------------------------------------------------------------
        // Loading
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the plan for the selected dwelling and level. Called when either changes - NOT when
        /// the operating mode changes, which only alters the numbers on the marks.
        /// </summary>
        private void LoadFloorPlan()
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            PartFComplianceResult complianceResult = Selected?.ComplianceResult;

            if (adjacencyCluster is null || complianceResult is null)
            {
                FloorPlan.Clear();
                renderer?.Clear();

                TextBlock_PlanMessage.Text = adjacencyCluster is null
                    ? "No analytical model was supplied to this window, so the floor plan cannot be drawn. Every schedule, the checks and the report are unaffected, and the Schematic tab shows the airflow as text."
                    : "This dwelling has no assessment to draw.";

                TextBlock_PlanMessage.Visibility = Visibility.Visible;

                return;
            }

            TextBlock_PlanMessage.Visibility = Visibility.Collapsed;

            LoadLevels(adjacencyCluster, complianceResult);

            double elevation = Elevation ?? 0;

            Plane plane = Geometry.Spatial.Create.Plane(elevation + cutHeight_M);

            TwoDimensionalViewSettings twoDimensionalViewSettings = new(
                Guid.NewGuid(),
                string.Format("Part F {0}", Selected?.Name ?? "dwelling"),
                plane,
                null,
                [typeof(Space), typeof(Panel), typeof(Aperture)],
                Geometry.Object.Query.DefaultTextAppearance(),
                null);

            GeometryObjectModel geometryObjectModel = analyticalModel.ToSAM_GeometryObjectModel(twoDimensionalViewSettings);

            FloorPlan.Load(geometryObjectModel);
            FloorPlan.ZoomExtents();

            ApplyViewSettings();

            //One dwelling on this tab: it reviews one assessment at a time. The saved 2D view passes every
            //dwelling on the level, and the renderer builds one overlay per result either way, so transfer
            //air is never routed between two flats.
            renderer.Load(adjacencyCluster, [complianceResult], geometryObjectModel);
        }

        /// <summary>
        /// Fills the level selector from the dwelling's OWN spaces, so a single-storey flat in a
        /// multi-storey block offers one level rather than the whole building's.
        /// </summary>
        private void LoadLevels(AdjacencyCluster adjacencyCluster, PartFComplianceResult partFComplianceResult)
        {
            HashSet<Guid> guids = [.. (partFComplianceResult.Terminals ?? []).Select(x => x.SpaceGuid)];

            List<double> result = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                if (space is null || !guids.Contains(space.Guid))
                {
                    continue;
                }

                double elevation = space.MinElevation(adjacencyCluster);
                if (double.IsNaN(elevation) || double.IsInfinity(elevation))
                {
                    continue;
                }

                //Rounded to the millimetre before grouping: two spaces on one floor slab can differ by a
                //rounding artefact, and an untidied list would offer the same level twice.
                elevation = System.Math.Round(elevation, 3);

                if (!result.Contains(elevation))
                {
                    result.Add(elevation);
                }
            }

            result.Sort();

            if (result.SequenceEqual(elevations))
            {
                return;
            }

            elevations = result;

            bool loading_Previous = loading;
            loading = true;

            ComboBox_Level.Items.Clear();
            foreach (double elevation in elevations)
            {
                ComboBox_Level.Items.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.###} m", elevation));
            }

            loading = loading_Previous;

            if (ComboBox_Level.Items.Count != 0)
            {
                ComboBox_Level.SelectedIndex = 0;
            }

            //A dwelling on one level has no level to choose, and a selector with one entry is clutter.
            ComboBox_Level.Visibility = elevations.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            TextBlock_Level.Visibility = ComboBox_Level.Visibility;
        }

        // ------------------------------------------------------------------
        // Selection
        // ------------------------------------------------------------------

        /// <summary>
        /// A space clicked on the plan shows its terminals and its net airflow; where the space carries
        /// more than one terminal, all of them are shown, because a studio's supply and its local kitchen
        /// extract are both the answer to "what does this room do".
        /// </summary>
        private void FloorPlan_ObjectSelectionChanged(object sender, ObjectSelectionChangedEventArgs e)
        {
            PartFComplianceResult complianceResult = Selected?.ComplianceResult;
            if (complianceResult is null)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = FloorPlan.SelectedSAMObjects();

            //A door first: clicking one is a more specific request than clicking the room it sits in.
            PartFDoorTransferData partFDoorTransferData = PartFSelectionResolver.TransferPath(sAMObjects, complianceResult);
            if (partFDoorTransferData is not null)
            {
                Show(partFDoorTransferData);
                return;
            }

            //Searched by guid across the WHOLE selection, not taken from the front of it. A click can
            //select several objects and they arrive unordered, so picking the first one reported a
            //bedroom as having no terminals while this same view drew its supply tag.
            Guid guid_Space = PartFSelectionResolver.SpaceGuid(sAMObjects, complianceResult);
            if (guid_Space == Guid.Empty)
            {
                Space space_Unassessed = sAMObjects?.OfType<Space>().FirstOrDefault();
                if (space_Unassessed is not null)
                {
                    TextBlock_Selection.Text = string.Format("{0}{1}{1}No Part F terminal is required in this space.{1}It may belong to another dwelling, to a communal area, or to a category that takes neither supply nor extract.", space_Unassessed.Name, Environment.NewLine);
                }

                return;
            }

            List<PartFVentilationTerminalRequirement> terminals = PartFSelectionResolver.Terminals(guid_Space, complianceResult);

            List<string> lines =
            [
                terminals[0].SpaceName,
                string.Format("Operating mode shown: {0}", Core.Query.Description(OperatingMode)),
                string.Empty,
            ];

            double net = 0;

            foreach (PartFVentilationTerminalRequirement terminal in terminals)
            {
                double? rate = PartFSchematic.Rate(terminal, OperatingMode);

                if (rate is not null && terminal.IsInBalancedFlow)
                {
                    net += terminal.IsExtract ? -rate.Value : rate.Value;
                }

                lines.Add(string.Format("{0}: {1}", Core.Query.Description(terminal.TerminalRole), Rate(rate)));
                lines.Add(string.Format("   continuous {0}, high {1}, setback {2}, measured {3}", Rate(terminal.ContinuousDesignFlowRate_Lps), Rate(terminal.HighFlowRate_Lps), Rate(terminal.SetbackFlowRate_Lps), Rate(terminal.MeasuredContinuousFlowRate_Lps)));
                lines.Add(string.Format("   required high rate {0}, provision {1}", Rate(terminal.RequiredHighFlowRate_Lps), Core.Query.Description(terminal.ProvisionStatus)));
                lines.Add(string.Format("   status {0}", Core.Query.Description(terminal.ComplianceStatus)));
                lines.Add(string.Empty);
            }

            lines.Add(string.Format("Net airflow: {0}", Rate(net)));

            List<PartFDoorTransferData> routes = PartFSelectionResolver.TransferPaths(guid_Space, complianceResult);
            if (routes.Count != 0)
            {
                lines.Add(string.Empty);
                lines.Add("Connected transfer routes:");
                routes.ForEach(x => lines.Add(string.Format("   {0} to {1}, {2}{3}", x.UpstreamSpaceName, x.DownstreamSpaceName, Rate(PartFSchematic.Rate(x, OperatingMode)), x.IsOpeningUnresolved ? " (opening not established)" : string.Empty)));
            }

            TextBlock_Selection.Text = string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// A click near a transfer route selects the route it stands for. Handled here rather than through
        /// the plan's hit testing because a transfer mark belongs to the opening between two spaces, which
        /// is not a thing the plan has a visual for.
        /// </summary>
        private void FloorPlan_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartFComplianceResult complianceResult = Selected?.ComplianceResult;
            if (renderer is null || complianceResult is null)
            {
                return;
            }

            System.Windows.Point point = e.GetPosition(FloorPlan);

            System.Windows.Media.Matrix matrix = FloorPlan.WorldToScreen;

            PartFOverlayMark mark_Nearest = null;
            double distance_Nearest = double.MaxValue;

            //Only marks the renderer actually placed, which is exactly the set it drew: a mark hidden by a
            //visibility setting must not be selectable, and asking the renderer is how this avoids keeping a
            //second copy of the visibility rules.
            foreach (PartFOverlayMark mark in renderer.Marks.Where(x => x.IsTransfer && renderer.Placement(x) is not null))
            {
                System.Windows.Point point_Mid = matrix.Transform(new System.Windows.Point((mark.Start.X + mark.End.X) / 2, (mark.Start.Y + mark.End.Y) / 2));

                double distance = (point_Mid - point).Length;
                if (distance < distance_Nearest)
                {
                    distance_Nearest = distance;
                    mark_Nearest = mark;
                }
            }

            //A generous but finite radius in SCREEN pixels: close enough to be a deliberate click on the
            //route, and never so far that clicking empty floor selects a route across the flat.
            if (mark_Nearest is null || distance_Nearest > 24)
            {
                return;
            }

            Show((complianceResult.TransferPaths ?? []).Find(x => x is not null && string.Equals(x.Name, mark_Nearest.DoorName, StringComparison.Ordinal) && x.UpstreamSpaceGuid == mark_Nearest.SpaceGuid));
        }
    }
}
