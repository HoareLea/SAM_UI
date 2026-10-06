// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The three inputs of an Approved Document O simulation a person genuinely owns - the weather, the output
    /// folder and the solar calculation method - as the Part O Hub's Simulation case holds them.
    /// <para>
    /// Everything else about the TAS case is fixed by <see cref="Create.SimulateOptions_PartO"/>, exactly as
    /// the locked Simulate dialog fixed it; see <see cref="Modify.SimulatePartO"/>.
    /// </para>
    /// </summary>
    public class PartOSimulationCase
    {
        public WeatherData WeatherData { get; set; }

        public string OutputDirectory { get; set; }

        public SolarCalculationMethod SolarCalculationMethod { get; set; } = SolarCalculationMethod.TAS;

        /// <summary>
        /// The case the Simulate dialog would have opened with: the model's own weather, the remembered output
        /// folder where it still exists (else the model's folder), and the remembered solar method.
        /// </summary>
        public static PartOSimulationCase Create(AnalyticalModel analyticalModel, string path_Model, SimulateOptions simulateOptions_Remembered)
        {
            SimulateOptions simulateOptions = WPF.Create.SimulateOptions_PartO(analyticalModel, path_Model, simulateOptions_Remembered);

            if (simulateOptions is null)
            {
                return new PartOSimulationCase();
            }

            return new PartOSimulationCase
            {
                WeatherData = simulateOptions.WeatherData,
                OutputDirectory = simulateOptions.OutputDirectory,
                SolarCalculationMethod = simulateOptions.SolarCalculationMethod,
            };
        }

        /// <summary>Why a simulation cannot start from this case, or null - the dialog's own OK checks.</summary>
        public string Refusal()
        {
            if (WeatherData is null)
            {
                return "Choose the weather data for the simulation.";
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                return "Choose the output folder for the simulation.";
            }

            if (SolarCalculationMethod == SolarCalculationMethod.Undefined)
            {
                return "Choose the solar calculation method.";
            }

            return null;
        }
    }

    /// <summary>What one guided Part O simulation did - what the Simulate path used to say in message boxes.</summary>
    public class PartOSimulationOutcome
    {
        /// <summary>Whether anything was run at all.</summary>
        public bool Ran { get; set; }

        /// <summary>Whether it completed the Part O run - the one outcome the Hub shows inline, with no dialog.</summary>
        public bool Completed { get; set; }

        public bool Cancelled { get; set; }

        public TimeSpan Elapsed { get; set; }

        /// <summary>The dialog path's own closing message, verbatim.</summary>
        public string Message { get; set; }

        /// <summary>Zone-identity and other notes the run raised - still shown, because a person has to read them.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Why a prepared run was not completed by this simulation, or null.</summary>
        public string Note_PartORun { get; set; }

        /// <summary>A refusal that stopped the run or its completion, or null.</summary>
        public string Refusal { get; set; }

        /// <summary>Whether anything in this outcome needs a person's attention beyond an inline line.</summary>
        public bool NeedsAttention => Refusal is not null || Note_PartORun is not null || Notes.Count != 0;
    }
}
