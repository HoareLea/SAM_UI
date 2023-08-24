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
    public partial class UIMollierPointControl : UserControl
    {
        private UIMollierPoint uIMollierPoint;
        public UIMollierPointControl()
        {
            InitializeComponent();

        }
        public UIMollierPointControl(UIMollierPoint uIMollierPoint)
        {
            InitializeComponent();
            if(uIMollierPoint == null)
            {
                return;
            }
            setUIMollierPoint(uIMollierPoint);
        }

        public UIMollierPoint UIMollierPoint
        {
            get
            {
                if(uIMollierPoint == null || uIMollierPoint.UIMollierAppearance == null)
                {
                    return null;
                }
                uIMollierPoint.UIMollierAppearance.Color = PointColor_Button.BackColor;
                uIMollierPoint.UIMollierAppearance.Label = PointLabel_TextBox.Text;
                return uIMollierPoint;
            }
            set
            {
                setUIMollierPoint(value);
            }
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
            }
        }
        private void setUIMollierPoint(UIMollierPoint uIMollierPoint)
        {
            if(uIMollierPoint == null)
            {
                return;
            }

            this.uIMollierPoint = new UIMollierPoint(uIMollierPoint);

            if(uIMollierPoint.UIMollierAppearance != null)
            {
                PointLabel_TextBox.Text = uIMollierPoint.UIMollierAppearance.Label;
                PointColor_Button.BackColor = uIMollierPoint.UIMollierAppearance.Color;
            }
        }
    }
}
