// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the "Start Iteration 2B" confirmation shows before any TAS time is spent: the run it starts from,
    /// what the optimisation may and may not change, and the settings it will run at, pre-filled.
    ///
    /// <para><b>Settings are confirmed when 2B starts, not when Iteration 2 was prepared</b></para>
    /// <para>
    /// The step, the round limit, the capacity envelope and the warm start are not preparation inputs -
    /// <c>SAM.Analytical.Modify.PreparePartOIteration</c> never reads them (see
    /// <c>PartORun.AdoptOptimisationSettings</c>) - so a completed Iteration 2 run does not need them recorded
    /// to be an Iteration 2B starting point. Whether it IS one is <c>Modify.CanOptimise</c>'s answer, asked
    /// before this is built and unchanged. The settings confirmed here go straight to
    /// <c>Modify.OptimisePartOTM59</c>, and the optimisation records them on
    /// <see cref="PartOOptimisationRun.Settings"/>, so what a run was asked to do is still on its record.
    /// </para>
    /// <para>
    /// <b>Pre-filled, never invented.</b> The settings recorded with the run's own preparation where there are
    /// any (the Prepare Iteration command can still record them), otherwise the ones last confirmed in this
    /// session, otherwise the <see cref="PartOOptimisationSettings"/> defaults - and the confirmation says
    /// which.
    /// </para>
    /// <para>
    /// <b>Presentation only.</b> The facts are read off the run's own record. Which spaces will be targeted
    /// is not listed: that depends on the starting design's TM59 assessment, which is the optimisation's
    /// first step, and naming them here would be a guess.
    /// </para>
    /// </summary>
    public class PartOOptimisationStart
    {
        /// <summary>What Iteration 2B is for, in one paragraph.</summary>
        public const string Text_Purpose = "Raises the design airflow of the spaces that fail TM59 by a fixed step, rebalances each dwelling, and re-simulates the same full year after every step. It stops when every eligible space passes, the selected ventilation unit cannot take another full step, or the round limit is reached.";

        /// <summary>What Iteration 2B may change.</summary>
        public static readonly List<string> Changes =
        [
            "Design airflow of mechanically ventilated spaces that fail TM59 — supply, or extract where a space has only extract terminals (targeted)",
            "Design airflow of other spaces in the same dwelling, only to keep it balanced",
            "The ventilation unit duty that follows from those airflows",
        ];

        /// <summary>What Iteration 2B never changes.</summary>
        public static readonly List<string> Keeps =
        [
            "The selected ventilation unit — its identity is locked and its capacity is a ceiling",
            "Approved Document F required airflow — a floor that is never lowered",
            "Geometry, constructions, weather and the dwelling scope",
            "Naturally ventilated spaces and spaces outside the dwelling scope — never targeted",
        ];

        private PartOOptimisationStart(List<PartOOptimisationSummary.Fact> facts, PartOOptimisationSettings settings, string settingsSource)
        {
            Facts = facts;
            Settings = settings;
            SettingsSource = settingsSource;
        }

        /// <summary>The run it starts from: scenario, results, dwellings, weather.</summary>
        public List<PartOOptimisationSummary.Fact> Facts { get; }

        /// <summary>The pre-filled settings - a copy, so editing them never writes back into a run's record.</summary>
        public PartOOptimisationSettings Settings { get; }

        /// <summary>Where <see cref="Settings"/> came from, in words.</summary>
        public string SettingsSource { get; }

        /// <param name="partORun">A run <c>Modify.CanOptimise</c> has already accepted.</param>
        /// <param name="partOOptimisationSettings_Session">The settings last confirmed in this session, or null.</param>
        public static PartOOptimisationStart Create(PartORun partORun, PartOOptimisationSettings? partOOptimisationSettings_Session = null)
        {
            PartOPreparationContext? partOPreparationContext = partORun?.PreparationContext;
            PartOSimulationContext? partOSimulationContext = partORun?.SimulationContext;

            PartOOptimisationSettings? partOOptimisationSettings_Recorded = partOPreparationContext?.OptimisationSettings;

            PartOOptimisationSettings source = partOOptimisationSettings_Recorded ?? partOOptimisationSettings_Session ?? new PartOOptimisationSettings();

            string settingsSource = partOOptimisationSettings_Recorded is not null
                ? "Pre-filled from the settings recorded when this run was prepared."
                : partOOptimisationSettings_Session is not null
                    ? "Pre-filled from the settings last used in this session."
                    : "Pre-filled with the default settings.";

            List<PartOOptimisationSummary.Fact> facts = [];

            string? path_TSD = partORun?.Path_TSD;

            string name = PartOWorkflowScenario.Find(partOPreparationContext)?.Name ?? "Iteration 2";

            //The round an earlier optimisation left this design at, read off the results file's own name - the
            //same convention the optimiser numbers its rounds by, so this never disagrees with where it
            //continues.
            int iteration = string.IsNullOrWhiteSpace(path_TSD) ? 0 : PartOSimulationContext.Iteration_ProjectName(Path.GetFileNameWithoutExtension(path_TSD));

            facts.Add(new PartOOptimisationSummary.Fact(
                "Starting from",
                iteration == 0
                    ? string.Format("The completed {0} run and its TM59 results", name)
                    : string.Format("The design kept by an earlier Iteration 2B run, saved as round {0} (-Opt{0:00})", iteration),
                iteration == 0
                    ? "Iteration 2B does not simulate the starting design again: it assesses the results it already has, then optimises from there."
                    : string.Format("Iteration 2B does not simulate the starting design again: it assesses the results it already has, then optimises from there. This optimisation numbers its own rounds from 1 (run 0 is this starting design); only the result files continue the earlier numbering, so its round 1 is saved as -Opt{0:00} and nothing earlier is overwritten.", iteration + 1)));

            if (!string.IsNullOrWhiteSpace(path_TSD))
            {
                facts.Add(new PartOOptimisationSummary.Fact("Results", Path.GetFileName(path_TSD), path_TSD));
            }

            int count_Dwelling = partOPreparationContext?.Zones?.Count ?? 0;
            if (count_Dwelling != 0)
            {
                facts.Add(new PartOOptimisationSummary.Fact("Dwellings", string.Format("{0} in scope", UI.Query.PartOCount(count_Dwelling, "dwelling", "dwellings"))));
            }

            string? weather = partOSimulationContext?.WeatherData?.Name;
            if (!string.IsNullOrWhiteSpace(weather))
            {
                facts.Add(new PartOOptimisationSummary.Fact("Weather", string.Format("{0} · full year, the same case every round", weather)));
            }

            PartOOptimisationSettings settings = new()
            {
                AirFlowStep_Lps = source.AirFlowStep_Lps,
                MaximumIterations = source.MaximumIterations,
                Tolerance_Lps = source.Tolerance_Lps,
                WarmStart = source.WarmStart,
                CapacityEnvelope = source.CapacityEnvelope,
            };

            return new PartOOptimisationStart(facts, settings, settingsSource);
        }

        /// <summary>
        /// The settings as typed, or null with the reason where they cannot be used. The rule is
        /// <c>PartOOptimisationSettings.IsValid</c>'s; only the parsing is here.
        /// </summary>
        public PartOOptimisationSettings? Parse(string? airFlowStep, string? maximumIterations, bool capacityEnvelope, bool warmStart, out string? refusal)
        {
            refusal = null;

            if (!double.TryParse(airFlowStep, out double airFlowStep_Lps))
            {
                refusal = string.Format("'{0}' is not an airflow step. Enter the number of litres per second each failing space's design airflow is raised by each round.", airFlowStep);

                return null;
            }

            if (!int.TryParse(maximumIterations, out int maximumIterations_Int))
            {
                refusal = string.Format("'{0}' is not a number of rounds. Enter the most optimisation rounds the run may take.", maximumIterations);

                return null;
            }

            PartOOptimisationSettings result = new()
            {
                AirFlowStep_Lps = airFlowStep_Lps,
                MaximumIterations = maximumIterations_Int,
                Tolerance_Lps = Settings.Tolerance_Lps,
                CapacityEnvelope = capacityEnvelope,
                WarmStart = warmStart,
            };

            return result.IsValid(out refusal) ? result : null;
        }
    }
}
