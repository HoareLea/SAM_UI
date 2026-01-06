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
