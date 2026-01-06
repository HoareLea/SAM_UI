// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI
{
    [AssociatedTypes(typeof(AnalyticalModel)), Description("AnalyticalModel Parameter")]
    public enum AnalyticalModelParameter
    {
        [ParameterProperties("UI Geometry Settings", "UI Geometry Settings"), SAMObjectParameterValue(typeof(Geometry.UI.UIGeometrySettings))] UIGeometrySettings,
    }
}
