// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static double ShapeSizeFactor(int deviceDpi, double initialFactor)
        {
            Math.LinearEquation linearEquation = Math.Create.LinearEquation(96.0, initialFactor, 120.0, 1.0);

            return linearEquation.Evaluate(deviceDpi);
        }
    }
}
