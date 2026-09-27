// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A clean pre-Part-O baseline of flats that SAM's mixed materialisation can build: each flat a bedroom with a
    /// continuous Part F supply and a bathroom with a matching extract, joined by an internal partition (so the MVHR
    /// design has a transfer-air path), plus one communal-corridor zone that is not a dwelling.
    /// </summary>
    internal static class PartOMixedDesignFixture
    {
        internal const string Corridor = "Corridor";

        internal static AnalyticalModel Baseline(int count_Flat = 3, bool corridor = true, int spacesPerFlat = 2, string category = null)
        {
            AdjacencyCluster adjacencyCluster = new();

            double x = 0;

            for (int i = 1; i <= count_Flat; i++)
            {
                string name_Flat = string.Format("Flat {0:00}", i);

                Zone zone = new(name_Flat);
                zone.SetValue(ZoneParameter.IsDwelling, true);
                if (category is not null)
                {
                    zone.SetValue(ZoneParameter.ZoneCategory, i % 2 == 0 ? category + " B" : category + " A");
                }

                adjacencyCluster.AddObject(zone);

                Space bedroom = Room(string.Format("{0} Bedroom", name_Flat), PartFType.Habitable, PartFVentilationType.supply, PartFTerminalRole.Supply, 8.0);
                Space bathroom = Room(string.Format("{0} Bathroom", name_Flat), PartFType.WetRoom, PartFVentilationType.extract, PartFTerminalRole.GeneralExtract, 8.0);

                adjacencyCluster.AddObject(bedroom);
                adjacencyCluster.AddObject(bathroom);
                adjacencyCluster.AddRelation(zone, bedroom);
                adjacencyCluster.AddRelation(zone, bathroom);

                Partition(adjacencyCluster, bedroom, bathroom, x);
                x += 10;

                //Further rooms with no Part F data - a store - so a flat can carry the ~10 spaces of a real one.
                for (int j = 2; j < spacesPerFlat; j++)
                {
                    Space store = new(string.Format("{0} Store {1}", name_Flat, j));
                    store.SetValue(SpaceParameter.Area, 4.0);
                    store.SetValue(SpaceParameter.Volume, 10.0);
                    store.InternalCondition = new InternalCondition(store.Name + " IC");
                    adjacencyCluster.AddObject(store);
                    adjacencyCluster.AddRelation(zone, store);
                }
            }

            if (corridor)
            {
                Space space_Corridor = new(Corridor);
                space_Corridor.SetValue(SpaceParameter.Area, 20.0);
                space_Corridor.SetValue(SpaceParameter.Volume, 50.0);
                space_Corridor.InternalCondition = new InternalCondition(TM59InternalConditionResolver.CommunalCorridorInternalConditionName);
                adjacencyCluster.AddObject(space_Corridor);

                Zone zone_Corridor = new(Corridor);
                zone_Corridor.SetValue(ZoneParameter.IsDwelling, false);
                adjacencyCluster.AddObject(zone_Corridor);
                adjacencyCluster.AddRelation(zone_Corridor, space_Corridor);
            }

            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, new MaterialLibrary("Materials"), new ProfileLibrary("Profiles"));
        }

        internal static Space Room(string name, PartFType partFType, PartFVentilationType partFVentilationType, PartFTerminalRole partFTerminalRole, double flow_Lps)
        {
            Space space = new(name);
            space.SetValue(SpaceParameter.Area, 10.0);
            space.SetValue(SpaceParameter.Volume, 25.0);

            InternalCondition internalCondition = new(name + " IC");
            internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, "Ventilation System");
            space.InternalCondition = internalCondition;

            bool supply = partFVentilationType == PartFVentilationType.supply;
            PartFSpaceData partFSpaceData = new(name, partFType, partFVentilationType, supply, null, true, true, supply, false, "Volume", flow_Lps);

            partFSpaceData.Terminals.Add(new PartFVentilationTerminalRequirement(name + (supply ? " - Supply" : " - Extract"), space.Guid, partFTerminalRole)
            {
                SpaceName = name,
                OperatingMode = PartFOperatingMode.ContinuousDesign,
                ContinuousDesignFlowRate_Lps = flow_Lps,
                IsInBalancedFlow = true,
                IsRequired = true,
                SourceReference = "PR2 fixture",
            });

            space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            return space;
        }

        private static void Partition(AdjacencyCluster adjacencyCluster, Space space_1, Space space_2, double x)
        {
            Face3D face3D = new(new Polygon3D(
            [
                new Point3D(x, 0, 0),
                new Point3D(x + 4, 0, 0),
                new Point3D(x + 4, 0, 3),
                new Point3D(x, 0, 3),
            ]));

            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, face3D);

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space_1, panel);
            adjacencyCluster.AddRelation(space_2, panel);
        }

        internal static List<Zone> Dwellings(AnalyticalModel analyticalModel)
        {
            List<Zone> result = analyticalModel.AdjacencyCluster.GetZones().FindAll(x => x.Name != Corridor);
            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }

        internal static Zone Zone(AnalyticalModel analyticalModel, string name) => analyticalModel.AdjacencyCluster.GetZones().Single(x => x.Name == name);

        /// <summary>The baseline carrying a strategy set: natural for the names given, MVHR for the rest.</summary>
        internal static AnalyticalModel WithStrategies(AnalyticalModel analyticalModel, Func<Zone, PartODwellingStrategy> func)
        {
            PartODwellingStrategySet partODwellingStrategySet = new();
            foreach (Zone zone in Dwellings(analyticalModel))
            {
                PartODwellingStrategy partODwellingStrategy = func(zone);
                if (partODwellingStrategy is not null)
                {
                    partODwellingStrategySet.Set(partODwellingStrategy);
                }
            }

            AnalyticalModel result = new(analyticalModel);
            result.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, partODwellingStrategySet);
            return result;
        }

        internal static PartODwellingStrategy Natural(Zone zone) => new(zone.Guid, PartOVentilationMode.NaturalVentilation);

        internal static PartODwellingStrategy Mvhr(Zone zone) => new(zone.Guid, PartOVentilationMode.MVHR);

        internal static PartOSimulationContext Context(string projectName = "Block_Test") => new(Path.GetTempPath(), projectName, null, SolarCalculationMethod.TAS, 1, 365);

        /// <summary>
        /// A stand-in for TAS + TM59: records what it was handed, and reports every occupied space of the listed dwellings
        /// as failing and every other as passing. Never touches the filesystem.
        /// </summary>
        internal sealed class FakeSimulator
        {
            internal List<AnalyticalModel> Models { get; } = [];

            internal List<string> ProjectNames { get; } = [];

            internal Func<AnalyticalModel, HashSet<string>> Failing { get; set; } = _ => [];

            internal bool Cancel { get; set; }

            internal string OutputDirectory { get; } = Path.Combine(Path.GetTempPath(), "SAM_PartOMixed_" + Guid.NewGuid().ToString("N"));

            internal string Refusal { get; set; }

            internal PartOStrategySetSimulation Simulate(AnalyticalModel analyticalModel_Materialised, List<OverheatingScenario> overheatingScenarios, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken)
            {
                Models.Add(analyticalModel_Materialised);
                ProjectNames.Add(partOSimulationContext.ProjectName);

                if (Cancel)
                {
                    return new PartOStrategySetSimulation { Cancelled = true };
                }

                if (Refusal is not null)
                {
                    return new PartOStrategySetSimulation { Refusal = Refusal };
                }

                HashSet<string> failing = Failing(analyticalModel_Materialised) ?? [];

                Dictionary<Guid, TM59ComplianceStatus> statuses = [];
                AdjacencyCluster adjacencyCluster = analyticalModel_Materialised.AdjacencyCluster;
                foreach (Zone zone in adjacencyCluster.GetZones())
                {
                    foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                    {
                        if (space.Name.Contains("Store"))
                        {
                            continue;
                        }

                        statuses[space.Guid] = failing.Contains(zone.Name) ? TM59ComplianceStatus.Fail : TM59ComplianceStatus.Pass;
                    }
                }

                //An assessed assessment with no hourly data behind it: SAM's result type has no public constructor, and nothing
                //here reads it beyond "there is one" - the per-space statuses are the whole of what the tally reads.
                TM59AssessmentResult tM59AssessmentResult = (TM59AssessmentResult)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TM59AssessmentResult));
                PartOTM59Assessment partOTM59Assessment = new(tM59AssessmentResult, new TM59AssessmentReport(null, [], [], []), [], [], [], null, null, null, statuses);

                //A real file in a directory of this fake's own, so the result's lineage (length and write time) is
                //checked exactly as production checks it.
                Directory.CreateDirectory(OutputDirectory);
                string path_TSD = Path.Combine(OutputDirectory, partOSimulationContext.ProjectName + ".tsd");
                File.WriteAllText(path_TSD, Guid.NewGuid().ToString());
                FileInfo fileInfo = new(path_TSD);

                return new PartOStrategySetSimulation
                {
                    Path_TSD = path_TSD,
                    Length_TSD = fileInfo.Length,
                    Timestamp_TSD = fileInfo.LastWriteTimeUtc.Ticks,
                    Assessment = partOTM59Assessment,
                };
            }
        }
    }
}
