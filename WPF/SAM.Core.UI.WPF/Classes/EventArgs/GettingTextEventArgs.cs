using System;

namespace SAM.Core.UI.WPF
{
    public class GettingTextEventArgs : EventArgs
    {
        private object @object;

        public string Text { get; set; } = null;
        
        public GettingTextEventArgs(object @object)
        {
            this.@object = @object;
        }

        public object Object
        {
            get
            {
                return @object;
            }
        }
    }
}
