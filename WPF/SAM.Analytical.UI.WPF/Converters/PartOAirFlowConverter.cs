// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Windows.Data;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// An airflow, as a Part O table cell shows it - and an absence shown as an absence.
    ///
    /// <para><b>Why this exists</b></para>
    /// <para>
    /// A space that was never sized carries no Approved Document F requirement, and the row states that as
    /// <see cref="double.NaN"/> rather than as a zero - deliberately, because zero is a rate and "nothing
    /// was stated" is not. <c>StringFormat=N1</c> renders that as <b>NaN</b>, which reads to an engineer
    /// as a broken calculation rather than as a space with no requirement. This renders it as an em dash,
    /// the same character <c>PartOSpaceRow.Unresolved</c> already uses for an unresolved dwelling.
    /// </para>
    ///
    /// <para><b>Display only, one direction only</b></para>
    /// <para>
    /// Nothing is rounded, clamped, substituted or converted between units on the way to the model: these
    /// columns are read-only, <see cref="ConvertBack"/> is not implemented, and the underlying value stays
    /// exactly what its authority produced. Only what the cell paints changes.
    /// </para>
    ///
    /// <para><b>The export reads it too</b></para>
    /// <para>
    /// <see cref="Text(double?, int)"/> is the whole of the formatting, and Copy All calls it directly.
    /// Held in one place because the two got out of step immediately once they were separate: the grid
    /// painted an em dash while the pasted report still said <c>NaN</c>, up to four times per equipment
    /// row, in a table whose entire purpose is to reproduce what was reviewed.
    /// </para>
    /// </summary>
    public class PartOAirFlowConverter : IValueConverter
    {
        /// <summary>The em dash a missing figure is shown as. Never a blank, and never a zero.</summary>
        public const string Unresolved = "—";

        /// <summary>How many decimals a stated figure is shown to. Matches the tables' existing N1.</summary>
        public int Decimals { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? value_Lps = value switch
            {
                double item => item,
                float item => item,
                int item => item,
                _ => null,
            };

            return Text(value_Lps, Decimals, culture);
        }

        /// <summary>
        /// One airflow as it is shown - the figure, or <see cref="Unresolved"/> where there is none.
        /// <para>
        /// The single implementation, so a cell and the Copy All export cannot render the same absent value
        /// differently. Static because the export has no converter instance and should not need one.
        /// </para>
        /// </summary>
        /// <param name="value_Lps">The airflow, or null / NaN / infinity where there is none.</param>
        /// <param name="decimals">How many decimals a stated figure is shown to.</param>
        /// <param name="culture">The culture to format in. Null uses the current one.</param>
        public static string Text(double? value_Lps, int decimals = 1, CultureInfo culture = null)
        {
            if (value_Lps is null || double.IsNaN(value_Lps.Value) || double.IsInfinity(value_Lps.Value))
            {
                return Unresolved;
            }

            return value_Lps.Value.ToString(string.Format("N{0}", decimals < 0 ? 0 : decimals), culture ?? CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //These columns are read-only. A parse here would be a second, weaker write path onto values
            //whose only authority is the preparation that produced them.
            throw new NotSupportedException();
        }
    }
}
