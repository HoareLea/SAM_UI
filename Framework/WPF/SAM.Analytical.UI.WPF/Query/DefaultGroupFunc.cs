using System;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Func<Space, string> DefaultGroupFunc()
        {
            return new Func<Space, string>( x => { return null; });
        }
    }
}
