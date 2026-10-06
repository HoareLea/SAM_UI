// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Licensed TAS acceptance of the Part O output folders: one full-year Mixed Design run (Flat 1 natural, Flat 2
    /// and Flat 3 MVHR - the IZAM route, through <c>Modify.RunPartOSimulation</c>, the core every Part O TAS run
    /// shares) into a Part O root. Proves what unit tests cannot: that SAM_Tas' own sibling files (<c>.t3d</c>,
    /// <c>.tsd</c>, workflow <c>.json</c>, <c>.timing.csv</c>) land beside the TBD in the case's <c>tas</c> folder and
    /// the timing file is then filed into <c>diagnostics</c>.
    /// <para>
    /// Needs <c>SAM_PARTO_OUTPUT_ROOT</c> (an empty scratch root) and <c>SAM_PARTO_MIXED_BASELINE</c> (a clean mixed
    /// baseline <c>.sam</c>); without them it does nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOOutputFolderAcceptance
    {
        [WpfFact]
        public void A_full_year_MixedDesign_run_writes_its_TAS_files_together_and_its_report_and_timing_apart()
        {
            string directory_Root = Environment.GetEnvironmentVariable("SAM_PARTO_OUTPUT_ROOT");
            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            if (string.IsNullOrWhiteSpace(directory_Root) || string.IsNullOrWhiteSpace(path_Baseline))
            {
                return;
            }

            Directory.CreateDirectory(directory_Root);
            StringBuilder log = new();
            void Log(string text)
            {
                log.AppendLine(text);
                File.WriteAllText(Path.Combine(directory_Root, "output-folders-acceptance.txt"), log.ToString());
            }

            AnalyticalModel baseline = Core.Convert.ToSAM<AnalyticalModel>(path_Baseline)?.Find(x => x is not null);
            Assert.NotNull(baseline);

            List<Zone> zones = baseline.AdjacencyCluster.GetZones();
            Zone Zone(string name) => zones.Find(x => x.Name == name);

            PartODwellingStrategySet set = new();
            set.Set(new PartODwellingStrategy(Zone("Flat 1").Guid, PartOVentilationMode.NaturalVentilation));
            set.Set(new PartODwellingStrategy(Zone("Flat 2").Guid, PartOVentilationMode.MVHR));
            set.Set(new PartODwellingStrategy(Zone("Flat 3").Guid, PartOVentilationMode.MVHR));

            AnalyticalModel withSet = new(baseline);
            withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            PartOMaterialisation partOMaterialisation = withSet.MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, string.Join(" | ", partOMaterialisation.Refusals.Select(x => x.Message)));

            //The person chooses only the root.
            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(baseline, path_Baseline, null);
            partOSimulationCase.OutputDirectory = directory_Root;

            string projectName = Create.PartOMixedProjectName(baseline, "Mixed");
            PartOSimulationContext partOSimulationContext = Create.PartOMixedSimulationContext(baseline, path_Baseline, partOSimulationCase, projectName);
            Log("context.OutputDirectory=" + partOSimulationContext.OutputDirectory);

            DateTime dateTime = DateTime.Now;
            PartOStrategySetSimulation simulation = Modify.SimulatePartOMaterialisation(partOMaterialisation.AnalyticalModel, partOMaterialisation.OverheatingScenarios, partOSimulationContext, CancellationToken.None);
            Log("tsd=" + simulation.Path_TSD + " refusal=" + simulation.Refusal + " elapsed=" + (DateTime.Now - dateTime));

            Assert.Null(simulation.Refusal);
            Assert.True(simulation.Assessment?.IsAssessed, simulation.Assessment?.Refusal);

            Assert.True(Modify.SavePartOTM59Report(simulation.Path_TSD, simulation.Assessment.Report, out string path_TM59Report, out string refusal_Report), refusal_Report);

            PartOOutputPaths partOOutputPaths = PartOOutputPaths.Create(directory_Root, PartOOutputCase.MixedDesign);

            foreach (string path in Directory.GetFiles(directory_Root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                Log("  " + Path.GetRelativePath(directory_Root, path) + "  " + new FileInfo(path).Length);
            }

            //Every TAS file of the run together in MixedDesign/tas, with the run model beside its results.
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Tas, projectName + ".tsd"), simulation.Path_TSD);
            foreach (string extension in new[] { ".tbd", ".tsd", ".sam" })
            {
                Assert.True(File.Exists(Path.Combine(partOOutputPaths.Directory_Tas, projectName + extension)), extension);
            }

            //The TM59 report in reports; SAM_Tas' timing file filed into diagnostics and none left in tas.
            Assert.Equal(Path.Combine(partOOutputPaths.Directory_Reports, projectName + "-TM59.txt"), path_TM59Report);
            Assert.True(File.Exists(path_TM59Report));
            Assert.True(File.Exists(Path.Combine(partOOutputPaths.Directory_Diagnostics, projectName + PartOOutputPaths.Suffix_Timing)));
            Assert.Empty(Directory.GetFiles(partOOutputPaths.Directory_Tas, "*" + PartOOutputPaths.Suffix_Timing));

            //Nothing written into the root itself except this log, and no other case folder created.
            Assert.Equal(["output-folders-acceptance.txt"], Array.ConvertAll(Directory.GetFiles(directory_Root), Path.GetFileName));
            Assert.Equal(["MixedDesign"], Array.ConvertAll(Directory.GetDirectories(directory_Root), Path.GetFileName));

            Log("DONE");
        }
    }
}
