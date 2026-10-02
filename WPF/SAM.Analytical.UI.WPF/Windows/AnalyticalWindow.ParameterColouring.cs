// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using SAM.Geometry.UI;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF.Windows
{
    // Colouring a 3D view by a parameter (Thermal Performance "Colour by U-value" is the first caller).
    //
    // It is VIEW state only. View Settings and the Legend window store their result in the model (UIGeometrySettings, with an Undo entry); this
    // does not: the colouring is a temporary override held by this window, per view, and applied only where the view is rendered
    // (UpdateTabItem -> RenderViewSettings). The saved view settings are never changed, nothing calls SetJSAMObject, there is no Undo entry,
    // and the save-time view-settings sync keeps the saved settings (StoredViewSettings), so the model on disk is the same whether or not a
    // view is coloured. The rendering is the existing pipeline: Create.ParameterColouredViewSettings builds the copy of the view's
    // settings, ToSAM_GeometryObjectModel renders it, and the viewport shows the legend that rendering carries.
    //
    // The override ends when the model is closed or opened, when the view is removed, or when the user takes the view over by opening
    // View Settings or Legend Settings (those edit the saved settings, which would otherwise be hidden behind the override).
    public partial class AnalyticalWindow
    {
        private readonly Dictionary<Guid, ParameterColouring> parameterColourings = new Dictionary<Guid, ParameterColouring>();

        /// <summary>The colouring the view <paramref name="viewGuid"/> is shown with; null when it is shown as saved.</summary>
        internal ParameterColouring ParameterColouringOf(Guid viewGuid)
        {
            return parameterColourings.TryGetValue(viewGuid, out ParameterColouring parameterColouring) ? parameterColouring : null;
        }

        /// <summary>The Guid of the active view when it is a 3D view, else <see cref="Guid.Empty"/>.</summary>
        internal Guid ActiveThreeDimensionalViewGuid(out bool showsPanels)
        {
            showsPanels = false;

            ViewportControl viewportControl = GetActiveViewportControl();
            if (viewportControl == null || GetActiveViewSettings() is not ThreeDimensionalViewSettings threeDimensionalViewSettings)
            {
                return Guid.Empty;
            }

            showsPanels = threeDimensionalViewSettings.ContainsType(typeof(Panel));
            return viewportControl.Guid;
        }

        /// <summary>
        /// Colours the view <paramref name="viewGuid"/> by <paramref name="colouring"/>, or shows it as saved again when
        /// <paramref name="colouring"/> is null. Only that view is regenerated. Returns false when the view cannot be coloured (not a
        /// 3D view, or it does not show the coloured type).
        /// </summary>
        internal bool SetParameterColouring(Guid viewGuid, ParameterColouring colouring)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || viewGuid == Guid.Empty)
            {
                return false;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return false;
            }

            if (uIGeometrySettings.GetViewSettings(viewGuid) is not ThreeDimensionalViewSettings stored)
            {
                return false;
            }

            if (colouring != null)
            {
                if (!stored.ContainsType(colouring.ElementType))
                {
                    return false;
                }

                parameterColourings[viewGuid] = colouring;
            }
            else if (!parameterColourings.Remove(viewGuid))
            {
                return true;
            }

            RegenerateView(analyticalModel, stored);
            RefreshThermalPerformance();
            return true;
        }

        // The view is regenerated in place, like any view-settings change that touches the geometry, but nothing is committed to the model.
        private void RegenerateView(AnalyticalModel analyticalModel, IViewSettings stored)
        {
            if (stored == null)
            {
                return;
            }

            tabControl.SelectionChanged -= TabControl_SelectionChanged;
            try
            {
                UpdateTabItem(tabControl, analyticalModel, new ModifiedEventArgs(new ViewSettingsModification(stored, true)), stored);
            }
            finally
            {
                tabControl.SelectionChanged += TabControl_SelectionChanged;
            }
        }

        /// <summary>Ends the colouring of the view (the user is about to edit its saved settings) and shows it as saved.</summary>
        private void ClearParameterColouring(Guid viewGuid)
        {
            if (viewGuid != Guid.Empty && parameterColourings.ContainsKey(viewGuid))
            {
                SetParameterColouring(viewGuid, null);
            }
        }

        /// <summary>The settings a view is rendered with: the saved ones, or a coloured copy of them while the view is coloured.</summary>
        private IViewSettings RenderViewSettings(AnalyticalModel analyticalModel, IViewSettings stored)
        {
            if (stored is ThreeDimensionalViewSettings threeDimensionalViewSettings && parameterColourings.TryGetValue(stored.Guid, out ParameterColouring colouring))
            {
                return analyticalModel.ParameterColouredViewSettings(threeDimensionalViewSettings, colouring) ?? stored;
            }

            return stored;
        }

        /// <summary>The settings of the active view as it is rendered now (the coloured copy while it is coloured); for reading only.</summary>
        private IViewSettings RenderedActiveViewSettings()
        {
            IViewSettings stored = GetActiveViewSettings();
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            return stored == null || analyticalModel == null ? stored : RenderViewSettings(analyticalModel, stored);
        }

        // The saved settings of a view that is coloured, in place of the copy its geometry carries; the settings unchanged otherwise.
        private IViewSettings StoredViewSettings(UIGeometrySettings uIGeometrySettings, IViewSettings viewSettings)
        {
            if (viewSettings != null && parameterColourings.ContainsKey(viewSettings.Guid))
            {
                return uIGeometrySettings?.GetViewSettings(viewSettings.Guid) ?? viewSettings;
            }

            return viewSettings;
        }
    }
}
