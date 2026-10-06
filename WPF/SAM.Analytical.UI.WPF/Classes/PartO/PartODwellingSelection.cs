// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The dwelling scope of one Prepare Part O Iteration dialog: which of the eligible dwelling zones are
    /// selected, and the search text that narrows the visible list.
    /// <para>
    /// <b>The discovery is not here.</b> Which zones are dwellings is <c>Query.PartFDwellingZones</c>'
    /// decision, made once by the window before this is built; this class only holds the result of that
    /// decision and the user's selection over it. There is no second dwelling rule to drift apart.
    /// </para>
    /// <para>
    /// <b>Identity is the zone's <see cref="Guid"/>, never its name.</b> Two dwellings may legitimately
    /// share a display name; selecting one must not select the other, and the scope handed to
    /// <c>Modify.PreparePartOIteration</c> is resolved from the selected items' guids.
    /// </para>
    /// <para>
    /// <b>Built for a large model.</b> One lightweight <see cref="Item"/> per dwelling zone - never one per
    /// space, and nothing here touches the analytical model after construction. Search matches against the
    /// already-discovered names, and Select All / None flip flags on the same records; neither rescans
    /// anything. The list is handed to the view once and filtered in place, so checking one dwelling or
    /// typing one character costs a pass over the dwelling records and nothing more.
    /// </para>
    /// <para>
    /// <b>Two notifications, because they mean two different things.</b>
    /// <see cref="SelectionChanged"/> says the selected SCOPE moved and a listener may answer it with work
    /// proportional to the model; <see cref="SearchTextChanged"/> says only what is VISIBLE moved. Neither
    /// is raised per row - a Select All, a None or a <see cref="RestoreSelection(IEnumerable{Guid})"/> over
    /// hundreds of dwellings is one notification.
    /// </para>
    /// </summary>
    public class PartODwellingSelection
    {
        /// <summary>
        /// One eligible dwelling zone as a selectable row: its identity, its display name and whether it is
        /// selected. Notifies on selection change so the bound checkbox and the code-driven Select All /
        /// None see the same state.
        /// </summary>
        public class Item : INotifyPropertyChanged
        {
            private bool selected = true;

            internal Item(Zone zone)
            {
                Zone = zone;
                Guid = zone.Guid;
                Name = zone.Name;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>The dwelling zone this row selects.</summary>
            public Zone Zone { get; }

            /// <summary>The zone's identity - the selection authority, never the name.</summary>
            public Guid Guid { get; }

            /// <summary>What the row shows. Display only: two dwellings may share it.</summary>
            public string Name { get; }

            /// <summary>Whether this dwelling is in the scope the iteration is prepared over. Selected by default.</summary>
            public bool IsSelected
            {
                get => selected;
                set
                {
                    if (selected == value)
                    {
                        return;
                    }

                    selected = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        private readonly List<Item> items = [];

        private string searchText = string.Empty;

        //Raised once per user gesture, not once per row: a Select All over a large block flips every
        //matching row's flag, and notifying per row would make one click cost a pass per row. Every bulk
        //operation on this class - Select All / None and the restore of a saved scope - mutates under this
        //guard and raises exactly one notification afterwards.
        private bool changing = false;

        /// <param name="zones_Dwelling">
        /// The eligible dwelling zones, exactly as <c>Query.PartFDwellingZones</c> returned them. Every one
        /// starts selected: the default scope is the whole eligible set, and narrowing it is a deliberate act.
        /// </param>
        public PartODwellingSelection(IEnumerable<Zone> zones_Dwelling)
        {
            foreach (Zone zone in zones_Dwelling ?? [])
            {
                if (zone is null)
                {
                    continue;
                }

                Item item = new(zone);
                item.PropertyChanged += (s, e) =>
                {
                    if (!changing)
                    {
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                };

                items.Add(item);
            }

            //A stable display order that does not depend on the model's internal ordering: name first, and
            //the guid as the tie-break so two same-named dwellings still have a defined sequence.
            items.Sort((x, y) =>
            {
                int comparison = string.CompareOrdinal(x.Name, y.Name);
                return comparison != 0 ? comparison : x.Guid.CompareTo(y.Guid);
            });
        }

        /// <summary>
        /// Raised whenever the SELECTED SCOPE changes - one item, or many through Select All / None or
        /// <see cref="RestoreSelection(IEnumerable{Guid})"/>.
        /// <para>
        /// <b>This means the scope changed, and nothing else.</b> A listener may legitimately answer it with
        /// work proportional to the model - the Part O workflow dialog re-inspects the analytical model on
        /// it - so nothing that leaves every dwelling's <see cref="Item.IsSelected"/> exactly as it was may
        /// raise it. Search text in particular does not: it narrows the view, and it has
        /// <see cref="SearchTextChanged"/> of its own.
        /// </para>
        /// <para>
        /// Raised at most once per gesture. A bulk operation over hundreds of dwellings is one notification,
        /// not one per row.
        /// </para>
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Raised when <see cref="SearchText"/> changes - what is VISIBLE moved, and no dwelling's selected
        /// state did.
        /// <para>
        /// Kept apart from <see cref="SelectionChanged"/> on purpose. A listener answers this by refreshing
        /// the filtered view and whatever line describes the search; answering it with work proportional to
        /// the model would put that work behind every keystroke for a scope that did not change.
        /// </para>
        /// </summary>
        public event EventHandler SearchTextChanged;

        /// <summary>Every eligible dwelling, in display order. The window's list binds to this once.</summary>
        public IReadOnlyList<Item> Items => items;

        /// <summary>How many dwellings are eligible.</summary>
        public int Count => items.Count;

        /// <summary>How many are currently selected.</summary>
        public int SelectedCount
        {
            get
            {
                int result = 0;
                foreach (Item item in items)
                {
                    if (item.IsSelected)
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The current search text. Matching is a case-insensitive substring over the already-discovered
        /// dwelling names; an empty text matches everything.
        /// </summary>
        public string SearchText
        {
            get => searchText;
            set
            {
                string text = value ?? string.Empty;
                if (string.Equals(searchText, text, StringComparison.Ordinal))
                {
                    return;
                }

                searchText = text;

                //NOT a selection change: every dwelling keeps the state it had, and a dwelling filtered out
                //of sight reappears with it intact. Announced as what it is, so a listener that re-inspects
                //the analytical model on a scope change does not do it on a keystroke.
                SearchTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>The selected dwelling zones - the scope the iteration is prepared over.</summary>
        public List<Zone> SelectedZones()
        {
            List<Zone> result = [];

            foreach (Item item in items)
            {
                if (item.IsSelected)
                {
                    result.Add(item.Zone);
                }
            }

            return result;
        }

        /// <summary>Whether the row is matched by the current search text - the view's filter predicate.</summary>
        public bool IsVisible(Item item)
        {
            return item is not null
                && (string.IsNullOrWhiteSpace(searchText) || (item.Name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        /// <summary>
        /// Selects or clears every dwelling the current search matches - the whole eligible set when the
        /// search is empty, so a block of hundreds of dwellings can be narrowed by name and then taken or
        /// dropped as a group.
        /// </summary>
        public void SetSelected(bool selected)
        {
            changing = true;

            try
            {
                foreach (Item item in items)
                {
                    if (!IsVisible(item))
                    {
                        continue;
                    }

                    item.IsSelected = selected;
                }
            }
            finally
            {
                changing = false;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Restores a previously recorded scope: every dwelling whose guid is in <paramref name="guids"/> is
        /// selected and every other one is cleared, as ONE selection change.
        /// <para>
        /// <b>By guid, never by name</b>, exactly as every other selection on this class - two dwellings may
        /// share a display name and restoring one must not restore the other. A guid that is no longer an
        /// eligible dwelling is simply not found: the model may have been re-zoned since the scope was
        /// recorded.
        /// </para>
        /// <para>
        /// <b>Over every dwelling, not only the visible ones.</b> Unlike <see cref="SetSelected(bool)"/> -
        /// which is a gesture on what the search matches - this states the whole scope, so a search left in
        /// the box cannot silently narrow what a restore is allowed to touch.
        /// </para>
        /// <para>
        /// <b>One notification.</b> The rows are mutated under the same guard Select All / None uses, so a
        /// listener that answers <see cref="SelectionChanged"/> with a full inspection of the analytical
        /// model does it once - not once per flipped dwelling. Each row still raises its own
        /// <see cref="INotifyPropertyChanged"/>, so the bound checkboxes update.
        /// </para>
        /// </summary>
        public void RestoreSelection(IEnumerable<Guid> guids)
        {
            HashSet<Guid> guids_Selected = [.. guids ?? []];

            changing = true;

            try
            {
                foreach (Item item in items)
                {
                    item.IsSelected = guids_Selected.Contains(item.Guid);
                }
            }
            finally
            {
                changing = false;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
