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

            result.Color = color;
            result.Start_Label = start_Label;
            result.Process_Label = process_Label;
            result.End_Label = end_Label;

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

                color = customizeProcessForm.Color;
                start_Label = customizeProcessForm.Start_Label;
                process_Label = customizeProcessForm.Process_Label;
                end_Label = customizeProcessForm.End_Label;
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

                case MollierProcessType.IsotermicHumidification:
                    IsotermicHumidificationProcessControl isotermicHumidificationProcessControl = new IsotermicHumidificationProcessControl() { MollierForm = MollierForm, Start = previousMollierPoint };
                    isotermicHumidificationProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;
                    return isotermicHumidificationProcessControl;

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
