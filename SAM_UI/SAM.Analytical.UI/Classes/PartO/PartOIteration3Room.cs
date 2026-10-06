// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One comparable room's identity: the design space, its name, and the dwelling it belongs to.
    /// <para>
    /// <b>The name is carried for display and joined on never.</b> Three flats hold three rooms called
    /// "Bedroom 2"; every join in the comparison is on <see cref="Guid_Space"/>.
    /// </para>
    /// </summary>
    public class PartOIteration3Room
    {
        public PartOIteration3Room(Guid guid_Space, string name_Space, Guid guid_Dwelling, string name_Dwelling)
        {
            Guid_Space = guid_Space;
            Name_Space = name_Space;
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
        }

        public Guid Guid_Space { get; }

        public string Name_Space { get; }

        /// <summary>The dwelling zone. <see cref="Guid.Empty"/> where the room is in none.</summary>
        public Guid Guid_Dwelling { get; }

        public string Name_Dwelling { get; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name_Space ?? Guid_Space.ToString(), Name_Dwelling ?? "-");
        }
    }
}
