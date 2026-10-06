// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The saved preparation that lets Iteration 3 start from a completed Iteration 1a in a later session:
    /// its files sit beside the run's results, it round-trips exactly, and anything unreadable is no sidecar.
    /// </summary>
    public class PartORunResumeTests
    {
        [Fact]
        public void TheSidecar_SitsBesideTheResults()
        {
            Assert.Equal("C:\\out\\Flat1.partorun.json", PartORunResume.Path_Resume("C:\\out\\Flat1.tsd"));
            Assert.Equal("C:\\out\\Flat1.prepared.sam", PartORunResume.Path_PreparedModel("C:\\out\\Flat1.tsd"));
            Assert.Null(PartORunResume.Path_Resume(null));
        }

        [Fact]
        public void TheSidecar_RoundTrips()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + PartORunResume.Suffix_Resume);

            PartORunResume partORunResume = new()
            {
                PartOIteration = PartOIteration.BasePassive,
                SolarCalculationMethod = "TAS",
                SimulateFrom = 1,
                SimulateTo = 365,
                UnmetHours = false,
                Sizing = false,
                UseWidths = true,
                UpdateConstructionLayersByPanelType = true,
                Length_TSD = 123,
                Timestamp_TSD = 456,
                Fingerprint_PreparedModel = "ABC",
            };
            partORunResume.Guids_Zone.Add(Guid.NewGuid());
            partORunResume.Guids_VentilationSystem.Add(Guid.NewGuid());

            try
            {
                File.WriteAllText(path, partORunResume.ToJsonObject().ToJsonString());
                PartORunResume read = PartORunResume.Read(path);

                Assert.NotNull(read);
                Assert.Equal(PartOIteration.BasePassive, read.PartOIteration);
                Assert.Equal(partORunResume.Guids_Zone, read.Guids_Zone);
                Assert.Equal(partORunResume.Guids_VentilationSystem, read.Guids_VentilationSystem);
                Assert.Equal(365, read.SimulateTo);
                Assert.True(read.UseWidths);
                Assert.False(read.Sizing);
                Assert.Equal(123, read.Length_TSD);
                Assert.Equal(456, read.Timestamp_TSD);
                Assert.Equal("ABC", read.Fingerprint_PreparedModel);

                File.WriteAllText(path, "{ \"Schema\": \"PartORunResume:v0\" }");
                Assert.Null(PartORunResume.Read(path));

                File.WriteAllText(path, "not json");
                Assert.Null(PartORunResume.Read(path));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
