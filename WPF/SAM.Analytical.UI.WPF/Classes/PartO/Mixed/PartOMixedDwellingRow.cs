// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One dwelling in the mixed Part O design matrix - the ONLY per-dwelling object the window holds.
    ///
    /// <para><b>Scale</b></para>
    /// <para>
    /// A project may have hundreds of dwellings and five thousand spaces, so the matrix is one row per dwelling and a
    /// row is plain text: no per-space object, no control, no model reference. Space-level evidence is summarised
    /// here (counts and the first few failing names) and read in full only on demand. The grid virtualises rows, so
    /// only the visible ones are ever realised as WPF elements.
    /// </para>
    ///
    /// <para><b>Four things, never merged</b></para>
    /// <list type="bullet">
    /// <item><b>Screening</b> - one cell per strategy, from current evidence; stale evidence shows STALE, never its old result.</item>
    /// <item><b>Suggested</b> - derived from current screening and the project constraints; never authority.</item>
    /// <item><b>Selected</b> - the designer's strategy, the only one that is built.</item>
    /// <item><b>Final</b> - the last mixed run's result, shown as current only while that run is current.</item>
    /// </list>
    /// </summary>
    public class PartOMixedDwellingRow : INotifyPropertyChanged
    {
        private readonly Dictionary<PartOScreeningStrategy, string> screening = [];

        private PartODwellingStrategy? selected;
        private PartODwellingSuggestion? suggestion;
        private PartODwellingResult? final;
        private bool final_Current;
        private string? final_RanAs;
        private string? attention;

        internal PartOMixedDwellingRow(Guid guid_Zone, string name, string group, int spaceCount, bool hasDesignTerminals)
        {
            ZoneGuid = guid_Zone;
            Name = name;
            Group = group;
            SpaceCount = spaceCount;
            HasDesignTerminals = hasDesignTerminals;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid ZoneGuid { get; }

        public string Name { get; }

        /// <summary>The zone category, or "Dwellings" - what the matrix groups by.</summary>
        public string Group { get; }

        public int SpaceCount { get; }

        /// <summary>Whether the baseline carries design terminals in this dwelling - what a retained design needs.</summary>
        public bool HasDesignTerminals { get; }

        // ---- Selected -------------------------------------------------------------------------------------------

        /// <summary>The selected strategy, as a copy, or null where none is selected.</summary>
        public PartODwellingStrategy? Selected => selected is null ? null : new PartODwellingStrategy(selected);

        public bool HasSelection => selected is not null;

        public string SelectedText => UI.Query.PartODwellingStrategyText(selected);

        /// <summary>Whether active cooling is on for this dwelling - intent only; its figures are the product's.</summary>
        public bool Cooled => selected?.ActiveCooling == PartOActiveCooling.SupplyAirCooling;

        /// <summary>
        /// The Active cooling cell, as a word: "On" / "Off" for a mechanically ventilated dwelling, "—" where there is no
        /// MVHR supply to cool (natural ventilation, or nothing selected) - unless cooling is recorded there anyway, which
        /// is shown so SAM's refusal of it has something to point at.
        /// </summary>
        public string CoolingText => Cooled ? "On" : selected?.VentilationMode == PartOVentilationMode.MVHR ? "Off" : "—";

        /// <summary>Cooling control room shown in the dwelling matrix; legacy cooled selections stay visibly unconfirmed.</summary>
        public string CoolingControlRoomText { get; internal set; } = "—";

        internal void SetCoolingControlRoomText(string text)
        {
            CoolingControlRoomText = text;
            Changed(nameof(CoolingControlRoomText));
        }

        internal void SetSelected(PartODwellingStrategy? partODwellingStrategy)
        {
            selected = partODwellingStrategy is null ? null : new PartODwellingStrategy(partODwellingStrategy);

            Changed(nameof(Selected), nameof(HasSelection), nameof(SelectedText), nameof(Cooled), nameof(CoolingText), nameof(SuggestionDiffers), nameof(FinalRanAs), nameof(FinalDetail));
        }

        // ---- Screening ------------------------------------------------------------------------------------------

        public string Screening_Natural => Screening(PartOScreeningStrategy.Natural);

        public string Screening_MechanicalBaseline => Screening(PartOScreeningStrategy.MechanicalBaseline);

        public string Screening_SelectedProduct => Screening(PartOScreeningStrategy.SelectedProduct);

        public string Screening_Optimised => Screening(PartOScreeningStrategy.Optimised);

        public string Screening_ActiveCooling => Screening(PartOScreeningStrategy.ActiveCooling);

        public string Screening(PartOScreeningStrategy partOScreeningStrategy)
        {
            return screening.TryGetValue(partOScreeningStrategy, out string? text) ? text : Core.Query.Description(PartODwellingOutcome.NotRun);
        }

        internal void SetScreening(PartOScreeningStrategy partOScreeningStrategy, string text)
        {
            screening[partOScreeningStrategy] = text;
        }

        internal void ScreeningChanged()
        {
            Changed(nameof(Screening_Natural), nameof(Screening_MechanicalBaseline), nameof(Screening_SelectedProduct), nameof(Screening_Optimised), nameof(Screening_ActiveCooling));
        }

        // ---- Suggested ------------------------------------------------------------------------------------------

        public PartODwellingSuggestion? Suggestion => suggestion;

        public string SuggestionText => suggestion?.Text ?? "—";

        public string SuggestionReason => suggestion?.Reason ?? "Not screened.";

        /// <summary>Whether an applicable suggestion differs from the selection - what "Apply suggestions" would change.</summary>
        public bool SuggestionDiffers => suggestion?.DwellingStrategy is PartODwellingStrategy partODwellingStrategy && (selected is null || !SameIntent(selected, partODwellingStrategy));

        internal void SetSuggestion(PartODwellingSuggestion? partODwellingSuggestion)
        {
            suggestion = partODwellingSuggestion;

            Changed(nameof(Suggestion), nameof(SuggestionText), nameof(SuggestionReason), nameof(SuggestionDiffers));
        }

        // ---- Final ----------------------------------------------------------------------------------------------

        public PartODwellingResult? Final => final;

        public bool FinalCurrent => final is not null && final_Current;

        /// <summary>The final outcome where the last mixed run is current; STALE where it is not; "—" where there is none.</summary>
        public string FinalText => final is null ? "—" : final_Current ? Core.Query.Description(final.Outcome) : "STALE";

        public bool FinalFail => FinalCurrent && final!.Outcome == PartODwellingOutcome.Fail;

        public string FailingSpacesText
        {
            get
            {
                if (!FinalCurrent || final!.SpaceCount_Fail == 0)
                {
                    return string.Empty;
                }

                string names = string.Join(", ", final.FailingSpaceNames);
                return final.SpaceCount_Fail > final.FailingSpaceNames.Count
                    ? string.Format("{0} ({1}, and {2} more)", UI.Query.PartOCount(final.SpaceCount_Fail, "space", "spaces"), names, final.SpaceCount_Fail - final.FailingSpaceNames.Count)
                    : string.Format("{0} ({1})", UI.Query.PartOCount(final.SpaceCount_Fail, "space", "spaces"), names);
            }
        }

        /// <summary>What the final run built this dwelling as, where that differs from the current selection.</summary>
        public string FinalRanAs => final_RanAs is not null && final_RanAs != SelectedText ? string.Format("ran as {0}", final_RanAs) : string.Empty;

        public string FinalDetail
        {
            get
            {
                if (final is null)
                {
                    return "No mixed run has assessed this dwelling.";
                }

                string counts = string.Format("{0} pass · {1} fail · {2} not assessed", final.SpaceCount_Pass, final.SpaceCount_Fail, final.SpaceCount_NotAssessed);

                return final_Current
                    ? string.Format("Final mixed run: {0} ({1}).{2}", Core.Query.Description(final.Outcome), counts, FinalRanAs.Length == 0 ? string.Empty : string.Format(" It {0}.", FinalRanAs))
                    : string.Format("The last mixed run no longer describes the design, so it is not this dwelling's result. It was {0} ({1}), simulated as {2}.", Core.Query.Description(final.Outcome), counts, final_RanAs ?? "—");
            }
        }

        internal void SetFinal(PartODwellingResult? partODwellingResult, bool current, string? ranAs)
        {
            final = partODwellingResult;
            final_Current = current;
            final_RanAs = ranAs;

            Changed(nameof(Final), nameof(FinalCurrent), nameof(FinalText), nameof(FinalFail), nameof(FailingSpacesText), nameof(FinalRanAs), nameof(FinalDetail));
        }

        // ---- Attention ------------------------------------------------------------------------------------------

        /// <summary>Why this dwelling needs attention before the mixed design can be built, or empty.</summary>
        public string Attention => attention ?? string.Empty;

        public bool NeedsAttention => !string.IsNullOrEmpty(attention);

        internal void SetAttention(string? text)
        {
            attention = string.IsNullOrWhiteSpace(text) ? null : text;

            Changed(nameof(Attention), nameof(NeedsAttention));
        }

        /// <summary>Whether two strategies state the same intent - SAM's canonical text, which is what the record fingerprints.</summary>
        internal static bool SameIntent(PartODwellingStrategy? x, PartODwellingStrategy? y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }

            PartODwellingStrategy x_Zoned = new(x) { ZoneGuid = Guid.Empty };
            PartODwellingStrategy y_Zoned = new(y) { ZoneGuid = Guid.Empty };

            return x_Zoned.CanonicalText() == y_Zoned.CanonicalText();
        }

        private void Changed(params string[] names)
        {
            PropertyChangedEventHandler? propertyChanged = PropertyChanged;
            if (propertyChanged is null)
            {
                return;
            }

            foreach (string name in names)
            {
                propertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
