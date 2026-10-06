// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Geometry.Spatial;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Warm starting an Iteration 2B round from the run's canonical TBD.</b>
    /// <para>
    /// Between rounds only the ventilation state changes - the design airflow, the balanced duty and the
    /// network rebuilt from them. The geometry, zones, surfaces, apertures, constructions and the shading
    /// calculation are identical every round, and on the licensed acceptance model that conversion is 41.6 s
    /// of a 64.2 s round while the full-year simulation itself is 3.6 s. So the conversion is done once and
    /// every later round starts from it.
    /// </para>
    /// <para>
    /// <b>The danger is a stale conversion surviving a change it should not have.</b> These tests are
    /// therefore almost entirely about <see cref="PartOCanonicalTBD"/> refusing to be reused: what the
    /// fingerprint covers, what it deliberately does not, and every way the baseline can stop being
    /// trustworthy. A fallback is never a failure - the full conversion is always available and always
    /// authoritative - so what is asserted is that the fallback <i>happens</i> and says why.
    /// </para>
    /// <para>
    /// That a warm-started round produces the same engineering result as the full conversion needs a
    /// licensed TAS and a real model; it is proven by the A/B comparison in the acceptance evidence, not
    /// here.
    /// </para>
    /// </summary>
    public class PartOWarmStartTests
    {
        // ---- 6. A compatible model warm starts -----------------------------------------------------------

        /// <summary>
        /// <b>6.</b> The same model and the same TAS case: the baseline is adopted and stays valid.
        /// </summary>
        [Fact]
        public void ACompatibleModelAndCase_WarmStarts()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string refusal);

            Assert.NotNull(partOCanonicalTBD);
            Assert.Null(refusal);

            Assert.Equal(fixture.Path_TBD, partOCanonicalTBD.Path_TBD);

            //A DIFFERENT model object carrying the same building - which is the real case, since every round
            //hands over a fresh generation of the model rather than the same instance.
            Assert.True(partOCanonicalTBD.IsValidFor(fixture.Model(), fixture.Context(), out string refusal_Valid), refusal_Valid);
            Assert.Null(refusal_Valid);
        }

        /// <summary>
        /// <b>3, and the reason the whole optimisation is possible.</b> A changed <b>design airflow</b> -
        /// which is what a round writes and the only thing that differs between rounds - does <b>not</b>
        /// invalidate the baseline. Including it in the fingerprint would make every round incompatible and
        /// turn the warm start off entirely; the warm-started run re-applies that state instead.
        /// </summary>
        [Fact]
        public void AChangedDesignAirflow_DoesNotInvalidateTheBaseline()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.NotNull(partOCanonicalTBD);

            //The next round's design: the same building, moving more air.
            AnalyticalModel analyticalModel = fixture.Model(designFlowRate_Lps: 35);

            Assert.True(partOCanonicalTBD.IsValidFor(analyticalModel, fixture.Context(), out string refusal), refusal);
        }

        // ---- 7. Geometry and topology changes invalidate --------------------------------------------------

        /// <summary>
        /// <b>7.</b> A moved surface is a different building, and the conversion - including the shading
        /// calculation - depends on it. Invalidated, with the category named.
        /// </summary>
        [Fact]
        public void MovedGeometry_InvalidatesAndFallsBackToTheFullPath()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(moved: true), fixture.Context(), out string refusal));

            Assert.Contains("changed in a way the canonical TBD's conversion depends on", refusal);
            Assert.Contains("panels", refusal);
        }

        /// <summary>
        /// <b>7.</b> An added surface changes the panel count, which the report shows directly - so a reader
        /// can tell "a wall was added" from "a construction was renamed" without opening either model.
        /// </summary>
        [Fact]
        public void AnAddedSurface_InvalidatesAndTheCountsSaySo()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(panels: 3), fixture.Context(), out string refusal));

            Assert.Contains("panels=2", refusal);
            Assert.Contains("panels=3", refusal);
        }

        /// <summary>
        /// <b>7.</b> Zone topology - which spaces belong to which zone. TAS zones and the internal
        /// conditions written into them are derived from it, so a space moved between zones is not the same
        /// prepared TBD.
        /// </summary>
        [Fact]
        public void AChangedZoneTopology_Invalidates()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(zoneScope: false), fixture.Context(), out string refusal));

            Assert.Contains("zones", refusal);
        }

        /// <summary>
        /// A renamed space invalidates, even though its identity is unchanged - because TAS matches a zone
        /// to a space <b>by name</b>, so a rename really is a change the conversion depends on.
        /// </summary>
        [Fact]
        public void ARenamedSpace_Invalidates()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(renamed: true), fixture.Context(), out string refusal));

            Assert.Contains("spaces", refusal);
        }

        /// <summary>A changed construction is a changed TBD, however unchanged the geometry.</summary>
        [Fact]
        public void AChangedConstruction_Invalidates()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(construction: "Something Else"), fixture.Context(), out string refusal));

            Assert.Contains("panels", refusal);
        }

        /// <summary>An aperture added, removed or re-constructed changes the conversion and the shading.</summary>
        [Fact]
        public void AChangedAperture_Invalidates()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(apertures: false), fixture.Context(), out string refusal));

            Assert.Contains("apertures", refusal);
        }

        // ---- 8. Settings changes invalidate ---------------------------------------------------------------

        /// <summary>
        /// <b>8.</b> Every workflow setting that changes the prepared TBD invalidates the baseline. The
        /// weather one matters most: a warm start reuses the solar calculation the canonical carries, so
        /// reusing it under different weather would attribute one climate's results to another's.
        /// </summary>
        [Theory]
        [InlineData("weather")]
        [InlineData("solar")]
        [InlineData("from")]
        [InlineData("to")]
        [InlineData("sizing")]
        [InlineData("unmetHours")]
        [InlineData("useWidths")]
        [InlineData("updateConstructionLayers")]
        public void AChangedSimulationSetting_Invalidates(string setting)
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(), fixture.Context(setting), out string refusal), setting);

            Assert.Contains("case", refusal);
        }

        /// <summary>
        /// The <b>project name</b> is the one thing every round must change, and it decides which files a
        /// round writes rather than what is in them - so it must NOT invalidate. If it did, the warm start
        /// would never engage even once.
        /// </summary>
        [Fact]
        public void AChangedProjectName_DoesNotInvalidate()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            PartOSimulationContext partOSimulationContext = new(fixture.Directory, "Project-Opt07", fixture.WeatherData, SolarCalculationMethod.TAS, 1, 365)
            {
                Sizing = false,
                UnmetHours = false,
            };

            Assert.True(partOCanonicalTBD.IsValidFor(fixture.Model(), partOSimulationContext, out string refusal), refusal);
        }

        // ---- 9. A stale, missing or corrupt baseline falls back safely -------------------------------------

        /// <summary>
        /// <b>9.</b> A baseline rewritten underneath a running optimisation is no longer known to be the
        /// conversion of this model, so it is not reused on the strength of its path. This is why the check
        /// is repeated every round rather than made once at adoption.
        /// </summary>
        [Fact]
        public void ABaselineRewrittenUnderneathTheRun_IsNotReused()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.True(partOCanonicalTBD.IsValidFor(fixture.Model(), fixture.Context(), out string _));

            //Something else replaced it - another SAM session, a rerun from outside this window.
            File.WriteAllText(fixture.Path_TBD, "somebody else's conversion, of a different model");

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(), fixture.Context(), out string refusal));

            Assert.Contains("has been rewritten since this optimisation adopted it", refusal);
        }

        /// <summary><b>9.</b> A baseline that has gone falls back rather than failing the round.</summary>
        [Fact]
        public void AMissingBaseline_FallsBackWithItsReason()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            File.Delete(fixture.Path_TBD);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(), fixture.Context(), out string refusal));

            Assert.Contains("no longer on disk", refusal);
        }

        /// <summary><b>9.</b> An empty file is not a conversion, and is refused at adoption rather than at the copy.</summary>
        [Fact]
        public void AnEmptyBaseline_IsNotAdopted()
        {
            using Fixture fixture = new();

            File.WriteAllText(fixture.Path_TBD, string.Empty);

            Assert.Null(PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string refusal));

            Assert.Contains("is empty", refusal);
        }

        /// <summary><b>9.</b> A baseline that was never written - a run whose simulation produced no TBD.</summary>
        [Fact]
        public void ABaselineThatWasNeverWritten_IsNotAdopted()
        {
            using Fixture fixture = new();

            Assert.Null(PartOCanonicalTBD.Adopt(Path.Combine(fixture.Directory, "Never.tbd"), fixture.Model(), fixture.Context(), out string refusal));

            Assert.Contains("is not on disk", refusal);

            //And no path at all - the state a run that never simulated leaves behind.
            Assert.Null(PartOCanonicalTBD.Adopt(null, fixture.Model(), fixture.Context(), out string refusal_Null));

            Assert.Contains("No TBD path", refusal_Null);
        }

        /// <summary>A model or a case that cannot be fingerprinted at all is not adopted either.</summary>
        [Fact]
        public void AModelOrCaseThatCannotBeFingerprinted_IsNotAdopted()
        {
            using Fixture fixture = new();

            Assert.Null(PartOCanonicalTBD.Adopt(fixture.Path_TBD, null, fixture.Context(), out string refusal_Model));
            Assert.Contains("no adjacency cluster", refusal_Model);

            Assert.Null(PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), null, out string refusal_Case));
            Assert.Contains("not recorded", refusal_Case);
        }

        // ---- Determinism -----------------------------------------------------------------------------------

        /// <summary>
        /// The fingerprint is a function of the model, not of the order the cluster enumerated it in, and not
        /// of the process it was computed in - so a baseline adopted in one round and checked in another
        /// agrees with itself.
        /// </summary>
        [Fact]
        public void TheFingerprint_IsStableAndOrderIndependent()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD_First = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);
            PartOCanonicalTBD partOCanonicalTBD_Second = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(reversed: true), fixture.Context(), out string _);

            Assert.Equal(partOCanonicalTBD_First.Fingerprint, partOCanonicalTBD_Second.Fingerprint);

            //And it is not a randomized hash: the same content gives the same digest, which is what lets it
            //be compared at all.
            Assert.Equal(partOCanonicalTBD_First.Fingerprint, PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _).Fingerprint);
        }

        /// <summary>
        /// Coordinates are compared to a millimetre. A surface that round-trips through a serialization at
        /// the last bit is the same wall, and treating it as a different one would turn the warm start off
        /// for no engineering reason - while a millimetre is far below anything the conversion resolves.
        /// </summary>
        [Fact]
        public void FloatingPointNoiseBelowAMillimetre_DoesNotInvalidate()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(), fixture.Context(), out string _);

            Assert.True(partOCanonicalTBD.IsValidFor(fixture.Model(nudge: 0.00000001), fixture.Context(), out string refusal), refusal);

            //A centimetre is a real move, and is caught.
            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(nudge: 0.01), fixture.Context(), out string _));
        }

        // ---- The settings gate ----------------------------------------------------------------------------

        /// <summary>
        /// Warm starting is on by default and can be turned off, which is what keeps the full conversion
        /// available as the reference path. It is not one of the things that can make the settings
        /// unusable - it is a workflow switch, not an airflow.
        /// </summary>
        [Fact]
        public void TheWarmStart_IsOnByDefaultAndCanBeTurnedOff()
        {
            Assert.True(new PartOOptimisationSettings().WarmStart);

            Assert.True(new PartOOptimisationSettings { WarmStart = false }.IsValid(out string _));
        }

        /// <summary>
        /// Whether an iteration warm started is recorded <b>on that iteration</b>, so a run in which one
        /// round fell back is readable - and the count on the run is derived from the steps rather than
        /// tracked beside them, so the two cannot disagree.
        /// </summary>
        [Fact]
        public void WhetherAnIterationWarmStarted_IsRecordedOnTheIteration()
        {
            PartOOptimisationRun partOOptimisationRun = new(new PartOOptimisationSettings());

            partOOptimisationRun.Steps.Add(new PartOOptimisationStep(0));
            partOOptimisationRun.Steps.Add(new PartOOptimisationStep(1) { WarmStarted = true });
            partOOptimisationRun.Steps.Add(new PartOOptimisationStep(2) { WarmStarted = false });
            partOOptimisationRun.Steps.Add(new PartOOptimisationStep(3) { WarmStarted = true });

            Assert.Equal(2, partOOptimisationRun.WarmStarted);

            //The baseline never warm starts - it is the conversion the others start from.
            Assert.False(partOOptimisationRun.Step_Baseline.WarmStarted);
        }

        // ---- Fixture ---------------------------------------------------------------------------------------

        /// <summary>
        /// A temporary directory holding a stand-in canonical TBD, plus a model and a TAS case that can each
        /// be varied one dimension at a time.
        /// <para>
        /// The TBD's <i>content</i> is never read by <see cref="PartOCanonicalTBD"/> - only its existence,
        /// length and write time - so a text file stands in for one perfectly, and no licensed TAS is
        /// needed to test what makes a baseline trustworthy.
        /// </para>
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            public Fixture()
            {
                Directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOWarmStartTests_{0}", Guid.NewGuid()));

                System.IO.Directory.CreateDirectory(Directory);

                Path_TBD = Path.Combine(Directory, "Project.tbd");

                File.WriteAllText(Path_TBD, "a converted model");

                WeatherData = new WeatherData("Z1_DSY1_2050s_HIGH90_CIBSE_v1.1", "Fixture", 51.5, -0.1, 25);
            }

            public string Directory { get; }

            public string Path_TBD { get; }

            public WeatherData WeatherData { get; }

            /// <summary>
            /// Two rooms in one zone, on two panels, one of which carries an aperture - the smallest thing
            /// that has every category the fingerprint covers.
            /// </summary>
            public AnalyticalModel Model(double designFlowRate_Lps = 30, bool moved = false, int panels = 2, bool zoneScope = true, bool renamed = false, string construction = "Fixture Wall", bool apertures = true, bool reversed = false, double nudge = 0)
            {
                AdjacencyCluster adjacencyCluster = new();

                Zone zone = new(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "Flat 1");

                adjacencyCluster.AddObject(zone);

                List<Space> spaces =
                [
                    new Space(new Guid("bbbbbbbb-0000-0000-0000-000000000001"), renamed ? "Bedroom renamed" : "Bedroom", null),
                    new Space(new Guid("bbbbbbbb-0000-0000-0000-000000000002"), "Kitchen", null),
                ];

                if (reversed)
                {
                    spaces.Reverse();
                }

                foreach (Space space in spaces)
                {
                    adjacencyCluster.AddObject(space);

                    //zoneScope false moves the kitchen out of the dwelling zone - a topology change.
                    if (zoneScope || space.Name == "Bedroom")
                    {
                        adjacencyCluster.AddRelation(zone, space);
                    }
                }

                VentilationTerminal ventilationTerminal = new("Bedroom terminal", FlowClassification.Supply, designFlowRate_Lps);

                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, spaces.Find(x => x.Name.StartsWith("Bedroom")));

                for (int i = 0; i < panels; i++)
                {
                    double x = (moved ? 10 : 0) + (i * 5) + nudge;

                    Face3D face3D = new(new Polygon3D(
                    [
                        new Point3D(x, 0, 0),
                        new Point3D(x + 4, 0, 0),
                        new Point3D(x + 4, 0, 3),
                        new Point3D(x, 0, 3),
                    ]));

                    //A stated guid, so two calls to Model() really are the same building rather than two
                    //buildings that happen to look alike - which is what every "does not invalidate" test
                    //below rests on.
                    Panel panel = SAM.Analytical.Create.Panel(
                        new Guid(string.Format("cccccccc-0000-0000-0000-00000000000{0}", i + 1)),
                        SAM.Analytical.Create.Panel(new Construction(construction), PanelType.Wall, face3D),
                        face3D);

                    if (apertures && i == 0)
                    {
                        Aperture aperture = new(new ApertureConstruction("Fixture Window", ApertureType.Window), new Polygon3D(
                        [
                            new Point3D(x + 1, 0, 1),
                            new Point3D(x + 2, 0, 1),
                            new Point3D(x + 2, 0, 2),
                            new Point3D(x + 1, 0, 2),
                        ]));

                        panel.AddAperture(new Aperture(new Guid("dddddddd-0000-0000-0000-000000000001"), aperture));
                    }

                    adjacencyCluster.AddObject(panel);
                }

                return new AnalyticalModel("Project", null, null, null, adjacencyCluster, null, null);
            }

            /// <summary>The TAS case, with one dimension of it changed where a test names one.</summary>
            public PartOSimulationContext Context(string changed = null)
            {
                PartOSimulationContext result = new(
                    Directory,
                    "Project",
                    changed == "weather" ? new WeatherData("Z1_DSY1_2080s_HIGH90_CIBSE_v1.1", "Fixture", 51.5, -0.1, 25) : WeatherData,
                    changed == "solar" ? SolarCalculationMethod.SAM : SolarCalculationMethod.TAS,
                    changed == "from" ? 2 : 1,
                    changed == "to" ? 364 : 365)
                {
                    Sizing = changed == "sizing",
                    UnmetHours = changed == "unmetHours",
                    UseWidths = changed == "useWidths",
                    UpdateConstructionLayersByPanelType = changed != "updateConstructionLayers",
                };

                return result;
            }

            public void Dispose()
            {
                try
                {
                    System.IO.Directory.Delete(Directory, true);
                }
                catch (IOException)
                {
                    //A temp directory left behind is not a test failure.
                }
            }
        }
    }
}
