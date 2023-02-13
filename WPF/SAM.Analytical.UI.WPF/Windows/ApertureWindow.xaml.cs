using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureWindow.xaml
    /// </summary>
    public partial class ApertureWindow : System.Windows.Window
    {
        public ApertureWindow()
        {
            InitializeComponent();
        }

        public ApertureWindow(IEnumerable<Aperture> apertures)
        {
            InitializeComponent();

            Apertures = apertures == null ? null : new List<Aperture>(apertures);
        }

        public List<Aperture> Apertures
        {
            get
            {
                return apertureControl.Apertures;
            }

            set
            {
                apertureControl.Apertures = value;
            }
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
