using System;

namespace SAM.Core.UI.WPF
{
    public class FilterAddingEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
        public Type Type { get; }
        public IUIFilter UIFilter { get; set; } = null;
        
        public FilterAddingEventArgs(Type type)
        {
            Type = type;
        }
    }
}
