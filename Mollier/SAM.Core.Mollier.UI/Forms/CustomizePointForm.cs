using SAM.Core.Mollier.UI.Controls;
using SAM.Geometry.Mollier;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class CustomizePointForm : Form
    {
        public CustomizePointForm()
        {
            InitializeComponent();
        }
        public CustomizePointForm(UIMollierPoint uIMollierPoint)
        {
            InitializeComponent();

            if(uIMollierPoint == null)
            {
                return;
            }
            CustomizePointControl_Main.UIMollierPoint = uIMollierPoint;
        }

        public UIMollierPoint UIMollierPoint
        { 
            get
            {
                return CustomizePointControl_Main.UIMollierPoint;
            }
            set
            {
                CustomizePointControl_Main.UIMollierPoint = value;
            }
        }

        public MollierControl MollierControl
        {
            get
            {
                return CustomizePointControl_Main?.MollierControl;
            }

            set
            {
                CustomizePointControl_Main.MollierControl = value;
            }
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }

        private void CustomizePointForm_Load(object sender, EventArgs e)
        {
            CustomizePointControl_Main.MollierPointSelected += CustomizePointControl_Main_MollierPointSelected;
            CustomizePointControl_Main.MollierPointSelecting += CustomizePointControl_Main_MollierPointSelecting;
        }

        private void CustomizePointControl_Main_MollierPointSelecting(object sender, EventArgs e)
        {
            Hide();
        }

        private void CustomizePointControl_Main_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            Show();
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();
        }
    }
}
