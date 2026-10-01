// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        /// <summary>
        /// The case a Part O output folder names, as SAM's <see cref="PartODerivedCase"/> - what a saved result states about
        /// itself in its <see cref="PartOBaselineReference"/>. Null where the case is not stated (a legacy or resumed run
        /// whose case could not be said), so nothing is guessed.
        /// </summary>
        public static PartODerivedCase? PartODerivedCaseOf(PartOOutputCase? partOOutputCase)
        {
            switch (partOOutputCase)
            {
                case PartOOutputCase.Iteration1a:
                    return PartODerivedCase.Iteration1a;

                case PartOOutputCase.Iteration1b:
                    return PartODerivedCase.Iteration1b;

                case PartOOutputCase.Iteration2:
                    return PartODerivedCase.Iteration2;

                case PartOOutputCase.Iteration2B:
                    return PartODerivedCase.Iteration2B;

                case PartOOutputCase.Iteration3:
                    return PartODerivedCase.Iteration3;

                case PartOOutputCase.MixedDesign:
                    return PartODerivedCase.MixedDesign;

                default:
                    return null;
            }
        }
    }
}
