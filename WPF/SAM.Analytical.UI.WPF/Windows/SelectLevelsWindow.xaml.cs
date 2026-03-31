using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SelectLevelsWindow.xaml
    /// </summary>
    public partial class SelectLevelsWindow : System.Windows.Window
    {
        public SelectLevelsWindow()
        {
            InitializeComponent();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void SetLevels(IEnumerable<double> levels)
        {
            SelectLevelsControl_Main.SetLevels(levels?.ToList(), false);
            SelectLevelsControl_Main.SetLevels(levels?.ToList(), true);
        }

        public List<double> GetLevels()
        {
            return SelectLevelsControl_Main.GetLevels(true);
        }
    }
}
