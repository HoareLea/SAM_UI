// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using SAM.Core;
using SAM.Geometry.Spatial;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The <b>scope</b> half of running selected dwellings in isolation: what the dialog offers, what the
    /// run is called so its evidence cannot land on another run's, what the warm start may and may not
    /// reuse, and what a reopened isolated result still knows about itself.
    /// <para>
    /// The extraction itself - which spaces survive, which panels become the adiabatic cut, which excluded
    /// geometry becomes shade, which plant is refused - is SAM's, and is pinned in
    /// <c>SAM.Tests.PartOIsolationTests</c>. Nothing here restates it.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOIsolationScopeTests
    {
        // ---- The dialog --------------------------------------------------------------------------------

        /// <summary>
        /// Isolation is <b>off</b> unless somebody asks for it. The whole-building simulation is the
        /// reference case; trading it for speed is a deliberate act, never a default.
        /// </summary>
        [WpfFact]
        public void Isolation_IsOffByDefault()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = Zones(),
            };

            Assert.False(partOIterationWindow.Isolate);
        }

        /// <summary>
        /// The dialog states the engineering assumption where the choice is made - that interfaces to
        /// excluded spaces become adiabatic, that results may therefore differ from a whole-building run,
        /// and that none of this moves the Part O or Part F answers. A person cannot accept an assumption
        /// they are only told about afterwards.
        /// </summary>
        [WpfFact]
        public void Isolation_StatesTheAssumptionAndItsLimits()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = Zones(),
            };

            string text = partOIterationWindow.IsolationDescription;

            Assert.Contains("adiabatic", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("shading context", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("may differ", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Part F", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// With every dwelling selected the tick is still <b>offered</b> - a large building is not only its
        /// dwellings, and excluding corridors, cores and plant is a real reduction - but the dialog says
        /// where the remaining saving would come from rather than implying a large one.
        /// </summary>
        [WpfFact]
        public void Isolation_IsOfferedWithACaveatWhenEveryDwellingIsSelected()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = Zones(),
            };

            Assert.True(partOIterationWindow.DwellingSelection.SelectedCount == partOIterationWindow.DwellingSelection.Count);

            Assert.Contains("not dwellings", partOIterationWindow.IsolationDescription, StringComparison.OrdinalIgnoreCase);
        }

        // ---- H. Artifact naming ------------------------------------------------------------------------

        /// <summary>
        /// <b>29.</b> A full run and an isolated run of one building derive different project names, so
        /// their TBD, TSD, <c>.sam</c> and TM59 report cannot overwrite one another.
        /// </summary>
        [Fact]
        public void FullAndIsolatedRun_DoNotCollide()
        {
            string token = PartOIsolationContext.Token([Guid.NewGuid()]);

            Assert.NotEqual("Project", Query.ProjectName_Isolated("Project", token));
        }

        /// <summary>
        /// <b>30.</b> Two different isolated selections derive different names - by identity, so two
        /// dwellings that share a display name still produce two sets of evidence.
        /// </summary>
        [Fact]
        public void TwoIsolatedScopes_DoNotCollide()
        {
            Guid guid_1 = Guid.NewGuid();
            Guid guid_2 = Guid.NewGuid();

            string name_1 = Query.ProjectName_Isolated("Project", PartOIsolationContext.Token([guid_1]));
            string name_2 = Query.ProjectName_Isolated("Project", PartOIsolationContext.Token([guid_2]));

            Assert.NotEqual(name_1, name_2);
        }

        /// <summary>
        /// Every Iteration 2B round re-prepares an already isolated model. The suffix is applied once - a
        /// name that grew one per round would put each round's evidence somewhere new and break the
        /// per-iteration naming the optimisation depends on.
        /// </summary>
        [Fact]
        public void IsolatedProjectName_IsNotAppliedTwice()
        {
            string token = PartOIsolationContext.Token([Guid.NewGuid()]);

            string name = Query.ProjectName_Isolated("Project", token);

            Assert.Equal(name, Query.ProjectName_Isolated(name, token));
        }

        /// <summary>A run with no isolation keeps its name exactly.</summary>
        [Fact]
        public void AFullRunsProjectName_IsUntouched()
        {
            Assert.Equal("Project", Query.ProjectName_Isolated("Project", null));
            Assert.Equal("Project", Query.ProjectName_Isolated("Project", string.Empty));
        }

        // ---- F. Warm start -----------------------------------------------------------------------------

        /// <summary>
        /// <b>21.</b> An isolated baseline may be reused by its own later rounds: the geometry and the
        /// shading scope do not change between a run's own airflow rounds, which is the whole premise of the
        /// warm start and is as true of an isolated run as of a full one.
        /// </summary>
        [Fact]
        public void AnIsolatedBaseline_IsReusedByItsOwnRound()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(isolated: true), fixture.Context(), out string refusal);

            Assert.NotNull(partOCanonicalTBD);
            Assert.Null(refusal);

            //The next round: the same isolated building, moving more air. Design airflow is deliberately not
            //in the fingerprint, so this is still the same conversion.
            Assert.True(
                partOCanonicalTBD.IsValidFor(fixture.Model(isolated: true, designFlowRate_Lps: 35), fixture.Context(), out string refusal_Valid),
                refusal_Valid);
        }

        /// <summary>
        /// <b>22.</b> A canonical TBD converted from the WHOLE building can never be reused by an isolated
        /// run. They are different geometries - different spaces, different surfaces, and a cut that exists
        /// in one and not the other - and reusing one for the other would simulate the wrong building.
        /// </summary>
        [Fact]
        public void AFullBuildingBaseline_IsNotReusedByAnIsolatedRun()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(isolated: false), fixture.Context(), out string _);

            Assert.NotNull(partOCanonicalTBD);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(isolated: true), fixture.Context(), out string refusal));
            Assert.NotNull(refusal);
        }

        /// <summary>
        /// <b>23.</b> And one isolated selection's baseline is not the other's. Isolating a different set of
        /// dwellings is a different geometry, and the fingerprint sees it.
        /// </summary>
        [Fact]
        public void OneIsolatedScopesBaseline_IsNotReusedByAnother()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(isolated: true), fixture.Context(), out string _);

            Assert.NotNull(partOCanonicalTBD);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(isolated: true, scope: 2), fixture.Context(), out string refusal));
            Assert.NotNull(refusal);
        }

        /// <summary>
        /// The isolation cut is expressed entirely as the adiabatic flag, and the conversion reads it -
        /// SAM_Tas nulls the link of every TBD surface that matches an adiabatic panel. Two models identical
        /// but for that flag therefore convert to different TBDs, and the fingerprint must say so.
        /// </summary>
        [Fact]
        public void AChangedAdiabaticFlag_InvalidatesTheBaseline()
        {
            using Fixture fixture = new();

            PartOCanonicalTBD partOCanonicalTBD = PartOCanonicalTBD.Adopt(fixture.Path_TBD, fixture.Model(isolated: false), fixture.Context(), out string _);

            Assert.NotNull(partOCanonicalTBD);

            Assert.False(partOCanonicalTBD.IsValidFor(fixture.Model(isolated: false, adiabatic: true), fixture.Context(), out string refusal));
            Assert.NotNull(refusal);
        }

        // ---- G. Persistence ----------------------------------------------------------------------------

        /// <summary>
        /// <b>24, 27.</b> The isolation context survives the run's real <c>.sam</c> archive and comes back
        /// out of it, so a session that reopens the run still knows the results came from an isolated
        /// thermal model - and which dwellings it was isolated for. <b>Not reconstructed from the
        /// filename</b>: the file is deliberately given an unrelated name here.
        /// </summary>
        [Fact]
        public void TheIsolationContext_SurvivesCloseAndReopen()
        {
            using Fixture fixture = new();

            AnalyticalModel analyticalModel = fixture.Model(isolated: true);

            PartOIsolationContext partOIsolationContext = analyticalModel.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            Assert.NotNull(partOIsolationContext);

            string path = Path.Combine(fixture.Directory, "nothing-about-this-name-says-isolated.sam");

            Assert.True(Core.Convert.ToFile(analyticalModel, path, SAMFileType.SAM));

            AnalyticalModel analyticalModel_Reopened = Core.Convert.ToSAM<AnalyticalModel>(path)?.Find(x => x is not null);

            Assert.NotNull(analyticalModel_Reopened);

            PartOIsolationContext read = analyticalModel_Reopened.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            Assert.NotNull(read);
            Assert.True(read.IsValid);
            Assert.Equal(partOIsolationContext.ScopeToken, read.ScopeToken);
            Assert.Equal(partOIsolationContext.Guids_Space, read.Guids_Space);
            Assert.Equal(["Flat 1"], read.Names_Dwelling);
        }

        /// <summary>
        /// A whole-building run carries no isolation context at all - so "isolated" is something a model
        /// states, never something absent that has to be guessed at.
        /// </summary>
        [Fact]
        public void AFullRunsModel_CarriesNoIsolationContext()
        {
            using Fixture fixture = new();

            Assert.Null(fixture.Model(isolated: false).GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext));
        }

        /// <summary>
        /// The preparation context records that the run was asked to isolate, so a later round can report
        /// the run's scope - and <b>does not</b> re-isolate, because the model it re-prepares is already
        /// isolated.
        /// </summary>
        [Fact]
        public void ThePreparationContext_RecordsIsolation()
        {
            PartOPreparationContext partOPreparationContext = new(Analytical.Enums.PartOIteration.BasePassive, [], [], null)
            {
                Isolated = true,
            };

            Assert.True(partOPreparationContext.Isolated);
            Assert.False(new PartOPreparationContext(Analytical.Enums.PartOIteration.BasePassive, [], [], null).Isolated);
        }

        // ---- The fixture -------------------------------------------------------------------------------

        private static List<Zone> Zones()
        {
            Zone zone = new(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "Flat 1");
            zone.SetValue(ZoneParameter.IsDwelling, true);

            Zone zone_2 = new(new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "Flat 2");
            zone_2.SetValue(ZoneParameter.IsDwelling, true);

            return [zone, zone_2];
        }

        /// <summary>
        /// A two-room dwelling on two panels, built either as the whole building or as an isolated derived
        /// model of it - the smallest thing that differs in every way an isolated run differs: fewer spaces,
        /// the adiabatic cut, and the stamped isolation context.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            public Fixture()
            {
                Directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOIsolationScopeTests_{0}", Guid.NewGuid()));

                System.IO.Directory.CreateDirectory(Directory);

                Path_TBD = Path.Combine(Directory, "Project.tbd");

                File.WriteAllText(Path_TBD, "a converted model");

                WeatherData = new WeatherData("Z1_DSY1_2050s_HIGH90_CIBSE_v1.1", "Fixture", 51.5, -0.1, 25);
            }

            public string Directory { get; }

            public string Path_TBD { get; }

            public WeatherData WeatherData { get; }

            /// <param name="isolated">Whether to build the isolated derived model rather than the building.</param>
            /// <param name="scope">Which dwelling the isolated model is of - 1 or 2, so two scopes differ.</param>
            /// <param name="adiabatic">Marks a panel as the cut without changing anything else.</param>
            public AnalyticalModel Model(bool isolated = false, int scope = 1, double designFlowRate_Lps = 30, bool adiabatic = false)
            {
                AdjacencyCluster adjacencyCluster = new();

                Zone zone = new(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "Flat 1");

                adjacencyCluster.AddObject(zone);

                //The isolated model has only the selected dwelling's rooms; the whole building has the
                //excluded flat's as well.
                List<Space> spaces =
                [
                    new Space(new Guid(string.Format("bbbbbbbb-0000-0000-0000-00000000000{0}", scope)), "Bedroom", null),
                    new Space(new Guid("bbbbbbbb-0000-0000-0000-000000000009"), "Kitchen", null),
                ];

                if (!isolated)
                {
                    spaces.Add(new Space(new Guid("bbbbbbbb-0000-0000-0000-000000000005"), "Corridor", null));
                }

                foreach (Space space in spaces)
                {
                    adjacencyCluster.AddObject(space);
                    adjacencyCluster.AddRelation(zone, space);
                }

                VentilationTerminal ventilationTerminal = new("Bedroom terminal", FlowClassification.Supply, designFlowRate_Lps);

                adjacencyCluster.AddObject(ventilationTerminal);
                adjacencyCluster.AddRelation(ventilationTerminal, spaces[0]);

                for (int i = 0; i < 2; i++)
                {
                    double x = i * 5;

                    Face3D face3D = new(new Polygon3D(
                    [
                        new Point3D(x, 0, 0),
                        new Point3D(x + 4, 0, 0),
                        new Point3D(x + 4, 0, 3),
                        new Point3D(x, 0, 3),
                    ]));

                    Panel panel = SAM.Analytical.Create.Panel(
                        new Guid(string.Format("cccccccc-0000-0000-0000-00000000000{0}", i + 1)),
                        SAM.Analytical.Create.Panel(new Construction("Fixture Wall", [new ConstructionLayer("Concrete", 0.2)]), PanelType.Wall, face3D),
                        face3D);

                    //The isolation cut, which is the only thing that marks a boundary onto an omitted space.
                    if ((isolated || adiabatic) && i == 0)
                    {
                        panel.SetValue(PanelParameter.Adiabatic, true);
                    }

                    adjacencyCluster.AddObject(panel);
                }

                AnalyticalModel result = new("Project", null, null, null, adjacencyCluster, null, null);

                if (isolated)
                {
                    result.SetValue(
                        Analytical.AnalyticalModelParameter.PartOIsolationContext,
                        new PartOIsolationContext(spaces.ConvertAll(x => x.Guid), [zone.Guid], ["Flat 1"]));
                }

                return result;
            }

            /// <summary>The TAS case, identical for every model here - only the model varies.</summary>
            public PartOSimulationContext Context()
            {
                return new PartOSimulationContext(Directory, "Project", WeatherData, SolarCalculationMethod.TAS, 1, 365);
            }

            public void Dispose()
            {
                try
                {
                    System.IO.Directory.Delete(Directory, true);
                }
                catch
                {
                    //A locked temp directory is not a test failure.
                }
            }
        }
    }
}
