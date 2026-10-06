// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Guided Part O refuses to simulate a model whose fabric is undefined.</b>
    ///
    /// <para><b>The workflow this sits in</b></para>
    /// <para>
    /// <c>original model -&gt; guided Part O preparation -&gt; UpdateConstructionLayersByPanelType gap-fill
    /// -&gt; THIS validation -&gt; TAS conversion -&gt; simulation</c>. The validation is part of
    /// <see cref="PartOPreSimulationCheck"/>, which <c>Modify.RunPartOSimulation</c> already builds after
    /// the gap-fill and before the first byte of any file is written - so what is judged is the model TAS
    /// will actually be given, with the library having had its chance to resolve the gaps first.
    /// </para>
    ///
    /// <para><b>Why a refusal rather than a repair</b></para>
    /// <para>
    /// Because TAS cannot represent fabric that is not there, and does not say so. A panel with no
    /// construction layers converts to a TBD construction with no materials, which
    /// <c>Query.Adiabatic</c> reports as adiabatic in its own right, so the wall is simulated as an
    /// adiabatic boundary; an aperture with no pane layers has a pane thickness of zero, and the conversion
    /// builds a pane surface only above zero thickness, so the opening is not in the TBD at all. Both change
    /// the thermal case invisibly. Inventing a default instead would change it too, just differently, so
    /// neither is done: the run stops and names what a person has to fix.
    /// </para>
    /// <para>
    /// <c>Create.Log</c> already reports these states, as WARNINGS - which is right for the Check command
    /// and useless as a gate, because warnings do not stop a run. The severity is raised here, on the Part O
    /// path only.
    /// </para>
    /// </summary>
    public class PartOFabricValidationTests
    {
        // ---- A. Established fabric passes --------------------------------------------------------------

        /// <summary>A layered panel carrying a layered door is simulated, and nothing is said about it.</summary>
        [Fact]
        public void LayeredPanelAndLayeredAperture_AreAccepted()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = Gate(Model(layered: true, paneLayers: true));

            Assert.True(partOPreSimulationCheck.IsValid);
            Assert.Null(partOPreSimulationCheck.Refusal());
        }

        // ---- B. The gap-fill gets its chance first ------------------------------------------------------

        /// <summary>
        /// Fabric that starts missing and is <b>resolved by the gap-fill</b> passes - which is the whole
        /// reason the validation runs after it rather than before.
        /// <para>
        /// The libraries here cover every panel and aperture type in the fixture, so this is the ordinary
        /// case on a machine with a configured library: nothing is unresolved by the time the gate looks.
        /// </para>
        /// </summary>
        [Fact]
        public void FabricResolvedByTheGapFill_IsAccepted()
        {
            AnalyticalModel analyticalModel = Model(layered: false, paneLayers: false);

            //The premise: before the fill, this model would be refused.
            Assert.False(Gate(analyticalModel).IsValid);

            AnalyticalModel analyticalModel_Filled = analyticalModel.UpdateConstructionLayersByPanelType(
                ConstructionLibrary(), ApertureConstructionLibrary(), Materials());

            PartOPreSimulationCheck partOPreSimulationCheck = Gate(analyticalModel_Filled);

            Assert.True(partOPreSimulationCheck.IsValid);
            Assert.Null(partOPreSimulationCheck.Refusal());
        }

        // ---- C, D, E. What nothing could resolve -------------------------------------------------------

        /// <summary>
        /// A panel still without construction layers after the gap-fill stops the run, and is named.
        /// <para>
        /// Named by what a person can act on - the panel's own name, its panel type, its construction's name
        /// and its Guid - rather than by a generic "invalid model".
        /// </para>
        /// </summary>
        [Fact]
        public void UnresolvedPanel_RefusesAndNamesIt()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = Gate(Model(layered: false, paneLayers: true));

            Assert.False(partOPreSimulationCheck.IsValid);

            string refusal = partOPreSimulationCheck.Refusal();

            Assert.Contains("Fixture Wall Internal", refusal, StringComparison.Ordinal);
            Assert.Contains("no construction layers", refusal, StringComparison.Ordinal);

            //And it says what to do about it, rather than only that something is wrong.
            Assert.Contains("UNRESOLVED", refusal, StringComparison.Ordinal);
            Assert.Contains("construction library", refusal, StringComparison.Ordinal);
        }

        /// <summary>
        /// An ordinary internal door still without pane layers after the gap-fill stops the run, and is
        /// named - the case that would otherwise have vanished from the TBD without a word.
        /// </summary>
        [Fact]
        public void UnresolvedAperture_RefusesAndNamesIt()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = Gate(Model(layered: true, paneLayers: false));

            Assert.False(partOPreSimulationCheck.IsValid);

            string refusal = partOPreSimulationCheck.Refusal();

            Assert.Contains("Door Studio Bathroom", refusal, StringComparison.Ordinal);
            Assert.Contains("no pane construction layers", refusal, StringComparison.Ordinal);
            Assert.Contains("Door", refusal, StringComparison.Ordinal);
        }

        /// <summary>
        /// Every unresolved element is listed, not the first one - a person fixing a model needs the whole
        /// list, or they discover the next one on the next run.
        /// </summary>
        [Fact]
        public void SeveralUnresolvedElements_AreAllListed()
        {
            PartOPreSimulationCheck partOPreSimulationCheck = Gate(Model(layered: false, paneLayers: false));

            Assert.False(partOPreSimulationCheck.IsValid);

            string refusal = partOPreSimulationCheck.Refusal();

            //Both panels and the door.
            Assert.Contains("Fixture Wall Internal", refusal, StringComparison.Ordinal);
            Assert.Contains("Fixture Wall External", refusal, StringComparison.Ordinal);
            Assert.Contains("Door Studio Bathroom", refusal, StringComparison.Ordinal);

            Assert.True(partOPreSimulationCheck.Errors.Count >= 3);
        }

        // ---- What is deliberately NOT unresolved --------------------------------------------------------

        /// <summary>
        /// An air boundary and a shade carry no fabric by design, and the conversion builds no construction
        /// for them. They are not unresolved, and a run is not refused for them - the same two
        /// <c>Create.Log</c> excludes.
        /// </summary>
        [Fact]
        public void AirAndShadePanels_AreNotUnresolvedFabric()
        {
            AdjacencyCluster adjacencyCluster = Cluster(layered: true, paneLayers: true);

            adjacencyCluster.AddObject(SAM.Analytical.Create.Panel(new Construction("Fixture Air"), PanelType.Air, Face(20)));
            adjacencyCluster.AddObject(SAM.Analytical.Create.Panel(new Construction("Fixture Shade"), PanelType.Shade, Face(25)));

            PartOPreSimulationCheck partOPreSimulationCheck = Gate(ModelFrom(adjacencyCluster));

            Assert.True(partOPreSimulationCheck.IsValid);
        }

        // ---- F. Manual Simulate is untouched -----------------------------------------------------------

        /// <summary>
        /// <b>The ordinary Simulate command is not gated at all</b>, so it cannot be refused by this rule.
        /// A long-standing model that simulates today must not stop simulating because the Part O path grew
        /// a fabric rule.
        /// </summary>
        [Fact]
        public void ManualSimulate_IsNotGatedByTheFabricRule()
        {
            AnalyticalModel analyticalModel = Model(layered: false, paneLayers: false);

            //No run at all - the ordinary Simulate command.
            Assert.Null(PartOPreSimulationCheck.Gate(null, analyticalModel));

            //And a run that has not been prepared is not a Part O simulation either.
            Assert.Null(PartOPreSimulationCheck.Gate(new PartORun(), analyticalModel));
        }

        /// <summary>
        /// And the rule is the Part O path's alone: <c>Create.Log</c> still reports the same states as
        /// warnings, so the Check command and every other caller see exactly what they saw before.
        /// </summary>
        [Fact]
        public void TheSharedCheckAuthority_StillReportsThemAsWarnings()
        {
            Log log = SAM.Analytical.Create.Log(Model(layered: false, paneLayers: false));

            Assert.NotNull(log);

            bool any_Warning = false;
            foreach (LogRecord logRecord in log)
            {
                if (logRecord?.LogRecordType == LogRecordType.Warning && logRecord.Text.Contains("has no ConstructionLayers", StringComparison.Ordinal))
                {
                    any_Warning = true;
                }
            }

            Assert.True(any_Warning);
        }

        // ---- The model is not touched -------------------------------------------------------------------

        /// <summary>Validating reads the model and writes nothing to it.</summary>
        [Fact]
        public void Validation_DoesNotMutateTheModel()
        {
            AnalyticalModel analyticalModel = Model(layered: false, paneLayers: false);

            string before = Fabric(analyticalModel);

            Gate(analyticalModel);
            Gate(analyticalModel);

            Assert.Equal(before, Fabric(analyticalModel));
        }

        // ---- Helpers ------------------------------------------------------------------------------------

        /// <summary>The gate as <c>RunPartOSimulation</c> builds it, over a prepared Part O run.</summary>
        private static PartOPreSimulationCheck Gate(AnalyticalModel analyticalModel)
        {
            PartORun partORun = new();

            Assert.True(partORun.Prepare(analyticalModel, Scenarios()));
            Assert.Equal(PartORunState.Prepared, partORun.State);

            PartOPreSimulationCheck result = PartOPreSimulationCheck.Gate(partORun, analyticalModel);

            Assert.NotNull(result);

            return result;
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(SAM.Analytical.Enums.PartOAssessmentScope.Dwelling, Guid.NewGuid(), SAM.Analytical.Enums.PartOIteration.BasePassive)];
        }

        private static string Fabric(AnalyticalModel analyticalModel)
        {
            List<string> result = [];
            foreach (Panel panel in analyticalModel.AdjacencyCluster.GetPanels() ?? [])
            {
                string apertures = string.Empty;
                foreach (Aperture aperture in panel.Apertures ?? [])
                {
                    apertures += string.Format(";{0}/{1}", aperture.Guid, aperture.ApertureConstruction?.PaneConstructionLayers?.Count ?? -1);
                }

                result.Add(string.Format(
                    "{0}|{1}|{2}|{3}{4}",
                    panel.Guid,
                    panel.PanelType,
                    panel.Construction?.Name,
                    panel.Construction?.ConstructionLayers?.Count ?? -1,
                    apertures));
            }

            result.Sort(StringComparer.Ordinal);

            return string.Join("\n", result);
        }

        /// <summary>A library covering every panel type the fixture uses.</summary>
        private static ConstructionLibrary ConstructionLibrary()
        {
            ConstructionLibrary result = new("Fixture");

            foreach (PanelType panelType in new[] { PanelType.WallInternal, PanelType.WallExternal })
            {
                Construction construction = new(Guid.NewGuid(), "Library " + panelType.ToString(), [new ConstructionLayer("Library Block", 0.15)]);

                //How ConstructionLibrary.GetConstructions(PanelType) matches.
                construction.SetValue(ConstructionParameter.DefaultPanelType, panelType.ToString());

                result.Add(construction);
            }

            return result;
        }

        /// <summary>A library covering the fixture's door.</summary>
        private static ApertureConstructionLibrary ApertureConstructionLibrary()
        {
            ApertureConstructionLibrary result = new("Fixture");

            ApertureConstruction apertureConstruction = new(
                Guid.NewGuid(),
                "Library Door",
                ApertureType.Door,
                [new ConstructionLayer("Library Timber", 0.044)],
                [new ConstructionLayer("Library Timber", 0.044)]);

            apertureConstruction.SetValue(ApertureConstructionParameter.DefaultPanelType, PanelType.WallInternal.ToString());

            result.Add(apertureConstruction);

            return result;
        }

        private static MaterialLibrary Materials()
        {
            MaterialLibrary result = new("Fixture");

            result.Add(new OpaqueMaterial(Guid.NewGuid(), "Library Block", "Library Block", "Fixture", 0.5, 1000, 1000));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), "Library Timber", "Library Timber", "Fixture", 0.14, 500, 1600));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), "Concrete", "Concrete", "Fixture", 2.3, 2300, 1000));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), "Timber", "Timber", "Fixture", 0.14, 500, 1600));

            return result;
        }

        private static Face3D Face(double x)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x, 0, 0),
                new Point3D(x + 4, 0, 0),
                new Point3D(x + 4, 0, 3),
                new Point3D(x, 0, 3),
            ]));
        }

        private static AnalyticalModel ModelFrom(AdjacencyCluster adjacencyCluster)
        {
            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, Materials(), new ProfileLibrary("Profiles"));
        }

        private static AnalyticalModel Model(bool layered, bool paneLayers)
        {
            return ModelFrom(Cluster(layered, paneLayers));
        }

        /// <summary>
        /// One internal partition carrying a door, and one external wall - the smallest thing that carries
        /// both a panel construction and an aperture construction, in both states.
        /// </summary>
        private static AdjacencyCluster Cluster(bool layered, bool paneLayers)
        {
            AdjacencyCluster result = new();

            Space space = new("Studio");

            //Create.Log reports a space with no area as an Error in its own right, and this fixture is about
            //fabric - so the space is a valid one and the only errors are the ones under test.
            space.SetValue(SpaceParameter.Area, 25.0);
            space.SetValue(SpaceParameter.Volume, 62.5);

            result.AddObject(space);

            Construction construction_Internal = layered
                ? new Construction(Guid.NewGuid(), "Fixture Wall Internal", [new ConstructionLayer("Concrete", 0.2)])
                : new Construction(Guid.NewGuid(), "Fixture Wall Internal", []);

            Construction construction_External = layered
                ? new Construction(Guid.NewGuid(), "Fixture Wall External", [new ConstructionLayer("Concrete", 0.2)])
                : new Construction(Guid.NewGuid(), "Fixture Wall External", []);

            construction_Internal.SetValue(ConstructionParameter.DefaultPanelType, PanelType.WallInternal.ToString());
            construction_External.SetValue(ConstructionParameter.DefaultPanelType, PanelType.WallExternal.ToString());

            Panel panel_Internal = SAM.Analytical.Create.Panel(construction_Internal, PanelType.WallInternal, Face(0));
            Panel panel_External = SAM.Analytical.Create.Panel(construction_External, PanelType.WallExternal, Face(10));

            ApertureConstruction apertureConstruction = paneLayers
                ? new ApertureConstruction(Guid.NewGuid(), "Door Studio Bathroom", ApertureType.Door, [new ConstructionLayer("Timber", 0.044)], [new ConstructionLayer("Timber", 0.044)])
                : new ApertureConstruction(Guid.NewGuid(), "Door Studio Bathroom", ApertureType.Door, [], []);

            apertureConstruction.SetValue(ApertureConstructionParameter.DefaultPanelType, PanelType.WallInternal.ToString());

            panel_Internal.AddAperture(SAM.Analytical.Create.Aperture(apertureConstruction, Door(0)));

            result.AddObject(panel_Internal);
            result.AddObject(panel_External);

            result.AddRelation(space, panel_Internal);
            result.AddRelation(space, panel_External);

            return result;
        }

        private static Face3D Door(double x)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x + 1, 0, 0),
                new Point3D(x + 1.9, 0, 0),
                new Point3D(x + 1.9, 0, 2),
                new Point3D(x + 1, 0, 2),
            ]));
        }
    }
}
