using SAM.Analytical.Classes;
using System.Collections.ObjectModel;

namespace SAM.Analytical.UI.WPF
{
    public class CreateCaseViewModel<TCase> where TCase: Case
    {
        public ObservableCollection<TCase> Items { get; set; } = [];

        public CreateCaseViewModel()
        {

        }
    }
}
