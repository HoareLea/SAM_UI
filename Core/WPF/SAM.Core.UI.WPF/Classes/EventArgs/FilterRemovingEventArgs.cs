using System;

namespace SAM.Core.UI.WPF
{
    public class FilterRemovingEventArgs : EventArgs
    {
        public IFilterControl FilterControl { get; } = null;
        
        public FilterRemovingEventArgs(IFilterControl filterControl)
        {
            FilterControl = filterControl;
        }
    }
}
