using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ColorGradientControl.xaml
    /// </summary>
    public partial class ColorGradientControl : System.Windows.Controls.UserControl
    {
        public ColorGradientControl()
        {
            InitializeComponent();
        }

        public bool HasMid
        {
            get
            {
                return CheckBox_Mid.IsChecked != null && CheckBox_Mid.IsChecked.Value;
            }

            set
            {
                CheckBox_Mid.IsChecked = value;
                UpdateHasMid();
            }
        }

        public System.Drawing.Color High
        {
            get
            {
                System.Windows.Media.Color color = (Button_High.Background as SolidColorBrush).Color;
                return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }

            set
            {
                Button_High.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B));
            }
        }

        public System.Drawing.Color Low
        {
            get
            {
                System.Windows.Media.Color color = (Button_Low.Background as SolidColorBrush).Color;
                return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }

            set
            {
                Button_Low.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B));
            }
        }

        public System.Drawing.Color Mid
        {
            get
            {
                System.Windows.Media.Color color = (Button_Mid.Background as SolidColorBrush).Color;
                return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }

            set
            {
                Button_Mid.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B));
            }
        }

        private void Button_High_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Button_High.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
        }

        private void Button_Low_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if(colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Button_Low.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
        }

        private void Button_Mid_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Button_Mid.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
        }
        
        private void CheckBox_Mid_Click(object sender, RoutedEventArgs e)
        {
            UpdateHasMid();
        }

        private void UpdateHasMid()
        {
            bool hasMid = HasMid;

            Button_Mid.IsEnabled = hasMid;
            Label_Mid.IsEnabled = hasMid;
        }

        private void Label_Loaded(object sender, RoutedEventArgs e)
        {
            HasMid = false;
        }
    }
}
