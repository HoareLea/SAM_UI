using System.Reflection;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static bool Modal(this Window window)
        {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }
    }
}