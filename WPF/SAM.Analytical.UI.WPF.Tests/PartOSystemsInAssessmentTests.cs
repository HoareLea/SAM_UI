// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>PR-6: "Systems in this assessment" - SAM's scope, displayed.</b>
    ///
    /// <para>
    /// What is under test is a mapping and a wiring, not a rule. The rule (which systems an assessment includes, which it
    /// leaves out, when it refuses) is SAM's <c>Query.PartOSystemsMaterialisationScope</c>, tested in SAM. These tests prove
    /// SAM_UI shows exactly that answer - by identity, in the words an engineer sees - from the one preflight Check design
    /// and Build &amp; Run share, and that it decides nothing of its own.
    /// </para>
    /// <para>
    /// The fixture is the production shape of <see cref="PartOMixedSystemsScopeTests"/>: the <c>AddMechanicalSystems</c>
    /// scaffolding (<c>NV 1</c>, <c>UV 1</c>, an inert <c>MV 1</c> naming <c>AHU1</c>) beside one natural, one MVHR and one
    /// cooled MVHR dwelling. No TAS.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOSystemsInAssessmentTests
    {
        private static PartOMixedDesignCheck Check(AnalyticalModel baseline) => Modify.CheckPartOMixedDesign(baseline, [PartOMixedSystemsScopeTests.Descriptor], [PartOMixedSystemsScopeTests.Template()]);

        /// <summary>
        /// The baseline plus <paramref name="count"/> authored natural-ventilation systems that all carry the very same name.
        /// (The model's cluster is handed out as a copy, so the systems go in before it is materialised.)
        /// </summary>
        private static AnalyticalModel WithExtraSystems(AnalyticalModel baseline, int count, out List<Guid> guids)
        {
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            VentilationSystemType ventilationSystemType = new("NV", "NV");

            guids = [];
            for (int i = 0; i < count; i++)
            {
                VentilationSystem ventilationSystem = new("1", ventilationSystemType);
                Assert.True(adjacencyCluster.AddObject(ventilationSystem));
                guids.Add(ventilationSystem.Guid);
            }

            return new AnalyticalModel(baseline, adjacencyCluster);
        }

        /// <summary>Everything a person reads of the answer, as one comparable string.</summary>
        private static string Describe(PartOSystemsInAssessment partOSystemsInAssessment)
        {
            return string.Join("\n", [
                partOSystemsInAssessment.Route.ToString(),
                partOSystemsInAssessment.ScopeApplied.ToString(),
                partOSystemsInAssessment.Summary,
                partOSystemsInAssessment.Note,
                //Systems Part O builds are new objects on every materialisation, so the included ones are compared by what they say;\r\n                //the authored retained ones are the model\x27s own objects and are compared by identity too.\r\n                "I:" + string.Join("|", partOSystemsInAssessment.Included.Select(x => x.Text)),
                "R:" + string.Join("|", partOSystemsInAssessment.Retained.Select(x => x.Guid + "=" + x.Text)),
                "X:" + string.Join("|", partOSystemsInAssessment.Refusals),
            ]);
        }

        // =================================================================================================
        // Included, retained: by identity, in the words an engineer sees
        // =================================================================================================

        [Fact]
        public void Check_ShowsTheSystemsPartOBuiltAsIncluded_ByIdentity_AndNamesTheirUnitsAndDwellings()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded();
            string json_Baseline = baseline.ToJsonObject().ToJsonString();

            PartOMixedDesignCheck partOMixedDesignCheck = Check(baseline);
            Assert.True(partOMixedDesignCheck.Passed, partOMixedDesignCheck.Refusal_Systems);

            PartOSystemsInAssessment partOSystemsInAssessment = partOMixedDesignCheck.SystemsInAssessment!;
            Assert.NotNull(partOSystemsInAssessment);
            Assert.False(partOSystemsInAssessment.IsRefused);
            Assert.True(partOSystemsInAssessment.ScopeApplied);

            //Exactly SAM's identities: the systems Part O built in THIS check's materialisation, no other.
            Assert.Equal(partOMixedDesignCheck.Materialisation!.Record.VentilationSystemGuids.Values.OrderBy(x => x), partOSystemsInAssessment.Included.Select(x => x.Guid).OrderBy(x => x));

            //In the words an engineer sees: the unit Part O named for the dwelling, and the dwelling.
            Assert.Equal(["MVHR Flat 02", "MVHR Flat 03"], partOSystemsInAssessment.Included.Select(x => x.Name));
            Assert.Contains("dwelling Flat 02", partOSystemsInAssessment.Included[0].Detail);
            Assert.Contains("dwelling Flat 03", partOSystemsInAssessment.Included[1].Detail);

            Assert.Equal("2 included · 3 retained on the design, not assessed", partOSystemsInAssessment.Summary);

            //The design is read, never changed.
            Assert.Equal(json_Baseline, baseline.ToJsonObject().ToJsonString());
        }

        [Fact]
        public void Check_ShowsTheAuthoredSystemsSAMLeftOutAsRetained_ByIdentity_AndNothingIsRemoved()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded();
            PartOMaterialisation partOMaterialisation = PartOMixedSystemsScopeTests.Materialise(baseline);
            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);
            Assert.True(scope.IsScoped, scope.Refusal);

            PartOSystemsInAssessment partOSystemsInAssessment = Check(baseline).SystemsInAssessment!;

            //SAM's identities, and SAM's only: what the scope removed from the input is what is shown as retained.
            Assert.Equal(scope.Guids_Removed.OrderBy(x => x), partOSystemsInAssessment.Retained.Select(x => x.Guid).OrderBy(x => x));
            Assert.Equal(["MV 1", "NV 1", "UV 1"], partOSystemsInAssessment.Retained.Select(x => x.Name));

            //Nothing is deleted from the design: every authored system is still on the materialised model, and on the baseline.
            Assert.Equal(5, partOMaterialisation.AnalyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>().Count);
            Assert.Equal(3, baseline.AdjacencyCluster.GetObjects<VentilationSystem>().Count);
            Assert.All(partOSystemsInAssessment.Retained, x => Assert.NotNull(baseline.AdjacencyCluster.GetObject<VentilationSystem>(x.Guid)));

            Assert.Contains("nothing has been removed", partOSystemsInAssessment.Note);
        }

        /// <summary>
        /// The real model's shape: <c>MV 1</c> names <c>AHU1</c> and states no duty. It is shown as retained - with the unit it
        /// names, so the engineer sees what the plant is - and never as a participant.
        /// </summary>
        [Fact]
        public void TheInertLegacyMV1_NamingAHU1_IsRetainedAndNeverAParticipant()
        {
            PartOSystemsInAssessment partOSystemsInAssessment = Check(PartOMixedSystemsScopeTests.Scaffolded()).SystemsInAssessment!;

            PartOSystemsInAssessmentEntry mv1 = Assert.Single(partOSystemsInAssessment.Retained, x => x.Name == "MV 1");
            Assert.Contains("AHU1", mv1.Detail);
            Assert.Contains("no design terminal", mv1.Detail);

            Assert.DoesNotContain(partOSystemsInAssessment.Included, x => x.Guid == mv1.Guid);
            Assert.DoesNotContain(partOSystemsInAssessment.Included, x => x.Name.Contains("MV 1") || x.Text.Contains("AHU1"));
        }

        /// <summary>
        /// Identity decides, never the name. A template system given the familiar label of a Part O system is retained, and
        /// the one Part O built is included, whatever either is called - including many systems with one and the same name.
        /// </summary>
        [Fact]
        public void Identity_IsAuthoritative_WhenNamesCollide()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded(type_Plant: "MVHR");
            PartOMaterialisation partOMaterialisation = PartOMixedSystemsScopeTests.Materialise(baseline);

            VentilationSystem ventilationSystem_Template = partOMaterialisation.AnalyticalModel.AdjacencyCluster.GetObjects<VentilationSystem>().Single(x => x.FullName == "MVHR 1" && !partOMaterialisation.Record.VentilationSystemGuids.Values.Contains(x.Guid));

            PartOSystemsInAssessment partOSystemsInAssessment = Query.PartOSystemsInAssessment(partOMaterialisation, Query.PartOMixedSystemsScope(partOMaterialisation));

            Assert.Contains(partOSystemsInAssessment.Retained, x => x.Guid == ventilationSystem_Template.Guid && x.Name == "MVHR 1");
            Assert.DoesNotContain(partOSystemsInAssessment.Included, x => x.Guid == ventilationSystem_Template.Guid);
            Assert.Equal(partOMaterialisation.Record.VentilationSystemGuids.Values.OrderBy(x => x), partOSystemsInAssessment.Included.Select(x => x.Guid).OrderBy(x => x));

            //Many authored systems called the very same thing: each is its own entry, and none is merged or dropped.
            AnalyticalModel baseline_Collide = WithExtraSystems(baseline, 5, out List<Guid> guids_Extra);
            PartOMaterialisation partOMaterialisation_Collide = PartOMixedSystemsScopeTests.Materialise(baseline_Collide);

            PartOSystemsInAssessment partOSystemsInAssessment_Collide = Query.PartOSystemsInAssessment(partOMaterialisation_Collide, Query.PartOMixedSystemsScope(partOMaterialisation_Collide));
            Assert.All(guids_Extra, guid => Assert.Single(partOSystemsInAssessment_Collide.Retained, x => x.Guid == guid));
            //Six systems are called "NV 1" - the five added and the template\x27s own - and every one is its own entry.\r\n            Assert.Equal(guids_Extra.Count + 1, partOSystemsInAssessment_Collide.Retained.Select(x => x.Name).GroupBy(x => x).Max(x => x.Count()));
            Assert.Equal(partOSystemsInAssessment.Retained.Count + guids_Extra.Count, partOSystemsInAssessment_Collide.Retained.Count);
        }

        // =================================================================================================
        // A genuine refusal is still surfaced
        // =================================================================================================

        [Fact]
        public void ARefusal_IsShownAsARefusal_NamesNoSystem_AndIsSAMsText()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded(plantSupply_Lps: 30.0);

            PartOMixedDesignCheck partOMixedDesignCheck = Check(baseline);
            Assert.False(partOMixedDesignCheck.Passed);

            PartOSystemsInAssessment partOSystemsInAssessment = partOMixedDesignCheck.SystemsInAssessment!;

            Assert.True(partOSystemsInAssessment.IsRefused);

            //A refused scope names no system - nothing is claimed as included or retained.
            Assert.Empty(partOSystemsInAssessment.Included);
            Assert.Empty(partOSystemsInAssessment.Retained);

            Assert.Contains("'MV 1'", partOSystemsInAssessment.Refusals[0]);
            Assert.Contains("outside the assessed dwellings", partOSystemsInAssessment.Refusals[0]);
            Assert.Contains("30 l/s", partOSystemsInAssessment.Refusals[0]);

            //SAM's own sentences: the dialog's text is these, joined.
            Assert.Contains(partOSystemsInAssessment.Refusals[0], partOMixedDesignCheck.Refusal_Systems);
            Assert.StartsWith("refused", partOSystemsInAssessment.Summary);
        }

        // =================================================================================================
        // Check and Build & Run: one answer
        // =================================================================================================

        [Fact]
        public void CheckAndBuildAndRun_ExposeTheSameSystems()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded();

            PartOSystemsInAssessment partOSystemsInAssessment_Check = Check(baseline).SystemsInAssessment!;

            PartOStrategySetRun partOStrategySetRun = PartOMixedSystemsScopeTests.BuildToThePreflight(baseline, new PartOMixedDesignFixture.FakeSimulator());
            PartOSystemsInAssessment? partOSystemsInAssessment_Build = partOStrategySetRun.Simulation?.SystemsInAssessment;

            Assert.NotNull(partOSystemsInAssessment_Build);
            Assert.Equal(Describe(partOSystemsInAssessment_Check), Describe(partOSystemsInAssessment_Build!));
            Assert.Equal(2, partOSystemsInAssessment_Build!.Included.Count);
            Assert.Equal(3, partOSystemsInAssessment_Build.Retained.Count);
        }

        [Fact]
        public void CheckAndBuildAndRun_ExposeTheSameRefusal()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded(plantSupply_Lps: 30.0);

            PartOSystemsInAssessment partOSystemsInAssessment_Check = Check(baseline).SystemsInAssessment!;

            PartOStrategySetRun partOStrategySetRun = PartOMixedSystemsScopeTests.BuildToThePreflight(baseline, new PartOMixedDesignFixture.FakeSimulator());
            PartOSystemsInAssessment? partOSystemsInAssessment_Build = partOStrategySetRun.Simulation?.SystemsInAssessment;

            Assert.NotNull(partOSystemsInAssessment_Build);
            Assert.True(partOSystemsInAssessment_Build!.IsRefused);
            Assert.Equal(Describe(partOSystemsInAssessment_Check), Describe(partOSystemsInAssessment_Build));
        }

        /// <summary>The preflight's two overloads are one function: the new output changes nothing the old callers read.</summary>
        [Fact]
        public void ThePreflightsOutputs_AreUnchanged_ByTheAdditionalOutput()
        {
            PartOMaterialisation partOMaterialisation = PartOMixedSystemsScopeTests.Materialise(PartOMixedSystemsScopeTests.Scaffolded());
            List<VentilationUnitTemplate> templates = [PartOMixedSystemsScopeTests.Template()];

            Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, templates, new PartOIteration3Pipeline(), out string? refusal_Old, out List<string> notes_Old);
            Modify.PartOMixedSystemsMaterialisation(partOMaterialisation, templates, new PartOIteration3Pipeline(), out string? refusal_New, out List<string> notes_New, out PartOSystemsInAssessment partOSystemsInAssessment);

            Assert.Equal(refusal_Old, refusal_New);
            Assert.Equal(notes_Old, notes_New);
            Assert.NotNull(partOSystemsInAssessment);
        }

        // =================================================================================================
        // Empty and route cases
        // =================================================================================================

        [Fact]
        public void ASystemFreeCooledDesign_HasOnlyIncludedSystems()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), x => x.Name == "Flat 01" ? PartOMixedDesignFixture.Natural(x) : new PartODwellingStrategy(x.Guid, PartOVentilationMode.MVHR, PartOMixedCoolingTests.Reference, PartOActiveCooling.SupplyAirCooling));

            PartOSystemsInAssessment partOSystemsInAssessment = Check(baseline).SystemsInAssessment!;

            Assert.Equal(2, partOSystemsInAssessment.Included.Count);
            Assert.Empty(partOSystemsInAssessment.Retained);
            Assert.True(partOSystemsInAssessment.ScopeApplied);
            Assert.Equal("Every ventilation system on the design is one Part O built.", partOSystemsInAssessment.Note);
        }

        /// <summary>No dwelling cooled: the IZAM route, no Systems input, so no scope is taken - and none is claimed.</summary>
        [Fact]
        public void OnTheIzamRoute_NoScopeIsClaimed_AndTheSystemsPartOBuiltAreStillNamed()
        {
            PartOMixedDesignCheck partOMixedDesignCheck = Check(PartOMixedSystemsScopeTests.Scaffolded(cooled: false));
            Assert.False(partOMixedDesignCheck.SystemsChecked);

            PartOSystemsInAssessment partOSystemsInAssessment = partOMixedDesignCheck.SystemsInAssessment!;

            Assert.Equal(PartOSimulationRoute.Izam, partOSystemsInAssessment.Route);
            Assert.False(partOSystemsInAssessment.ScopeApplied);
            Assert.Equal(["MVHR Flat 02", "MVHR Flat 03"], partOSystemsInAssessment.Included.Select(x => x.Name));
            Assert.Empty(partOSystemsInAssessment.Retained);
            Assert.Equal("2 included", partOSystemsInAssessment.Summary);
            Assert.Contains("no Systems scope was taken", partOSystemsInAssessment.Note);
        }

        [Fact]
        public void ADesignWithNoMechanicalSystem_SaysSoPlainly()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);

            PartOSystemsInAssessment partOSystemsInAssessment = Check(baseline).SystemsInAssessment!;

            Assert.True(partOSystemsInAssessment.IsEmpty);
            Assert.Equal("no ventilation system is involved", partOSystemsInAssessment.Summary);
            Assert.Empty(partOSystemsInAssessment.Included);
            Assert.Empty(partOSystemsInAssessment.Retained);
        }

        [Fact]
        public void ADesignSAMRefusesToMaterialise_HasNoSystemsAnswer()
        {
            //No strategy selected at all: SAM refuses before there is a scope to show.
            PartOMixedDesignCheck partOMixedDesignCheck = Check(PartOMixedDesignFixture.Baseline());

            Assert.False(partOMixedDesignCheck.IsMaterialised);
            Assert.Null(partOMixedDesignCheck.SystemsInAssessment);
        }

        // =================================================================================================
        // The session: an answer is only ever shown for the design it was given for
        // =================================================================================================

        [Fact]
        public void TheSessionShowsAnAnswerOnlyWhileItDescribesTheDesignOnScreen()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded();
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);

            Assert.Null(partOMixedDesignSession.SystemsInAssessment);
            Assert.Null(partOMixedDesignSession.SystemsInAssessmentStale);

            partOMixedDesignSession.SetSystemsInAssessment(Check(partOMixedDesignSession.WithSelection()).SystemsInAssessment);
            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);
            Assert.Null(partOMixedDesignSession.SystemsInAssessmentStale);

            //Another selection is another question: the old answer is withdrawn, and says why.
            PartOMixedDwellingRow row = partOMixedDesignSession.Rows.Single(x => x.Name == "Flat 02");
            Assert.Null(partOMixedDesignSession.SetNatural([row]));

            Assert.Null(partOMixedDesignSession.SystemsInAssessment);
            Assert.Contains("changed", partOMixedDesignSession.SystemsInAssessmentStale);

            //Back to the selection SAM answered: the answer describes it again.
            Assert.Null(partOMixedDesignSession.SetMvhr([row], PartOMixedCoolingTests.Reference));
            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);

            //SAM refusing to materialise clears it.
            partOMixedDesignSession.SetSystemsInAssessment(null);
            Assert.Null(partOMixedDesignSession.SystemsInAssessment);
            Assert.Null(partOMixedDesignSession.SystemsInAssessmentStale);
        }

        /// <summary>The command's two hand-offs: Check design and Build &amp; Run each give the session SAM's answer for the design on screen.</summary>
        [Fact]
        public void TheCommandHandsTheSessionTheSameAnswer_FromCheckAndFromBuild_AndWithdrawsItWhenSAMRefusesToMaterialise()
        {
            AnalyticalModel baseline = PartOMixedSystemsScopeTests.Scaffolded();
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);

            //Check design.
            PartOMixedDesignCheck partOMixedDesignCheck = Check(partOMixedDesignSession.WithSelection());
            Modify.RecordSystemsInAssessment(partOMixedDesignSession, partOMixedDesignCheck);

            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);
            Assert.Equal(Describe(partOMixedDesignCheck.SystemsInAssessment!), Describe(partOMixedDesignSession.SystemsInAssessment!));

            //Build & Run: the same answer, from the run's own preflight.
            partOMixedDesignSession.SetSystemsInAssessment(null);
            PartOStrategySetRun partOStrategySetRun = PartOMixedSystemsScopeTests.BuildToThePreflight(partOMixedDesignSession.WithSelection(), new PartOMixedDesignFixture.FakeSimulator());
            Assert.True(partOStrategySetRun.IsMaterialised);

            Modify.RecordSystemsInAssessment(partOMixedDesignSession, partOStrategySetRun);

            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);
            Assert.Equal(Describe(partOMixedDesignCheck.SystemsInAssessment!), Describe(partOMixedDesignSession.SystemsInAssessment!));

            //A run cancelled before its preflight has no answer to give: the earlier one for this design stays.
            PartOStrategySetRun partOStrategySetRun_Cancelled = new() { Materialisation = partOStrategySetRun.Materialisation, Simulation = new PartOStrategySetSimulation { Cancelled = true } };
            Modify.RecordSystemsInAssessment(partOMixedDesignSession, partOStrategySetRun_Cancelled);
            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);

            //SAM refusing to materialise the design withdraws it - from Build and from Check alike.
            PartOStrategySetRun partOStrategySetRun_Refused = PartOMixedSystemsScopeTests.BuildToThePreflight(PartOMixedDesignFixture.Baseline(), new PartOMixedDesignFixture.FakeSimulator());
            Assert.False(partOStrategySetRun_Refused.IsMaterialised);
            Modify.RecordSystemsInAssessment(partOMixedDesignSession, partOStrategySetRun_Refused);
            Assert.Null(partOMixedDesignSession.SystemsInAssessment);

            Modify.RecordSystemsInAssessment(partOMixedDesignSession, partOMixedDesignCheck);
            Assert.NotNull(partOMixedDesignSession.SystemsInAssessment);
            Modify.RecordSystemsInAssessment(partOMixedDesignSession, Check(PartOMixedDesignFixture.Baseline()));
            Assert.Null(partOMixedDesignSession.SystemsInAssessment);
        }

        // =================================================================================================
        // The window: displays it, four states, bounded at scale
        // =================================================================================================

        private static PartOMixedDesignWindow Window(PartOMixedDesignSession partOMixedDesignSession)
        {
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            PartOMixedDesignWindow partOMixedDesignWindow = new() { Session = partOMixedDesignSession };
            partOMixedDesignWindow.Show();
            partOMixedDesignWindow.UpdateLayout();

            return partOMixedDesignWindow;
        }

        [WpfFact]
        public void Window_BeforeAnyCheck_SaysSAMHasNotBeenAskedYet_AndClaimsNoSystem()
        {
            PartOMixedDesignWindow partOMixedDesignWindow = Window(new PartOMixedDesignSession(PartOMixedSystemsScopeTests.Scaffolded(), null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]));

            try
            {
                Assert.Equal(" — not checked yet", partOMixedDesignWindow.SystemsSummaryText);
                Assert.Contains("Check design asks SAM", partOMixedDesignWindow.SystemsNoteText);
                Assert.False(partOMixedDesignWindow.SystemsListsVisible);
                Assert.Null(partOMixedDesignWindow.SystemsRefusalText);
                Assert.False(partOMixedDesignWindow.SystemsExpanded);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [WpfFact]
        public void Window_ShowsIncludedAndRetained_ForTheRealModelsShape()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedSystemsScopeTests.Scaffolded(), null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);
            partOMixedDesignSession.SetSystemsInAssessment(Check(partOMixedDesignSession.WithSelection()).SystemsInAssessment);

            PartOMixedDesignWindow partOMixedDesignWindow = Window(partOMixedDesignSession);

            try
            {
                Assert.Equal(" — 2 included · 3 retained on the design, not assessed", partOMixedDesignWindow.SystemsSummaryText);
                Assert.True(partOMixedDesignWindow.SystemsExpanded);
                Assert.True(partOMixedDesignWindow.SystemsListsVisible);

                Assert.Equal(2, partOMixedDesignWindow.SystemsIncludedItems.Count);
                Assert.StartsWith("MVHR Flat 02", partOMixedDesignWindow.SystemsIncludedItems[0]);
                Assert.StartsWith("MVHR Flat 03", partOMixedDesignWindow.SystemsIncludedItems[1]);

                Assert.Equal(3, partOMixedDesignWindow.SystemsRetainedItems.Count);
                Assert.StartsWith("MV 1", partOMixedDesignWindow.SystemsRetainedItems[0]);
                Assert.Contains("AHU1", partOMixedDesignWindow.SystemsRetainedItems[0]);
                Assert.StartsWith("NV 1", partOMixedDesignWindow.SystemsRetainedItems[1]);
                Assert.StartsWith("UV 1", partOMixedDesignWindow.SystemsRetainedItems[2]);

                Assert.Null(partOMixedDesignWindow.SystemsRefusalText);
                Assert.Contains("nothing has been removed", partOMixedDesignWindow.SystemsNoteText);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [WpfFact]
        public void Window_ShowsARefusalAsARefusal_NotAsAnEmptyScope()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedSystemsScopeTests.Scaffolded(plantSupply_Lps: 30.0), null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);
            partOMixedDesignSession.SetSystemsInAssessment(Check(partOMixedDesignSession.WithSelection()).SystemsInAssessment);

            PartOMixedDesignWindow partOMixedDesignWindow = Window(partOMixedDesignSession);

            try
            {
                Assert.StartsWith(" — refused", partOMixedDesignWindow.SystemsSummaryText);
                Assert.NotNull(partOMixedDesignWindow.SystemsRefusalText);
                Assert.Contains("'MV 1'", partOMixedDesignWindow.SystemsRefusalText);
                Assert.Contains("outside the assessed dwellings", partOMixedDesignWindow.SystemsRefusalText);

                //Not "0 included, 0 retained": no list is shown at all.
                Assert.False(partOMixedDesignWindow.SystemsListsVisible);
                Assert.True(partOMixedDesignWindow.SystemsExpanded);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [WpfFact]
        public void Window_AfterABuildInputChanges_SaysTheAnswerIsOutOfDate_AndShowsNoSystems()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedSystemsScopeTests.Scaffolded(), null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);
            partOMixedDesignSession.SetSystemsInAssessment(Check(partOMixedDesignSession.WithSelection()).SystemsInAssessment);

            //A build input changes - the catalogue is no longer offered. (Not a selection edit: a window closed with unsaved
            //edits asks whether to save them, and a test must not leave a dialog open.)
            partOMixedDesignSession.CatalogueOffered = false;
            Assert.Null(partOMixedDesignSession.SystemsInAssessment);

            PartOMixedDesignWindow partOMixedDesignWindow = Window(partOMixedDesignSession);

            try
            {
                Assert.Equal(" — out of date", partOMixedDesignWindow.SystemsSummaryText);
                Assert.Contains("changed since SAM last answered", partOMixedDesignWindow.SystemsNoteText);
                Assert.False(partOMixedDesignWindow.SystemsListsVisible);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [WpfFact]
        public void Window_WithNoSystemInvolved_RendersASensibleEmptyState()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.SetSystemsInAssessment(Check(partOMixedDesignSession.WithSelection()).SystemsInAssessment);

            PartOMixedDesignWindow partOMixedDesignWindow = Window(partOMixedDesignSession);

            try
            {
                Assert.Equal(" — no ventilation system is involved", partOMixedDesignWindow.SystemsSummaryText);
                Assert.False(partOMixedDesignWindow.SystemsListsVisible);
                Assert.Empty(partOMixedDesignWindow.SystemsIncludedItems);
                Assert.Empty(partOMixedDesignWindow.SystemsRetainedItems);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        /// <summary>
        /// A large project: five thousand retained systems are one bounded, virtualised list - a few dozen realised rows,
        /// never a control per system - and SAM's scope over a thousand same-named authored systems is built quickly.
        /// </summary>
        [WpfFact]
        public void Window_WithThousandsOfSystems_IsOneVirtualisedList()
        {
            List<PartOSystemsInAssessmentEntry> retained = [.. Enumerable.Range(0, 5000).Select(i => new PartOSystemsInAssessmentEntry(Guid.NewGuid(), "NV " + (i + 1), "no design terminal"))];
            List<PartOSystemsInAssessmentEntry> included = [.. Enumerable.Range(0, 300).Select(i => new PartOSystemsInAssessmentEntry(Guid.NewGuid(), "MVHR Flat " + (i + 1), "dwelling Flat " + (i + 1)))];

            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedSystemsScopeTests.Scaffolded(), null, [PartOMixedSystemsScopeTests.Descriptor], null, [PartOMixedSystemsScopeTests.Template()]);
            partOMixedDesignSession.SetSystemsInAssessment(new PartOSystemsInAssessment(PartOSimulationRoute.Systems, true, included, retained, null));

            PartOMixedDesignWindow partOMixedDesignWindow = Window(partOMixedDesignSession);

            try
            {
                Assert.Equal(" — 300 included · 5000 retained on the design, not assessed", partOMixedDesignWindow.SystemsSummaryText);
                Assert.Equal(5000, partOMixedDesignWindow.SystemsRetainedItems.Count);

                int realised = Realised(partOMixedDesignWindow.ListBox_SystemsRetained);
                Assert.True(realised > 0, "the list was not laid out, so this test would pass without testing anything");
                Assert.True(realised < 100, string.Format("{0} list items realised of 5000", realised));

                //Bounded: the list scrolls inside its own region and does not grow the window.
                Assert.True(partOMixedDesignWindow.ListBox_SystemsRetained.ActualHeight <= 130, partOMixedDesignWindow.ListBox_SystemsRetained.ActualHeight.ToString());
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [Fact]
        public void TheMapping_IsLinear_OverAThousandSameNamedAuthoredSystems()
        {
            const int count = 1000;
            PartOMaterialisation partOMaterialisation = PartOMixedSystemsScopeTests.Materialise(WithExtraSystems(PartOMixedSystemsScopeTests.Scaffolded(), count, out _));

            PartOSystemsMaterialisationScope scope = Query.PartOMixedSystemsScope(partOMaterialisation);
            Assert.True(scope.IsScoped, scope.Refusal);

            Stopwatch stopwatch = Stopwatch.StartNew();
            PartOSystemsInAssessment partOSystemsInAssessment = Query.PartOSystemsInAssessment(partOMaterialisation, scope);
            stopwatch.Stop();

            Assert.Equal(count + 3, partOSystemsInAssessment.Retained.Count);
            Assert.Equal(2, partOSystemsInAssessment.Included.Count);
            Assert.Equal(count + 3, partOSystemsInAssessment.Retained.Select(x => x.Guid).Distinct().Count());
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), stopwatch.Elapsed.ToString());
        }

        private static int Realised(DependencyObject dependencyObject)
        {
            int result = dependencyObject is ListBoxItem ? 1 : 0;

            int count = VisualTreeHelper.GetChildrenCount(dependencyObject);
            for (int i = 0; i < count; i++)
            {
                result += Realised(VisualTreeHelper.GetChild(dependencyObject, i));
            }

            return result;
        }
    }
}
