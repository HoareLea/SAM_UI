using System;

namespace SAM.Core.UI.WPF
{
    public class FilterChangedEventArgs : EventArgs
    {
        public IUIFilter UIFilter { get; } = null;
        
        public FilterChangedEventArgs(IUIFilter uIFilter)
        {
            UIFilter = uIFilter;
        }
    }
}
