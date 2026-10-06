// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.TPD;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Shows where the TAS Systems route is, from the coarse events SAM_Tas reports (one per air system,
        /// never per hour or per cell). Called on the thread that runs the route.
        ///
        /// <para>
        /// Where the operation has a stage of that name (an Iteration 3 run) the stage is started and the
        /// item - "Air system 2 of 3" - is its detail. Where it does not (the mixed-design run's one TAS
        /// stage) the whole line goes in the detail, so the same words appear either way. A count is shown
        /// only where SAM_Tas counted real air systems; nothing here derives a percentage.
        /// </para>
        /// </summary>
        internal static void ReportPartOSystemVentilationProgress(PartOProgressHost? partOProgressHost, SystemVentilationRouteProgress? systemVentilationRouteProgress)
        {
            if (partOProgressHost is null || systemVentilationRouteProgress is null)
            {
                return;
            }

            string? stage = null;
            string? item = null;

            switch (systemVentilationRouteProgress.Stage)
            {
                case SystemVentilationRouteStage.ConvertingAirSystems:
                    stage = PartOProgressStages.CreatingVentilationSystems;
                    item = Item("Air system", systemVentilationRouteProgress);
                    break;

                case SystemVentilationRouteStage.ReconcilingConversion:
                    stage = PartOProgressStages.CreatingVentilationSystems;
                    item = "Checking the systems against the ventilation design";
                    break;

                case SystemVentilationRouteStage.SimulatingAirSystems:
                    stage = PartOProgressStages.RunningTasSystems;
                    item = Item("Air system", systemVentilationRouteProgress);
                    break;

                case SystemVentilationRouteStage.ReadingRecirculationCooling:
                    stage = PartOProgressStages.EvaluatingCoolingModules;
                    break;

                case SystemVentilationRouteStage.ReadingManufacturerGuidance:
                    stage = PartOProgressStages.EvaluatingManufacturerGuidance;
                    break;
            }

            if (stage is null)
            {
                return;
            }

            int index = partOProgressHost.State.IndexOf(stage);

            if (index >= 0)
            {
                partOProgressHost.Start(index);
                partOProgressHost.Detail(item!);

                return;
            }

            partOProgressHost.Detail(item is null ? stage : string.Format("{0} — {1}", stage, item));
        }

        private static string? Item(string noun, SystemVentilationRouteProgress systemVentilationRouteProgress)
        {
            return systemVentilationRouteProgress.HasCount
                ? string.Format("{0} {1} of {2}", noun, systemVentilationRouteProgress.Current, systemVentilationRouteProgress.Total)
                : null;
        }
    }
}
