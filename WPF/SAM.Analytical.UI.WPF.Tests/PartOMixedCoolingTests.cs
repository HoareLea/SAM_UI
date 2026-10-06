// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Mixed Part O PR3C: per-dwelling active cooling in the Mixed Design workflow, over the merged PR3B authority (SAM
    /// PR3B record). Cooling is intent on the strategy, orthogonal to the ventilation strategy; SAM decides whether it can
    /// be built (the product's manufacturer guidance, the published range, the unit's capacity) and puts a cooled design
    /// on the TAS Systems route for the whole building; the final run then makes ONE mixed SAM_Systems call.
    /// <para>
    /// TAS is a stand-in on both routes. The SAM_Systems call is the real one - it needs no licence. The fixture product's
    /// figures are Nuaire-shaped test data (SAM's own PR3B-1 fixture shape), never read by production code: production
    /// reads every cooling number from the product template, which is what <see cref="CoolingAirflow_FollowsTheProductTemplate_NotSamUi"/> pins.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedCoolingTests
    {
        internal static readonly VentilationUnitReference Reference = new("Fixture", "MVHR-C", "COOL");

        internal static readonly VentilationUnitCapacityDescriptor Descriptor = new(Reference, 150, 150);

        internal static VentilationUnitTemplate Template(double default_Lps = 80.0, double minimum_Lps = 60.0, double maximum_Lps = 120.0)
        {
            return new VentilationUnitTemplate(Reference, "PR3C fixture")
            {
                MaximumSupplyFlowRate_Lps = 150,
                MaximumExtractFlowRate_Lps = 150,
                OperatingStrategy = new VentilationUnitOperatingStrategy
                {
                    Source = "PR3C fixture - Nuaire-shaped figures, not a product",
                    CoolingActivationSignal = CoolingActivationSignal.RoomTemperature,
                    CoolingActivationTemperature_C = 22.0,
                    MinimumCoolingActivationTemperature_C = 22.0,
                    MaximumCoolingActivationTemperature_C = 25.0,
                    BypassMinimumIntakeTemperature_C = 12.0,
                    BypassMinimumExtractTemperature_C = 19.0,
                    DefaultElevatedAirFlow_Lps = default_Lps,
                    MinimumElevatedAirFlow_Lps = minimum_Lps,
                    MaximumElevatedAirFlow_Lps = maximum_Lps,
                    SummerBypassSupplyTemperatureRule = SupplyTemperatureRule.OutdoorAir(),
                    HeatCoolthRecoverySupplyTemperatureRule = SupplyTemperatureRule.LinearBlend(0.8),
                    CoolingSupplyTemperatureRule = SupplyTemperatureRule.ExchangerThenCoil([60.0, 80.0, 100.0, 120.0], [0.8796, 0.8576, 0.8356, 0.8136], [9.265, 8.745, 8.225, 7.705], [0.3, 0.5, 0.8, 1.1], 13.0),
                },
            };
        }

        private static PartODwellingStrategy Cooled(Zone zone) => new(zone.Guid, PartOVentilationMode.MVHR, Reference, PartOActiveCooling.SupplyAirCooling);

        private static PartODwellingStrategy Product(Zone zone) => new(zone.Guid, PartOVentilationMode.MVHR, Reference);

        [Fact]
        public void LegacyCooledSelection_IsVisibleAndBlockedUntilEngineerConfirmsRoom()
        {
            AnalyticalModel baseline = Representative();
            Zone zone = PartOMixedDesignFixture.Zone(baseline, "Flat 03");
            PartODwellingStrategySet set = baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            set.Set(new PartODwellingStrategy(set.Strategy(zone.Guid)) { CoolingStatSpaceGuid = Guid.Empty });
            baseline.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            PartOMixedDesignSession session = Session(new AnalyticalModel(baseline.ToJsonObject()));
            PartOMixedDwellingRow row = session.Rows.Single(x => x.ZoneGuid == zone.Guid);
            Assert.Equal("Select room", row.CoolingControlRoomText);
            Assert.False(session.Readiness().CanBuild);
            Assert.Contains("Select and confirm", row.Attention);

            Space chosen = session.CoolingControlRooms(row).Single(x => x.Name.EndsWith(" Bedroom", StringComparison.Ordinal));
            Assert.Null(session.SetCoolingControlRoom(row, chosen.Guid));
            Assert.Equal(chosen.Name, row.CoolingControlRoomText);
            Assert.True(session.Readiness().CanBuild);

            AnalyticalModel reopened = new(session.WithSelection().ToJsonObject());
            Assert.Equal(chosen.Guid, reopened.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).Strategy(zone.Guid).CoolingStatSpaceGuid);
        }

        /// <summary>The representative mixed case: Flat 01 Natural, Flat 02 MVHR uncooled, Flat 03 MVHR cooled, corridor free-running.</summary>
        private static AnalyticalModel Representative(AnalyticalModel? analyticalModel = null)
        {
            return PartOMixedDesignFixture.WithStrategies(analyticalModel ?? PartOMixedDesignFixture.Baseline(), x => x.Name switch
            {
                "Flat 01" => PartOMixedDesignFixture.Natural(x),
                "Flat 02" => Product(x),
                _ => Cooled(x),
            });
        }

        private static PartOMixedDesignSession Session(AnalyticalModel analyticalModel)
        {
            PartOMixedDesignSession result = new(analyticalModel, null, [Descriptor], null, [Template()]) { SimulationCaseKey = PartOMixedDesignFixture.CaseKey };
            result.CatalogueOffered = true;
            return result;
        }

        /// <summary>A stand-in for the Systems route: records the materialisation it was handed and answers as TAS + TM59 would.</summary>
        private sealed class FakeSystems
        {
            internal List<PartOMaterialisation> Materialisations { get; } = [];

            internal List<IReadOnlyList<VentilationUnitTemplate>?> Templates { get; } = [];

            internal PartOMixedDesignFixture.FakeSimulator Simulator { get; } = new();

            internal PartOStrategySetSimulation Simulate(PartOMaterialisation partOMaterialisation, IReadOnlyList<VentilationUnitTemplate>? ventilationUnitTemplates, PartOSimulationContext partOSimulationContext, CancellationToken cancellationToken)
            {
                Materialisations.Add(partOMaterialisation);
                Templates.Add(ventilationUnitTemplates);

                PartOStrategySetSimulation result = Simulator.Simulate(partOMaterialisation.AnalyticalModel, [.. partOMaterialisation.OverheatingScenarios], partOSimulationContext, cancellationToken);
                result.Route = PartOSimulationRoute.Systems;
                result.Path_TPD = System.IO.Path.ChangeExtension(result.Path_TSD, ".tpd");
                result.GuidanceSummaries.Add("fixture read-back");
                return result;
            }
        }

        private static PartOMixedRunEvidence? Build(AnalyticalModel baseline, PartOMixedDesignFixture.FakeSimulator izam, FakeSystems systems, out PartOStrategySetRun run, bool catalogueOffered = true, List<VentilationUnitTemplate>? templates = null)
        {
            return Modify.BuildAndRunPartOMixedDesign(baseline, catalogueOffered, [Descriptor], PartOMixedDesignFixture.Context("Block_Mixed"), CancellationToken.None, out run, izam.Simulate, null, templates ?? [Template()], systems.Simulate);
        }

        private static PartOIteration? Iteration(PartOMaterialisation partOMaterialisation, Guid guid_Zone) => partOMaterialisation.OverheatingScenarios.SingleOrDefault(x => x.ZoneGuid == guid_Zone)?.Iteration;

        // =================================================================================================
        // The project rule and the per-dwelling toggle
        // =================================================================================================

        [Fact]
        public void CoolingConstraint_AllowsCoolingByDefault_AndIsAPersistedProjectRule()
        {
            Zone zone = PartOMixedDesignFixture.Dwellings(PartOMixedDesignFixture.Baseline())[0];

            PartOMixedDesignConstraints constraints = new();
            Assert.True(constraints.CoolingAllowed);
            Assert.Null(constraints.Refusal(Cooled(zone)));
            Assert.True(constraints.Allows(PartOScreeningStrategy.ActiveCooling));

            constraints.CoolingAllowed = false;
            Assert.Equal("The project does not allow active cooling.", constraints.Refusal(Cooled(zone)));
            Assert.Null(constraints.Refusal(Product(zone)));

            //Kept beside the model, and a state written before PR3C (no key) reads as allowed.
            Assert.False(PartOMixedDesignConstraints.Read(constraints.ToJsonObject()).CoolingAllowed);
            Assert.True(PartOMixedDesignConstraints.Read(new System.Text.Json.Nodes.JsonObject { ["NaturalVentilationAllowed"] = true }).CoolingAllowed);
        }

        [Fact]
        public void CoolingOn_IsOrthogonalToTheStrategy_AndOnlyForMechanicalDwellings()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 01" ? PartOMixedDesignFixture.Natural(x) : Product(x));
            PartOMixedDesignSession session = Session(baseline);
            PartOMixedDwellingRow flat01 = session.Rows.Single(x => x.Name == "Flat 01");
            PartOMixedDwellingRow flat03 = session.Rows.Single(x => x.Name == "Flat 03");

            //Natural + cooling is never created: refused whole, with the reason, and nothing changes.
            string? refusal = session.SetCooling([flat01, flat03], true);
            Assert.Contains("MVHR", refusal);
            Assert.Contains("Flat 01", refusal);
            Assert.False(flat03.Cooled);
            Assert.False(session.IsDirty);

            //On for an MVHR dwelling: the same strategy with cooling intent, and nothing else.
            PartODwellingStrategy before = flat03.Selected!;
            Assert.Null(session.SetCooling([flat03], true));
            PartODwellingStrategy after = flat03.Selected!;
            Assert.Equal(PartOActiveCooling.SupplyAirCooling, after.ActiveCooling);
            Assert.Equal(before.VentilationMode, after.VentilationMode);
            Assert.Equal(before.VentilationUnitReference.ToString(), after.VentilationUnitReference.ToString());
            Assert.Equal(before.DesignAirFlowBasis, after.DesignAirFlowBasis);

            //A word per row - no per-row control.
            Assert.Equal("On", flat03.CoolingText);
            Assert.Equal("Off", session.Rows.Single(x => x.Name == "Flat 02").CoolingText);
            Assert.Equal("—", flat01.CoolingText);
            Assert.EndsWith("· active cooling", flat03.SelectedText);

            PartOMixedReadiness readiness = session.Readiness();
            Assert.Equal(1, readiness.Cooled);
            Assert.Equal(2, readiness.Mvhr);
            Assert.Contains("1 with active cooling", readiness.Text);
            Assert.True(session.IsDirty);

            //The project rule refuses it, and flags a cooled selection made before the rule changed - never rewritten.
            session.Constraints.CoolingAllowed = false;
            session.Refresh();
            Assert.True(flat03.Cooled);
            Assert.Contains("does not allow active cooling", flat03.Attention);
            Assert.Equal("The project does not allow active cooling.", session.SetCooling([session.Rows.Single(x => x.Name == "Flat 02")], true));
        }

        [Fact]
        public void VentilationEdits_KeepCooling_AndNaturalIsRefusedUntilCoolingIsOff()
        {
            AnalyticalModel baseline = Representative();
            PartOMixedDesignSession session = Session(baseline);
            PartOMixedDwellingRow flat03 = session.Rows.Single(x => x.Name == "Flat 03");
            Assert.True(flat03.Cooled);

            //Choosing the product again (or automatic) is a ventilation edit - the cooling intent stays.
            Assert.Null(session.SetMvhr([flat03], null));
            Assert.True(flat03.Cooled);
            Assert.Null(flat03.Selected!.VentilationUnitReference);

            //Natural would silently drop the cooling: refused until cooling is turned off explicitly.
            string? refusal = session.SetNatural([flat03]);
            Assert.Contains("Turn active cooling off first", refusal);
            Assert.Equal(PartOVentilationMode.MVHR, flat03.Selected!.VentilationMode);

            Assert.Null(session.SetCooling([flat03], false));
            Assert.Null(session.SetNatural([flat03]));
            Assert.Equal(PartOVentilationMode.NaturalVentilation, flat03.Selected!.VentilationMode);
            Assert.Equal(PartOActiveCooling.None, flat03.Selected.ActiveCooling);
        }

        [Fact]
        public void CoolingOff_AfterOn_IsExactlyTheUncooledSelection_AndIsNeverBlockedByAnotherRule()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), Product);
            PartOMixedDesignSession session = Session(baseline);
            List<PartOMixedDwellingRow> rows = [.. session.Rows];

            Assert.Null(session.SetCooling(rows, true));
            Assert.True(session.IsDirty);

            //Even with the project now forbidding cooling (so On would be refused), Off always goes through.
            session.Constraints.CoolingAllowed = false;
            Assert.Null(session.SetCooling(rows, false));

            Assert.All(rows, x => Assert.False(x.Cooled));
            Assert.False(session.IsDirty);
            Assert.Equal(baseline.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).ToJsonObject().ToJsonString(), session.Draft.ToJsonObject().ToJsonString());
        }

        [Fact]
        public void CoolingIntent_IsSavedOnTheModel_AndSurvivesReopen()
        {
            PartOMixedDesignSession session = Session(PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), Product));
            Assert.Null(session.SetCooling([session.Rows.Single(x => x.Name == "Flat 02")], true));

            AnalyticalModel saved = session.WithSelection();

            //Through SAM's own serialisation, as Save / Open do.
            AnalyticalModel reopened = new(saved.ToJsonObject());
            PartOMixedDesignSession session_Reopened = Session(reopened);

            Assert.False(session_Reopened.IsDirty);
            Assert.Equal(["Flat 02"], session_Reopened.Rows.Where(x => x.Cooled).Select(x => x.Name));
            Assert.Equal("On", session_Reopened.Rows.Single(x => x.Name == "Flat 02").CoolingText);

            //Intent only: no cooling figure is stored on the strategy.
            string json = reopened.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies).ToJsonObject().ToJsonString();
            //(SAM's own DesignAirFlowBasis key names the basis, never a figure.)
            Assert.DoesNotContain("Lps", json);
            Assert.DoesNotContain("CoolingOperating", json);
            Assert.DoesNotContain("ElevatedAirFlow", json);
        }

        [Fact]
        public void Corridor_IsNeverACoolingRow_AndACorridorStrategyIsRefusedBySam()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), Product);
            PartOMixedDesignSession session = Session(baseline);

            //Only dwellings are rows, so "every row" cannot reach the corridor.
            Assert.DoesNotContain(session.Rows, x => x.Name == PartOMixedDesignFixture.Corridor);
            Assert.Null(session.SetCooling([.. session.Rows], true));
            foreach (PartOMixedDwellingRow row in session.Rows)
            {
                Assert.Null(session.SetCoolingControlRoom(row, session.CoolingControlRooms(row).Single(x => x.Name.EndsWith(" Bedroom", StringComparison.Ordinal)).Guid));
            }
            Zone corridor = PartOMixedDesignFixture.Zone(baseline, PartOMixedDesignFixture.Corridor);
            Assert.Null(session.Draft.Strategy(corridor.Guid));

            //A strategy set that names the corridor anyway (hand-edited) is SAM's to refuse - structured, per zone.
            AnalyticalModel leaked = session.WithSelection();
            PartODwellingStrategySet set = leaked.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);
            set.Set(Cooled(corridor));
            leaked.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, set);

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();
            Assert.Null(Build(leaked, izam, systems, out PartOStrategySetRun run));
            Assert.Contains(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.NotADwelling && x.ZoneGuid == corridor.Guid);
            Assert.Empty(izam.Models);
            Assert.Empty(systems.Materialisations);

            //The accepted cooled design leaves the corridor dwelling-independent, and outside the Systems scope.
            Assert.NotNull(Build(session.WithSelection(), izam, systems, out run));
            PartOMaterialisation partOMaterialisation = Assert.Single(systems.Materialisations);
            Assert.Equal(PartOIteration.DwellingIndependent, Iteration(partOMaterialisation, corridor.Guid));
            PartOMixedSystemsCall call = Query.PartOMixedSystemsCall(partOMaterialisation, [Template()]);
            Assert.DoesNotContain(call.Spaces, x => x.Name == PartOMixedDesignFixture.Corridor);
        }

        // =================================================================================================
        // The production pipeline: SAM's authority, the route, the mixed SAM_Systems call
        // =================================================================================================

        [Fact]
        public void CooledDesign_RunsTheWholeModelOnTheSystemsRoute_OnceWithTheMaterialisersScenarios()
        {
            AnalyticalModel baseline = Representative();
            string json_Before = baseline.ToJsonObject().ToJsonString();

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();
            PartOMixedRunEvidence? evidence = Build(baseline, izam, systems, out PartOStrategySetRun run);

            Assert.NotNull(evidence);

            //ONE model, on ONE route: the Systems simulator, never the IZAM one - no hybrid.
            Assert.Empty(izam.Models);
            PartOMaterialisation partOMaterialisation = Assert.Single(systems.Materialisations);
            Assert.Equal(PartOSimulationRoute.Systems, partOMaterialisation.Route);

            //The catalogue's templates reached SAM and the Systems route.
            Assert.Single(Assert.Single(systems.Templates)!);

            //Truthful identity per dwelling, from SAM's materialiser.
            Zone Z(string name) => PartOMixedDesignFixture.Zone(baseline, name);
            Assert.Equal(PartOIteration.BaseNaturalVentilation, Iteration(partOMaterialisation, Z("Flat 01").Guid));
            Assert.Equal(PartOIteration.BasePassive, Iteration(partOMaterialisation, Z("Flat 02").Guid));
            Assert.Equal(PartOIteration.ActiveTrimCooling, Iteration(partOMaterialisation, Z("Flat 03").Guid));
            Assert.Equal(PartOIteration.DwellingIndependent, Iteration(partOMaterialisation, Z(PartOMixedDesignFixture.Corridor).Guid));

            //The evidence says the route and the cooled dwelling (SAM's record), and what each dwelling ran as.
            Assert.Equal(PartOSimulationRoute.Systems, evidence!.Route);
            Assert.Equal(Z("Flat 03").Guid, Assert.Single(evidence.Record.CooledDwellings).ZoneGuid);
            Assert.Equal(PartOActiveCooling.SupplyAirCooling, evidence.Strategies.Strategy(Z("Flat 03").Guid).ActiveCooling);
            Assert.Equal(PartOActiveCooling.None, evidence.Strategies.Strategy(Z("Flat 02").Guid).ActiveCooling);
            Assert.NotNull(evidence.Path_TPD);
            Assert.Equal(["fixture read-back"], evidence.GuidanceSummaries);

            //Persisted and read back, and current only with the templates it was built with (SAM's cooled-record rule).
            PartOMixedRunEvidence read = PartOMixedRunEvidence.Read(evidence.ToJsonObject());
            Assert.Equal(PartOSimulationRoute.Systems, read.Route);
            Assert.Equal(evidence.Path_TPD, read.Path_TPD);
            Assert.True(read.IsCurrent(baseline, [Descriptor], [Template()], out string? reason), reason);
            Assert.False(read.IsCurrent(baseline, [Descriptor], null, out _));
            Assert.False(read.IsCurrent(baseline, [Descriptor], [Template(default_Lps: 90)], out reason));
            Assert.Contains("guidance", reason);

            Assert.Equal(json_Before, baseline.ToJsonObject().ToJsonString());
        }

        [Fact]
        public void UncooledDesign_StaysOnTheUnchangedIzamRoute()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 01" ? PartOMixedDesignFixture.Natural(x) : Product(x));

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();
            PartOMixedRunEvidence? evidence = Build(baseline, izam, systems, out PartOStrategySetRun run);

            Assert.NotNull(evidence);
            Assert.Single(izam.Models);
            Assert.Empty(systems.Materialisations);
            Assert.Equal(PartOSimulationRoute.Izam, evidence!.Route);
            Assert.Null(evidence.Path_TPD);
            Assert.Empty(evidence.GuidanceSummaries);
            Assert.Empty(evidence.Record.CooledDwellings);

            //PR2's record is written exactly as before (v1, no cooling keys), so PR2 sidecars never go stale.
            string json_Record = run.Materialisation!.Record.ToJsonObject().ToJsonString();
            Assert.Contains(PartOMaterialisationRecord.Schema, json_Record);
            Assert.DoesNotContain(PartOMaterialisationRecord.Schema_Cooled, json_Record);
            Assert.DoesNotContain("CooledDwellings", json_Record);
        }

        [Fact]
        public void CoolingWithoutTheCatalogue_IsRefusedBySam_TheTemplatesAreNeverOfferedAlone()
        {
            AnalyticalModel baseline = Representative();
            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();

            //Generic units (catalogue not offered): no templates reach SAM, so SAM refuses the cooled dwelling.
            Assert.Null(Build(baseline, izam, systems, out PartOStrategySetRun run, catalogueOffered: false));
            Assert.Contains(run.Refusals, x => x.Reason is PartOMaterialisationRefusalReason.CoolingWithoutProductGuidance or PartOMaterialisationRefusalReason.VentilationUnitUnresolved);
            Assert.Empty(izam.Models);
            Assert.Empty(systems.Materialisations);

            //The catalogue offered but its product carries no manufacturer guidance: SAM's structured refusal, on Flat 03.
            Assert.Null(Build(baseline, izam, systems, out run, templates: [new VentilationUnitTemplate(Reference, "no guidance") { MaximumSupplyFlowRate_Lps = 150, MaximumExtractFlowRate_Lps = 150 }]));
            PartOMaterialisationRefusal refusal = Assert.Single(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.CoolingWithoutProductGuidance);
            Assert.Equal(PartOMixedDesignFixture.Zone(baseline, "Flat 03").Guid, refusal.ZoneGuid);

            //The session offers the templates only with the catalogue.
            PartOMixedDesignSession session = Session(baseline);
            Assert.NotNull(session.TemplatesOffered);
            session.CatalogueOffered = false;
            Assert.Null(session.TemplatesOffered);
        }

        [Fact]
        public void DesignAirflowBeyondTheProductsCoolingRange_IsRefusedBySam_AndShownOnTheDwelling()
        {
            AnalyticalModel baseline = Representative();
            string json_Before = baseline.ToJsonObject().ToJsonString();

            //The product publishes cooling data only up to a figure below this dwelling's accepted design airflow - the
            //shape of the real 143 l/s design against a 120 l/s product. The design is never reduced to fit.
            List<VentilationUnitTemplate> templates = [Template(default_Lps: 1.0, minimum_Lps: 1.0, maximum_Lps: 2.0)];

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();
            Assert.Null(Build(baseline, izam, systems, out PartOStrategySetRun run, templates: templates));

            PartOMaterialisationRefusal refusal = Assert.Single(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.CoolingAirFlowOutsideGuidance);
            Assert.Equal(PartOMixedDesignFixture.Zone(baseline, "Flat 03").Guid, refusal.ZoneGuid);
            Assert.Empty(izam.Models);
            Assert.Empty(systems.Materialisations);
            Assert.Equal(json_Before, baseline.ToJsonObject().ToJsonString());

            //Surfaced as SAM states it - by reason, with SAM's own message - and on the dwelling it names.
            string text = Modify.RefusalText(run.Refusals);
            Assert.Contains(Core.Query.Description(PartOMaterialisationRefusalReason.CoolingAirFlowOutsideGuidance), text);
            Assert.Contains(refusal.Message, text);

            PartOMixedDesignSession session = Session(baseline);
            session.SetRefusals(run.Refusals);
            PartOMixedDwellingRow flat03 = session.Rows.Single(x => x.Name == "Flat 03");
            Assert.Contains(refusal.Message, flat03.Attention);
            Assert.False(session.Readiness().CanBuild);

            //Turning cooling off answers it: the refusal leaves the edited dwelling.
            Assert.Null(session.SetCooling([flat03], false));
            Assert.False(flat03.NeedsAttention);
        }

        [Fact]
        public void AuthoredTransferFromACooledDwelling_IsRefusedBySam()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline();
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            Space from = adjacencyCluster.GetSpaces().Single(x => x.Name == "Flat 03 Bedroom");
            Space to = adjacencyCluster.GetSpaces().Single(x => x.Name == PartOMixedDesignFixture.Corridor);

            SpaceAirMovement spaceAirMovement = new("Flat 03 Bedroom authored", 0.01, new Core.ObjectReference(from).ToString(), new Core.ObjectReference(to).ToString());
            adjacencyCluster.AddObject(spaceAirMovement);
            adjacencyCluster.AddRelation(spaceAirMovement, from);
            adjacencyCluster.AddRelation(spaceAirMovement, to);
            AnalyticalModel transfer = Representative(new AnalyticalModel(baseline, adjacencyCluster));

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();
            Assert.Null(Build(transfer, izam, systems, out PartOStrategySetRun run));

            Assert.Contains(run.Refusals, x => x.Reason == PartOMaterialisationRefusalReason.AuthoredAirMovementConflict);
            Assert.Empty(izam.Models);
            Assert.Empty(systems.Materialisations);
        }

        [Fact]
        public void MixedSystemsCall_CoolsOnlyTheCooledUnit_AtSamsAirflow_AndLeavesNaturalAndCorridorOut()
        {
            AnalyticalModel baseline = Representative();
            PartOMaterialisation partOMaterialisation = baseline.MaterialisePartODwellingStrategies([Descriptor], null, [Template()]);
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            AdjacencyCluster adjacencyCluster = partOMaterialisation.AnalyticalModel.AdjacencyCluster;
            PartOCooledDwelling cooledDwelling = Assert.Single(partOMaterialisation.Record.CooledDwellings);

            //The scope: every room of the two MVHR dwellings, none of Flat 01 (natural) or the corridor.
            PartOMixedSystemsCall call = Query.PartOMixedSystemsCall(partOMaterialisation, [Template()]);
            Assert.True(call.IsValid, string.Join(" | ", call.Refusals));
            Assert.Equal(["Flat 02 Bathroom", "Flat 02 Bedroom", "Flat 03 Bathroom", "Flat 03 Bedroom"], call.Spaces.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal([cooledDwelling.AirHandlingUnitGuid], call.GuidanceSettings.Keys);

            //The real SAM_Systems call, checked against SAM's record before any TAS time is spent.
            MechanicalVentilationMaterialisation? mixed = Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [Template()], new PartOIteration3Pipeline(), out string? refusal);
            Assert.True(mixed is not null, refusal);

            MechanicalVentilationGuidanceCooling guidanceCooling = Assert.Single(mixed!.GuidanceCoolings);
            Assert.Equal(cooledDwelling.AirHandlingUnitGuid, guidanceCooling.Guid_AirHandlingUnit);
            Assert.Equal(cooledDwelling.CoolingOperatingAirFlow_Lps, guidanceCooling.ElevatedAirFlow_Lps, 9);

            //One plant room; a DX coil only on the cooled unit's air system; DisplacementVentilation false throughout.
            SystemPlantRoom systemPlantRoom = Assert.Single(mixed.SystemEnergyCentre.GetSystemPlantRooms());
            List<AirSystem> airSystems = systemPlantRoom.GetSystems<AirSystem>();
            Assert.Equal(2, airSystems.Count);
            foreach (AirSystem airSystem in airSystems)
            {
                int coils = (systemPlantRoom.GetSystemComponents<ISystemComponent>(airSystem) ?? []).Count(x => x is SystemDXCoil);
                Assert.Equal(airSystem.Guid == guidanceCooling.Guid_AirSystem ? 1 : 0, coils);
            }

            List<SystemSpace> systemSpaces = systemPlantRoom.GetSystemComponents<SystemSpace>() ?? [];
            Assert.NotEmpty(systemSpaces);
            Assert.All(systemSpaces, x => Assert.False(x.DisplacementVentilation));
        }

        [Fact]
        public void CoolingAirflow_FollowsTheProductTemplate_NotSamUi()
        {
            //The same design with the product's guidance default moved: the recorded airflow and the SAM_Systems unit both
            //move with it - SAM_UI carries no cooling figure of its own.
            foreach (double default_Lps in new[] { 80.0, 95.0 })
            {
                PartOMaterialisation partOMaterialisation = Representative().MaterialisePartODwellingStrategies([Descriptor], null, [Template(default_Lps: default_Lps)]);
                Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

                PartOCooledDwelling cooledDwelling = Assert.Single(partOMaterialisation.Record.CooledDwellings);
                double expected = Template(default_Lps: default_Lps).PartOCoolingOperatingAirFlow(cooledDwelling.DesignSupply_Lps, cooledDwelling.DesignExtract_Lps, out string? refusal);
                Assert.Null(refusal);
                Assert.Equal(expected, cooledDwelling.CoolingOperatingAirFlow_Lps, 9);

                PartOMixedSystemsCall call = Query.PartOMixedSystemsCall(partOMaterialisation, [Template(default_Lps: default_Lps)]);
                Assert.Equal(expected, Assert.Single(call.GuidanceSettings.Values).OperatingStrategy.ElevatedAirFlow_Lps, 9);
            }
        }

        [Fact]
        public void MixedSystemsMaterialisation_RefusesGuidanceThatDisagreesWithSamsRecord()
        {
            //Materialised with the product's guidance at 80 l/s, then simulated with a template that now says 95: the unit
            //would not run at the airflow SAM recorded, so no TAS time is spent on it.
            PartOMaterialisation partOMaterialisation = Representative().MaterialisePartODwellingStrategies([Descriptor], null, [Template()]);
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            Assert.Null(Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [Template(default_Lps: 95)], new PartOIteration3Pipeline(), out string? refusal));
            Assert.Contains("not at the", refusal);
            Assert.NotNull(Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, [Template()], new PartOIteration3Pipeline(), out refusal));
        }

        [Fact]
        public void MixedSystemsCall_RefusesWithoutTheCooledProductsTemplate_AndForAnIzamModel()
        {
            PartOMaterialisation cooled = Representative().MaterialisePartODwellingStrategies([Descriptor], null, [Template()]);
            Assert.True(cooled.IsMaterialised, cooled.Refusal);

            PartOMixedSystemsCall call = Query.PartOMixedSystemsCall(cooled, []);
            Assert.False(call.IsValid);
            Assert.Contains(call.Refusals, x => x.Contains("manufacturer guidance"));

            //Two templates for one product: ambiguous, never guessed.
            Assert.False(Query.PartOMixedSystemsCall(cooled, [Template(), Template(default_Lps: 90)]).IsValid);

            PartOMaterialisation uncooled = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), Product).MaterialisePartODwellingStrategies([Descriptor], null, [Template()]);
            Assert.True(uncooled.IsMaterialised, uncooled.Refusal);
            Assert.Contains("IZAM route", Assert.Single(Query.PartOMixedSystemsCall(uncooled, [Template()]).Refusals));
        }

        // =================================================================================================
        // Cooling removed: rebuilt from the clean baseline
        // =================================================================================================

        [Fact]
        public void CoolingRemoved_RebuildsFromTheCleanBaseline_WithNoCoolingLeft()
        {
            AnalyticalModel baseline_Clean = PartOMixedDesignFixture.Baseline();
            string json_Clean = baseline_Clean.ToJsonObject().ToJsonString();

            PartOMixedDesignFixture.FakeSimulator izam = new();
            FakeSystems systems = new();

            //Cooling on -> materialise -> run.
            AnalyticalModel baseline_Cooled = Representative(baseline_Clean);
            PartOMixedRunEvidence? cooled = Build(baseline_Cooled, izam, systems, out PartOStrategySetRun run_Cooled);
            Assert.NotNull(cooled);
            Assert.Equal(PartOSimulationRoute.Systems, cooled!.Route);

            //Cooling off in the session, saved, rebuilt.
            PartOMixedDesignSession session = Session(baseline_Cooled);
            session.ApplyFinal(cooled);
            Assert.True(session.FinalCurrent, session.FinalStale);
            PartOMixedDwellingRow flat03 = session.Rows.Single(x => x.Name == "Flat 03");
            Assert.Null(session.SetCooling([flat03], false));
            Assert.False(session.FinalCurrent);
            Assert.Equal("STALE", flat03.FinalText);

            AnalyticalModel baseline_Uncooled = session.WithSelection();
            PartOMixedRunEvidence? uncooled = Build(baseline_Uncooled, izam, systems, out PartOStrategySetRun run_Uncooled);
            Assert.NotNull(uncooled);

            //A fresh materialisation of the baseline on the IZAM route - the cooled model is never read or patched.
            Assert.Single(systems.Materialisations);
            AnalyticalModel model_Uncooled = Assert.Single(izam.Models);
            Assert.NotSame(run_Cooled.Materialisation!.AnalyticalModel, model_Uncooled);
            Assert.False(model_Uncooled.HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance));
            Assert.Equal(PartOSimulationRoute.Izam, uncooled!.Route);
            Assert.Empty(uncooled.Record.CooledDwellings);
            Assert.Equal(PartOIteration.BasePassive, Iteration(run_Uncooled.Materialisation!, flat03.ZoneGuid));
            Assert.DoesNotContain(run_Uncooled.Materialisation.OverheatingScenarios, x => x.Iteration == PartOIteration.ActiveTrimCooling);
            Assert.DoesNotContain("CooledDwellings", uncooled.Record.ToJsonObject().ToJsonString());

            //No generated cooling on the rebuilt model: no unit supply setpoint, and SAM_Systems has nothing to cool.
            Assert.All(model_Uncooled.AdjacencyCluster.GetObjects<AirHandlingUnit>() ?? [], x => Assert.True(double.IsNaN(x.SummerSupplyTemperature)));
            Assert.False(Query.PartOMixedSystemsCall(run_Uncooled.Materialisation, [Template()]).IsValid);

            //The cooled run is stale against the rebuilt design, SAM naming the strategy change; the uncooled one is current.
            Assert.False(cooled.IsCurrent(baseline_Uncooled, [Descriptor], [Template()], out string? reason));
            Assert.Contains("strategy", reason);
            Assert.True(uncooled.IsCurrent(baseline_Uncooled, [Descriptor], [Template()], out reason), reason);

            //The clean source baseline is unchanged throughout.
            Assert.Equal(json_Clean, baseline_Clean.ToJsonObject().ToJsonString());
        }

        [WpfFact]
        public void Window_OffersCoolingAsAConstraintAColumnAndABulkEdit()
        {
            PartOMixedDesignSession session = Session(Representative());

            PartOMixedDesignWindow window = new() { Session = session };
            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.True(((System.Windows.Controls.CheckBox)window.FindName("checkBox_Cooling")).IsEnabled);
                Assert.True(((System.Windows.Controls.CheckBox)window.FindName("checkBox_Cooling")).IsChecked);
                Assert.NotNull(window.FindName("button_CoolingOn"));
                Assert.NotNull(window.FindName("button_CoolingOff"));

                //A plain text column bound to the row's word - the grid stays virtualised, no per-row control.
                System.Windows.Controls.DataGridTextColumn column = (System.Windows.Controls.DataGridTextColumn)window.FindName("column_Cooling");
                Assert.Equal(nameof(PartOMixedDwellingRow.CoolingText), ((System.Windows.Data.Binding)column.Binding).Path.Path);
                Assert.True(window.Grid_Dwellings.EnableRowVirtualization);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
