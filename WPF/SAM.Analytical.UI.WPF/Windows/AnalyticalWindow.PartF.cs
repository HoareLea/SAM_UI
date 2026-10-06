// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Part F airflow annotation on the NORMAL saved 2D views.
    /// <para>
    /// This is where Part F becomes a drawing rather than a checking tool. A saved Section or Floor Plan view
    /// carries a <see cref="PartFAirflowViewSettings"/>, so a view called "Level 0 [0m] Part F" can hold its
    /// colour scheme AND its airflow annotation and reopen with both. The assessment window's airflow tab
    /// remains for checking the assessment; both draw through the same
    /// <see cref="PartFAirflowRenderer"/>, so what is checked there is what is issued here.
    /// </para>
    /// <para>
    /// The existing <c>PartF Data</c> colour scheme is untouched and orthogonal: it is a
    /// <c>ValueAppearanceSettings</c> that colours the space fills, and it works with or without the
    /// annotation. Both at once is the intended combination.
    /// </para>
    /// </summary>
    public partial class AnalyticalWindow
    {
        /// <summary>
        /// One renderer per view, keyed on the view's guid. Views come and go with their tabs, and each one
        /// draws on its own plan with its own settings.
        /// </summary>
        private readonly Dictionary<Guid, PartFAirflowRenderer> dictionary_PartFAirflowRenderer = [];

        /// <summary>
        /// The assessment behind every Part F view in this window, and the gate that stops one being made
        /// for a drawing whose dwelling scope nobody has chosen. See <see cref="PartFAssessmentCache"/>.
        /// </summary>
        private readonly PartFAssessmentCache partFAssessmentCache = new();

        /// <summary>
        /// Draws, refreshes or removes the Part F annotation on one view, from the view's own settings.
        /// Called after the view's geometry has been loaded, so the plan and its text are there to annotate
        /// and to keep clear of.
        /// </summary>
        private void UpdatePartFAirflow(ViewportControl viewportControl, AnalyticalModel analyticalModel, IViewSettings viewSettings, GeometryObjectModel geometryObjectModel)
        {
            if (viewportControl is null || viewSettings is null)
            {
                return;
            }

            PartFAirflowViewSettings partFAirflowViewSettings = PartFAirflowViewSettings(viewSettings);

            FloorPlan2DControl floorPlan2DControl = viewportControl.FloorPlan2D;

            //Nothing to draw, or nowhere to draw it: a 3D view, or the legacy orthographic 2D path. Any
            //renderer this view had is torn down rather than left holding a plan that is no longer shown.
            //A view whose dwelling scope has not been chosen takes this branch too: it is not yet a drawing
            //of anything, so it gets no renderer and - see PartFAssessmentCache - no assessment either.
            if (partFAirflowViewSettings is null || !partFAirflowViewSettings.Enabled || !partFAirflowViewSettings.HasDwellingScope || floorPlan2DControl is null || analyticalModel?.AdjacencyCluster is null)
            {
                RemovePartFAirflow(viewportControl.Guid);
                return;
            }

            if (!dictionary_PartFAirflowRenderer.TryGetValue(viewportControl.Guid, out PartFAirflowRenderer partFAirflowRenderer) || partFAirflowRenderer is null)
            {
                partFAirflowRenderer = new PartFAirflowRenderer(floorPlan2DControl);

                dictionary_PartFAirflowRenderer[viewportControl.Guid] = partFAirflowRenderer;
            }

            partFAirflowRenderer.ViewSettings = partFAirflowViewSettings;

            partFAirflowRenderer.Load(analyticalModel.AdjacencyCluster, partFAssessmentCache.Results(analyticalModel, partFAirflowViewSettings), geometryObjectModel);
        }

        /// <summary>Takes the Part F annotation off a view and stops the renderer listening to it.</summary>
        private void RemovePartFAirflow(Guid guid)
        {
            if (!dictionary_PartFAirflowRenderer.TryGetValue(guid, out PartFAirflowRenderer partFAirflowRenderer))
            {
                return;
            }

            partFAirflowRenderer?.Clear();
            partFAirflowRenderer?.Detach();

            dictionary_PartFAirflowRenderer.Remove(guid);
        }

        /// <summary>
        /// The view's Part F presentation settings, or null where it has none.
        /// <para>
        /// Absence means OFF, deliberately. Every view saved before the annotation existed carries no such
        /// parameter, and reading absence as "defaults on" would make every one of them sprout tags the first
        /// time it was reopened.
        /// </para>
        /// </summary>
        private static PartFAirflowViewSettings PartFAirflowViewSettings(IViewSettings viewSettings)
        {
            return viewSettings is ViewSettings viewSettings_Temp
                && viewSettings_Temp.TryGetValue(AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings result)
                ? result
                : null;
        }
    }
}
