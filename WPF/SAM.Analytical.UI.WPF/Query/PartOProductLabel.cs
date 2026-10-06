// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// One product's identity, in the words an engineer reads.
        ///
        /// <para><b>Why the UI spells this rather than printing the identity</b></para>
        /// <para>
        /// A project test product's identity is deliberately two fields - the literal manufacturer
        /// <c>Project test</c> and the name the engineer typed - and
        /// <c>VentilationUnitReference.ToString()</c> joins them with a space. A product somebody called
        /// "test" therefore reads as <b>Project test test</b>, which looks like a typo and reads like a
        /// manufacturer nobody has heard of. This states the same two facts unambiguously instead:
        /// <c>test (project test)</c>.
        /// </para>
        ///
        /// <para><b>Presentation only</b></para>
        /// <para>
        /// Nothing here is stored, compared, matched or persisted. The identity a pool holds and an
        /// assignment writes is untouched - see <c>VentilationUnitReference.Matches</c>, which this never
        /// participates in. A manufacturer product is spelled exactly as it always was.
        /// </para>
        /// </summary>
        internal static string PartOProductLabel(VentilationUnitReference? ventilationUnitReference)
        {
            if (ventilationUnitReference is null)
            {
                return "-";
            }

            if (!IsPartOProjectTest(ventilationUnitReference))
            {
                return ventilationUnitReference.ToString();
            }

            string name = string.IsNullOrWhiteSpace(ventilationUnitReference.Model)
                ? "unnamed"
                : ventilationUnitReference.Model;

            return string.IsNullOrWhiteSpace(ventilationUnitReference.Reference)
                ? string.Format("{0} (project test)", name)
                : string.Format("{0} ({1}) (project test)", name, ventilationUnitReference.Reference);
        }

        /// <summary>
        /// One product's identity and both of its maximum airflows, in the words an engineer reads - what a
        /// picker shows.
        /// <para>
        /// The same figures <c>VentilationUnitCapacityDescriptor.Label</c> states, in the same order and
        /// with the same separator; only the identity half is spelled by
        /// <see cref="PartOProductLabel(VentilationUnitReference?)"/>. The catalogue's internal rank is
        /// absent here exactly as it is absent there - it is a tie-breaker and not a rating.
        /// </para>
        /// </summary>
        internal static string PartOProductLabel(VentilationUnitCapacityDescriptor? ventilationUnitCapacityDescriptor)
        {
            if (ventilationUnitCapacityDescriptor is null)
            {
                return "-";
            }

            return string.Format(
                "{0} — {1:0.###} / {2:0.###} l/s",
                PartOProductLabel(ventilationUnitCapacityDescriptor.VentilationUnitReference),
                ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps,
                ventilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps);
        }

        /// <summary>
        /// Whether an identity is the project's own test product rather than a manufacturer catalogue entry
        /// - read off the one field that says so, <c>PartOProjectTestVentilationUnit.Manufacturer</c>.
        /// </summary>
        internal static bool IsPartOProjectTest(VentilationUnitReference? ventilationUnitReference)
        {
            return ventilationUnitReference is not null
                && string.Equals(ventilationUnitReference.Manufacturer?.Trim(), Analytical.PartOProjectTestVentilationUnit.Manufacturer, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
