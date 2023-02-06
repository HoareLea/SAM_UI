using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for InternalConditionWindow.xaml
    /// </summary>
    public partial class InternalConditionWindow : System.Windows.Window
    {
        private UIAnalyticalModel uIAnalyticalModel;

        public InternalConditionWindow(UIAnalyticalModel uIAnalyticalModel)
        {
            InitializeComponent();

            this.uIAnalyticalModel = uIAnalyticalModel;

            listBoxControl.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;

            Load();
        }

        private void Load()
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            List<InternalConditionData> internalConditionDatas = analyticalModel.GetSpaces()?.ToList().ConvertAll(x => new InternalConditionData(x));

            listBoxControl.SelectionChanged += ListBoxControl_SelectionChanged;
            listBoxControl.SetValues(internalConditionDatas, x => x?.Space?.Name);
            listBoxControl.SelectAll();
        }

        private void ListBoxControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            internalConditionControl.InternalConditionDatas =  listBoxControl.GetValues<InternalConditionData>(true);
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
