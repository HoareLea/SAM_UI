// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Windows.Data;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A product, as a Part O picker shows it: <c>Query.PartOProductLabel</c> applied to whichever of the
    /// two product types a template happens to be bound to.
    /// <para>
    /// A binding converter rather than <c>DisplayMemberPath</c>, because the string a picker shows is a
    /// presentation decision and <c>VentilationUnitCapacityDescriptor.Label</c> belongs to the analytical
    /// assembly. Nothing is matched, stored or persisted through this - see
    /// <c>Query.PartOProductLabel</c>.
    /// </para>
    /// </summary>
    public class PartOProductLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor => Query.PartOProductLabel(ventilationUnitCapacityDescriptor),
                VentilationUnitReference ventilationUnitReference => Query.PartOProductLabel(ventilationUnitReference),
                _ => value?.ToString() ?? string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //A label is not an identity. Reading a product back out of its display string is exactly the
            //kind of name-matching the equipment work exists to avoid.
            throw new NotSupportedException();
        }
    }
}
