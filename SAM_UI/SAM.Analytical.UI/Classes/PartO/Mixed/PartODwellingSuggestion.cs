// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What screening suggests for one dwelling, and why - derived, never persisted, and never authority.
    /// <para>
    /// A suggestion changes nothing on its own. Only an explicit <i>Apply suggestions</i>, after its changes have been
    /// shown, writes a suggested strategy into the selection; see <c>Query.PartODwellingSuggestion</c> for the rule.
    /// </para>
    /// </summary>
    public class PartODwellingSuggestion
    {
        public PartODwellingSuggestion(PartOScreeningStrategy partOScreeningStrategy, PartODwellingStrategy partODwellingStrategy, string reason)
        {
            Strategy = partOScreeningStrategy;
            DwellingStrategy = partODwellingStrategy;
            Reason = reason;
        }

        /// <summary>The screening strategy whose PASS the suggestion rests on, or Undefined where there is none.</summary>
        public PartOScreeningStrategy Strategy { get; }

        /// <summary>
        /// The strategy that would be selected by applying the suggestion, or null where there is nothing that can be
        /// applied (nothing passed, nothing was screened, or the passing strategy needs a design edit first).
        /// </summary>
        public PartODwellingStrategy DwellingStrategy { get; }

        /// <summary>One line an engineer reads: what passed, and which project rule narrowed the choice.</summary>
        public string Reason { get; }

        public bool CanApply => DwellingStrategy is not null;

        public string Text => Strategy == PartOScreeningStrategy.Undefined ? "—" : Query.PartOScreeningStrategyLabel(Strategy);

        public override string ToString()
        {
            return string.Format("{0} ({1})", Text, Reason);
        }
    }
}
