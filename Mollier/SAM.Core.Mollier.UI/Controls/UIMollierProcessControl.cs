using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierProcessControl : UserControl
    {
        private UIMollierProcess uIMollierProcess;
        public UIMollierProcessControl()
        {
            InitializeComponent();
        }
        public UIMollierProcessControl(UIMollierProcess uIMollierProcess)
        {
            InitializeComponent();
            if(uIMollierProcess == null)
            {
                return;
            }
            setUIMollierProcess(uIMollierProcess);
        }

        public UIMollierProcess UIMollierProcess
        {
            get
            {
                if(uIMollierProcess == null || uIMollierProcess.UIMollierAppearance == null)
                {
                    return null;
                }
                uIMollierProcess.UIMollierAppearance.Color = ProcessColor_button.BackColor;
                uIMollierProcess.UIMollierPointAppearance_Start.Label = StartLabel_Value.Text;
                uIMollierProcess.UIMollierAppearance.Label = ProcessLabel_Value.Text;
                uIMollierProcess.UIMollierPointAppearance_End.Label = EndLabel_Value.Text;
                return uIMollierProcess;
            }
            set
            {
                setUIMollierProcess(value);
            }
        }

        private void setUIMollierProcess(UIMollierProcess uIMollierProcess)
        {
            if(uIMollierProcess == null)
            {
                return;
            }

            this.uIMollierProcess = uIMollierProcess;

            if (uIMollierProcess.UIMollierAppearance != null)
            {
                ProcessColor_button.BackColor = uIMollierProcess.UIMollierAppearance.Color;
                StartLabel_Value.Text = uIMollierProcess.UIMollierPointAppearance_Start.Label;
                ProcessLabel_Value.Text = uIMollierProcess.UIMollierAppearance.Label;
                EndLabel_Value.Text = uIMollierProcess.UIMollierPointAppearance_End.Label;
            }
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
            }
        }
    }
}
