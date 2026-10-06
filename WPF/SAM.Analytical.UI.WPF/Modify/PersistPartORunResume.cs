// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.IO;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes, beside a completed full-year run's model and results, what a later session needs to start
        /// Iteration 3 from it without re-running Prepare &amp; Run: the prepared model and a
        /// <see cref="PartORunResume"/> sidecar bound to these results. A failure is a note, never a failed run -
        /// the run is simply review-only when reopened, as before.
        /// </summary>
        internal static bool PersistPartORunResume(PartORun partORun, PartOSimulationContext partOSimulationContext, SimulationResultProvenance simulationResultProvenance, string path_TSD, out string note)
        {
            note = null;

            AnalyticalModel analyticalModel_Prepared = partORun?.AnalyticalModel_Prepared;
            PartOPreparationContext partOPreparationContext = partORun?.PreparationContext;

            if (analyticalModel_Prepared is null || partOPreparationContext is null || partOSimulationContext is null || simulationResultProvenance is null)
            {
                note = "No saved preparation was written beside these results, so reopening them later will allow review but not a new Iteration 3.";
                return false;
            }

            string path_Prepared = PartORunResume.Path_PreparedModel(path_TSD);
            string path_Resume = PartORunResume.Path_Resume(path_TSD);

            try
            {
                if (!Core.Convert.ToFile(analyticalModel_Prepared, path_Prepared, SAMFileType.SAM))
                {
                    note = string.Format("The prepared model could not be written to '{0}', so reopening these results later will allow review but not a new Iteration 3.", path_Prepared);
                    return false;
                }

                PartORunResume partORunResume = new()
                {
                    PartOIteration = partOPreparationContext.PartOIteration,
                    VentilationUnitCatalogueOffered = partOPreparationContext.VentilationUnitCatalogueOffered,
                    SolarCalculationMethod = partOSimulationContext.SolarCalculationMethod.ToString(),
                    SimulateFrom = partOSimulationContext.SimulateFrom,
                    SimulateTo = partOSimulationContext.SimulateTo,
                    UnmetHours = partOSimulationContext.UnmetHours,
                    Sizing = partOSimulationContext.Sizing,
                    UseWidths = partOSimulationContext.UseWidths,
                    UpdateConstructionLayersByPanelType = partOSimulationContext.UpdateConstructionLayersByPanelType,
                    Length_TSD = simulationResultProvenance.Length_TSD,
                    Timestamp_TSD = simulationResultProvenance.Timestamp_TSD,
                    Fingerprint_PreparedModel = SimulationResultProvenance.Fingerprint(analyticalModel_Prepared),
                };

                partOPreparationContext.Zones.ForEach(x => { if (x is not null) partORunResume.Guids_Zone.Add(x.Guid); });
                partORunResume.Guids_VentilationSystem.AddRange(partORun.Guids_VentilationSystem_Prepared);

                File.WriteAllText(path_Resume, partORunResume.ToJsonObject().ToJsonString());
            }
            catch (Exception exception)
            {
                note = string.Format("The saved preparation could not be written beside these results ({0}), so reopening them later will allow review but not a new Iteration 3.", exception.Message);
                return false;
            }

            return true;
        }
    }
}
