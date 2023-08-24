using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class CustomizePointControl : UserControl
    {
        private Color pointColor;
        private string pointLabel;
        public CustomizePointControl()
        {
            InitializeComponent();

        }

        public CustomizePointControl(UIMollierPoint uIMollierPoint)
        {
            InitializeComponent();
            if(uIMollierPoint == null)
            {
                return;
            }
            InitializeControlElements(uIMollierPoint);
        }
        public void InitializeControlElements(UIMollierPoint uIMollierPoint)
        {
            pointLabel = uIMollierPoint.UIMollierAppearance.Label;
            pointColor = uIMollierPoint.UIMollierAppearance.Color;

            PointLabel_TextBox.Text = uIMollierPoint.UIMollierAppearance.Label;
            PointColor_Button.BackColor = pointColor;
        }
        private void PointColor_Button_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                PointColor_Button.BackColor = colorDialog.Color;
                pointColor = PointColor_Button.BackColor;
            }
        }
        private void PointLabel_TextBox_TextChanged(object sender, EventArgs e)
        {
            pointLabel = PointLabel_TextBox.Text;
        }

        public Color Color => pointColor;
        public string PointLabel => pointLabel;
    }
}
