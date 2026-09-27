// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What happened to one screening strategy in one screening.</summary>
    internal sealed class PartOScreeningStep
    {
        public PartOScreeningStep(PartOScreeningStrategy partOScreeningStrategy)
        {
            Strategy = partOScreeningStrategy;
        }

        public PartOScreeningStrategy Strategy { get; }

        /// <summary>The evidence, where the strategy was simulated and assessed.</summary>
        public PartOScreeningEvidence? Evidence { get; set; }

        /// <summary>Why the strategy was not simulated at all - unavailable, not permitted, or nothing left to screen.</summary>
        public string? Skipped { get; set; }

        /// <summary>SAM's structured refusals, where the materialisation refused.</summary>
        public List<PartOMaterialisationRefusal> Refusals { get; } = [];

        /// <summary>Why the simulation did not produce assessable results, where it ran and failed.</summary>
        public string? Refusal_Simulation { get; set; }

        public List<string> Notes { get; } = [];

        public bool Cancelled { get; set; }

        /// <summary>How many dwellings the run assessed.</summary>
        public int DwellingCount { get; set; }
    }

    /// <summary>The whole of one screening.</summary>
    internal sealed class PartOScreeningOutcome
    {
        public List<PartOScreeningStep> Steps { get; } = [];

        public bool Cancelled { get; set; }

        public List<PartOScreeningEvidence> Evidence
        {
            get
            {
                List<PartOScreeningEvidence> result = [];
                Steps.ForEach(x => { if (x.Evidence is not null) result.Add(x.Evidence); });
                return result;
            }
        }
    }

    public static partial class Modify
    {
        /// <summary>
        /// Screens the dwellings of a clean baseline against the chosen engineering strategies - each one applied to
        /// every screened dwelling, materialised by SAM, simulated and assessed - and returns the evidence.
        ///
        /// <para><b>Evidence only - the selection is never touched</b></para>
        /// <para>
        /// Each strategy is materialised from a COPY of the baseline carrying that strategy for the screened dwellings;
        /// the baseline's own selection is neither read nor written. Screening therefore cannot change the selected
        /// design, and a cancelled screening leaves it exactly as it was. Suggestions derived from the evidence reach the
        /// selection only through an explicit, previewed <i>Apply suggestions</i>.
        /// </para>
        ///
        /// <para><b>One engineering implementation</b></para>
        /// <para>
        /// A screening run IS the homogeneous case of the mixed authority: natural ventilation is SAM's scoped Iteration
        /// 1b state, MVHR baseline its Iteration 1a design with generic units, and selected-product MVHR the same design
        /// with the project's product pool offered, as Iteration 2 does (PR1 §4). Screening and the final mixed run are
        /// built by the same call, so they differ only in the strategy set - and nothing in the UI restates a Part O
        /// rule. A baseline SAM would refuse to build a mixed design from is refused here too, with the same reasons.
        /// </para>
        ///
        /// <para><b>Minimum screening skips only what cannot contribute</b></para>
        /// <para>
        /// TAS simulates the whole building whichever dwellings a run assesses, so the only saving is a whole
        /// strategy's run. In <see cref="PartOScreeningMode.Minimum"/> each strategy assesses only the dwellings with no
        /// passing, permitted strategy yet; a strategy the project does not permit is not run; and once every dwelling is
        /// resolved the remaining strategies are not run. A dwelling outside a run's scope has no result from it and
        /// reads NOT RUN - no PASS is ever inferred. <see cref="PartOScreeningMode.FullComparison"/> assesses every
        /// dwelling under every chosen, screenable strategy.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Baseline">The clean baseline. Not modified.</param>
        /// <param name="guids_Dwelling">The dwellings to screen.</param>
        /// <param name="strategies">The strategies chosen. Taken in the least-intervention order whatever order they are given in.</param>
        /// <param name="ventilationUnitCapacityDescriptors">The catalogue, offered only to the selected-product strategy.</param>
        /// <param name="func_Context">The TAS case of each strategy's run - its project name is the strategy's own.</param>
        /// <param name="onStart">Called as each strategy's run starts, with its index in the screening.</param>
        internal static PartOScreeningOutcome ScreenPartODwellingStrategies(
            AnalyticalModel analyticalModel_Baseline,
            IEnumerable<Guid> guids_Dwelling,
            IEnumerable<PartOScreeningStrategy> strategies,
            PartOScreeningMode partOScreeningMode,
            PartOMixedDesignConstraints partOMixedDesignConstraints,
            IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors,
            Func<PartOScreeningStrategy, PartOSimulationContext> func_Context,
            CancellationToken cancellationToken,
            PartOStrategySetSimulator? partOStrategySetSimulator = null,
            Action<int, PartOScreeningStrategy>? onStart = null)
        {
            PartOScreeningOutcome result = new();

            partOMixedDesignConstraints ??= new PartOMixedDesignConstraints();

            List<VentilationUnitCapacityDescriptor> descriptors = [.. ventilationUnitCapacityDescriptors ?? []];
            bool catalogueHasProducts = descriptors.Count != 0;

            HashSet<PartOScreeningStrategy> chosen = [.. strategies ?? []];

            List<Guid> guids_All = [];
            foreach (Guid guid in guids_Dwelling ?? [])
            {
                if (guid != Guid.Empty && !guids_All.Contains(guid))
                {
                    guids_All.Add(guid);
                }
            }

            //Taken ONCE, before any run: what every piece of evidence this screening produces is bound to.
            string fingerprint_Design = UI.Query.PartOScreeningDesignFingerprint(analyticalModel_Baseline);
            string fingerprint_Catalogue = UI.Query.PartOMixedCatalogueFingerprint(analyticalModel_Baseline, descriptors);

            //Minimum screening's working set: dwellings with no passing, permitted strategy yet.
            List<Guid> guids_Remaining = [.. guids_All];

            int index = 0;
            foreach (PartOScreeningStrategy partOScreeningStrategy in UI.Query.PartOScreeningStrategies())
            {
                if (!chosen.Contains(partOScreeningStrategy))
                {
                    continue;
                }

                PartOScreeningStep partOScreeningStep = new(partOScreeningStrategy);
                result.Steps.Add(partOScreeningStep);

                if (result.Cancelled)
                {
                    partOScreeningStep.Skipped = "The screening was cancelled before this strategy started.";
                    continue;
                }

                string? unavailable = UI.Query.PartOScreeningStrategyUnavailable(partOScreeningStrategy, catalogueHasProducts);
                if (unavailable is not null)
                {
                    partOScreeningStep.Skipped = unavailable;
                    continue;
                }

                List<Guid> guids_Scope;
                if (partOScreeningMode == PartOScreeningMode.Minimum)
                {
                    if (!partOMixedDesignConstraints.Allows(partOScreeningStrategy))
                    {
                        partOScreeningStep.Skipped = "Not run: the project constraints do not permit this strategy, and minimum screening looks only for a permitted one.";
                        continue;
                    }

                    if (guids_Remaining.Count == 0)
                    {
                        partOScreeningStep.Skipped = "Not run: every screened dwelling already passes with a permitted, less invasive strategy.";
                        continue;
                    }

                    guids_Scope = [.. guids_Remaining];
                }
                else
                {
                    guids_Scope = [.. guids_All];
                }

                if (guids_Scope.Count == 0)
                {
                    partOScreeningStep.Skipped = "Not run: there is no dwelling to screen.";
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    result.Cancelled = true;
                    partOScreeningStep.Cancelled = true;
                    continue;
                }

                onStart?.Invoke(index, partOScreeningStrategy);
                index++;

                //A copy carrying ONLY this strategy's homogeneous set: the baseline's own selection is never read here,
                //and the copy constructor clones the parameter sets, so the baseline is not written.
                AnalyticalModel analyticalModel_Screening = new(analyticalModel_Baseline);
                analyticalModel_Screening.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, UI.Query.PartOScreeningStrategySet(partOScreeningStrategy, guids_Scope));

                bool catalogueOffered = UI.Query.PartOScreeningCatalogueOffered(partOScreeningStrategy);

                PartOStrategySetRun partOStrategySetRun = RunPartOStrategySet(analyticalModel_Screening, catalogueOffered ? descriptors : null, guids_Scope, func_Context(partOScreeningStrategy), cancellationToken, partOStrategySetSimulator);

                partOScreeningStep.DwellingCount = guids_Scope.Count;
                partOScreeningStep.Refusals.AddRange(partOStrategySetRun.Refusals);
                partOScreeningStep.Notes.AddRange(partOStrategySetRun.Simulation?.Notes ?? []);

                if (partOStrategySetRun.Cancelled)
                {
                    //The strategy that was running is discarded whole; the ones completed before it stand.
                    result.Cancelled = true;
                    partOScreeningStep.Cancelled = true;
                    continue;
                }

                if (!partOStrategySetRun.IsMaterialised)
                {
                    continue;
                }

                if (!partOStrategySetRun.Completed)
                {
                    partOScreeningStep.Refusal_Simulation = partOStrategySetRun.Simulation?.Refusal ?? partOStrategySetRun.Simulation?.Assessment?.Refusal ?? "The screening simulation produced no assessable results.";
                    continue;
                }

                PartOScreeningEvidence partOScreeningEvidence = new(partOScreeningStrategy)
                {
                    Fingerprint_Design = fingerprint_Design,
                    CatalogueOffered = catalogueOffered,
                    Fingerprint_Catalogue = catalogueOffered ? fingerprint_Catalogue : null,
                    Path_TSD = partOStrategySetRun.Simulation!.Path_TSD,
                    Length_TSD = partOStrategySetRun.Simulation.Length_TSD,
                    Timestamp_TSD = partOStrategySetRun.Simulation.Timestamp_TSD,
                };

                partOScreeningEvidence.Guids_Zone_Assessed.AddRange(guids_Scope);
                partOStrategySetRun.Results.ForEach(partOScreeningEvidence.Add);

                partOScreeningStep.Evidence = partOScreeningEvidence;

                if (partOScreeningMode == PartOScreeningMode.Minimum)
                {
                    guids_Remaining.RemoveAll(x => partOScreeningEvidence.Outcome(x) == PartODwellingOutcome.Pass);
                }
            }

            return result;
        }

        /// <summary>
        /// Builds and runs the selected mixed design: the clean baseline and the strategy set it carries, materialised
        /// by SAM into ONE mixed model, simulated once, assessed, and recorded as the final mixed result.
        ///
        /// <para><b>Always from the baseline, never incrementally</b></para>
        /// <para>
        /// Every build is a fresh materialisation of the baseline with the WHOLE current selection, however few
        /// dwellings changed since the last run. A previous run's model is never read, patched or re-simulated: it is a
        /// run artefact beside its own results, and the open model is never replaced by it.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Baseline">The open baseline, carrying the saved selection. Not modified.</param>
        /// <param name="catalogueOffered">Whether products are selected from the catalogue (Iteration 2 terms) or units stay generic (Iteration 1a terms).</param>
        internal static PartOMixedRunEvidence? BuildAndRunPartOMixedDesign(AnalyticalModel analyticalModel_Baseline, bool catalogueOffered, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken, out PartOStrategySetRun partOStrategySetRun, PartOStrategySetSimulator? partOStrategySetSimulator = null, Action? onMaterialised = null)
        {
            partOStrategySetRun = RunPartOStrategySet(analyticalModel_Baseline, catalogueOffered ? ventilationUnitCapacityDescriptors : null, null, partOSimulationContext, cancellationToken, partOStrategySetSimulator, onMaterialised);

            if (!partOStrategySetRun.Completed)
            {
                return null;
            }

            PartOMaterialisation partOMaterialisation = partOStrategySetRun.Materialisation!;

            //What each assessed dwelling ran as - SAM's own set, in SAM's own serialisation, never text.
            PartODwellingStrategySet partODwellingStrategySet_Baseline = analyticalModel_Baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            PartODwellingStrategySet partODwellingStrategySet_Ran = new();
            foreach (Guid guid_Zone in partOMaterialisation.Record?.ZoneGuids_Assessed ?? [])
            {
                PartODwellingStrategy? partODwellingStrategy = partODwellingStrategySet_Baseline?.Strategy(guid_Zone);
                if (partODwellingStrategy is not null)
                {
                    partODwellingStrategySet_Ran.Set(new PartODwellingStrategy(partODwellingStrategy));
                }
            }

            PartOMixedRunEvidence result = new()
            {
                Record = partOMaterialisation.Record is null ? null : new PartOMaterialisationRecord(partOMaterialisation.Record),
                Strategies = partODwellingStrategySet_Ran,
                CatalogueOffered = catalogueOffered,
                Path_TSD = partOStrategySetRun.Simulation!.Path_TSD,
                Length_TSD = partOStrategySetRun.Simulation.Length_TSD,
                Timestamp_TSD = partOStrategySetRun.Simulation.Timestamp_TSD,
                Path_RunModel = partOStrategySetRun.Simulation.Path_RunModel,
                Refusal_Assessment = partOStrategySetRun.Simulation.Assessment!.IsAssessed ? null : partOStrategySetRun.Simulation.Assessment.Refusal,
            };

            partOStrategySetRun.Results.ForEach(result.Add);

            //The project verdict and the communal-corridor state are SAM's report's own, kept beside the dwelling tally.
            PartOTM59Assessment partOTM59Assessment = partOStrategySetRun.Simulation.Assessment;
            if (partOTM59Assessment.IsAssessed && partOTM59Assessment.Report is TM59AssessmentReport tM59AssessmentReport)
            {
                result.OccupiedSpaceComplianceStatus = tM59AssessmentReport.OccupiedSpaceComplianceStatus;
                result.CorridorRiskStatus = tM59AssessmentReport.CorridorRiskStatus;

                foreach (IGrouping<string, TM59AssessmentReportCheck> grouping in (tM59AssessmentReport.CorridorChecks ?? []).GroupBy(x => x.Reference))
                {
                    result.Corridors.Add((grouping.First().SpaceName ?? grouping.Key, grouping.Any(x => x.RiskStatus == TM59RiskStatus.SignificantRisk) ? TM59RiskStatus.SignificantRisk : TM59RiskStatus.Acceptable));
                }
            }

            return result;
        }
    }
}
