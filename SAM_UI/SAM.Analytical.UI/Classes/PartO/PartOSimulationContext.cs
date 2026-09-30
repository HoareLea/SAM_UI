// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Weather;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The TAS case one Approved Document O run was simulated as - the weather, the solar method, the day
    /// range and the workflow options - kept so the <b>same</b> case can be run again over a changed design.
    /// <para>
    /// <b>This is what makes an optimisation's iterations comparable.</b> Iteration 2B changes design
    /// airflow and reruns; if the second run used different weather, a different day range or a different
    /// solar calculation, the change in TM59 results would no longer be attributable to the airflow. So the
    /// case is captured once, from the simulation the baseline was actually produced by, and reused
    /// verbatim.
    /// </para>
    /// <para>
    /// <b>Everything except the project name.</b> That is the one thing an optimisation iteration must
    /// change, because it is what gives each iteration its own TBD and its own TSD - see
    /// <see cref="ProjectName"/>. Overwriting the previous iteration's results would destroy the evidence
    /// for the round that produced them.
    /// </para>
    /// <para>
    /// <b>Simulation only.</b> The deliverable exports a person might also have ticked - room data sheets,
    /// the SAP or Part L export, the TAS domestic-overheating XML, the TPD - are deliberately not carried.
    /// They are outputs of a run somebody asked for, not part of the thermal case, and producing a dozen
    /// copies of each during an optimisation would be noise.
    /// </para>
    /// </summary>
    public class PartOSimulationContext
    {
        /// <param name="outputDirectory">Where the TBD and TSD are written.</param>
        /// <param name="projectName">The base project name the run used.</param>
        /// <param name="weatherData">The weather the run was simulated against.</param>
        /// <param name="solarCalculationMethod">Which solar calculation produced it.</param>
        /// <param name="simulateFrom">First day of the simulated range.</param>
        /// <param name="simulateTo">Last day of the simulated range.</param>
        public PartOSimulationContext(string outputDirectory, string projectName, WeatherData weatherData, SolarCalculationMethod solarCalculationMethod, int simulateFrom, int simulateTo)
        {
            OutputDirectory = outputDirectory;
            ProjectName = projectName;
            WeatherData = weatherData;
            SolarCalculationMethod = solarCalculationMethod;
            SimulateFrom = simulateFrom;
            SimulateTo = simulateTo;
        }

        /// <summary>The directory the TBD and TSD are written to.</summary>
        public string OutputDirectory { get; }

        /// <summary>
        /// The design model's file, where the run was started from one that has been saved - a locator for the
        /// <see cref="PartOBaselineReference"/> the result carries (PR-5), for a case whose materialiser does not know the
        /// file. Null or empty where unknown. Identity is never taken from it.
        /// </summary>
        public string Path_DesignModel { get; set; }

        /// <summary>
        /// The project name the baseline used. An optimisation iteration derives its own name from this -
        /// <c>&lt;project&gt;-Opt01</c>, <c>-Opt02</c> - so every iteration keeps its own results file and no
        /// round can overwrite the evidence for another.
        /// </summary>
        public string ProjectName { get; }

        /// <summary>The weather. Never changed by an optimisation.</summary>
        public WeatherData WeatherData { get; }

        /// <summary>Which solar calculation the case uses.</summary>
        public SolarCalculationMethod SolarCalculationMethod { get; }

        /// <summary>First day of the simulated range.</summary>
        public int SimulateFrom { get; }

        /// <summary>Last day of the simulated range.</summary>
        public int SimulateTo { get; }

        /// <summary>Whether unmet hours are calculated.</summary>
        public bool UnmetHours { get; set; } = true;

        /// <summary>Whether plant sizing runs.</summary>
        public bool Sizing { get; set; } = true;

        /// <summary>Whether aperture widths are used.</summary>
        public bool UseWidths { get; set; }

        /// <summary>Whether construction layers are updated by panel type before conversion.</summary>
        public bool UpdateConstructionLayersByPanelType { get; set; } = true;

        /// <summary>
        /// This case again, writing somewhere else - the <b>only</b> way an Approved Document O Iteration 3
        /// Candidate B is allowed to be built.
        ///
        /// <para><b>Why a copy, and why it changes only two things</b></para>
        /// <para>
        /// Candidate B is a second thermal route over the same design, and the whole point of comparing it
        /// with Reference A is that any difference in the results is attributable to the route. That
        /// survives only if every other input is identical: the same weather file, the same solar
        /// calculation, the same annual day range, the same unmet-hours and sizing behaviour, the same
        /// aperture-width treatment and the same construction-layer preparation. A field silently left at
        /// its default here would make the two cases different simulations of different buildings, and the
        /// A/B statistics would be measuring that instead.
        /// </para>
        /// <para>
        /// So this copies <b>everything</b> and overrides only where the files go and what they are called
        /// - which must differ, or Candidate B would write over Reference A's own evidence. A reflection
        /// test asserts that every public property of this type is carried, so a property added later
        /// cannot quietly fail to be copied.
        /// </para>
        /// <para>
        /// <b>The weather is shared, not cloned.</b> <c>WeatherData</c> is an input both cases read and
        /// neither writes; handing Candidate B a copy of it would let the two drift apart for no gain and
        /// would duplicate a year of hourly weather per run.
        /// </para>
        /// </summary>
        /// <param name="projectName">The copy's project name. Null or blank keeps this one's.</param>
        /// <param name="outputDirectory">Where the copy writes. Null or blank keeps this one's.</param>
        public PartOSimulationContext Copy(string projectName = null, string outputDirectory = null)
        {
            return new PartOSimulationContext(
                string.IsNullOrWhiteSpace(outputDirectory) ? OutputDirectory : outputDirectory,
                string.IsNullOrWhiteSpace(projectName) ? ProjectName : projectName,
                WeatherData,
                SolarCalculationMethod,
                SimulateFrom,
                SimulateTo)
            {
                UnmetHours = UnmetHours,
                Sizing = Sizing,
                UseWidths = UseWidths,
                UpdateConstructionLayersByPanelType = UpdateConstructionLayersByPanelType,
                Path_DesignModel = Path_DesignModel,
            };
        }

        /// <summary>
        /// Whether this is the full annual hourly series a TM59 assessment can read. Anything less cannot
        /// complete a Part O run, so an optimisation cannot be started from it either.
        /// </summary>
        public bool IsFullYear => SimulateFrom == Day_First_FullYear && SimulateTo == Day_Last_FullYear;

        /// <summary>The first day of an Approved Document O full-year simulation.</summary>
        public const int Day_First_FullYear = 1;

        /// <summary>The last day of an Approved Document O full-year simulation.</summary>
        public const int Day_Last_FullYear = 365;

        /// <summary>
        /// How many hourly values a full-year Approved Document O results series must contain -
        /// <b>the requested day range, not anything read back out of a results file</b>.
        ///
        /// <para><b>Why this cannot come from the TSD</b></para>
        /// <para>
        /// It was, briefly, counted from the weather year the TSD carries. That defeats the check it exists
        /// for: a damaged file that lost two thirds of its weather also lost two thirds of its results, so
        /// the requirement fell to 2400 hours, the 2400-hour series met it, and the partial year was
        /// assessed and reported. A results file may not decide how much of a year it was supposed to
        /// contain. The authority has to be independent of the payload being validated, and this is.
        /// </para>
        ///
        /// <para><b>Why a constant is the honest form of it</b></para>
        /// <para>
        /// It is derived from the two days that DEFINE a Part O full year - the same pair
        /// <see cref="IsFullYear"/> and <c>Query.IsPartOFullYearSimulation</c> test against, which is why
        /// they are named here rather than written out at each site. Approved Document O's criteria are
        /// defined over days 1 to 365; <c>IsPartOFullYearSimulation</c> already refuses anything else
        /// outright, including 2-366 and 1-364, so there is no other range a completable Part O run can
        /// have.
        /// </para>
        /// <para>
        /// It is therefore <b>static rather than read off an instance</b>, and deliberately: a
        /// <b>restored</b> run - one reopened from disk to review its results - carries no
        /// <see cref="PartOSimulationContext"/> at all (<c>PartORun.Restore</c> nulls it on purpose, which
        /// is what keeps a restored run out of Iteration 2B). Asking the instance would leave exactly the
        /// reopened-results path, the one most likely to meet an old or damaged file, with no requirement
        /// stated. It does not need one: a run can only have completed - and so only be restorable - if it
        /// was a full-year simulation, so 1 to 365 is already established for every model that can reach an
        /// assessment.
        /// </para>
        /// </summary>
        public static int HourCount_FullYear => (Day_Last_FullYear - Day_First_FullYear + 1) * 24;

        /// <summary>
        /// The project name for one optimisation iteration - <c>&lt;project&gt;-Opt00</c> for the baseline,
        /// <c>-Opt01</c> onwards for each round.
        /// <para>
        /// Zero padded to two digits so the files sort in the order they were produced in, which is the
        /// order anybody auditing the run will want to read them in.
        /// </para>
        /// </summary>
        public string ProjectName_Iteration(int iteration)
        {
            return string.Format("{0}-Opt{1:00}", ProjectName, iteration);
        }

        /// <summary>
        /// The project name for the diagnostic selected-equipment capacity envelope -
        /// <c>&lt;project&gt;-OptMax</c> for a first optimisation, and
        /// <c>&lt;project&gt;-Opt</c><i>nn</i><c>-Max</c> for one continuing from the design a previous
        /// optimisation left behind.
        /// <para>
        /// <b>Named, not numbered, deliberately.</b> A bare <c>-Opt</c><i>nn</i> suffix would put the
        /// diagnostic in the same sequence as the rounds, where a reader sorting the output directory would
        /// find it beside them and read it as the last and best of them. The <c>Max</c> is what keeps it out
        /// of that sequence, so its TBD and TSD sit beside the rounds' without overwriting any of them and
        /// without pretending to be one.
        /// </para>
        ///
        /// <para><b>Why the iteration baseline is part of it</b></para>
        /// <para>
        /// It used to be <c>&lt;project&gt;-OptMax</c> unconditionally, which made the envelope the one
        /// piece of an optimisation's evidence that a SECOND optimisation destroyed. Every round already
        /// carries the baseline it continues from - <see cref="ProjectName_Iteration(int)"/> is given
        /// <c>iteration_Baseline + iteration</c>, so a second run numbers from <c>-Opt06</c> rather than
        /// starting again at <c>-Opt01</c> - and the envelope was the only case that ignored it. Optimise
        /// <c>Flat1</c>, then optimise again from its result, and the second envelope overwrote the first's
        /// <c>Flat1-OptMax.tbd</c>, <c>.tsd</c>, <c>.sam</c> and <c>-TM59.txt</c>, because every one of
        /// those derives from this name. Now the second is <c>Flat1-Opt05-Max</c>, beside the round it was
        /// measured from rather than on top of the previous session's.
        /// </para>
        /// <para>
        /// <b>The same authority the rounds use, not a second counter.</b> The baseline is whatever
        /// <see cref="Iteration_ProjectName(string)"/> reads off the run being optimised, so the envelope's
        /// identity and its rounds' identities come from one place and cannot drift apart. It follows that
        /// two optimisations started from the SAME round collide here exactly as their rounds already do -
        /// that is a property of the iteration baseline itself, not of this name, and it is the reason a
        /// repeated optimisation continues the numbering rather than restarting it.
        /// </para>
        /// <para>
        /// <b>Iteration 0 keeps the old spelling</b>, so a first optimisation - the ordinary case, and every
        /// run already saved - still writes and reopens <c>&lt;project&gt;-OptMax</c>. Qualifying that name
        /// too would have gained nothing and orphaned the evidence of every historic run.
        /// </para>
        /// <para>
        /// <see cref="Iteration_ProjectName(string)"/> reads either spelling back as 0 - neither
        /// <c>Max</c> nor <c>05-Max</c> parses as a number - so an optimisation started from an envelope
        /// design, which is not a supported thing to do but is a thing a person could reach, would number
        /// from <c>-Opt01</c> and could overwrite a round's evidence. The envelope is therefore never handed
        /// back as a design to continue from; see
        /// <see cref="PartOOptimisationRun.AnalyticalModel_CapacityEnvelope"/>.
        /// </para>
        /// </summary>
        /// <param name="iteration_Baseline">
        /// The optimisation iteration this session started from, as
        /// <see cref="Iteration_ProjectName(string)"/> read it off the baseline run's own results file.
        /// <b>0</b> - the default, and a first optimisation - gives the unqualified
        /// <c>&lt;project&gt;-OptMax</c>.
        /// </param>
        public string ProjectName_CapacityEnvelope(int iteration_Baseline = 0)
        {
            return iteration_Baseline <= 0
                ? string.Format("{0}-OptMax", ProjectName)
                : string.Format("{0}-Opt{1:00}-Max", ProjectName, iteration_Baseline);
        }

        /// <summary>
        /// The optimisation iteration a project name already carries, or <b>0</b> where it carries none -
        /// the inverse of <see cref="ProjectName_Iteration(int)"/>.
        /// <para>
        /// <b>What it is for.</b> An optimisation can be run again from the design a previous one left
        /// behind. Numbering the second run's rounds from 1 would point them at the first run's files and
        /// overwrite the evidence for it - which is the one thing the per-iteration naming exists to
        /// prevent. Reading the iteration back off the name of the run being started from lets the second
        /// optimisation continue the numbering instead, so <c>-Opt11</c> follows <c>-Opt10</c> and nothing
        /// is ever written twice.
        /// </para>
        /// <para>
        /// A name that does not end in the suffix this class writes - the ordinary first optimisation, or a
        /// project a person happened to call something ending in "-Optional" - reads as 0, which starts the
        /// numbering at <c>-Opt01</c>.
        /// </para>
        /// </summary>
        public static int Iteration_ProjectName(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return 0;
            }

            int index = projectName.LastIndexOf("-Opt", System.StringComparison.Ordinal);
            if (index < 0)
            {
                return 0;
            }

            return int.TryParse(projectName.Substring(index + 4), out int result) && result >= 0 ? result : 0;
        }
    }
}
