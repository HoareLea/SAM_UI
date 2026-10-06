// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Core.Reporting.Pdf;
using SAM.Units;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// A one-Space report PDF command (Space Assumptions, Space Design Load Summary): resolves the one selected
        /// Space, asks where to save, writes the PDF and offers to open it. Every calculation, result read, unit,
        /// placeholder and layout decision belongs to SAM.Analytical.Reporting and SAM.Core.Reporting.Pdf; this is
        /// orchestration only.
        /// <para>
        /// Units: SI (the reporting default, air flow in L/s). SAM_UI has no unit-system preference to follow, and
        /// adds none; <see cref="WriteSpaceReportPdf"/> takes the unit system for callers that have one.
        /// </para>
        /// </summary>
        /// <returns>The result, or null when nothing was attempted (no model, a refused selection, or Save cancelled).</returns>
        public static SpaceReportPdfResult? CreateSpaceReportPdf(this UIAnalyticalModel? uIAnalyticalModel, IEnumerable<Space>? spaces, SpaceReportPdf spaceReportPdf, System.Windows.Window? owner = null)
        {
            return CreateSpaceReportPdf(uIAnalyticalModel, spaces, spaceReportPdf, SpaceReportPdfPrompts.Dialogs(owner));
        }

        /// <summary>
        /// The command workflow with its user interaction supplied, so tests can drive it. Units are always the
        /// reporting default (SI): the command has no unit option to pass.
        /// </summary>
        internal static SpaceReportPdfResult? CreateSpaceReportPdf(UIAnalyticalModel? uIAnalyticalModel, IEnumerable<Space>? spaces, SpaceReportPdf spaceReportPdf, SpaceReportPdfPrompts spaceReportPdfPrompts, IDocumentRenderer? documentRenderer = null)
        {
            if (spaceReportPdf == null)
            {
                throw new ArgumentNullException(nameof(spaceReportPdf));
            }

            if (spaceReportPdfPrompts == null)
            {
                throw new ArgumentNullException(nameof(spaceReportPdfPrompts));
            }

            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;

            Space? space = Query.SpaceReportPdfSpace(analyticalModel, spaces, spaceReportPdf, out string? refusal);
            if (space == null || analyticalModel == null || uIAnalyticalModel == null)
            {
                spaceReportPdfPrompts.ShowMessage(refusal ?? "Select one Space.", spaceReportPdf.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            string? directory = null;
            try
            {
                directory = string.IsNullOrWhiteSpace(uIAnalyticalModel.Path) ? null : Path.GetDirectoryName(uIAnalyticalModel.Path);
            }
            catch (ArgumentException)
            {
            }

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                directory = null;
            }

            string? path = spaceReportPdfPrompts.ChoosePath("Save " + spaceReportPdf.Title, Query.SpaceReportPdfFileName(space, spaceReportPdf), directory);
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            SpaceReportPdfResult result;

            Cursor? cursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                result = WriteSpaceReportPdf(analyticalModel, space, path, spaceReportPdf, documentRenderer: documentRenderer);
            }
            finally
            {
                Mouse.OverrideCursor = cursor;
            }

            if (!result.Succeeded)
            {
                System.Diagnostics.Trace.TraceError("{0} ({1}) failed for '{2}': {3}", spaceReportPdf.Title, result.Failure, result.Path, result.Exception);

                spaceReportPdfPrompts.ShowMessage(result.Message ?? string.Format("The {0} could not be created.", spaceReportPdf.Title), spaceReportPdf.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return result;
            }

            MessageBoxResult messageBoxResult = spaceReportPdfPrompts.ShowMessage(string.Format("{0} saved:\n{1}\n\nOpen it now?", spaceReportPdf.Title, result.Path), spaceReportPdf.Title, MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                try
                {
                    spaceReportPdfPrompts.Open(result.Path!);
                }
                catch (Exception exception)
                {
                    spaceReportPdfPrompts.ShowMessage(string.Format("The PDF was saved, but it could not be opened: {0}", exception.Message), spaceReportPdf.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return result;
        }

        /// <summary>
        /// Builds the <paramref name="spaceReportPdf"/> document for <paramref name="space"/> with the SAM reporting
        /// API, renders it and writes it to <paramref name="path"/>. Missing engineering data or results are not a
        /// failure: the document prints them (not set, not simulated, peaks not recorded, ambiguous source). A
        /// software failure is returned, with its exception, as a failed result naming the stage.
        /// <para>
        /// The PDF is rendered in memory and staged beside the destination before it replaces it, so a failure
        /// never leaves a partial file and never destroys a PDF already at the path.
        /// </para>
        /// </summary>
        /// <param name="unitStyle">Passed to the reporting framework unchanged; SI is its default.</param>
        /// <param name="documentRenderer">The renderer; null means <see cref="PdfRenderer"/>. Tests pass a stand-in.</param>
        public static SpaceReportPdfResult WriteSpaceReportPdf(AnalyticalModel analyticalModel, Space space, string? path, SpaceReportPdf spaceReportPdf, UnitStyle unitStyle = UnitStyle.SI, IDocumentRenderer? documentRenderer = null)
        {
            if (analyticalModel == null)
            {
                throw new ArgumentNullException(nameof(analyticalModel));
            }

            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }

            if (spaceReportPdf == null)
            {
                throw new ArgumentNullException(nameof(spaceReportPdf));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Output, "No file was chosen for the PDF.", null);
            }

            Analytical.Reporting.DocumentContext documentContext;
            try
            {
                documentContext = Analytical.Reporting.Create.DocumentContext(analyticalModel, new DocumentOptions() { UnitSystem = unitStyle });
            }
            catch (Exception exception)
            {
                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Document, string.Format("The {0} report could not be built for this Space: {1}", spaceReportPdf.Name, exception.Message), exception);
            }

            return WriteSpaceReportPdf(documentContext, space, path, spaceReportPdf, documentRenderer);
        }

        /// <summary>
        /// <see cref="WriteSpaceReportPdf(AnalyticalModel, Space, string?, SpaceReportPdf, UnitStyle, IDocumentRenderer?)"/>
        /// over a context the caller built: the batch export passes one model snapshot's
        /// <see cref="Analytical.Reporting.DocumentContext.WithNewDiagnostics"/> per document, so no document copies
        /// the model again. <paramref name="space"/> should come from that snapshot. The unit system is the context's.
        /// </summary>
        public static SpaceReportPdfResult WriteSpaceReportPdf(Analytical.Reporting.DocumentContext documentContext, Space space, string? path, SpaceReportPdf spaceReportPdf, IDocumentRenderer? documentRenderer = null)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }

            if (spaceReportPdf == null)
            {
                throw new ArgumentNullException(nameof(spaceReportPdf));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Output, "No file was chosen for the PDF.", null);
            }

            Document document;
            List<string> notes;
            try
            {
                document = spaceReportPdf.CreateDocument(documentContext, space);

                notes = documentContext.Diagnostics.Select(x => x.Text).ToList();
            }
            catch (Exception exception)
            {
                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Document, string.Format("The {0} report could not be built for this Space: {1}", spaceReportPdf.Name, exception.Message), exception);
            }

            byte[] bytes;
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    (documentRenderer ?? new PdfRenderer()).Render(document, memoryStream);
                    bytes = memoryStream.ToArray();
                }

                if (bytes.Length == 0)
                {
                    throw new InvalidOperationException("The renderer produced an empty PDF.");
                }
            }
            catch (Exception exception)
            {
                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Rendering, string.Format("The PDF could not be rendered: {0}", exception.Message), exception);
            }

            string? path_Temp = null;
            try
            {
                path = Path.GetFullPath(path);
                path_Temp = path + ".tmp";

                File.WriteAllBytes(path_Temp, bytes);
                File.Move(path_Temp, path, true);
            }
            catch (Exception exception)
            {
                DeleteSpaceReportPdfTemp(path_Temp);

                return SpaceReportPdfResult.Failed(path, SpaceReportPdfFailure.Output, string.Format("The PDF could not be saved to '{0}': {1}\n\nChoose another folder, or close the file if it is open in a PDF viewer.", path, exception.Message), exception);
            }

            return SpaceReportPdfResult.Created(path, bytes.LongLength, notes);
        }

        private static void DeleteSpaceReportPdfTemp(string? path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                //Best effort: a staged file left behind is housekeeping, not a wrong PDF.
            }
        }
    }
}
