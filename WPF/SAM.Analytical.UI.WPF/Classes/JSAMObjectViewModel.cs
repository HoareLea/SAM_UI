using SAM.Core;
using System.Collections.ObjectModel;

namespace SAM.Analytical.UI.WPF
{
    public class JSAMObjectViewModel<TJSAMObject> where TJSAMObject : IJSAMObject
    {
        public ObservableCollection<TJSAMObject> Items { get; set; } = [];

        public JSAMObjectViewModel()
        {

        }
    }
}
