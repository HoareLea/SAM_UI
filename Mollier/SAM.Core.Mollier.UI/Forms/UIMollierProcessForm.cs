using SAM.Core.Mollier.UI.Controls;
using SAM.Geometry.Mollier;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class UIMollierProcessForm : Form
    {
        public UIMollierProcessForm()
        {
            InitializeComponent();
        }

        public UIMollierProcessForm(UIMollierProcess uIMollierProcess)
        {
            InitializeComponent();

            UIMollierProcess = uIMollierProcess;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIMollierProcess UIMollierProcess
        {
            get
            {
                if (UIMollierProcessControl_Main == null)
                {
                    return null;
                }

                return UIMollierProcessControl_Main.UIMollierProcess;
                return null;
            }

            set
            {
                if (UIMollierProcessControl_Main != null)
                {
                    UIMollierProcessControl_Main.UIMollierProcess = value;
                }
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MollierControl MollierControl
        {
            get
            {
                return UIMollierProcessControl_Main.MollierControl;
                return null;
            }

            set
            {
                UIMollierProcessControl_Main.MollierControl = value;
            }
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();
        }

        private void UIMollierProcessForm_Load(object sender, EventArgs e)
        {
            UIMollierProcessControl_Main.MollierPointSelected += UIMollierProcessControl_Main_MollierPointSelected;
            UIMollierProcessControl_Main.MollierPointSelecting += UIMollierProcessControl_Main_MollierPointSelecting;
        }

        private void UIMollierProcessControl_Main_MollierPointSelecting(object sender, EventArgs e)
        {
            Hide();
        }

        private void UIMollierProcessControl_Main_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            Show();
        }

        private void Button_Apply_Click(object sender, EventArgs e)
        {
            UIMollierProcessControl_Main.MollierControl.Apply(this);
        }
    }
}
