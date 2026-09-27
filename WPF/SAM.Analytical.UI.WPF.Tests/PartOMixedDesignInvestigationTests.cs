// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Investigation only (27 Sep 2026): headless evidence for the PR2 Optimised MVHR / 2B gap and the real example model.
    /// Writes a log into <c>SAM_PARTO_MIXED_INVESTIGATION</c>; without that variable it does nothing.
    /// </summary>
    public class PartOMixedDesignInvestigationTests
    {
        private static string Directory_Out => Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_INVESTIGATION");

        private static string Path_RealModel => Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_MODEL");

        [Fact]
        public void Investigation_RealModel()
        {
            if (string.IsNullOrWhiteSpace(Directory_Out) || string.IsNullOrWhiteSpace(Path_RealModel))
            {
                return;
            }

            StringBuilder log = new();
            AnalyticalModel model = Core.Convert.ToSAM<AnalyticalModel>(Path_RealModel)?.Find(x => x is not null);
            log.AppendLine("Model: " + Path_RealModel + " loaded=" + (model is not null));

            Describe(model, log, "AS SAVED");

            //Map IC (TM59) exactly as the window's automatic mapping does: TM59Manager over the default text map / library,
            //the default zone type chosen by the control's own rule.
            AnalyticalModel mapped = MapTM59(model, log);
            Describe(mapped, log, "AFTER MAP IC (TM59)");

            //The PR2 mixed strategies on whatever flats exist.
            List<Zone> dwellings = Analytical.Query.PartFDwellingZones(mapped.AdjacencyCluster.GetZones()) ?? [];
            dwellings.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            PartODwellingStrategySet set = new();
            for (int i = 0; i < dwellings.Count; i++)
            {
                set.Set(i % 2 == 0 ? new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.NaturalVentilation) : new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.MVHR));
            }

            AnalyticalModel withSet = new(mapped);
            withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);
            PartOMaterialisation materialisation = withSet.MaterialisePartODwellingStrategies();
            log.AppendLine("== MATERIALISE (alternating Natural/MVHR) IsMaterialised=" + materialisation.IsMaterialised);
            foreach (PartOMaterialisationRefusal refusal in materialisation.Refusals)
            {
                log.AppendLine("  REFUSAL " + refusal.Reason + " zone=" + refusal.ZoneGuid + " : " + refusal.Message);
            }

            //What the PR2 session would show on opening this model.
            PartOMixedDesignSession session = new(mapped, null, null, null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            PartOMixedReadiness readiness = session.Readiness();
            log.AppendLine("== SESSION rows=" + session.Rows.Count + " (" + string.Join(", ", session.Rows.Select(r => r.Name)) + ") canBuild=" + readiness.CanBuild + " summary=" + readiness.Text);
            readiness.Blockers.ForEach(x => log.AppendLine("  BLOCKER " + x));

            File.WriteAllText(Path.Combine(Directory_Out, Path.GetFileNameWithoutExtension(Path_RealModel) + ".log"), log.ToString());
        }

        [Fact]
        public void Investigation_OptimisedMvhrGap()
        {
            if (string.IsNullOrWhiteSpace(Directory_Out))
            {
                return;
            }

            StringBuilder log = new();
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(4);
            Zone flat4 = PartOMixedDesignFixture.Zone(baseline, "Flat 04");
            VentilationUnitCapacityDescriptor product = new(new VentilationUnitReference("Maker", "Unit", "U-1"), 200, 200);

            log.AppendLine("Clean fixture baseline: clean=" + baseline.IsPartOCleanBaseline(out _) + " terminals=" + (baseline.AdjacencyCluster.GetObjects<VentilationTerminal>()?.Count ?? 0));

            //A. A retained design on a clean baseline with no terminals (what a 2B 'accept' has to overcome).
            string fingerprint_Empty = baseline.AdjacencyCluster.PartODwellingDesignFingerprint(flat4);
            PartOMaterialisation a = Mixed(baseline, flat4, product, fingerprint_Empty).MaterialisePartODwellingStrategies([product]);
            log.AppendLine("A. RetainedDesign on terminal-less baseline: materialised=" + a.IsMaterialised);
            a.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));

            //B. The D3 'accept' edit, on a COPY of the baseline, with SAM's public calls only: realise Flat 04's terminals, then
            //write a balanced raise (supply +3, extract +3) - what a 2B round would have found.
            AnalyticalModel accepted = Accept(baseline, flat4, 3.0, log);
            string fingerprint = accepted.AdjacencyCluster.PartODwellingDesignFingerprint(flat4);
            log.AppendLine("B. accepted baseline clean=" + accepted.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> findings) + " " + string.Join(" | ", findings.Select(x => x.Message)));
            log.AppendLine("   source baseline still terminal-less=" + ((baseline.AdjacencyCluster.GetObjects<VentilationTerminal>()?.Count ?? 0) == 0));

            AnalyticalModel withRetained = Mixed(accepted, flat4, product, fingerprint);
            string json_Before = Core.Convert.ToString(withRetained);
            PartOMaterialisation b = withRetained.MaterialisePartODwellingStrategies([product]);
            log.AppendLine("   Flat01 NV / Flat02 MVHR / Flat03 MVHR+U-1 / Flat04 MVHR RetainedDesign: materialised=" + b.IsMaterialised);
            b.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));
            log.AppendLine("   baseline JSON byte-identical after materialise=" + (json_Before == Core.Convert.ToString(withRetained)));
            if (b.IsMaterialised)
            {
                AdjacencyCluster cluster = b.AnalyticalModel.AdjacencyCluster;
                foreach (Zone zone in PartOMixedDesignFixture.Dwellings(b.AnalyticalModel))
                {
                    foreach (Space space in cluster.GetRelatedObjects<Space>(zone) ?? [])
                    {
                        List<VentilationTerminal> terminals = cluster.GetRelatedObjects<VentilationTerminal>(space) ?? [];
                        log.AppendLine(string.Format("   {0} / {1}: terminals={2} flows={3}", zone.Name, space.Name, terminals.Count, string.Join(",", terminals.Select(t => t.FlowClassification + ":" + t.DesignFlowRate_Lps))));
                    }
                }

                log.AppendLine("   systems=" + b.VentilationSystems.Count + " units=" + string.Join(",", b.VentilationUnitSelections.Select(x => x.ToString())));
                log.AppendLine("   scenarios=" + string.Join(" | ", b.OverheatingScenarios.Select(x => x.ZoneGuid + ":" + x.Iteration)));
                string json_Set = accepted.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies)?.ToJsonObject()?.ToJsonString() ?? withRetained.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).ToJsonObject().ToJsonString();
                log.AppendLine("   strategy JSON carries no airflow: " + !(json_Set.Contains("_Lps") || json_Set.Contains("FlowRate")));
            }

            //C. The same raised terminals under a PartFRequirement basis refuse (never reset silently).
            PartOMaterialisation c = Mixed(accepted, flat4, product, null).MaterialisePartODwellingStrategies([product]);
            log.AppendLine("C. Flat04 MVHR PartFRequirement over raised terminals: materialised=" + c.IsMaterialised);
            c.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));

            //D. PR2 session over the accepted baseline: how does the row read, and what does the UI offer?
            PartOMixedDesignSession session = new(withRetained, null, [product], null);
            session.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            foreach (PartOMixedDwellingRow row in session.Rows)
            {
                log.AppendLine(string.Format("D. row {0}: selected='{1}' designTerminals={2} attention='{3}'", row.Name, row.SelectedText, row.HasDesignTerminals, row.Attention));
            }

            log.AppendLine("D. readiness: " + session.Readiness().Text + " canBuild=" + session.Readiness().CanBuild);
            log.AppendLine("D. screening availability: " + string.Join(" | ", UI.Query.PartOScreeningStrategies().Select(x => x + "=" + (UI.Query.PartOScreeningStrategyUnavailable(x, true) ?? "available"))));

            File.WriteAllText(Path.Combine(Directory_Out, "optimised-gap.log"), log.ToString());
        }

        /// <summary>
        /// A TEST FIXTURE only (not a sanitiser offered to users): the example model with Map IC (TM59) applied, then every
        /// run output (IResult objects, cluster design days) and every piece of earlier Part O / authored ventilation plant
        /// (ventilation systems, air handling units, air movements, ventilation terminals) removed. Geometry, zones, Part F
        /// requirements, weather, heating/cooling systems are kept. Written as a NEW file.
        /// </summary>
        [Fact]
        public void Investigation_DeriveCleanBaseline()
        {
            if (string.IsNullOrWhiteSpace(Directory_Out) || string.IsNullOrWhiteSpace(Path_RealModel))
            {
                return;
            }

            StringBuilder log = new();
            AnalyticalModel model = Core.Convert.ToSAM<AnalyticalModel>(Path_RealModel)?.Find(x => x is not null);
            AnalyticalModel mapped = MapTM59(model, log);
            AdjacencyCluster cluster = mapped.AdjacencyCluster;

            List<Type> types = [typeof(DesignDay), typeof(VentilationSystem), typeof(AirHandlingUnit), typeof(SpaceAirMovement), typeof(AirHandlingUnitAirMovement), typeof(VentilationTerminal)];
            types.AddRange((cluster.GetTypes() ?? []).Where(x => x is not null && typeof(IResult).IsAssignableFrom(x)));
            foreach (Type type in types.Distinct())
            {
                List<object> objects = cluster.GetObjects(type)?.Cast<object>().ToList() ?? [];
                int removed = 0;
                foreach (object @object in objects)
                {
                    if (@object is Core.IJSAMObject jSAMObject && cluster.RemoveObject(jSAMObject))
                    {
                        removed++;
                    }
                }

                log.AppendLine(string.Format("removed {0}: {1}/{2}", type.Name, removed, objects.Count));
            }

            AnalyticalModel derived = new(mapped, cluster);
            Describe(derived, log, "DERIVED");

            VentilationUnitCatalogue catalogue = VentilationUnitCatalogue.Read();
            List<VentilationUnitCapacityDescriptor> descriptors = catalogue.HasSelectableProducts ? catalogue.CapacityDescriptors : null;
            log.AppendLine("catalogue: " + catalogue.Path + " products=" + (descriptors?.Count ?? 0) + " first=" + descriptors?.FirstOrDefault()?.VentilationUnitReference);

            List<Zone> dwellings = Analytical.Query.PartFDwellingZones(cluster.GetZones()) ?? [];
            dwellings.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach ((string title, Func<int, PartODwellingStrategy> func, bool catalogueOffered) in new (string, Func<int, PartODwellingStrategy>, bool)[]
            {
                ("F1 NV / F2 MVHR generic / F3 MVHR generic (no catalogue)", i => i == 0 ? new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.NaturalVentilation) : new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.MVHR), false),
                ("F1 NV / F2 MVHR automatic / F3 MVHR first product (catalogue)", i => i == 0 ? new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.NaturalVentilation) : i == 1 ? new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.MVHR) : new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.MVHR, descriptors?.FirstOrDefault()?.VentilationUnitReference), true),
                ("all NV", i => new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.NaturalVentilation), false),
                ("all MVHR generic", i => new PartODwellingStrategy(dwellings[i].Guid, PartOVentilationMode.MVHR), false),
            })
            {
                PartODwellingStrategySet set = new();
                for (int i = 0; i < dwellings.Count; i++)
                {
                    set.Set(func(i));
                }

                AnalyticalModel withSet = new(derived);
                withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);
                PartOMaterialisation materialisation = withSet.MaterialisePartODwellingStrategies(catalogueOffered ? descriptors : null);
                log.AppendLine("== " + title + ": materialised=" + materialisation.IsMaterialised + " systems=" + materialisation.VentilationSystems.Count + " units=" + string.Join(" ; ", materialisation.VentilationUnitSelections.Select(x => x.ToString())));
                materialisation.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));
                materialisation.Warnings.ForEach(x => log.AppendLine("   warning " + x));
                log.AppendLine("   scenarios=" + string.Join(" | ", materialisation.OverheatingScenarios.Select(x => x.Iteration + "/" + x.ZoneGuid)));
            }

            //Retained design on Flat 3 with the real Part F data (accept on a copy).
            Zone flat3 = dwellings.Last();
            AnalyticalModel accepted = Accept(derived, flat3, 5.0, log);
            string fingerprint = accepted.AdjacencyCluster.PartODwellingDesignFingerprint(flat3);
            PartODwellingStrategySet set_R = new();
            set_R.Set(new PartODwellingStrategy(dwellings[0].Guid, PartOVentilationMode.NaturalVentilation));
            set_R.Set(new PartODwellingStrategy(dwellings[1].Guid, PartOVentilationMode.MVHR));
            set_R.Set(new PartODwellingStrategy(flat3.Guid, PartOVentilationMode.MVHR, null, PartOActiveCooling.None, PartODesignAirFlowBasis.RetainedDesign, fingerprint));
            AnalyticalModel withRetained = new(accepted);
            withRetained.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set_R);
            log.AppendLine("accepted clean=" + accepted.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> f_A) + " " + string.Join(" | ", f_A.Select(x => x.Message)));
            PartOMaterialisation m_R = withRetained.MaterialisePartODwellingStrategies();
            log.AppendLine("== F1 NV / F2 MVHR / F3 RetainedDesign(+5): materialised=" + m_R.IsMaterialised);
            m_R.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));

            string path_Out = Path.Combine(Directory_Out, Path.GetFileNameWithoutExtension(Path_RealModel) + "-MixedBaseline.sam");
            log.AppendLine("saved " + path_Out + " = " + Core.Convert.ToFile(derived, path_Out, SAMFileType.SAM));
            AnalyticalModel reopened = Core.Convert.ToSAM<AnalyticalModel>(path_Out)?.Find(x => x is not null);
            log.AppendLine("reopened clean=" + reopened?.IsPartOCleanBaseline(out _));

            File.WriteAllText(Path.Combine(Directory_Out, "derive.log"), log.ToString());
        }

        /// <summary>
        /// The smallest 2B seam, with REAL 2B airflows: a completed Iteration 2B round model (legacy route) is the source;
        /// its per-space design totals are matched to the clean baseline by space guid + flow direction, written with SAM's
        /// SetSpaceDesignFlowRate onto terminals realised for that one dwelling, and the strategy records RetainedDesign +
        /// fingerprint only. Inputs: SAM_PARTO_MIXED_BASELINE (derived clean baseline), SAM_PARTO_MIXED_2B (2B round .sam).
        /// </summary>
        [Fact]
        public void Investigation_AcceptReal2BAirflow()
        {
            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            string path_2B = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_2B");
            if (string.IsNullOrWhiteSpace(Directory_Out) || string.IsNullOrWhiteSpace(path_Baseline) || string.IsNullOrWhiteSpace(path_2B))
            {
                return;
            }

            StringBuilder log = new();
            AnalyticalModel baseline = Core.Convert.ToSAM<AnalyticalModel>(path_Baseline)?.Find(x => x is not null);
            AnalyticalModel source = Core.Convert.ToSAM<AnalyticalModel>(path_2B)?.Find(x => x is not null);
            log.AppendLine("baseline " + path_Baseline + " clean=" + baseline.IsPartOCleanBaseline(out _));
            log.AppendLine("2B source " + path_2B);

            AdjacencyCluster cluster_Source = source.AdjacencyCluster;
            AdjacencyCluster cluster = baseline.AdjacencyCluster;
            List<Zone> dwellings = Analytical.Query.PartFDwellingZones(cluster.GetZones()) ?? [];
            dwellings.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            Zone zone = dwellings.Last();
            List<Space> spaces = cluster.GetRelatedObjects<Space>(zone) ?? [];

            cluster.RealizePartFVentilationTerminals(spaces, out _, out List<string> refusals_Realise);
            log.AppendLine("realise " + zone.Name + " refusals=" + string.Join(" | ", refusals_Realise));

            foreach (Space space in spaces)
            {
                Space space_Source = cluster_Source.GetObject<Space>(space.Guid);
                foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                {
                    List<VentilationTerminal> terminals_Source = (space_Source is null ? null : cluster_Source.GetRelatedObjects<VentilationTerminal>(space_Source))?.FindAll(x => x.FlowClassification == flowClassification) ?? [];
                    List<VentilationTerminal> terminals = cluster.GetRelatedObjects<VentilationTerminal>(space)?.FindAll(x => x.FlowClassification == flowClassification) ?? [];
                    if (terminals_Source.Count == 0 && terminals.Count == 0)
                    {
                        continue;
                    }

                    double total_Source = terminals_Source.Sum(x => x.DesignFlowRate_Lps ?? 0);
                    double total = terminals.Sum(x => x.DesignFlowRate_Lps ?? 0);
                    cluster.SetSpaceDesignFlowRate(space, flowClassification, total_Source, out _, out List<string> refusals_Set);
                    log.AppendLine(string.Format("  {0} {1}: requirement {2:0.###} -> 2B {3:0.###} l/s (source terminals {4}, baseline terminals {5}) refusals={6}", space.Name, flowClassification, total, total_Source, terminals_Source.Count, terminals.Count, string.Join(" | ", refusals_Set)));
                }
            }

            AnalyticalModel accepted = new(baseline, cluster);
            log.AppendLine("accepted baseline clean=" + accepted.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> findings) + " " + string.Join(" | ", findings.Select(x => x.Message)));
            string fingerprint = accepted.AdjacencyCluster.PartODwellingDesignFingerprint(zone);

            PartODwellingStrategySet set = new();
            set.Set(new PartODwellingStrategy(dwellings[0].Guid, PartOVentilationMode.NaturalVentilation));
            set.Set(new PartODwellingStrategy(dwellings[1].Guid, PartOVentilationMode.MVHR));
            set.Set(new PartODwellingStrategy(zone.Guid, PartOVentilationMode.MVHR, null, PartOActiveCooling.None, PartODesignAirFlowBasis.RetainedDesign, fingerprint));
            AnalyticalModel withSet = new(accepted);
            withSet.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            PartOMaterialisation materialisation = withSet.MaterialisePartODwellingStrategies();
            log.AppendLine("materialise F1 NV / F2 MVHR / " + zone.Name + " Optimised (retained 2B): " + materialisation.IsMaterialised);
            materialisation.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x.Reason + " : " + x.Message));
            if (materialisation.IsMaterialised)
            {
                AdjacencyCluster cluster_M = materialisation.AnalyticalModel.AdjacencyCluster;
                foreach (Space space in spaces)
                {
                    log.AppendLine("   materialised " + space.Name + ": " + string.Join(", ", (cluster_M.GetRelatedObjects<VentilationTerminal>(cluster_M.GetObject<Space>(space.Guid)) ?? []).Select(t => t.FlowClassification + " " + t.DesignFlowRate_Lps)));
                }

                log.AppendLine("   record current=" + materialisation.Record?.IsCurrent(withSet, null, out string reason) + " " );
            }

            string path_Accepted = Path.Combine(Directory_Out, Path.GetFileNameWithoutExtension(path_Baseline) + "-Flat3Accepted2B.sam");
            log.AppendLine("saved accepted baseline (no strategy set) " + path_Accepted + " = " + Core.Convert.ToFile(accepted, path_Accepted, SAMFileType.SAM));
            File.WriteAllText(Path.Combine(Directory_Out, "accept-real-2b.log"), log.ToString());
        }

        /// <summary>The production seam on real data: SAM's AcceptPartODwellingDesign with the real 2B round model.</summary>
        [Fact]
        public void Investigation_AcceptReal2BViaSam()
        {
            string path_Baseline = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_BASELINE");
            string path_2B = Environment.GetEnvironmentVariable("SAM_PARTO_MIXED_2B");
            if (string.IsNullOrWhiteSpace(Directory_Out) || string.IsNullOrWhiteSpace(path_Baseline) || string.IsNullOrWhiteSpace(path_2B))
            {
                return;
            }

            StringBuilder log = new();
            AnalyticalModel baseline = Core.Convert.ToSAM<AnalyticalModel>(path_Baseline)?.Find(x => x is not null);
            AnalyticalModel source = Core.Convert.ToSAM<AnalyticalModel>(path_2B)?.Find(x => x is not null);
            List<Zone> dwellings = Analytical.Query.PartFDwellingZones(baseline.AdjacencyCluster.GetZones()) ?? [];
            dwellings.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (Zone zone in dwellings)
            {
                PartODwellingDesignAcceptance acceptance = baseline.AcceptPartODwellingDesign(zone.Guid, source);
                log.AppendLine(string.Format("{0}: accepted={1} changes={2} fingerprint={3}", zone.Name, acceptance.IsAccepted, acceptance.Changes.Count, acceptance.DesignFingerprint));
                acceptance.Changes.ForEach(x => log.AppendLine(string.Format("   {0} {1}: {2} -> {3}", x.SpaceName, x.FlowClassification, x.Before_Lps, x.After_Lps)));
                acceptance.Refusals.ForEach(x => log.AppendLine("   REFUSAL " + x));
            }

            File.WriteAllText(Path.Combine(Directory_Out, "accept-real-2b-via-sam.log"), log.ToString());
        }

        private static AnalyticalModel Mixed(AnalyticalModel baseline, Zone flat4, VentilationUnitCapacityDescriptor product, string fingerprint_Flat4)
        {
            return PartOMixedDesignFixture.WithStrategies(baseline, zone => zone.Name switch
            {
                "Flat 01" => PartOMixedDesignFixture.Natural(zone),
                "Flat 02" => PartOMixedDesignFixture.Mvhr(zone),
                "Flat 03" => new PartODwellingStrategy(zone.Guid, PartOVentilationMode.MVHR, product.VentilationUnitReference),
                _ => fingerprint_Flat4 is null
                    ? PartOMixedDesignFixture.Mvhr(zone)
                    : new PartODwellingStrategy(zone.Guid, PartOVentilationMode.MVHR, null, PartOActiveCooling.None, PartODesignAirFlowBasis.RetainedDesign, fingerprint_Flat4),
            });
        }

        private static AnalyticalModel Accept(AnalyticalModel baseline, Zone zone, double raise_Lps, StringBuilder log)
        {
            AdjacencyCluster cluster = baseline.AdjacencyCluster;
            List<Space> spaces = cluster.GetRelatedObjects<Space>(zone) ?? [];
            cluster.RealizePartFVentilationTerminals(spaces, out List<string> notes, out List<string> refusals);
            log.AppendLine("   realise terminals " + zone.Name + ": refusals=" + refusals.Count + " " + string.Join(" | ", refusals));

            //Balanced, as a 2B round is: every supply raised by raise_Lps, the whole extract raise on the first extract terminal.
            List<(Space, VentilationTerminal)> pairs = [.. spaces.SelectMany(space => (cluster.GetRelatedObjects<VentilationTerminal>(space) ?? []).Select(t => (space, t)))];
            int count_Supply = pairs.Count(x => x.Item2.FlowClassification == FlowClassification.Supply);
            bool extractDone = false;
            foreach ((Space space, VentilationTerminal terminal) in pairs)
            {
                double raise = 0;
                if (terminal.FlowClassification == FlowClassification.Supply)
                {
                    raise = raise_Lps;
                }
                else if (!extractDone)
                {
                    raise = raise_Lps * count_Supply;
                    extractDone = true;
                }

                if (raise == 0)
                {
                    continue;
                }

                double from = terminal.DesignFlowRate_Lps ?? 0;
                cluster.SetSpaceDesignFlowRate(space, terminal.FlowClassification, from + raise, out _, out List<string> refusals_Set);
                log.AppendLine(string.Format("   set {0} {1} {2} -> {3}: refusals={4}", space.Name, terminal.FlowClassification, from, from + raise, string.Join(" | ", refusals_Set)));
            }

            return new AnalyticalModel(baseline, cluster);
        }

        private static void Describe(AnalyticalModel model, StringBuilder log, string title)
        {
            log.AppendLine("== " + title);
            AdjacencyCluster cluster = model.AdjacencyCluster;
            foreach (Zone zone in cluster.GetZones() ?? [])
            {
                zone.TryGetValue(ZoneParameter.IsDwelling, out bool isDwelling);
                zone.TryGetValue(ZoneParameter.ZoneCategory, out string category);
                log.AppendLine(string.Format("  zone '{0}' dwelling={1} category='{2}' spaces={3}", zone.Name, isDwelling, category, string.Join(", ", (cluster.GetRelatedObjects<Space>(zone) ?? []).Select(s => s.Name + " [" + s.InternalCondition?.Name + "]"))));
            }

            log.AppendLine("  terminals=" + (cluster.GetObjects<VentilationTerminal>()?.Count ?? 0) + " systems=" + (cluster.GetObjects<VentilationSystem>()?.Count ?? 0) + " AHUs=" + (cluster.GetObjects<AirHandlingUnit>()?.Count ?? 0) + " spaceMovements=" + (cluster.GetObjects<SpaceAirMovement>()?.Count ?? 0) + " designDays(cluster)=" + (cluster.GetObjects<DesignDay>()?.Count ?? 0));
            log.AppendLine("  clean baseline=" + model.IsPartOCleanBaseline(out List<PartOMaterialisationRefusal> findings));
            foreach (PartOMaterialisationRefusal finding in findings)
            {
                log.AppendLine("  FINDING " + finding.Reason + " : " + finding.Message);
            }
        }

        private static AnalyticalModel MapTM59(AnalyticalModel model, StringBuilder log)
        {
            AdjacencyCluster cluster = model.AdjacencyCluster;
            TextMap textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();
            InternalConditionLibrary library = Analytical.Query.DefaultInternalConditionLibrary_TM59();

            HashSet<string> categories = [];
            foreach (Zone zone in cluster.GetZones() ?? [])
            {
                if (zone.TryGetValue(ZoneParameter.ZoneCategory, out string category) && !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            MethodInfo methodInfo = typeof(MapTM59InternalConditionsControl).GetMethod("SelectDefaultZoneCategory", BindingFlags.NonPublic | BindingFlags.Static);
            string zoneType = methodInfo?.Invoke(null, [categories]) as string;
            log.AppendLine("== MAP IC (TM59) zone type='" + zoneType + "' of {" + string.Join(", ", categories) + "}");

            TM59Manager tM59Manager = new(textMap);
            foreach (Space space in model.GetSpaces() ?? [])
            {
                var result = tM59Manager.GetInternalConditionResult(cluster, library, space, zoneType);
                InternalCondition internalCondition = result?.InternalCondition;
                log.AppendLine(string.Format("  {0}: '{1}' -> '{2}'  ({3})", space.Name, space.InternalCondition?.Name, internalCondition?.Name, result?.Diagnostic?.Replace("\n", " ")));
                if (internalCondition is null)
                {
                    continue;
                }

                Space space_New = new(space) { InternalCondition = internalCondition };
                space_New.SetValue(SpaceParameter.Occupancy, TM59Manager.TM59Occupancy(internalCondition));
                space_New.UpdateAreaPerPerson();
                cluster.AddObject(space_New);
            }

            foreach (InternalCondition internalCondition in library.GetInternalConditions() ?? [])
            {
                if (!cluster.Contains<InternalCondition>(internalCondition.Guid))
                {
                    cluster.AddObject(internalCondition);
                }
            }

            return new AnalyticalModel(model, cluster);
        }
    }
}
