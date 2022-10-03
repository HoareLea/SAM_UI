using SAM.Core.Mollier.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm : Form
    {
        public UIMollierProcess UI_MollierProcess;
        private Control control;
        private Color color;
        private string start_Label;
        private string process_Label;
        private string end_Label;
        private MollierControl mollierControl;
        public event EventHandler SelectPointClicked;
        private MollierPoint mollierPoint;
        private MollierPoint previousMollierPoint;
        public MollierProcessForm()
        {
            InitializeComponent();
            
        }    
        public MollierProcessForm(MollierControl mollierControl)
        {
            InitializeComponent();
            this.mollierControl = mollierControl;

        }
        private void OK_Button_Click(object sender, EventArgs e)    
        {
            MollierProcessType mollierProcessType = Core.Query.Enum<MollierProcessType>(MollierProcessType_ComboBox.Text);
            switch (mollierProcessType)
            {
                case MollierProcessType.Heating:
                    UI_MollierProcess = (control as HeatingProcessControl).CreateHeatingProcess();
                    break;
                case MollierProcessType.Cooling:
                    UI_MollierProcess = (control as CoolingProcessControl).CreateCoolingProcess();
                    break;
                case MollierProcessType.HeatRecovery:
                    UI_MollierProcess = (control as HeatRecoveryProcessControl).CreateHeatRecoveryProcess();
                    break;
                case MollierProcessType.Mixing:
                    UI_MollierProcess = (control as MixingProcessControl).CreateMixingProcess();
                    break;
                case MollierProcessType.AdiabaticHumidification:
                    UI_MollierProcess = (control as AdiabaticHumidificationProcessControl).CreateAdiabaticHumidificationProcess();
                    break;
                case MollierProcessType.IsotermicHumidification:
                    UI_MollierProcess = (control as IsotermicHumidificationProcessControl).CreateIsotermicHumidificationProcess();
                    break;
            }
            UI_MollierProcess.Color = color;
            UI_MollierProcess.Start_Label = start_Label;
            UI_MollierProcess.Process_Label = process_Label;
            UI_MollierProcess.End_Label = end_Label;
            DialogResult = DialogResult.OK;
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
        public UIMollierProcess UIMollierProcess
        {
            get
            {
                return UI_MollierProcess;
            }

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
            }
        }
        private void MollierProcessType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MollierProcessType mollierProcessType = Core.Query.Enum<MollierProcessType>(MollierProcessType_ComboBox.Text);
            control?.Dispose();
            switch (mollierProcessType)
            {
                case MollierProcessType.Heating:
                    addControl(mollierProcessType);
                    (control as HeatingProcessControl).SelectPointClicked += MollierProcessForm_SelectPointClicked;
                    (control as HeatingProcessControl).Start = previousMollierPoint;
                    break;
                case MollierProcessType.Cooling:
                    addControl(mollierProcessType);
                    (control as CoolingProcessControl).Start = previousMollierPoint;
                    break;
                case MollierProcessType.HeatRecovery:
                    addControl(mollierProcessType);
                    (control as HeatRecoveryProcessControl).Supply = previousMollierPoint;
                    break;
                case MollierProcessType.Mixing:
                    addControl(mollierProcessType);
                    (control as MixingProcessControl).FirstPoint = previousMollierPoint;
                    break;
                case MollierProcessType.AdiabaticHumidification:
                    addControl(mollierProcessType);
                    (control as AdiabaticHumidificationProcessControl).Start = previousMollierPoint;
                    break;
                case MollierProcessType.IsotermicHumidification:
                    addControl(mollierProcessType);
                    (control as IsotermicHumidificationProcessControl).Start = previousMollierPoint;
                    break;
            }

        }
        public MollierPoint PreviousMollierPoint
        {
            get
            {
                return previousMollierPoint;
            }
            set
            {
                if (value != null)
                {
                    previousMollierPoint = value;
                }
            }
        }
        private void MollierProcessForm_SelectPointClicked(object sender, EventArgs e)
        {
            SelectPointClicked?.Invoke(this, e);
        }

        private void addControl(MollierProcessType mollierProcessType)
        {
            control?.Dispose();
            switch (mollierProcessType)
            {
                case MollierProcessType.Heating:
                    control = new HeatingProcessControl();
                    break;
                case MollierProcessType.Cooling:
                    control = new CoolingProcessControl(mollierControl);
                    break;
                case MollierProcessType.HeatRecovery:
                    control = new HeatRecoveryProcessControl();
                    break;
                case MollierProcessType.Mixing:
                    control = new MixingProcessControl();
                    break;
                case MollierProcessType.AdiabaticHumidification:
                    control = new AdiabaticHumidificationProcessControl();
                    break;
                case MollierProcessType.IsotermicHumidification:
                    control = new IsotermicHumidificationProcessControl();
                    break;
            }
            if (control != null)
            {
                control.Parent = splitContainer1.Panel2;
                control.Dock = DockStyle.Fill;
            }
        }

        private void Customize_Button_Click(object sender, EventArgs e)
        {
            using(CustomizeProcessForm customizeProcessForm = new CustomizeProcessForm())
            {
               if(customizeProcessForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                color = customizeProcessForm.Color;
                start_Label = customizeProcessForm.Start_Label;
                process_Label = customizeProcessForm.Process_Label;
                end_Label = customizeProcessForm.End_Label;
            }
        }
    }
}
