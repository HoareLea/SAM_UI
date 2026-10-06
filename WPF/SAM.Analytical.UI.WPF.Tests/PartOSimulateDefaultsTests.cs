// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Weather;
using System;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The Simulate dialog an Approved Document O run opens.</b>
    /// <para>
    /// Part O reaches the Simulate dialog through the ordinary <c>Modify.Simulate</c>, which seeds itself
    /// from the manual command's remembered options and, failing those, from <c>UI.Create.SimulateOptions</c>
    /// - whose <c>FullYearSimulation</c> is <b>false</b>. A person could therefore prepare an iteration,
    /// press Run, accept a dialog that looked reasonable, wait out a TAS run, and be told the run could not
    /// be completed because it was not a full year. The one setting a Part O run cannot do without was the
    /// one nobody was asked to turn on.
    /// </para>
    /// <para>
    /// <b>What is pinned here.</b> Part O gets the annual full-year case automatically and cannot have it
    /// turned off; the exact TAS case is stated rather than inherited; the other workflows' exports are not
    /// forced on a Part O user; the project name is always the model's; the weather and output directory are
    /// prepopulated from the project and carried between runs only while they remain valid; and nothing else
    /// survives from one run to the next. The manual Simulate command's own defaults are pinned unchanged
    /// alongside, because the whole point of a second settings key is that neither path can move the other.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOSimulateDefaultsTests
    {
        private static AnalyticalModel Model(string name, WeatherData weatherData = null)
        {
            AnalyticalModel result = new(name, null, null, null, new AdjacencyCluster(), null, null);

            if (weatherData is not null)
            {
                result.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);
            }

            return result;
        }

        private static WeatherData Weather(string name)
        {
            return new WeatherData(name, name, 51.5, -0.45, 25);
        }

        /// <summary>A remembered dialog state with every fixed field set to the WRONG value.</summary>
        private static SimulateOptions Remembered_AllWrong()
        {
            return new SimulateOptions
            {
                ProjectName = "SomeOtherProject",
                Simulate = false,
                FullYearSimulation = false,
                Sizing = true,
                UnmetHours = true,
                UseWidths = true,
                UpdateConstructionLayersByPanelType = false,
                RoomDataSheets = true,
                CreateSAP = true,
                CreatePartL = true,
                CreateTPD = true,
                CreateTM59 = true,
            };
        }

        // ---- A: the deterministic Part O case ---------------------------------------------------------

        /// <summary>
        /// <b>The headline.</b> A Part O run is an annual, full-year simulation, and the preset says so
        /// without being asked. Days 1 to 365 is not a preference - <c>Query.IsPartOFullYearSimulation</c>
        /// refuses every other range, so there is no completable Part O run with any other value.
        /// </summary>
        [Fact]
        public void PartO_GetsTheAnnualFullYearSimulation()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, null);

            Assert.True(simulateOptions.Simulate);
            Assert.True(simulateOptions.FullYearSimulation);
        }

        /// <summary>
        /// The rest of the TAS case, stated exactly. These four are what <c>PartOCanonicalTBD</c>
        /// fingerprints, so they have to be the same for a baseline and for every Iteration 2B round that
        /// repeats it - which is why they are fixed here rather than left to whatever the dialog last held.
        /// <para>
        /// <b>Sizing is off.</b> Part O asks for a full year of the building as designed, not a plant sizing
        /// exercise, and nothing in the assessment or in 2B reads a design load - the only consumer of one
        /// anywhere is <c>Modify.PrintRoomDataSheets</c>, which is fixed off. <c>Tas.Query.Sizing</c> is not
        /// a read-only step either: it re-saves the very TBD that is about to be simulated, recomputing every
        /// zone's max loads and sizing types and applying oversizing factors, and writes an
        /// <c>_Uncapped.tbd</c> and an <c>_HDDCDD.tbd</c> beside the run because the workflow's call site
        /// leaves <c>GenerateUncappedFile</c> and <c>GenerateHDDCDDFile</c> at their true defaults. On was
        /// only ever "what every run to date used".
        /// </para>
        /// <para>
        /// <b>Manual Simulate keeps sizing on</b> - see <c>TheManualSimulateDefaults_AreUnchanged</c>. This
        /// is a Part O preset decision and not a change to the ordinary command.
        /// </para>
        /// </summary>
        [Fact]
        public void PartO_StatesTheWholeTasCase()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, null);

            Assert.False(simulateOptions.Sizing);
            Assert.False(simulateOptions.UnmetHours);
            Assert.False(simulateOptions.UseWidths);
            Assert.True(simulateOptions.UpdateConstructionLayersByPanelType);
        }

        /// <summary>
        /// <b>D: other workflows' deliverables, and not a Part O user's problem.</b> Part O produces its
        /// overheating assessment through <c>Modify.AssessPartOTM59</c>, which reads the TSD directly; the
        /// dialog's "Domestic Overheating" box is a separate TAS XML export that would re-open the TBD after
        /// every run to write a file nothing in this workflow reads.
        /// </summary>
        [Fact]
        public void PartO_DoesNotForceTheOtherWorkflowsExports()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, null);

            Assert.False(simulateOptions.RoomDataSheets);
            Assert.False(simulateOptions.CreateSAP);
            Assert.False(simulateOptions.CreatePartL);
            Assert.False(simulateOptions.CreateTPD);
            Assert.False(simulateOptions.CreateTM59);
        }

        /// <summary>
        /// <b>The staleness rule, as a property rather than a promise.</b> Handed a remembered state with
        /// every fixed field set to the opposite of what Part O needs, the preset still produces the Part O
        /// case: those fields are never read. A change of scenario, of dwelling scope or of model therefore
        /// cannot carry a stale TAS setting into the next run, because there is no path by which one could.
        /// </summary>
        [Fact]
        public void PartO_ReadsNoFixedSettingOutOfTheRememberedState()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, Remembered_AllWrong());

            Assert.True(simulateOptions.Simulate);
            Assert.True(simulateOptions.FullYearSimulation);
            Assert.False(simulateOptions.Sizing);
            Assert.False(simulateOptions.UnmetHours);
            Assert.False(simulateOptions.UseWidths);
            Assert.True(simulateOptions.UpdateConstructionLayersByPanelType);
            Assert.False(simulateOptions.RoomDataSheets);
            Assert.False(simulateOptions.CreateSAP);
            Assert.False(simulateOptions.CreatePartL);
            Assert.False(simulateOptions.CreateTPD);
            Assert.False(simulateOptions.CreateTM59);
        }

        // ---- B: derived from the project --------------------------------------------------------------

        /// <summary>
        /// The project name is the model's, always. It names the TBD, the TSD, the per-run <c>.sam</c> and
        /// the TM59 report, and on an isolated run it already carries the scope token
        /// <c>Query.ProjectName_Isolated</c> put there to keep that run's files off a full run's. A name
        /// remembered from the previous run would be the previous scope's - the exact collision the token
        /// exists to prevent.
        /// </summary>
        [Fact]
        public void PartO_TakesTheProjectNameFromTheModel_NeverFromTheRememberedState()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1-Isolated-3f2a"), null, Remembered_AllWrong());

            Assert.Equal("Flat1-Isolated-3f2a", simulateOptions.ProjectName);
        }

        // ---- C: the person's own inputs, carried but re-validated -------------------------------------

        /// <summary>
        /// The project's weather wins. It is what the model states, it is what the run will be stamped with,
        /// and a file remembered from another project must not quietly displace it.
        /// </summary>
        [Fact]
        public void PartO_PreselectsTheProjectsOwnWeather()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(
                Model("Flat1", Weather("LondonHeathrowDSY1")),
                null,
                new SimulateOptions { WeatherData = Weather("ManchesterTRY") });

            Assert.Equal("LondonHeathrowDSY1", simulateOptions.WeatherData?.Name);
        }

        /// <summary>
        /// And the previous run's weather is offered only where the model states none - so a second
        /// iteration in one session does not send anybody back to the file picker.
        /// </summary>
        [Fact]
        public void PartO_CarriesThePreviousWeather_OnlyWhereTheModelStatesNone()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(
                Model("Flat1"),
                null,
                new SimulateOptions { WeatherData = Weather("ManchesterTRY") });

            Assert.Equal("ManchesterTRY", simulateOptions.WeatherData?.Name);
        }

        /// <summary>
        /// With neither, the field is left empty rather than guessed at - and the dialog's own OK handler
        /// already refuses to run without a weather file selected.
        /// </summary>
        [Fact]
        public void PartO_LeavesTheWeatherEmptyWithNoSourceForIt()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, null);

            Assert.Null(simulateOptions.WeatherData);
        }

        /// <summary>
        /// An output directory a person redirected to in this session is offered again for the next run -
        /// while it is still there.
        /// </summary>
        [Fact]
        public void PartO_CarriesAnOutputDirectoryThatStillExists()
        {
            string directory_Remembered = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOSimulateDefaults_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory_Remembered);

            try
            {
                SimulateOptions simulateOptions = Create.SimulateOptions_PartO(
                    Model("Flat1"),
                    Path.Combine(Path.GetTempPath(), "Project.sam"),
                    new SimulateOptions { OutputDirectory = directory_Remembered });

                Assert.Equal(directory_Remembered, simulateOptions.OutputDirectory);
            }
            finally
            {
                Directory.Delete(directory_Remembered);
            }
        }

        /// <summary>
        /// <b>A remembered path that has gone is not a setting, it is a failure waiting for the first
        /// write.</b> The model's own directory takes over.
        /// </summary>
        [Fact]
        public void PartO_DropsAnOutputDirectoryThatIsNoLongerThere()
        {
            string directory_Gone = Path.Combine(Path.GetTempPath(), string.Format("SAM_PartOSimulateDefaults_Gone_{0}", Guid.NewGuid()));

            string directory_Model = Path.Combine(Path.GetTempPath(), "SAM_PartOSimulateDefaults_Model");

            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(
                Model("Flat1"),
                Path.Combine(directory_Model, "Project.sam"),
                new SimulateOptions { OutputDirectory = directory_Gone });

            Assert.Equal(directory_Model, simulateOptions.OutputDirectory);
        }

        /// <summary>With nothing remembered, the model's own directory - the first run of a session.</summary>
        [Fact]
        public void PartO_DefaultsTheOutputDirectoryToTheModels()
        {
            string directory_Model = Path.Combine(Path.GetTempPath(), "SAM_PartOSimulateDefaults_Model");

            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), Path.Combine(directory_Model, "Project.sam"), null);

            Assert.Equal(directory_Model, simulateOptions.OutputDirectory);
        }

        /// <summary>
        /// The solar method is a genuine engineering input, carried between runs because neither of its two
        /// values can go stale the way a path or a file can - and TAS where nothing was chosen yet.
        /// </summary>
        [Theory]
        [InlineData(SolarCalculationMethod.SAM, SolarCalculationMethod.SAM)]
        [InlineData(SolarCalculationMethod.TAS, SolarCalculationMethod.TAS)]
        [InlineData(SolarCalculationMethod.Undefined, SolarCalculationMethod.TAS)]
        public void PartO_CarriesTheSolarCalculationMethod(SolarCalculationMethod solarCalculationMethod_Remembered, SolarCalculationMethod solarCalculationMethod_Expected)
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(
                Model("Flat1"),
                null,
                new SimulateOptions { SolarCalculationMethod = solarCalculationMethod_Remembered });

            Assert.Equal(solarCalculationMethod_Expected, simulateOptions.SolarCalculationMethod);
        }

        /// <summary>No model, no preset - and no exception either.</summary>
        [Fact]
        public void PartO_WithNoModel_IsNoPreset()
        {
            Assert.Null(Create.SimulateOptions_PartO(null, null, null));
        }

        /// <summary>
        /// <b>The performance guardrail, as a behaviour rather than a comment.</b> The zone category list
        /// feeds one combo box, and that combo is enabled only while the SAP or domestic-overheating export
        /// is ticked - both fixed off and locked for a Part O run. Reading it would walk the model's zones to
        /// fill a control nobody can reach, on models that carry thousands of spaces. The ordinary Simulate
        /// command still reads it, because there those two boxes are a person's to tick.
        /// </summary>
        [Fact]
        public void PartO_DoesNotWalkTheModelForAControlItHasLocked()
        {
            SimulateOptions simulateOptions = Create.SimulateOptions_PartO(Model("Flat1"), null, null);

            Assert.Null(simulateOptions.ZoneCategories);
        }

        // ---- Regression safety: the manual Simulate command --------------------------------------------

        /// <summary>
        /// <b>The ordinary Simulate command is untouched.</b> Its defaults are the ones an expert has always
        /// met: the annual simulation on, the full-year box <b>off</b>, sizing on, every export off. This is
        /// the assertion that would fail if somebody ever "fixed" Part O by moving the shared default - which
        /// is exactly what this work was told not to do.
        /// </summary>
        [Fact]
        public void TheManualSimulateDefaults_AreUnchanged()
        {
            SimulateOptions simulateOptions = new();

            Assert.True(simulateOptions.Simulate);
            Assert.False(simulateOptions.FullYearSimulation);
            Assert.True(simulateOptions.Sizing);
            Assert.False(simulateOptions.UnmetHours);
            Assert.False(simulateOptions.UseWidths);
            Assert.True(simulateOptions.UpdateConstructionLayersByPanelType);
            Assert.False(simulateOptions.RoomDataSheets);
            Assert.False(simulateOptions.CreateSAP);
            Assert.False(simulateOptions.CreatePartL);
            Assert.False(simulateOptions.CreateTPD);
            Assert.False(simulateOptions.CreateTM59);
            Assert.Equal(SolarCalculationMethod.TAS, simulateOptions.SolarCalculationMethod);
        }

        /// <summary>
        /// The two are kept apart by two settings keys, not by a convention. Reading them as one enum proves
        /// they are distinct members, so a Part O run writing its state back cannot land on the manual
        /// command's remembered options.
        /// </summary>
        [Fact]
        public void PartO_AndTheManualCommand_RememberSeparately()
        {
            Assert.NotEqual(AnalyticalSettingParameter.SimulateOptions, AnalyticalSettingParameter.SimulateOptions_PartO);
        }

        /// <summary>
        /// <b>Opting in is what turns the Part O dialog on, and the default is off.</b>
        /// <para>
        /// The ribbon's own Simulate button hands <c>Modify.Simulate</c> the session's Part O run - it has to,
        /// so a workflow over a prepared model can complete it - so "a run is prepared" is NOT enough to mean
        /// "this is the Part O command". Had it been, the expert dialog would have been retuned and locked
        /// whenever a Part O iteration happened to be prepared, taking away the sizing-only and export runs an
        /// engineer may want over that same model.
        /// </para>
        /// <para>
        /// This pins the seam by reflection rather than by reading the call sites: the parameter exists, it is
        /// a <c>bool</c>, and it is optional and false. Every caller that does not name it - which is every
        /// caller except <c>RunPartOWorkflow</c> - therefore reaches exactly the dialog it reached before.
        /// </para>
        /// </summary>
        [Fact]
        public void TheManualSimulateCommand_IsNotOptedIn()
        {
            System.Reflection.ParameterInfo[] parameterInfos = typeof(Modify)
                .GetMethod(nameof(Modify.Simulate), [typeof(UIAnalyticalModel), typeof(PartORun), typeof(bool)])
                .GetParameters();

            System.Reflection.ParameterInfo parameterInfo = parameterInfos[2];

            Assert.Equal(typeof(bool), parameterInfo.ParameterType);
            Assert.True(parameterInfo.IsOptional);
            Assert.Equal(false, parameterInfo.DefaultValue);
        }

        // ---- The dialog itself -------------------------------------------------------------------------

        /// <summary>
        /// <b>The preset through the real dialog.</b> Applied to a live <c>SimulateWindow</c> and read back
        /// off its controls: the annual run is on, the full-year box is ticked, and the day range the
        /// workflow will be handed is 1 to 365 - which is what
        /// <c>Query.IsPartOFullYearSimulation</c> requires and <c>Modify.Simulate</c> reads out of these two
        /// text boxes.
        /// </summary>
        [WpfFact]
        public void PartO_TheDialogShowsTheFullYearRange()
        {
            SimulateWindow simulateWindow = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            simulateWindow.LockPartOSettings();

            Assert.True(simulateWindow.Simulate);
            Assert.True(simulateWindow.FullYearSimulation);
            Assert.Equal(PartOSimulationContext.Day_First_FullYear, simulateWindow.FullYearSimulation_From);
            Assert.Equal(PartOSimulationContext.Day_Last_FullYear, simulateWindow.FullYearSimulation_To);
        }

        /// <summary>
        /// <b>And they cannot be unticked.</b> Preselecting the full-year box would still have left a person
        /// one click away from paying for a TAS run that cannot complete the iteration; locking it is what
        /// makes the guarantee hold. The same for the four settings that make up the TAS case an Iteration 2B
        /// round has to repeat, and for the five exports that belong to other workflows.
        /// </summary>
        [WpfFact]
        public void PartO_TheDeterministicSettingsAreLocked()
        {
            SimulateControl simulateControl = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            simulateControl.LockPartOSettings();

            foreach (string name in new[]
            {
                "checkBox_Simulate",
                "checkBox_FullYearSimulation",
                "checkBox_Sizing",
                "checkBox_UnmetHours",
                "checkBox_UseWidths",
                "checkBox_UpdateConstructionLayersByPanelType",
                "checkBox_RoomDataSheets",
                "checkBox_CreateSAP",
                "checkBox_CreatePartL",
                "checkBox_CreateTPD",
                "checkBox_CreateTM59",
            })
            {
                Assert.False(
                    (simulateControl.FindName(name) as System.Windows.UIElement)?.IsEnabled,
                    string.Format("{0} should be locked for a Part O run.", name));
            }
        }

        /// <summary>
        /// <b>What stays a person's.</b> Locking the deterministic settings must not lock the run's actual
        /// engineering inputs with them: the weather file, the output directory and the solar calculation
        /// method are all still open. A dialog with nothing left to change would be a dialog worth removing,
        /// and these three are the reason it stays.
        /// <para>
        /// The output directory in particular: <b>where</b> the evidence is written is a person's to
        /// redirect, because moving a run's files changes nothing about what the run is. That is exactly the
        /// line that separates it from the project name - see
        /// <see cref="PartO_ProjectName_IsDerivedAndLocked"/>.
        /// </para>
        /// </summary>
        [WpfFact]
        public void PartO_TheGenuineChoicesStayOpen()
        {
            SimulateControl simulateControl = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            simulateControl.LockPartOSettings();

            foreach (string name in new[]
            {
                "selectSAMObjectComboBoxControl_WeatherData",
                "textBox_OutputDirectory",
                "button_OutputDirectory",
                "comboBox_SolarCalculationMethod",
            })
            {
                Assert.True(
                    (simulateControl.FindName(name) as System.Windows.UIElement)?.IsEnabled,
                    string.Format("{0} is a Part O run's own input and should stay open.", name));
            }
        }

        /// <summary>
        /// <b>The project name is the run's identity, so a Part O user cannot retype it.</b>
        ///
        /// <para><b>Why this is not merely tidiness</b></para>
        /// <para>
        /// Every artifact a Part O run is judged by derives from the name - <c>&lt;project&gt;.tbd</c>,
        /// <c>.tsd</c>, the per-run <c>.sam</c> and <c>&lt;project&gt;-TM59.txt</c>. On an isolated run it
        /// carries the scope token <c>Query.ProjectName_Isolated</c> put there so that run's evidence cannot
        /// land on a full run's or on another selection's, and
        /// <see cref="PartOSimulationContext.Iteration_ProjectName"/> reads the optimisation round back out of
        /// it - so a hand-edited name can restart Iteration 2B's numbering at <c>-Opt01</c> and overwrite a
        /// previous optimisation's evidence. Nothing downstream refuses an edited name; it is simply believed.
        /// </para>
        /// <para>
        /// So the value is derived from the prepared model, the control is locked, and locking does not move
        /// the value. All three are asserted here, including over an isolated run's name - the case where the
        /// token being removed would actually cost somebody their evidence.
        /// </para>
        /// </summary>
        [WpfFact]
        public void PartO_ProjectName_IsDerivedAndLocked()
        {
            //An isolated run: the name carries the scope token that keeps this run's files off a full run's.
            AnalyticalModel analyticalModel = Model("Flat1-Isolated-3f2a", Weather("LondonHeathrowDSY1"));

            SimulateControl simulateControl = new()
            {
                //Handed a remembered state naming a DIFFERENT project, to prove the value is derived rather
                //than restored.
                SimulateOptions = Create.SimulateOptions_PartO(analyticalModel, null, Remembered_AllWrong()),
            };

            //Derived from the model, before anything is locked.
            Assert.Equal(analyticalModel.Name, simulateControl.ProjectName);

            simulateControl.LockPartOSettings();

            //Locked - both the way WPF disables a control and the way a TextBox refuses typing, so the answer
            //is the same whichever one is asked.
            System.Windows.Controls.TextBox textBox_ProjectName = simulateControl.FindName("textBox_ProjectName") as System.Windows.Controls.TextBox;

            Assert.NotNull(textBox_ProjectName);
            Assert.False(textBox_ProjectName.IsEnabled);
            Assert.True(textBox_ProjectName.IsReadOnly);

            //And locking moved nothing: the name the run will be written under is still the model's, and it
            //still carries the scope token.
            Assert.Equal(analyticalModel.Name, simulateControl.ProjectName);
            Assert.Equal(analyticalModel.Name, simulateControl.SimulateOptions.ProjectName);
        }

        /// <summary>
        /// <b>And the locked box says why, on the box.</b>
        /// <para>
        /// This is the same dialog the ordinary Energy Simulation command opens with the field editable, so
        /// an engineer has every reason to expect to be able to type in it - and a locked field with no
        /// explanation reads as a defect rather than as a derived identity. The tooltip has to be marked
        /// shown-on-disabled, or WPF never shows it at all on a disabled control, which would leave the
        /// explanation written and invisible.
        /// </para>
        /// </summary>
        [WpfFact]
        public void PartO_ProjectName_SaysWhyItIsLocked()
        {
            SimulateControl simulateControl = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            System.Windows.Controls.TextBox textBox_ProjectName = simulateControl.FindName("textBox_ProjectName") as System.Windows.Controls.TextBox;

            Assert.NotNull(textBox_ProjectName);

            //Nothing before it is locked: the ordinary dialog carries no explanation because it has nothing
            //to explain.
            Assert.Null(textBox_ProjectName.ToolTip);

            simulateControl.LockPartOSettings();

            string toolTip = textBox_ProjectName.ToolTip as string;

            Assert.False(string.IsNullOrWhiteSpace(toolTip));

            //It has to reach a person looking at a DISABLED box.
            Assert.True(System.Windows.Controls.ToolTipService.GetShowOnDisabled(textBox_ProjectName));

            //And it has to name the three reasons, because each is a different way an edit does damage.
            Assert.Contains("identity", toolTip);
            Assert.Contains(".tsd", toolTip);
            Assert.Contains("isolated", toolTip);
            Assert.Contains("Iteration 2B", toolTip);

            //Plus where the field IS editable, so the answer is not merely "no".
            Assert.Contains("Energy Simulation", toolTip);
        }

        /// <summary>
        /// <b>The two routes are distinguishable.</b> The guided Part O run and the ordinary Energy
        /// Simulation open the SAME window; until the Part O one retitled itself they were identical on
        /// screen, so a derived, locked project name read as the ordinary dialog having broken.
        /// </summary>
        [WpfFact]
        public void PartO_TheDialogSaysWhichRouteItIs()
        {
            SimulateWindow simulateWindow_Manual = new()
            {
                SimulateOptions = new SimulateOptions { ProjectName = "SomeProject" },
            };

            Assert.Equal(SimulateWindow.Title_Manual, simulateWindow_Manual.Title);

            SimulateWindow simulateWindow_PartO = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            //Untitled as Part O until the Part O path claims it - LockPartOSettings is a call, not a
            //constructor, and that is what keeps the manual route the manual route.
            Assert.Equal(SimulateWindow.Title_Manual, simulateWindow_PartO.Title);

            simulateWindow_PartO.LockPartOSettings();

            Assert.Equal(SimulateWindow.Title_PartO, simulateWindow_PartO.Title);
            Assert.Contains("Part O", simulateWindow_PartO.Title);

            //TAS, not Tas - the whole family agrees on the product's name.
            Assert.DoesNotContain("Tas", simulateWindow_PartO.Title, System.StringComparison.Ordinal);

            //AND THE TWO TITLES CLAIM ONLY WHAT THEIR ROUTE DOES. The manual one promises the conversion
            //alone, because Modify.Simulate supports an unticked Simulate box alongside SAP or the
            //domestic-overheating XML - a conversion and export with no simulation at all. The Part O one
            //promises the simulation, and is entitled to: the preset ticks it and LockPartOSettings takes
            //the box away, so a Part O run always simulates.
            Assert.DoesNotContain("simulate", SimulateWindow.Title_Manual, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("simulate", SimulateWindow.Title_PartO, System.StringComparison.OrdinalIgnoreCase);

            Assert.True(simulateWindow_PartO.Simulate);
            Assert.False((simulateWindow_PartO.FindName("simulateControl") as SimulateControl)?.FindName("checkBox_Simulate") is System.Windows.UIElement uIElement && uIElement.IsEnabled);

            //Where the manual dialog leaves it: the user's, and on by default rather than promised.
            Assert.True((simulateWindow_Manual.FindName("simulateControl") as SimulateControl)?.FindName("checkBox_Simulate") is System.Windows.UIElement uIElement_Manual && uIElement_Manual.IsEnabled);
        }

        /// <summary>
        /// <b>And the manual dialog's project name is untouched.</b> Renaming the output of an ordinary
        /// simulation is an ordinary thing to want; nothing there derives an identity from it. The lock exists
        /// only on the guided Part O path, which is what <c>LockPartOSettings</c> being a call rather than a
        /// constructor makes true - this asserts it of a control nobody has locked.
        /// </summary>
        [WpfFact]
        public void TheManualDialog_ProjectName_StaysEditable()
        {
            SimulateControl simulateControl = new()
            {
                SimulateOptions = new SimulateOptions { ProjectName = "SomeProject" },
            };

            System.Windows.Controls.TextBox textBox_ProjectName = simulateControl.FindName("textBox_ProjectName") as System.Windows.Controls.TextBox;

            Assert.NotNull(textBox_ProjectName);
            Assert.True(textBox_ProjectName.IsEnabled);
            Assert.False(textBox_ProjectName.IsReadOnly);

            //Editable in the sense that matters: a typed name is what the command then runs under.
            simulateControl.ProjectName = "RenamedByHand";

            Assert.Equal("RenamedByHand", simulateControl.SimulateOptions.ProjectName);

            //And nothing explains a restriction that is not there.
            Assert.Null(textBox_ProjectName.ToolTip);
        }

        /// <summary>
        /// <b>The whole chain, closed.</b> The preset goes into the real dialog; the dialog's values are read
        /// back exactly as <c>Modify.Simulate</c> reads them - the full-year box deciding whether the two day
        /// boxes are used at all, which is the step that turned an unticked box into <c>-1</c> and a workflow
        /// that simulated nothing - and the <see cref="PartOSimulationContext"/> built from them is the
        /// full-year case. <c>Modify.CanOptimise</c> asks the same question of the same property before it
        /// will start an Iteration 2B, and <c>Query.IsPartOFullYearSimulation</c> asks it of the settings the
        /// workflow is actually handed.
        /// <para>
        /// Without this the run reached TAS, spent the time, and was then refused for not being a full year.
        /// </para>
        /// </summary>
        [WpfFact]
        public void PartO_TheDialogProducesAFullYearCase()
        {
            SimulateWindow simulateWindow = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            simulateWindow.LockPartOSettings();

            //Read exactly as Modify.Simulate reads them, including the conditional that used to be the trap.
            bool fullYearSimulation = simulateWindow.FullYearSimulation;

            PartOSimulationContext partOSimulationContext = new(
                @"C:\Output",
                simulateWindow.ProjectName,
                simulateWindow.SelectedWeatherData,
                simulateWindow.SolarCalculationMethod,
                fullYearSimulation ? simulateWindow.FullYearSimulation_From : -1,
                fullYearSimulation ? simulateWindow.FullYearSimulation_To : -1)
            {
                UnmetHours = simulateWindow.UnmetHours,
                Sizing = simulateWindow.Sizing,
                UseWidths = simulateWindow.UseWidths,
                UpdateConstructionLayersByPanelType = simulateWindow.UpdateConstructionLayersByPanelType,
            };

            Assert.True(partOSimulationContext.IsFullYear);
            Assert.Equal(PartOSimulationContext.Day_First_FullYear, partOSimulationContext.SimulateFrom);
            Assert.Equal(PartOSimulationContext.Day_Last_FullYear, partOSimulationContext.SimulateTo);

            //And the rest of the case is the one an Iteration 2B round will repeat verbatim.
            Assert.False(partOSimulationContext.Sizing);
            Assert.False(partOSimulationContext.UnmetHours);
            Assert.False(partOSimulationContext.UseWidths);
            Assert.True(partOSimulationContext.UpdateConstructionLayersByPanelType);
        }

        /// <summary>
        /// Locking changes what may be pressed, never what is set. Read back through
        /// <see cref="SimulateControl.SimulateOptions"/> - the same property <c>Modify.Simulate</c> writes
        /// into the session's remembered state - the case is exactly the one the preset stated.
        /// </summary>
        [WpfFact]
        public void PartO_LockingChangesNoValue()
        {
            SimulateControl simulateControl = new()
            {
                SimulateOptions = Create.SimulateOptions_PartO(Model("Flat1", Weather("LondonHeathrowDSY1")), null, null),
            };

            simulateControl.LockPartOSettings();

            SimulateOptions simulateOptions = simulateControl.SimulateOptions;

            Assert.True(simulateOptions.Simulate);
            Assert.True(simulateOptions.FullYearSimulation);
            Assert.False(simulateOptions.Sizing);
            Assert.False(simulateOptions.CreateTM59);
            Assert.False(simulateOptions.CreateTPD);
            Assert.Equal("Flat1", simulateOptions.ProjectName);
        }
    }
}
