// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF;
using SAM.Weather;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Part O's TM59 assessment asks SAM_Tas for a full-year simulation.</b>
    /// <para>
    /// SAM_Tas#73 measured that TSD answers 8760 hours for ANY simulated day range and pads the days it did not
    /// simulate with -1, so <c>TM59AssessmentCalculator.HourCount_Expected</c> cannot see a part-year file: a
    /// days 1..364 TSD reached the assessment with its last day read as -1 degC. The same PR added an opt-in
    /// check of the file's stated day range, <see cref="TSDConversionSettings.RequireFullYear"/>, default off
    /// because a part-year TSD is legitimate elsewhere. These tests pin that Part O switches it on, that a full
    /// year then assesses as before, that a refused file stops before anything is assessed, and that the
    /// generic default is untouched. The day-range rule itself is SAM_Tas's and is tested there; the
    /// conversion is supplied here so that no TAS COM type is loaded.
    /// </para>
    /// </summary>
    public class PartOTM59FullYearGuardTests
    {
        private const string ZoneGuid = "{6A0B1C2D-3E4F-4A5B-8C6D-7E8F9A0B1C2D}";

        [Fact]
        public void PartO_RequestsFullYearValidation_AndTheTwoTM59Series()
        {
            TSDConversionSettings tSDConversionSettings = PartOTM59Assessment.PartOTSDConversionSettings();

            Assert.True(tSDConversionSettings.RequireFullYear);
            Assert.Equal(new HashSet<SpaceDataType> { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain }, tSDConversionSettings.SpaceDataTypes);
            Assert.True(tSDConversionSettings.ConvertWeaterData);
            Assert.True(tSDConversionSettings.ConvertZones);

            //A fresh instance every call: nothing a caller does to one can change what the next assessment reads.
            Assert.NotSame(tSDConversionSettings, PartOTM59Assessment.PartOTSDConversionSettings());
        }

        [Fact]
        public void AFullYearTsd_IsAssessedNormally_ThroughTheFullYearRequest()
        {
            List<TSDConversionSettings> requested = [];

            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(
                DesignModel(),
                @"C:\TasOut\full.tsd",
                null,
                false,
                null,
                (string path_TSD, TSDConversionSettings tSDConversionSettings, out string? refusal) =>
                {
                    requested.Add(tSDConversionSettings);
                    refusal = null;
                    return SimulatedModel();
                });

            Assert.True(Assert.Single(requested).RequireFullYear);
            Assert.Null(partOTM59Assessment.Refusal);
            Assert.True(partOTM59Assessment.IsAssessed);
            Assert.NotNull(partOTM59Assessment.Result);
        }

        [Fact]
        public void APartYearTsd_IsRefusedBeforeTheAssessment_WithSamTasReason()
        {
            //SAM_Tas's own refusal for a days 1..364 file - the text a real conversion hands back.
            string refusal_SamTas = Analytical.Tas.Query.FullYearRefusal(1, 364);
            Assert.NotNull(refusal_SamTas);

            bool converted = false;

            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(
                DesignModel(),
                @"C:\TasOut\part.tsd",
                null,
                false,
                null,
                (string path_TSD, TSDConversionSettings tSDConversionSettings, out string? refusal) =>
                {
                    //Honoured only because Part O asked for it: without the request this file would convert.
                    if (!tSDConversionSettings.RequireFullYear)
                    {
                        converted = true;
                        refusal = null;
                        return SimulatedModel();
                    }

                    refusal = refusal_SamTas;
                    return null;
                });

            Assert.False(converted);
            Assert.False(partOTM59Assessment.IsAssessed);
            Assert.Null(partOTM59Assessment.Result);
            Assert.Null(partOTM59Assessment.Report);
            Assert.Empty(partOTM59Assessment.SpaceResults ?? []);
            Assert.Contains("days 1..364", partOTM59Assessment.Refusal);
            Assert.Contains(@"C:\TasOut\part.tsd", partOTM59Assessment.Refusal);
        }

        [Fact]
        public void AnUnreadableTsd_StillSaysItCouldNotBeRead()
        {
            PartOTM59Assessment partOTM59Assessment = PartOTM59Assessment.Assess(
                DesignModel(),
                @"C:\TasOut\missing.tsd",
                null,
                false,
                null,
                (string path_TSD, TSDConversionSettings tSDConversionSettings, out string? refusal) =>
                {
                    refusal = null;
                    return null;
                });

            Assert.False(partOTM59Assessment.IsAssessed);
            Assert.Equal(@"The simulation results at 'C:\TasOut\missing.tsd' could not be read.", partOTM59Assessment.Refusal);
        }

        /// <summary>Generic / Grasshopper TSD conversion keeps converting a part year: the default is unchanged.</summary>
        [Fact]
        public void TheGenericDefault_StillAcceptsAPartYear()
        {
            TSDConversionSettings tSDConversionSettings = new();

            Assert.False(tSDConversionSettings.RequireFullYear);
            Assert.False(tSDConversionSettings.ToJsonObject().ContainsKey("RequireFullYear"));

            //Part O's request leaves no trace on a settings object it did not create.
            PartOTM59Assessment.PartOTSDConversionSettings();
            Assert.False(new TSDConversionSettings().RequireFullYear);
        }

        // -----------------------------------------------------------------------------------------------

        private static Space Stamped(string name)
        {
            Space result = new(name);
            result.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, ZoneGuid);
            return result;
        }

        private static AnalyticalModel DesignModel()
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(Stamped("Bedroom 1"));
            return new AnalyticalModel("Flat1", null, null, null, adjacencyCluster, null, null);
        }

        /// <summary>What a full-year TSD converts to: the room, stamped, carrying both series for 8760 hours, and the weather.</summary>
        private static AnalyticalModel SimulatedModel()
        {
            Space space = Stamped("Bedroom 1");

            Core.ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");
            parameterSet.Add(SpaceDataType.ResultantTemperature.Text(), Values(24.0));
            parameterSet.Add(SpaceDataType.OccupantSensibleGain.Text(), Values(80.0));
            space.Add(parameterSet);

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(space);

            AnalyticalModel result = new("Flat1", null, null, null, adjacencyCluster);
            result.SetValue(SAM.Analytical.AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));
            return result;
        }

        private static WeatherYear WeatherYear()
        {
            WeatherYear result = new(2018);
            for (int day = 0; day < 365; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    result.Add(day, hour, new Dictionary<string, double> { { WeatherDataType.DryBulbTemperature.ToString(), 20.0 } });
                }
            }

            return result;
        }

        private static System.Text.Json.Nodes.JsonArray Values(double value)
        {
            System.Text.Json.Nodes.JsonArray result = [];
            for (int i = 0; i < 8760; i++)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
