// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The stage names of the Part O progress windows, in one place. Each names one real operation the run
    /// performs; none is an umbrella over several. A stage is listed only for a run that performs it.
    /// </summary>
    internal static class PartOProgressStages
    {
        //Prepare & Run - Iteration 1a and 2: the TBD / IZAM route. No TAS Systems document is built.
        public const string PrepareAndReview = "Prepare and review the iteration";
        public const string BuildingSimulationFullYear = "TAS building simulation (full year)";
        public const string Tm59Assessment = "TM59 assessment";

        //Iteration 3 - the system case.
        public const string PreparingSystemCase = "Preparing the system case";
        public const string BuildingSimulationThermalSource = "TAS building simulation (thermal source)";
        public const string CreatingVentilationSystems = "Creating ventilation systems";
        public const string RunningTasSystems = "Running TAS systems";
        public const string EvaluatingManufacturerGuidance = "Evaluating manufacturer guidance";
        public const string EvaluatingCoolingModules = "Evaluating cooling-module behaviour";
        public const string CalculatingResultantTemperatures = "Calculating resultant temperatures";
        public const string AssessingTm59 = "Assessing TM59";
        public const string ComparingAndSaving = "Comparing and saving results";

        //Iteration 3 resumed from this session's earlier attempt: its TAS stages are reused and checked, not run, so a
        //resumed run lists this one stage in their place rather than showing TAS work that does not happen.
        public const string ReusingTasResults = "Reusing the completed TAS results";
    }
}
