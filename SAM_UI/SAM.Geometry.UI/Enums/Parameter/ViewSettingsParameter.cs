// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Geometry.UI
{
    [AssociatedTypes(typeof(ViewSettings)), Description("ViewSettings Parameter")]
    public enum ViewSettingsParameter
    {
        [ParameterProperties("Use Default Name", "Use Default Name"), ParameterValue(Core.ParameterType.Boolean)] UseDefaultName,
        [ParameterProperties("Group", "Group"), ParameterValue(Core.ParameterType.String)] Group,
    }
}
