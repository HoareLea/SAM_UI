using System;

namespace SAM.Core.UI.WPF
{
    public class GettingCategoryEventArgs : EventArgs
    {
        private object @object;

        public Category Category { get; set; } = null;
        
        public GettingCategoryEventArgs(object @object)
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
