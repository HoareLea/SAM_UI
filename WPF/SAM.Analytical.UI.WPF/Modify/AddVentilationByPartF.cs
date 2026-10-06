// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        private const string PartFReportTitle = "Part F Ventilation";

        public static void AddVentilationByPartF(this UIAnalyticalModel? uIAnalyticalModel, IWin32Window? owner = null)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            PartFCalculator partFCalculator = Analytical.Query.DefaultPartFCalculator();
            if (partFCalculator is null)
            {
                return;
            }

            AdjacencyCluster? adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster is null)
            {
                return;
            }

            PartFVentilationWindow partFVentilationWindow = new()
            {
                ZoneCategories = [.. adjacencyCluster.GetZoneCategories() ?? []],
                SetbackFlowRateFactor = Analytical.Query.DefaultPartFData()?.SetbackFlowRateFactor ?? PartFData.DefaultSetbackFlowRateFactor,
            };

            bool? showdialog = partFVentilationWindow.ShowDialog();
            if (showdialog == null || !showdialog.HasValue || !showdialog.Value)
            {
                return;
            }

            string? zoneCategoryName = partFVentilationWindow.SelectedZoneCategory;

            partFCalculator.AdjacencyCluster = adjacencyCluster;

            //Set on the calculator, not on the shared default rule set: PartFData is held by
            //ActiveSetting for the whole session, so writing the factor there would silently change every
            //later calculation. The property validates the factor and substitutes the documented default
            //for a negative value, a value above 1, NaN or infinity.
            partFCalculator.SetbackFlowRateFactor = partFVentilationWindow.SetbackFlowRateFactor;

            //One call, whether or not a zone category was chosen. PartFCalculator.Calculate(string)
            //handles both single house mode (empty category) and zoned mode, and only it applies the
            //explicit dwelling filter, the duplicate-zone check and the unzoned-space report. The
            //previous per-zone loop here bypassed all of that, so a shared corridor sitting in the flats
            //category was silently sized as a dwelling and nothing was reported to the user.
            partFCalculator.Calculate(zoneCategoryName);

            //The assessment window is the review and editing workflow: terminals, internal doors, purge and
            //the clause-level checks, plus the airflow overlay drawn from the same data. Anything the
            //engineer records there is an input the calculation cannot derive, so it is written back into
            //the model before the model is published.
            //The model goes in as well as the results: the airflow view draws the dwelling's REAL floor
            //plan and puts the arrows on it, so it needs the geometry, not only the numbers. Order matters
            //- DwellingResults selects the first dwelling and triggers the first plan load, so the model
            //has to be there before it does.
            PartFAssessmentWindow partFAssessmentWindow = new()
            {
                AnalyticalModel = analyticalModel,
                DwellingResults = partFCalculator.DwellingResults,
            };

            if (owner != null)
            {
                new System.Windows.Interop.WindowInteropHelper(partFAssessmentWindow).Owner = owner.Handle;
            }

            partFAssessmentWindow.ShowDialog();

            if (partFAssessmentWindow.Applied)
            {
                PersistEngineeringInputs(partFCalculator);
            }

            analyticalModel = new AnalyticalModel(analyticalModel, partFCalculator.AdjacencyCluster);

            uIAnalyticalModel?.SetJSAMObject(analyticalModel, new FullModification());
        }

        /// <summary>
        /// Writes the engineer's recorded inputs back into the model, so they survive the next
        /// recalculation instead of living only in the window that collected them.
        /// <para>
        /// Door transfer records go onto their door apertures. Purge records go onto their spaces. Check
        /// confirmations go into the dwelling zone's commissioning record, which is where the calculation
        /// reads them from - Appendix C Parts 2a and 2b are an installation and inspection checklist, so a
        /// person's answers to the requirements no model contains belong with it.
        /// </para>
        /// </summary>
        private static void PersistEngineeringInputs(PartFCalculator partFCalculator)
        {
            AdjacencyCluster? adjacencyCluster = partFCalculator?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                return;
            }

            foreach (PartFDwellingResult dwellingResult in partFCalculator!.DwellingResults ?? [])
            {
                PartFComplianceResult? complianceResult = dwellingResult?.ComplianceResult;
                if (complianceResult is null)
                {
                    continue;
                }

                foreach (PartFDoorTransferData partFDoorTransferData in complianceResult.TransferPaths ?? [])
                {
                    if (partFDoorTransferData.ApertureGuid != Guid.Empty)
                    {
                        adjacencyCluster.SetPartFDoorTransferData(partFDoorTransferData.ApertureGuid, partFDoorTransferData);
                    }
                }

                foreach (PartFPurgeVentilationData partFPurgeVentilationData in complianceResult.PurgeVentilation ?? [])
                {
                    Space? space = adjacencyCluster.GetObject<Space>(partFPurgeVentilationData.SpaceGuid);

                    if (space?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData) is PartFSpaceData partFSpaceData)
                    {
                        partFSpaceData.Purge = partFPurgeVentilationData;
                        space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);
                        adjacencyCluster.AddObject(space);
                    }
                }

                PersistConfirmations(adjacencyCluster, dwellingResult!, complianceResult);
            }
        }

        internal static void PersistConfirmations(AdjacencyCluster adjacencyCluster, PartFDwellingResult partFDwellingResult, PartFComplianceResult partFComplianceResult)
        {
            //Everything a person entered is kept, not only the checks that ended up confirmed. A
            //confirmation recorded against a check the calculation found FAILING does not produce a pass -
            //it lands on engineering review, or on an alternative solution pending approval - and if only
            //UserConfirmed checks were persisted, the evidence, the alternative method and the reason
            //behind exactly those entries would be silently discarded on Apply. They are the entries most
            //worth keeping.
            List<PartFComplianceCheck> checks_Recorded = [.. (partFComplianceResult.Checks ?? []).Where(Recorded)];
            if (checks_Recorded.Count == 0)
            {
                return;
            }

            //Single dwelling mode has no zone to hold the record. The confirmations stand for this session
            //and are reported, but there is nowhere in the model to keep them; putting a dwelling in a zone
            //is what gives it somewhere.
            Zone? zone = string.IsNullOrWhiteSpace(partFDwellingResult.Name)
                ? null
                : adjacencyCluster.GetZones()?.Find(x => x.Name == partFDwellingResult.Name);

            if (zone is null)
            {
                return;
            }

            PartFCommissioningData partFCommissioningData = partFComplianceResult.Commissioning ?? new PartFCommissioningData(partFDwellingResult.Name)
            {
                DwellingName = partFDwellingResult.Name,
            };

            foreach (PartFComplianceCheck check in checks_Recorded)
            {
                PartFComplianceCheck? check_Persisted = partFCommissioningData.InstallationChecks.Find(x => x is not null && string.Equals(x.Name, check.Name, StringComparison.Ordinal));

                if (check_Persisted is null)
                {
                    check_Persisted = new PartFComplianceCheck(check.Name, check.SourceReference, check.Requirement);
                    partFCommissioningData.InstallationChecks.Add(check_Persisted);
                }

                //What is stored is the ANSWER the person gave, not the status the guard let the check
                //report. A confirmation against a calculated failure is redirected to engineering review
                //or an alternative solution, but the answer was still "confirmed", so it is stored as
                //UserConfirmed and re-tested on the next calculation. A check the person WITHDREW has
                //returned to its calculated status, and storing UserConfirmed anyway would reinstate the
                //confirmation they removed - so it is stored as NotAssessed, and the supporting notes
                //below are kept either way.
                check_Persisted.Status = check.Status == PartFComplianceStatus.UserConfirmed || check.IsUserResolved
                    ? PartFComplianceStatus.UserConfirmed
                    : PartFComplianceStatus.NotAssessed;
                check_Persisted.ConfirmedBy = check.ConfirmedBy;
                check_Persisted.ConfirmationDate = check.ConfirmationDate;
                check_Persisted.Notes = check.Notes;
                check_Persisted.UserEvidence = check.UserEvidence;
                check_Persisted.AlternativeComplianceMethod = check.AlternativeComplianceMethod;
                check_Persisted.OverrideReason = check.OverrideReason;
            }

            partFComplianceResult.Commissioning = partFCommissioningData;

            zone.SetValue(ZoneParameter.PartFCommissioningData, partFCommissioningData);
            adjacencyCluster.AddObject(zone);
        }

        /// <summary>
        /// True where a person has entered something against this check that the model could not have
        /// produced, and which must therefore survive the next recalculation.
        /// <para>
        /// The supporting fields count even on their own. A withdrawn confirmation whose only remaining
        /// trace is a note, or the name and date of the person who gave it, is still a recorded answer -
        /// PersistConfirmations must reach it to store the withdrawal, or the previous UserConfirmed record
        /// would survive and the next calculation would reinstate the confirmation.
        /// </para>
        /// </summary>
        private static bool Recorded(PartFComplianceCheck partFComplianceCheck)
        {
            if (partFComplianceCheck is null)
            {
                return false;
            }

            return partFComplianceCheck.Status == PartFComplianceStatus.UserConfirmed
                || partFComplianceCheck.IsUserResolved
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.Notes)
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.ConfirmedBy)
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.ConfirmationDate)
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.UserEvidence)
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.AlternativeComplianceMethod)
                || !string.IsNullOrWhiteSpace(partFComplianceCheck.OverrideReason);
        }

        /// <summary>
        /// Builds the Part F conformance assessment report shown to the user: the shared engineering
        /// report from SAM.Analytical, followed by the model-level notes that belong to the run rather
        /// than to any one dwelling.
        /// <para>
        /// The report body itself is generated by <see cref="PartFReport"/>, which has no user interface
        /// dependency at all. The same text therefore appears here, on the clipboard, in the Grasshopper
        /// output and in the regression tests, and a change to it is caught by a test rather than noticed
        /// on screen. This layer adds only what is specific to a SAM_UI run and wraps the result for
        /// reading.
        /// </para>
        /// <para>
        /// Pure text generation, so it can be unit tested directly: a model with thousands of spaces can
        /// produce thousands of report lines, and this must not be the layer that struggles with that.
        /// </para>
        /// </summary>
        public static string BuildReportText(PartFCalculator partFCalculator, PartFOperatingMode partFOperatingMode = PartFOperatingMode.ContinuousDesign)
        {
            StringBuilder stringBuilder = new();

            //The assessment opens with its assumptions, before any number, so a reader can see the basis
            //of it before reading a result. That ordering is fixed and asserted by a regression test.
            stringBuilder.Append(PartFReport.Build(partFCalculator, partFOperatingMode));

            List<string> notes = [];

            if (partFCalculator?.ExcludedZoneNames is not null && partFCalculator.ExcludedZoneNames.Count != 0)
            {
                notes.Add("Zones not sized as dwellings: " + string.Join(", ", partFCalculator.ExcludedZoneNames));
            }

            if (partFCalculator?.UnclassifiedSpaceNames is not null && partFCalculator.UnclassifiedSpaceNames.Count != 0)
            {
                notes.Add("Unclassified space(s): " + string.Join(", ", partFCalculator.UnclassifiedSpaceNames));
            }

            //Warnings that belong to the model rather than to a dwelling - a missing zone category, a
            //space in two dwelling zones - never reach a dwelling result, so they would be lost if this
            //layer did not add them.
            List<string> warnings_Model = [.. (partFCalculator?.Warnings ?? []).Where(x => !partFCalculator.DwellingResults.Exists(y => y.Warnings.Exists(z => x.EndsWith(z, StringComparison.Ordinal))))];

            if (warnings_Model.Count != 0)
            {
                notes.Add("Model-level warnings:");
                notes.AddRange(warnings_Model.ConvertAll(x => "- " + x));
            }

            if (notes.Count != 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("MODEL NOTES");
                stringBuilder.AppendLine(new string('-', "MODEL NOTES".Length));

                foreach (string note in notes)
                {
                    stringBuilder.AppendLine(note);
                }
            }

            return Wrap(stringBuilder.ToString(), reportLineLength);
        }

        /// <summary>
        /// Column at which report lines are wrapped. The Approved Document warnings are full paragraphs
        /// and the unzoned-space list can name thousands of spaces, so without this a line runs several
        /// screens wide and the reader has to scroll horizontally to read one sentence.
        /// </summary>
        private const int reportLineLength = 100;

        /// <summary>
        /// Hard-wraps each line at <paramref name="maxLineLength"/> on whitespace, so the report reads
        /// as a paragraph rather than one very long line. Existing line breaks and blank lines are kept,
        /// so the section structure survives.
        /// <para>
        /// A single word longer than the limit is emitted whole on its own line rather than split: the
        /// long tokens here are space and zone names, and breaking one in half would make the report
        /// name a space that does not exist.
        /// </para>
        /// </summary>
        private static string Wrap(string text, int maxLineLength)
        {
            if (string.IsNullOrEmpty(text) || maxLineLength <= 0)
            {
                return text;
            }

            StringBuilder result = new();

            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                //The text ends with a line break, so the final split segment is empty. Appending it
                //would add a blank line the report did not have.
                if (i == lines.Length - 1 && line.Length == 0)
                {
                    break;
                }

                string line_Trimmed = line.TrimEnd('\r');

                if (line_Trimmed.Length <= maxLineLength || IsDiagram(line_Trimmed))
                {
                    result.AppendLine(line_Trimmed);
                    continue;
                }

                StringBuilder line_Current = new();

                foreach (string word in line_Trimmed.Split(' '))
                {
                    //+1 for the space that would be needed before this word.
                    if (line_Current.Length != 0 && line_Current.Length + 1 + word.Length > maxLineLength)
                    {
                        result.AppendLine(line_Current.ToString());
                        line_Current.Clear();
                    }

                    if (line_Current.Length != 0)
                    {
                        line_Current.Append(' ');
                    }

                    line_Current.Append(word);
                }

                if (line_Current.Length != 0)
                {
                    result.AppendLine(line_Current.ToString());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// True for a line of the airflow schematic. Those lines are a DRAWING: their indentation places
        /// each branch under the arrow above it, and breaking one at a column would leave the diagram
        /// pointing at nothing. A long branch line is left to scroll instead.
        /// </summary>
        private static bool IsDiagram(string line)
        {
            return line.IndexOfAny(diagramCharacters) != -1;
        }

        /// <summary>
        /// The box-drawing and arrow characters the schematic is built from. Taken from the renderer
        /// itself rather than repeated here, so the two cannot drift apart.
        /// </summary>
        private static readonly char[] diagramCharacters =
        [
            PartFSchematic.Vertical[0],
            PartFSchematic.Horizontal[0],
            PartFSchematic.CornerLast[0],
            PartFSchematic.CornerTee[0],
            PartFSchematic.ArrowDown[0],
            PartFSchematic.ArrowRight[0],
        ];

        /// <summary>
        /// Surfaces the calculation's warnings and remarks. Without this the dwelling filter, the
        /// unclassified spaces and the local kitchen extract limitation were all computed and then discarded,
        /// leaving the user with rates and no indication that anything needed checking.
        /// </summary>
        private static void Report(PartFCalculator partFCalculator, IWin32Window? owner)
        {
            string text = BuildReportText(partFCalculator);

            SAM.Core.UI.WPF.ReportWindow reportWindow = new(PartFReportTitle, text);

            //Bridge the WinForms IWin32Window owner to the WPF window's native owner handle.
            if (owner != null)
            {
                new System.Windows.Interop.WindowInteropHelper(reportWindow).Owner = owner.Handle;
            }

            reportWindow.ShowDialog();
        }
    }
}
