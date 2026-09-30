// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Which Approved Document O case a run's files belong to - and so which folder beneath the Part O output
    /// root they are written into. See <see cref="PartOOutputPaths"/>.
    /// <para>
    /// <b>Storage only.</b> This names a folder, never an engineering state: nothing decides how a run is
    /// prepared, simulated or assessed from it. It exists because the cases reuse the same file names -
    /// Iteration 1a, 1b and 2 all write <c>&lt;model&gt;.tsd</c> - so a single flat folder let one case
    /// overwrite another's results, including the Iteration 2 results an Iteration 3 run is paired with.
    /// </para>
    /// </summary>
    public enum PartOOutputCase
    {
        /// <summary>Iteration 1a - MVHR design duty, no manufacturer unit.</summary>
        Iteration1a,

        /// <summary>Iteration 1b - natural ventilation.</summary>
        Iteration1b,

        /// <summary>Iteration 2 - MVHR with a manufacturer unit.</summary>
        Iteration2,

        /// <summary>Iteration 2B - ventilation optimisation (every round and the capacity envelope).</summary>
        Iteration2B,

        /// <summary>Iteration 3 - explicit system and cooling assessment.</summary>
        Iteration3,

        /// <summary>Mixed Design - the screening runs and the final mixed run.</summary>
        MixedDesign,
    }
}
