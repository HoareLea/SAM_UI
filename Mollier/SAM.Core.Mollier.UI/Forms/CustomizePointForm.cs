using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            customizePointControl.UIMollierPoint = uIMollierPoint;
        }

        public UIMollierPoint UIMollierPoint
        { 
            get
            {
                return customizePointControl.UIMollierPoint;
            }
            set
            {
                customizePointControl.UIMollierPoint = value;
            }
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
