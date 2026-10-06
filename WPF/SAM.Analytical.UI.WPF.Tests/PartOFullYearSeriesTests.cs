// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using SAM.Weather;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The hour count a Part O assessment requires a room's hourly series to reach, and where that
    /// number is allowed to come from.</b>
    ///
    /// <para><b>The gap this closes</b></para>
    /// <para>
    /// Approved Document O's dynamic method assesses annual and summer criteria, so a verdict produced from
    /// part of a year is not the verdict the document asks for. Part O checked that the simulation's
    /// nominal DATE RANGE was a full year - <c>PartOSimulationContext.IsFullYear</c>, and the
    /// <c>fullYear</c> flag <c>RunPartOSimulation</c> hands back - which states what was asked of TAS and
    /// not what the results file actually contains. A damaged or partially written TSD therefore reached
    /// the assessment, where <c>TMOverheatingCalculator</c> walked whatever hours the two series had and
    /// reported the answer as the room's.
    /// </para>
    ///
    /// <para><b>And the mistake made closing it, which is what most of this fixture is for</b></para>
    /// <para>
    /// The requirement was first counted from the weather year the TSD itself carries. That reads well -
    /// the weather year is already the authority for the comfort band the criteria are measured against -
    /// and it defeats the check completely. A file damaged to a third of its length loses its weather and
    /// its results together, so the requirement falls to match the truncated series, and the partial year
    /// is assessed and reported as an ordinary verdict.
    /// </para>
    /// <para>
    /// <b>A results file may not decide how much of a year it was supposed to contain.</b> The authority
    /// has to be independent of the payload being validated, so it is
    /// <c>PartOSimulationContext.HourCount_FullYear</c> - the requested day range that defines a Part O
    /// full year.
    /// </para>
    /// </summary>
    public class PartOFullYearSeriesTests
    {
        // -----------------------------------------------------------------------------------------------
        // The authority
        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// The requirement is the requested year - days 1 to 365 - derived from the same two days
        /// <c>IsFullYear</c> tests against rather than written down a second time.
        /// </summary>
        [Fact]
        public void TheRequirement_IsTheRequestedFullYear()
        {
            Assert.Equal(8760, PartOSimulationContext.HourCount_FullYear);

            Assert.Equal(
                (PartOSimulationContext.Day_Last_FullYear - PartOSimulationContext.Day_First_FullYear + 1) * 24,
                PartOSimulationContext.HourCount_FullYear);

            //And those are the days IsFullYear accepts, so the requirement and the gate cannot drift apart.
            Assert.True(Context(PartOSimulationContext.Day_First_FullYear, PartOSimulationContext.Day_Last_FullYear).IsFullYear);
            Assert.False(Context(1, 100).IsFullYear);
        }

        /// <summary>
        /// <b>The defect, stated as the thing that must not happen.</b> A TSD whose weather record is
        /// damaged to a third of a year does not get to lower the bar to a third of a year: the requirement
        /// is unmoved by anything the file says about itself.
        /// </summary>
        [Fact]
        public void ADamagedWeatherYear_DoesNotRedefineTheRequirement()
        {
            WeatherYear weatherYear_Damaged = WeatherYear(100);

            //What the file would have been believed to require, had it been asked.
            Assert.Equal(2400, weatherYear_Damaged.GetWeatherHours().Count);

            Assert.Equal(8760, PartOSimulationContext.HourCount_FullYear);
            Assert.NotEqual(weatherYear_Damaged.GetWeatherHours().Count, PartOSimulationContext.HourCount_FullYear);
        }

        /// <summary>
        /// The requirement is stated for a <b>restored</b> run too - the reopened-results path, which is the
        /// one most likely to meet an old or damaged file, and which deliberately carries no
        /// <c>PartOSimulationContext</c> instance at all (<c>PartORun.Restore</c> nulls it, which is what
        /// keeps a restored run out of Iteration 2B). A requirement read off an instance would be absent
        /// exactly there.
        /// </summary>
        [Fact]
        public void TheRequirement_NeedsNoContextInstance()
        {
            Assert.True(PartOSimulationContext.HourCount_FullYear > 0);
        }

        // -----------------------------------------------------------------------------------------------
        // What the requirement then does
        // -----------------------------------------------------------------------------------------------

        /// <summary>Requested full year, both series the full length: assessed.</summary>
        [Fact]
        public void AFullLengthSeries_IsAssessed()
        {
            AnalyticalModel analyticalModel = SeriesModel(WeatherData(365), 8760, 8760);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Empty(tMOverheatingCalculator.HourlySeriesRefusals);
        }

        /// <summary>
        /// <b>The case the weather-derived requirement passed.</b> A results file 2400 hours long - the
        /// length a TSD damaged to 100 days carries - is refused, because the bar is the requested year and
        /// not anything the file says about itself. Under the weather-derived requirement this same series
        /// met a 2400-hour bar and was assessed.
        /// <para>
        /// The weather year here is intact, and deliberately: see
        /// <see cref="ATruncatedWeatherYear_CannotReachTheCalculationAtAll"/> for why the fully damaged file
        /// cannot be driven through the calculator, and why that does not weaken this. What is being pinned
        /// is that the RESULTS length does not set the requirement - which
        /// <see cref="ADamagedWeatherYear_DoesNotRedefineTheRequirement"/> establishes for the weather side.
        /// </para>
        /// </summary>
        [Fact]
        public void AResultsFileTruncatedToItsDamagedLength_IsRefused()
        {
            AnalyticalModel analyticalModel = SeriesModel(WeatherData(365), 2400, 2400);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            string refusal = Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals);
            Assert.Contains("Bedroom 1", refusal);
            Assert.Contains("only 2400 of the 8760", refusal);
        }

        /// <summary>
        /// <b>Characterization.</b> A TSD whose weather record is itself truncated cannot reach the
        /// assessment at all: the comfort band the criteria are measured against is a running mean over the
        /// year, and <c>SAM.Weather.Query.RunningMeanDryBulbTemperatures</c> throws on a short one.
        /// <para>
        /// Pre-existing behaviour in the weather layer, already characterised by
        /// <c>TMOverheatingCalculatorTests.NoWeatherData_ThrowsToday_PreExistingBehaviourNotAContract</c>,
        /// and recorded here because it is what closes the remaining half of this case: a fully damaged file
        /// cannot produce a partial verdict either, it simply cannot be assessed. Throwing is not a good way
        /// to say so and fixing it reaches wider than Part O, which is why it is recorded rather than
        /// asserted as a contract.
        /// </para>
        /// </summary>
        [Fact]
        public void ATruncatedWeatherYear_CannotReachTheCalculationAtAll()
        {
            AnalyticalModel analyticalModel = SeriesModel(WeatherData(100), 2400, 2400);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.ThrowsAny<System.Exception>(() => tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
        }

        /// <summary>Requested full year, weather intact, results truncated: refused.</summary>
        [Fact]
        public void AFullWeatherYearWithTruncatedResults_IsRefused()
        {
            AnalyticalModel analyticalModel = SeriesModel(WeatherData(365), 5000, 5000);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            Assert.Contains("only 5000 of the 8760", Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals));
        }

        /// <summary>
        /// Two series of different lengths are refused whatever the requested year is - the mismatch rule,
        /// which the calculator applies on its own account.
        /// </summary>
        [Fact]
        public void MismatchedSeries_AreRefused()
        {
            AnalyticalModel analyticalModel = SeriesModel(WeatherData(365), 8760, 5000);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            Assert.Contains("different lengths", Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals));
        }

        // -----------------------------------------------------------------------------------------------

        private static PartOSimulationContext Context(int simulateFrom, int simulateTo)
        {
            return new PartOSimulationContext("C:\\Temp", "Flat1", null, SolarCalculationMethod.TAS, simulateFrom, simulateTo);
        }

        /// <summary>A populated weather year of the stated number of days.</summary>
        private static WeatherYear WeatherYear(int days)
        {
            WeatherYear result = new(2018);

            for (int day = 0; day < days; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    result.Add(day, hour, new System.Collections.Generic.Dictionary<string, double> { { WeatherDataType.DryBulbTemperature.ToString(), 20.0 } });
                }
            }

            return result;
        }

        private static WeatherData WeatherData(int days)
        {
            return new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear(days));
        }

        /// <summary>One space carrying the two hourly series at the stated lengths.</summary>
        private static AnalyticalModel SeriesModel(WeatherData weatherData, int count_ResultantTemperature, int count_OccupancySensibleGain)
        {
            Space space = new("Bedroom 1");

            Core.ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");
            parameterSet.Add(Core.Query.Name(SpaceSimulationResultParameter.ResultantTemperature), Values(count_ResultantTemperature, 24.0));
            parameterSet.Add(Core.Query.Name(SpaceSimulationResultParameter.OccupancySensibleGain), Values(count_OccupancySensibleGain, 80.0));
            space.Add(parameterSet);

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(space);

            AnalyticalModel result = new("Flat1", null, null, null, adjacencyCluster);
            result.SetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, weatherData);

            return result;
        }

        private static System.Text.Json.Nodes.JsonArray Values(int count, double value)
        {
            System.Text.Json.Nodes.JsonArray result = [];

            for (int i = 0; i < count; i++)
            {
                result.Add(value);
            }

            return result;
        }

        /// <summary>
        /// A calculator carrying the PRODUCTION requirement - the same value
        /// <c>PartOTM59Assessment.Assess</c> states on it - with an explicit empty <c>TextMap</c>, so the
        /// space resolves to no TM59 application and the criterion selection is deterministic without
        /// depending on a shipped resource file being installed on the machine running the tests.
        /// </summary>
        private static TMOverheatingCalculator Calculator(AnalyticalModel analyticalModel)
        {
            return new TMOverheatingCalculator(analyticalModel)
            {
                TextMap = Core.Create.TextMap("TM59"),
                HourCount_Expected = PartOSimulationContext.HourCount_FullYear,
            };
        }
    }
}
