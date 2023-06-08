using SAM.Core.Mollier.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm : Form
    {
        public event SelectMollierPointEventHandler SelectMollierPoint;

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
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
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
                    HeatingProcessControl HeatingProcessControl = new HeatingProcessControl() { Start = previousMollierPoint };
                    return HeatingProcessControl;

                case MollierProcessType.Cooling:
                    return new CoolingProcessControl() { Start = previousMollierPoint };

                case MollierProcessType.HeatRecovery:
                    return new HeatRecoveryProcessControl() { Supply = previousMollierPoint };

                case MollierProcessType.Mixing:
                    return new MixingProcessControl() { FirstPoint = previousMollierPoint };

                case MollierProcessType.AdiabaticHumidification:
                    return new AdiabaticHumidificationProcessControl() { Start = previousMollierPoint };

                case MollierProcessType.IsotermicHumidification:
                    return new IsotermicHumidificationProcessControl() { Start = previousMollierPoint };

                case MollierProcessType.Undefined:
                    RoomProcessControl roomProcessControl = new RoomProcessControl() { StartMollierPoint = previousMollierPoint };
                    roomProcessControl.SelectMollierPoint += ProcessControl_SelectMollierPoint;

                    return roomProcessControl;
            }

            return null;
        }

        private void ProcessControl_SelectMollierPoint(object sender, SelectMollierPointEventArgs e)
        {
            Hide();

            SelectMollierPoint?.Invoke(this, e);

            //Visible = true;
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
