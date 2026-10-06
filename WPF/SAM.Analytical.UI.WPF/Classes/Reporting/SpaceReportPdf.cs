// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using System;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A one-Space PDF report the Reports commands can create. It names the report and the SAM.Analytical.Reporting
    /// entry point that builds its document; the command workflow (selection, save, render, errors, open) is shared
    /// and lives in <see cref="Modify.CreateSpaceReportPdf"/>. SAM_UI adds nothing to the document.
    /// </summary>
    public sealed class SpaceReportPdf
    {
        /// <summary>SAM Documentation Framework Phase 1: <c>Create.SpaceAssumptions</c>.</summary>
        public static SpaceReportPdf SpaceAssumptions { get; } = new SpaceReportPdf(
            "SpaceAssumptionsPdf",
            "Space Assumptions",
            (documentContext, space) => Analytical.Reporting.Create.SpaceAssumptions(documentContext, space));

        /// <summary>
        /// SAM Documentation Framework Phase 2: <c>Create.SpaceDesignLoadSummary</c>. No result source is passed, so
        /// every source is read and more than one candidate stays Ambiguous in the document: SAM_UI never picks one.
        /// </summary>
        public static SpaceReportPdf SpaceDesignLoadSummary { get; } = new SpaceReportPdf(
            "SpaceDesignLoadSummaryPdf",
            "Space Design Load Summary",
            (documentContext, space) => Analytical.Reporting.Create.SpaceDesignLoadSummary(documentContext, space));

        internal SpaceReportPdf(string id, string name, Func<Analytical.Reporting.DocumentContext, Space, Document> createDocument)
        {
            Id = id;
            Name = name;
            CreateDocument = createDocument;
        }

        /// <summary>Used in control names, e.g. "MenuItem_SpaceAssumptionsPdf".</summary>
        public string Id { get; }

        /// <summary>The report's name, e.g. "Space Assumptions"; also the default file name's suffix.</summary>
        public string Name { get; }

        /// <summary>The command's name and message caption, e.g. "Space Assumptions PDF".</summary>
        public string Title => Name + " PDF";

        internal Func<Analytical.Reporting.DocumentContext, Space, Document> CreateDocument { get; }
    }
}
