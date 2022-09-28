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
    public partial class CustomizeProcessForm : Form
    {
        UIMollierProcess UI_MollierProcess;
        private Color color = Color.Empty;
        private string start_Label;
        private string process_Label;
        private string end_Label;
        public CustomizeProcessForm()
        {
            InitializeComponent();
        }

        public CustomizeProcessForm(UIMollierProcess UI_MollierProcess)
        {
            InitializeComponent();
            this.UI_MollierProcess = UI_MollierProcess;
        }
        private void CustomizeForm_Load(object sender, EventArgs e)
        {

        }
        public Color Color
        {
            get
            {
                return color;
            }
        }
        public string Start_Label
        {
            get
            {
                return start_Label;
            }
        }

        public string Process_Label
        {
            get
            {
                return process_Label;
            }
        }
        public string End_Label
        {
            get
            {
                return end_Label;
            }
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            color = customizeProcessControl1.Color;
            start_Label = customizeProcessControl1.Start_Label;
            process_Label = customizeProcessControl1.Process_Label;
            end_Label = customizeProcessControl1.End_Label;
            DialogResult = DialogResult.OK;
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
