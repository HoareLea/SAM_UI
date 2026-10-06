// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object;
using SAM.Geometry.Planar;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Ventilation Design airflow annotation on the NORMAL saved 2D views.
    /// <para>
    /// The design counterpart to <see cref="AnalyticalWindow.PartF"/>: a saved Section or Floor Plan view
    /// carries a <see cref="DesignAirFlowViewSettings"/> alongside its (independent) Part F settings, so
    /// both overlays can be shown together, separately, or not at all. Nothing here reads or writes
    /// <c>PartFComplianceResult</c>, a <c>PartFSpaceData</c> parameter, or any other Part F authority - the
    /// values drawn come only from <c>VentilationTerminal.DesignFlowRate_Lps</c> through
    /// <see cref="DesignAirFlowFloorPlanOverlay"/>.
    /// </para>
    /// <para>
    /// <b>Called after <see cref="UpdatePartFAirflow"/>, on purpose.</b> Where a <see cref="PartFAirflowRenderer"/>
    /// is already drawing on this same view, its currently solved tag rectangles are read here - through
    /// <see cref="PartFAirflowRenderer.PlacedRectangle2Ds"/> - and handed to the design renderer as
    /// read-only obstacles, so the two overlays' tags do not land on top of one another without merging
    /// into a single renderer or a second collision solver. Nothing is ever read back the other way: Part
    /// F's own layout never depends on whether this overlay exists.
    /// </para>
    /// </summary>
    public partial class AnalyticalWindow
    {
        /// <summary>
        /// One renderer per view, keyed on the view's guid, matching <c>dictionary_PartFAirflowRenderer</c>.
        /// </summary>
        private readonly Dictionary<Guid, DesignAirFlowRenderer> dictionary_DesignAirFlowRenderer = [];

        /// <summary>
        /// Draws, refreshes or removes the Ventilation Design overlay on one view, from the view's own
        /// settings. Called after the view's geometry has been loaded, alongside <see cref="UpdatePartFAirflow"/>.
        /// </summary>
        private void UpdateVentilationDesignAirflow(ViewportControl viewportControl, AnalyticalModel analyticalModel, IViewSettings viewSettings, GeometryObjectModel geometryObjectModel)
        {
            if (viewportControl is null || viewSettings is null)
            {
                return;
            }

            DesignAirFlowViewSettings designAirFlowViewSettings = DesignAirFlowViewSettings(viewSettings);

            FloorPlan2DControl floorPlan2DControl = viewportControl.FloorPlan2D;

            //Nothing to draw, or nowhere to draw it: a 3D view, or a view never told about the overlay. Any
            //renderer this view had is torn down rather than left holding a plan that is no longer shown.
            if (designAirFlowViewSettings is null || !designAirFlowViewSettings.Enabled || floorPlan2DControl is null || analyticalModel?.AdjacencyCluster is null)
            {
                RemoveVentilationDesignAirflow(viewportControl.Guid);
                return;
            }

            if (!dictionary_DesignAirFlowRenderer.TryGetValue(viewportControl.Guid, out DesignAirFlowRenderer designAirFlowRenderer) || designAirFlowRenderer is null)
            {
                designAirFlowRenderer = new DesignAirFlowRenderer(floorPlan2DControl);

                dictionary_DesignAirFlowRenderer[viewportControl.Guid] = designAirFlowRenderer;
            }

            designAirFlowRenderer.ViewSettings = designAirFlowViewSettings;

            double annotationScale = DesignAnnotationScale(viewSettings);

            //Part F's own tags on this SAME view, read-only, so this overlay's tags are solved clear of
            //them - see the type's own remarks. Absent where Part F is not drawing on this view at all, in
            //which case this overlay has the room to itself.
            List<Rectangle2D> partFObstacle2Ds = null;

            if (dictionary_PartFAirflowRenderer.TryGetValue(viewportControl.Guid, out PartFAirflowRenderer partFAirflowRenderer)
                && partFAirflowRenderer is not null
                && partFAirflowRenderer.ViewSettings.Enabled)
            {
                partFObstacle2Ds = partFAirflowRenderer.PlacedRectangle2Ds();
            }

            designAirFlowRenderer.Load(analyticalModel.AdjacencyCluster, geometryObjectModel, partFObstacle2Ds, annotationScale);
        }

        /// <summary>Takes the Ventilation Design overlay off a view and stops the renderer listening to it.</summary>
        private void RemoveVentilationDesignAirflow(Guid guid)
        {
            if (!dictionary_DesignAirFlowRenderer.TryGetValue(guid, out DesignAirFlowRenderer designAirFlowRenderer))
            {
                return;
            }

            designAirFlowRenderer?.Clear();
            designAirFlowRenderer?.Detach();

            dictionary_DesignAirFlowRenderer.Remove(guid);
        }

        /// <summary>
        /// The view's Ventilation Design presentation settings, or null where it has none. Absence means
        /// OFF, matching <see cref="PartFAirflowViewSettings"/>'s own rule.
        /// </summary>
        private static DesignAirFlowViewSettings DesignAirFlowViewSettings(IViewSettings viewSettings)
        {
            return viewSettings is ViewSettings viewSettings_Temp
                && viewSettings_Temp.TryGetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, out DesignAirFlowViewSettings result)
                ? result
                : null;
        }

        /// <summary>
        /// The drawing scale this overlay's tags are sized and solved at, for one view.
        /// <para>
        /// Read from the view's own <see cref="PartFAirflowViewSettings"/> whenever it has one - WHETHER OR
        /// NOT Part F is currently enabled on that view. A view that has Part F set up at, say, 1:100 and
        /// then merely switches the Part F overlay off must not have its Ventilation Design tags jump to a
        /// different physical size for no reason a person asked for: the scale is a property of the
        /// DRAWING, the same way <see cref="PartFAirflowViewSettings.AnnotationScale"/>'s own remarks
        /// describe, and it should not follow a checkbox that has nothing to do with it.
        /// </para>
        /// <para>
        /// Only a view that has NEVER carried Part F settings at all - <c>PartFAirflowViewSettings(viewSettings)</c>
        /// is null - falls back to <see cref="PartFTagPlacement.DefaultAnnotationScale"/>. This is
        /// deliberately independent of which rectangles are used as OBSTACLES in
        /// <see cref="UpdateVentilationDesignAirflow"/>, which correctly does stop reading Part F's tags the
        /// moment its overlay is disabled - there is nothing left on the drawing to keep clear of - so the
        /// two questions ("what size" and "what to avoid") are answered from different things on purpose.
        /// </para>
        /// </summary>
        private static double DesignAnnotationScale(IViewSettings viewSettings)
        {
            return PartFAirflowViewSettings(viewSettings)?.AnnotationScale ?? PartFTagPlacement.DefaultAnnotationScale;
        }
    }
}
