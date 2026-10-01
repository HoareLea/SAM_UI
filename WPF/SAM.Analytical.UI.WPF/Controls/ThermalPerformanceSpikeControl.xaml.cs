// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// B0 docking spike (read-only): the Thermal Performance side panel. It only displays; the host (the analytical window,
    /// or a modeless tool window) hands it the model and the current selection.
    /// </summary>
    public partial class ThermalPerformanceSpikeControl : UserControl
    {
        public ThermalPerformanceSpikeControl()
        {
            InitializeComponent();
            Update(null, null);
        }

        /// <summary>Shows the selected panels / apertures of <paramref name="analyticalModel"/> by construction.</summary>
        public void Update(AnalyticalModel analyticalModel, IEnumerable<SAMObject> selected)
        {
            List<ThermalSelectionRow> rows = Query.ThermalSelectionRows(analyticalModel, selected);
            itemsControl_Rows.ItemsSource = rows;

            int elements = rows.Sum(x => x.SelectedCount);
            textBlock_Summary.Text = analyticalModel == null
                ? "No model."
                : elements == 0
                    ? "Select panels or apertures in the view."
                    : string.Format(CultureInfo.CurrentCulture, "{0} {1} selected · {2} {3}", elements, elements == 1 ? "element" : "elements", rows.Count, rows.Count == 1 ? "construction" : "constructions");
        }
    }
}
