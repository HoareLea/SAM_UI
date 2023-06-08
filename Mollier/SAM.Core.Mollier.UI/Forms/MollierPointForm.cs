using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierPointForm : Form
    {
        private MollierPoint mollierPoint;
        public event EventHandler SelectPointClicked;
        public MollierPointForm()
        {
            InitializeComponent();
            mollierPointControl1.SelectMollierPoint += MollierPointControl1_SelectPointClicked;
        }

        private void MollierPointControl1_SelectPointClicked(object sender, EventArgs e)
        {
            SelectPointClicked?.Invoke(this, e);
        }

        public MollierPoint MollierPoint
        {
            get
            {
                return mollierPoint;
            }
            set
            {
                mollierPoint = value;
                mollierPointControl1.MollierPoint = value;
            }
        }
        private void Button_OK_Click(object sender, EventArgs e)
        {
            mollierPoint = mollierPointControl1.MollierPoint;
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
