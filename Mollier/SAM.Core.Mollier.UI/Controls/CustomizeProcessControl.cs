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
            color = UI_MollierProcess.Color;
            start_Label = UI_MollierProcess.Start_Label;
            process_Label = UI_MollierProcess.Process_Label;
            end_Label = UI_MollierProcess.End_Label;
            ProcessColor_button.BackColor = color;
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

        private void CustomizeProcessControl_Load(object sender, EventArgs e)
        {
            
        }

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
