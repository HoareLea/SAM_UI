using SAM.Core.Mollier.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm : Form
    {
        private Color color;
        private string start_Label;
        private string process_Label;
        private string end_Label;
        
        private MollierPoint mollierPoint;
        private MollierPoint previousMollierPoint;
        
        public MollierProcessForm()
        {
            InitializeComponent();
            
        }

        public MollierForm MollierForm { get; set; }

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

        public UIMollierProcess GetUIMollierProcess()
        {
            Control control = GetControl();
            if (control == null)
            {
                return null;
            }

            IMollierProcessControl mollierProcessControl = control as IMollierProcessControl;
            if(mollierProcessControl == null)
            {
                return null;
            }

            UIMollierProcess result = mollierProcessControl.GetUIMollierProcess();
            if(result == null)
            {
                return null;
            }

            result.UIMollierAppearance.Color = color;
            result.UIMollierAppearance.Label = process_Label;

            result.UIMollierPointAppearance_Start = Create.UIMollierPointAppearance(DisplayPointType.Process, start_Label);
            //result.UIMollierPointAppearance_Start.Color = color;

            result.UIMollierPointAppearance_End = Create.UIMollierPointAppearance(DisplayPointType.Process, end_Label);
            //result.UIMollierPointAppearance_End.Color = color;

            return result;
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void MollierProcessType_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControl();
        }

        private void Customize_Button_Click(object sender, EventArgs e)
        {
            using (CustomizeProcessForm customizeProcessForm = new CustomizeProcessForm())
            {
                if (customizeProcessForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                color = customizeProcessForm.UIMollierProcess.UIMollierAppearance.Color;
                start_Label = customizeProcessForm.UIMollierProcess.UIMollierPointAppearance_Start.Label;
                process_Label = customizeProcessForm.UIMollierProcess.UIMollierAppearance.Label;
                end_Label = customizeProcessForm.UIMollierProcess.UIMollierPointAppearance_End.Label;
            }
        }

        private Control GetControl()
        {
            Control.ControlCollection controlCollection = splitContainer1?.Panel2?.Controls;
            if(controlCollection == null || controlCollection.Count == 0)
            {
                return null;
            }

            return controlCollection[0];
        }







        private Control CreateControl()
        {
            MollierProcessType mollierProcessType = Core.Query.Enum<MollierProcessType>(MollierProcessType_ComboBox.Text); 

            switch(mollierProcessType)
            {
                case MollierProcessType.Heating:
                    HeatingProcessControl heatingProcessControl = new HeatingProcessControl() { MollierForm = MollierForm, Start = previousMollierPoint };
                    heatingProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return heatingProcessControl;

                case MollierProcessType.Cooling:
                    CoolingProcessControl coolingProcessControl = new CoolingProcessControl() { MollierForm = MollierForm, Start = previousMollierPoint };
                    coolingProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return coolingProcessControl;

                case MollierProcessType.HeatRecovery:
                    HeatRecoveryProcessControl heatRecoveryProcessControl = new HeatRecoveryProcessControl() { MollierForm = MollierForm, Supply = previousMollierPoint };
                    heatRecoveryProcessControl.SelectSupplyMollierPoint += ProcessControl_SelectMollierPoint;
                    heatRecoveryProcessControl.SelectReturnMollierPoint += ProcessControl_SelectMollierPoint;
                    return heatRecoveryProcessControl;

                case MollierProcessType.Mixing:
                    MixingProcessControl mixingProcessControl = new MixingProcessControl() { MollierForm = MollierForm, FirstPoint = previousMollierPoint };
                    mixingProcessControl.SelectFirstMollierPoint += ProcessControl_SelectMollierPoint;
                    mixingProcessControl.SelectSecondMollierPoint += ProcessControl_SelectMollierPoint;
                    return mixingProcessControl;

                case MollierProcessType.AdiabaticHumidification:
                    AdiabaticHumidificationProcessControl adiabaticHumidificationProcessControl = new AdiabaticHumidificationProcessControl() { MollierForm = MollierForm, Start = previousMollierPoint };
                    
                    adiabaticHumidificationProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return adiabaticHumidificationProcessControl;

                case MollierProcessType.IsothermicHumidification:
                    IsothermicHumidificationProcessControl isothermicHumidificationProcessControl = new IsothermicHumidificationProcessControl() { MollierForm = MollierForm, Start = previousMollierPoint };
                    isothermicHumidificationProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return isothermicHumidificationProcessControl;

                case MollierProcessType.Undefined:
                    RoomProcessControl roomProcessControl = new RoomProcessControl() { MollierForm = MollierForm, StartMollierPoint = previousMollierPoint };
                    roomProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return roomProcessControl;
            }

            return null;
        }

        private void ProcessControl_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            Hide();
        }

        private void UpdateControl()
        {
            splitContainer1?.Panel2?.Controls.Clear();

            Control control = CreateControl();
            if(control == null)
            {
                return;
            }

            control.Parent = splitContainer1.Panel2;
            control.Dock = DockStyle.Fill;
        }
    }
}
