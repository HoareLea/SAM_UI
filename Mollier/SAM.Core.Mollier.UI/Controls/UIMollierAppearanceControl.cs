using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierAppearanceControl : UserControl
    {
        public UIMollierAppearanceControl()
        {
            InitializeComponent();
        }

        public UIMollierAppearanceControl(UIMollierAppearance uIMollierAppearance)
        {
            InitializeComponent();

            SetUIMollierAppearance(uIMollierAppearance);
        }

        public UIMollierAppearance UIMollierAppearance
        {
            get
            {
                return GetUIMollierAppearance();
            }

            set
            {
                SetUIMollierAppearance(value);
            }
        }

        private UIMollierAppearance GetUIMollierAppearance()
        {
            return new UIMollierAppearance(Button_Color.BackColor == SystemColors.Control ? Color.Empty : Button_Color.BackColor, TextBox_Label.Text);
        }

        private void SetUIMollierAppearance(UIMollierAppearance uIMollierAppearance)
        {
            if(uIMollierAppearance == null)
            {
                Button_Color.BackColor = SystemColors.Control;
                TextBox_Label.Text = null;
                return;
            }

            Button_Color.BackColor = uIMollierAppearance.Color;
            TextBox_Label.Text = uIMollierAppearance.Label;
        }

        private void Button_Color_Click(object sender, System.EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = Button_Color.BackColor == SystemColors.Control ? Color.Empty : Button_Color.BackColor;
                if(colorDialog.ShowDialog() == DialogResult.OK)
                {
                    Button_Color.BackColor = colorDialog.Color;
                }
            }
        }
    }
}
