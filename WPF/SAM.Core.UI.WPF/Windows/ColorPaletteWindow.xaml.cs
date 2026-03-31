using System.Collections.Generic;
using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ColorPaletteWindow.xaml
    /// </summary>
    public partial class ColorPaletteWindow : Window
    {
        public ColorPaletteWindow()
        {
            InitializeComponent();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public List<System.Drawing.Color> Colors
        {
            get
            {
                return ColorPaletteControl_Main.Colors;
            }

            set
            {
                ColorPaletteControl_Main.Colors = value;
            }
        }
    }
}
