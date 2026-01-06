// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.UI.WPF
{
    public class RangeChangedEventArgs<T> : EventArgs
    {
        private Range<T> range;

        public RangeChangedEventArgs(Range<T> range)
        {
            this.range = range;
        }

        public Range<T> Range
        {
            get
            {
                return range == null ? null : new Range<T>(range);
            }
        }
    }
}
