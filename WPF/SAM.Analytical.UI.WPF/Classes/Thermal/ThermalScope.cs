// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Which elements a thermal change reaches: the shared scope of "Set U-value" and "Set glazing" (and later of the
    /// Thermal Performance panel), free of WPF types so it is unit-testable. It holds the elements that use the
    /// construction, the selected ones among them, the user's choice (<see cref="Scope"/>) and the one builder of the
    /// scope sentence and the choice labels.
    /// <para>
    /// <b>Pinned.</b> The two Guid lists are captured when the scope is created and never change: a selection or a
    /// model that changes afterwards does not change what Apply will touch. Only <see cref="Scope"/> is chosen.
    /// </para>
    /// </summary>
    public sealed class ThermalScope
    {
        private readonly string singular;
        private readonly string plural;

        /// <param name="constructionName">The construction the elements use (shown only; identity is by Guid).</param>
        /// <param name="singular">The element noun, e.g. "panel".</param>
        /// <param name="plural">The element noun in the plural, e.g. "panels".</param>
        /// <param name="usingGuids">Every element that uses the construction.</param>
        /// <param name="selectedGuids">The selected elements; only those using the construction count.</param>
        public ThermalScope(string constructionName, string singular, string plural, IEnumerable<Guid> usingGuids, IEnumerable<Guid> selectedGuids)
        {
            ConstructionName = constructionName;
            this.singular = singular ?? throw new ArgumentNullException(nameof(singular));
            this.plural = plural ?? throw new ArgumentNullException(nameof(plural));

            UsingGuids = (usingGuids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
            HashSet<Guid> using_Set = new HashSet<Guid>(UsingGuids);

            List<Guid> selected_All = (selectedGuids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
            SelectedGuids = selected_All.Where(using_Set.Contains).ToList();
            OtherSelectedCount = selected_All.Count - SelectedGuids.Count;
        }

        public string ConstructionName { get; }

        /// <summary>The elements using the construction, as they were when the scope was created.</summary>
        public IReadOnlyList<Guid> UsingGuids { get; }

        /// <summary>The selected elements that use the construction, as they were when the scope was created.</summary>
        public IReadOnlyList<Guid> SelectedGuids { get; }

        public int UsingCount => UsingGuids.Count;

        public int SelectedCount => SelectedGuids.Count;

        /// <summary>Selected elements that use another construction (not affected).</summary>
        public int OtherSelectedCount { get; }

        /// <summary>The user's choice; <see cref="ThermalApplyScope.AllUsing"/> until changed.</summary>
        public ThermalApplyScope Scope { get; set; } = ThermalApplyScope.AllUsing;

        /// <summary>"Only the selected" can be chosen only while a selected element uses the construction.</summary>
        public bool SelectedAvailable => SelectedCount > 0;

        /// <summary>Why "only the selected" cannot be chosen; null while it can.</summary>
        public string SelectedUnavailableReason => SelectedAvailable
            ? null
            : string.Format(CultureInfo.CurrentCulture, "No selected {0} uses {1}.", singular, ConstructionName);

        /// <summary>The label of the "all" choice, e.g. "All 12 panels using it".</summary>
        public string AllLabel => string.Format(CultureInfo.CurrentCulture, "All {0} {1} using it", UsingCount, Noun(UsingCount));

        /// <summary>The label of the "selected" choice, e.g. "Only the 3 selected".</summary>
        public string SelectedLabel => SelectedAvailable
            ? string.Format(CultureInfo.CurrentCulture, "Only the {0} selected", SelectedCount)
            : "Only the selected";

        /// <summary>The elements the change reaches (and its basis, e.g. for Uw) under <paramref name="scope"/>.</summary>
        public IReadOnlyList<Guid> BasisGuids(ThermalApplyScope scope)
        {
            return scope == ThermalApplyScope.SelectedOnly && SelectedCount != 0 ? SelectedGuids : UsingGuids;
        }

        /// <summary>
        /// The inline scope sentence, e.g. "Applies to 40 panels using SIM_EXT_SLD (3 selected)." or "Applies to 3
        /// selected panels of the 40 using SIM_EXT_SLD.".
        /// </summary>
        /// <param name="scope">The scope that applies.</param>
        /// <param name="dontAssignText">The sentence for <see cref="ThermalApplyScope.DontAssign"/>, which only the caller can word.</param>
        /// <param name="noneUsingText">The sentence when no element uses the construction; null for the ordinary one.</param>
        public string Text(ThermalApplyScope scope, string dontAssignText, string noneUsingText = null)
        {
            switch (scope)
            {
                case ThermalApplyScope.SelectedOnly:
                    return string.Format(CultureInfo.CurrentCulture, "Applies to {0} selected {1} of the {2} using {3}.", SelectedCount, Noun(SelectedCount), UsingCount, ConstructionName);

                case ThermalApplyScope.DontAssign:
                    return dontAssignText;

                default:
                    if (UsingCount == 0 && noneUsingText != null)
                    {
                        return noneUsingText;
                    }

                    string selected = SelectedCount > 0 ? string.Format(CultureInfo.CurrentCulture, " ({0} selected)", SelectedCount) : string.Empty;
                    return string.Format(CultureInfo.CurrentCulture, "Applies to {0} {1} using {2}{3}.", UsingCount, Noun(UsingCount), ConstructionName, selected);
            }
        }

        private string Noun(int count)
        {
            return count == 1 ? singular : plural;
        }
    }
}
