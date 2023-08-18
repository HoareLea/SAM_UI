using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class CustomizeProcessControl : UserControl
    {
        private Color color = Color.Empty;
        private string start_Label;
        private string process_Label;
        private string end_Label;


        public CustomizeProcessControl()
        {
            InitializeComponent();
        }
        public CustomizeProcessControl(UIMollierProcess UI_MollierProcess)
        {
            InitializeComponent();
            if(UI_MollierProcess == null)
            {
                return;
            }
            InitializeControlElements(UI_MollierProcess);
        }

        private void ProcessColor_button_Click(object sender, EventArgs e)
        {
            using(ColorDialog colorDialog = new ColorDialog())
            {
                if(colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                ProcessColor_button.BackColor = colorDialog.Color;
                color = ProcessColor_button.BackColor;
            }
        }


        public void InitializeControlElements(UIMollierProcess uIMollierProcess)
        {
            color = uIMollierProcess.UIMollierAppearance.Color;
            start_Label = uIMollierProcess.UIMollierAppearance_Start.Label;
            process_Label = uIMollierProcess.UIMollierAppearance.Label;
            end_Label = uIMollierProcess.UIMollierAppearance_End.Label;

            ProcessColor_button.BackColor = color;
            StartLabel_Value.Text = start_Label;
            ProcessLabel_Value.Text = process_Label;
            EndLabel_Value.Text = end_Label;
        }

        public Color Color => color;
        public string Start_Label
        {
            get
            {
                return start_Label;
            }
        }
        
        public string Process_Label => process_Label;
        public string End_Label => end_Label;


        private void StartLabel_Value_TextChanged(object sender, EventArgs e)
        {
            start_Label = StartLabel_Value.Text;
        }

        private void ProcessLabel_Value_TextChanged(object sender, EventArgs e)
        {
            process_Label = ProcessLabel_Value.Text;
        }

        private void EndLabel_Value_TextChanged(object sender, EventArgs e)
        {
            end_Label = EndLabel_Value.Text;
        }
    }
}
