using SAM.Geometry.Mollier;
using SAM.Geometry.Planar;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierProcessPointControl : UserControl
    {
        private static string locationSetText = "SET";
        private Color color_Empty = Color.Empty;

        private UIMollierProcessPoint uIMollierProcessPoint;

        public event MollierPointSelectingEventHandler MollierPointSelecting;
        public event MollierPointSelectedEventHandler MollierPointSelected;

        private MollierControl mollierControl = null;

        public UIMollierProcessPointControl()
        {
            InitializeComponent();

            if(Button_Color != null)
            {
                color_Empty = Button_Color.BackColor;
            }
        }

        public UIMollierProcessPointControl(UIMollierProcessPoint uIMollierProcessPoint)
        {
            InitializeComponent();

            color_Empty = Button_Color.BackColor;

            SetUIMollierProcessPoint(uIMollierProcessPoint);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIMollierProcessPoint UIMollierProcessPoint
        {
            get
            {
                return GetUIMollierProcessPoint();
            }

            set
            {
                SetUIMollierProcessPoint(value);
            }
        }

        private UIMollierProcessPoint GetUIMollierProcessPoint()
        {
            UIMollierProcessPoint uIMollierProcessPoint = this.uIMollierProcessPoint == null ? null : new UIMollierProcessPoint(this.uIMollierProcessPoint);
            if(uIMollierProcessPoint == null)
            {
                return null;
            }

            UIMollierAppearance uIMollierAppearance = uIMollierProcessPoint?.UIMollierAppearance as UIMollierAppearance;
            if (uIMollierAppearance == null)
            {
                uIMollierAppearance = new UIMollierAppearance();
            }

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierAppearance?.UIMollierLabelAppearance;
            if(uIMollierLabelAppearance == null)
            {
                uIMollierLabelAppearance = new UIMollierLabelAppearance();
            }

            uIMollierLabelAppearance.Text = string.IsNullOrEmpty(TextBox_Label.Text) ? null : TextBox_Label.Text;
            uIMollierLabelAppearance.Color = Button_Color.BackColor == color_Empty ? Color.Empty : Button_Color.BackColor;


            uIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;

            uIMollierProcessPoint.UIMollierAppearance = uIMollierAppearance;

            return uIMollierProcessPoint;
        }

        public string LabelName
        {
            get
            {
                return Label_Name.Text;
            }

            set
            {
                Label_Name.Text = value;
            }
        }

        private void SetUIMollierProcessPoint(UIMollierProcessPoint uIMollierProcessPoint)
        {
            this.uIMollierProcessPoint = uIMollierProcessPoint;
            if(uIMollierProcessPoint == null)
            {
                return;
            }

            UIMollierAppearance uIMollierAppearance = uIMollierProcessPoint?.UIMollierAppearance as UIMollierAppearance;
            if(uIMollierAppearance == null)
            {
                return;
            }

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierAppearance?.UIMollierLabelAppearance;
            if(uIMollierLabelAppearance != null)
            {
                TextBox_Label.Text = uIMollierLabelAppearance.Text;
                if(uIMollierLabelAppearance.Vector2D == null)
                {
                    Button_Vector2D.Text = string.Empty;
                }
                else
                {
                    Button_Vector2D.Text = locationSetText;
                }

                Button_Color.BackColor = uIMollierLabelAppearance.Color == Color.Empty ? color_Empty : uIMollierLabelAppearance.Color;
            }
        }

        private void Button_Vector2D_Clear_Click(object sender, EventArgs e)
        {
            UIMollierAppearance uIMollierAppearance = uIMollierProcessPoint.UIMollierAppearance as UIMollierAppearance;
            if (uIMollierAppearance == null)
            {
                return;
            }

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierAppearance.UIMollierLabelAppearance;
            if (uIMollierLabelAppearance == null)
            {
                return;
            }

            uIMollierLabelAppearance.Vector2D = null;

            uIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;

            uIMollierProcessPoint.UIMollierAppearance = uIMollierAppearance;
        }

        private void Button_Color_Clear_Click(object sender, EventArgs e)
        {
            Button_Color.BackColor = color_Empty;
        }

        private void Button_Vector2D_Click(object sender, EventArgs e)
        {
            MollierPointSelecting?.Invoke(this, EventArgs.Empty);

            mollierControl.MollierPointSelected += MollierControl_MollierPointSelected;
        }

        private void MollierControl_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            MollierPointSelected?.Invoke(this, e);

            mollierControl.MollierPointSelected -= MollierControl_MollierPointSelected;

            UIMollierAppearance uIMollierAppearance = uIMollierProcessPoint.UIMollierAppearance as UIMollierAppearance;

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierAppearance?.UIMollierLabelAppearance;
            if (uIMollierLabelAppearance == null)
            {
                uIMollierLabelAppearance = new UIMollierLabelAppearance();
            }

            Point2D point2D_Selected = Convert.ToSAM(e.MollierPoint, mollierControl.MollierControlSettings.ChartType);
            Point2D point2D = Convert.ToSAM(uIMollierProcessPoint, mollierControl.MollierControlSettings.ChartType);

            uIMollierLabelAppearance.Vector2D = point2D_Selected - point2D;

            uIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;

            uIMollierProcessPoint.UIMollierAppearance = uIMollierAppearance;

            Button_Vector2D.Text = locationSetText;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MollierControl MollierControl
        {
            get
            {
                return mollierControl;
            }

            set
            {
                mollierControl = value;
            }
        }

        private void Button_Color_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                Button_Color.BackColor = colorDialog.Color;
            }
        }
    }
}
