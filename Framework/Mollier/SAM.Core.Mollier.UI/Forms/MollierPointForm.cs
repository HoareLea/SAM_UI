using SAM.Geometry.Mollier;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierPointForm : Form
    {
        public event EventHandler SelectPointClicked;
        
        public MollierPointForm()
        {
            InitializeComponent();
            MollierPointControl_Main.SelectMollierPoint += MollierPointControl_Main_SelectPointClicked;
        }

        private void MollierPointControl_Main_SelectPointClicked(object sender, EventArgs e)
        {
            SelectPointClicked?.Invoke(this, e);
        }

        public UIMollierPoint UIMollierPoint
        {
            get
            {
                UIMollierAppearance uIMollierAppearance = UIMollierAppearance;
                if(uIMollierAppearance == null)
                {
                    return new UIMollierPoint(MollierPoint, null);
                }
                
                if (uIMollierAppearance is UIMollierPointAppearance)
                {
                    return new UIMollierPoint(MollierPoint, (UIMollierPointAppearance)uIMollierAppearance);
                }

                if (uIMollierAppearance is UIMollierAppearance)
                {
                    return new UIMollierPoint(MollierPoint, new UIMollierPointAppearance(uIMollierAppearance));
                }

                return null;

            }

            set
            {
                MollierPoint = value;
                UIMollierAppearance = value?.UIMollierAppearance as UIMollierAppearance;
            }
        }

        public MollierPoint MollierPoint
        {
            get
            {
                return MollierPointControl_Main.MollierPoint;
            }
            set
            {
                MollierPointControl_Main.MollierPoint = value;
            }
        }

        public UIMollierAppearance UIMollierAppearance
        {
            get
            {
                return UIMollierAppearanceControl_Main.UIMollierAppearance;
            }

            set
            {
                UIMollierAppearanceControl_Main.UIMollierAppearance = value;
            }
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
