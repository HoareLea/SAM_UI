using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierPointControl : UserControl
    {
        private Color color_Empty = Color.Empty;

        private UIMollierPoint uIMollierPoint;
        public UIMollierPointControl()
        {
            InitializeComponent();

            color_Empty = PointColor_Button.BackColor;

        }
        public UIMollierPointControl(UIMollierPoint uIMollierPoint)
        {
            InitializeComponent();

            color_Empty = PointColor_Button.BackColor;

            if (uIMollierPoint != null)
            {
                setUIMollierPoint(uIMollierPoint);
            }
        }

        public UIMollierPoint UIMollierPoint
        {
            get
            {
                if(uIMollierPoint == null || uIMollierPoint.UIMollierAppearance == null)
                {
                    return null;
                }

                uIMollierPoint.UIMollierAppearance.Color = PointColor_Button.BackColor == color_Empty ? Color.Empty : PointColor_Button.BackColor;
                uIMollierPoint.UIMollierAppearance.Label = PointLabel_TextBox.Text;

                UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierPoint.UIMollierAppearance.UIMollierLabelAppearance;

                if (LabelColor_Button.BackColor == color_Empty)
                {
                    if (uIMollierLabelAppearance != null)
                    {
                        uIMollierLabelAppearance.Color = Color.Empty;
                    }
                }
                else
                {
                    if (uIMollierLabelAppearance == null)
                    {
                        uIMollierLabelAppearance = new UIMollierLabelAppearance();
                    }

                    uIMollierLabelAppearance.Color = LabelColor_Button.BackColor;
                    uIMollierPoint.UIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;
                }

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

            this.uIMollierPoint = uIMollierPoint;

            if (uIMollierPoint.UIMollierAppearance != null)
            {
                PointLabel_TextBox.Text = uIMollierPoint.UIMollierAppearance.Label;
                PointColor_Button.BackColor = uIMollierPoint.UIMollierAppearance.Color == Color.Empty ? color_Empty : uIMollierPoint.UIMollierAppearance.Color;
                if (uIMollierPoint.UIMollierAppearance.UIMollierLabelAppearance != null)
                {
                    UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierPoint.UIMollierAppearance.UIMollierLabelAppearance;

                    LabelColor_Button.BackColor = uIMollierLabelAppearance.Color == Color.Empty ? color_Empty : uIMollierLabelAppearance.Color;
                }
                else
                {
                    LabelColor_Button.BackColor = color_Empty;
                }
            }
        }

        private void LabelColor_Button_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                LabelColor_Button.BackColor = colorDialog.Color;
            }
        }

        private void Button_PointClear_Click(object sender, EventArgs e)
        {
            PointColor_Button.BackColor = color_Empty;
        }

        private void Button_LabelClear_Click(object sender, EventArgs e)
        {
            LabelColor_Button.BackColor = color_Empty;
        }
    }
}
