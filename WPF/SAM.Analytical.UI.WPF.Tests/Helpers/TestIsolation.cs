// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// What every test of this assembly shares: the process's "My glazing systems" (<see cref="UserGlazingLibrary.Shared"/>, which every panel
    /// without an injected library reads) points at a file that never exists, so a user library on the machine running the tests never adds
    /// candidates to them. Tests of the user library inject their own, on a temporary file.
    /// </summary>
    internal static class TestIsolation
    {
#pragma warning disable CA2255 // A module initializer in a test assembly is the intended use here.
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void Initialize()
        {
            UserGlazingLibrary.Shared = new UserGlazingLibrary(Path.Combine(Path.GetTempPath(), "SAM-E0-2-tests", Guid.NewGuid().ToString("N"), "Glazing Systems.json"));
        }
    }
}
