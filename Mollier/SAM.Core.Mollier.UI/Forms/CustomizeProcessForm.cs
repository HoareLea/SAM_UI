using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class CustomizeProcessForm : Form
    {
        UIMollierProcess uIMollierProcess;
        private Color color = Color.Empty;
        private string start_Label;
        private string process_Label;
        private string end_Label;
        public CustomizeProcessForm()
        {
            InitializeComponent();
        }

        public CustomizeProcessForm(UIMollierProcess uIMollierProcess)
        {
            InitializeComponent();
            this.uIMollierProcess = uIMollierProcess;
            customizeProcessControl.InitializeControlElements(uIMollierProcess);
        }

        private void CustomizeForm_Load(object sender, EventArgs e)
        {

        }
        public Color Color => color;
        public string Start_Label => start_Label;
        public string Process_Label => process_Label;
        public string End_Label => end_Label;

        private void OK_Button_Click(object sender, EventArgs e)
        {
            color = customizeProcessControl.Color;
            start_Label = customizeProcessControl.Start_Label;
            process_Label = customizeProcessControl.Process_Label;
            end_Label = customizeProcessControl.End_Label;
            DialogResult = DialogResult.OK;
        }
        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
