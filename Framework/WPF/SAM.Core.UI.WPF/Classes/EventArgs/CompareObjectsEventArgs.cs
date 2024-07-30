using System;

namespace SAM.Core.UI.WPF
{
    public class CompareObjectsEventArgs : EventArgs
    {
        public object Object_1 { get; }

        public object Object_2 { get; }

        public bool? Equals { get; set; } = null;

        public CompareObjectsEventArgs(object object_1, object object_2)
        {
            Object_1 = object_1;
            Object_2 = object_2;
        }
    }
}
