// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;
using SAM.Core;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI.WPF
{
    [AssociatedTypes(typeof(Setting)), Description("Analytical Setting Parameter")]
    public enum AnalyticalSettingParameter
    {
        [ParameterProperties("Simulate Options", "Simulate Options"), SAMObjectParameterValue(typeof(SimulateOptions))] SimulateOptions,

        /// <summary>
        /// The Simulate dialog's remembered state for an <b>Approved Document O</b> run, kept apart from
        /// <see cref="SimulateOptions"/> - which is the ordinary Simulate command's.
        /// <para>
        /// <b>Two keys, deliberately.</b> A Part O run states a fixed TAS case: the annual simulation is on,
        /// the range is days 1 to 365, and the export tick boxes are somebody else's deliverables. Writing
        /// that back over the manual command's remembered options would silently retune the expert dialog
        /// every time anybody ran Part O, and reading the manual command's options into Part O is how the
        /// full-year box came to be unticked on the run that needed it most. Neither can happen across two
        /// keys.
        /// </para>
        /// <para>
        /// Only the fields <c>Create.SimulateOptions_PartO</c> documents as carried are ever read back out of
        /// this - the weather, the output directory and the solar method. Everything else is re-derived, so
        /// nothing stored here can go stale into a later run.
        /// </para>
        /// </summary>
        [ParameterProperties("Part O Simulate Options", "Part O Simulate Options"), SAMObjectParameterValue(typeof(SimulateOptions))] SimulateOptions_PartO,
    }
}
