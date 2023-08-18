using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class ProcessManageControl : UserControl
    {
        private UIMollierProcess uIMollierProcess;
        private MollierControlSettings mollierControlSettings;

        public event MollierProcessRemovedEventHandler MollierProcessRemoved;

        public ProcessManageControl()
        {
            InitializeComponent();
        }
        public ProcessManageControl(UIMollierProcess mollierProcess, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierProcess = mollierProcess;
            this.mollierControlSettings = mollierControlSettings;
        }

        private void ProcessCustomizeControl_Load(object sender, EventArgs e)
        {
            string startLabel = uIMollierProcess.UIMollierAppearance_Start.Label;
            string endLabel = uIMollierProcess.UIMollierAppearance_End.Label;
            int maxNameLength = 5;

            processNameLabel.Text = uIMollierProcess.ChartDataType.ToString();
            start_Name.Text = startLabel == string.Empty || startLabel == null ? "-" : startLabel.Substring(0, System.Math.Min(startLabel.Length, maxNameLength));
            end_Name.Text = endLabel == string.Empty || endLabel == null ? "-" : endLabel.Substring(0, System.Math.Min(endLabel.Length, maxNameLength));
            
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                start_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.HumidityRatio * 1000, 2));
                start_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.DryBulbTemperature, 2));
                end_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.HumidityRatio * 1000, 2));
                end_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.DryBulbTemperature, 2));
            }
            else
            {
                start_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.DryBulbTemperature, 2));
                start_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.HumidityRatio * 1000, 2));
                end_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.DryBulbTemperature, 2));
                end_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.HumidityRatio * 1000, 2));
            }
        }
        

        private void settingsButton_Click(object sender, EventArgs e)
        {
            using (Forms.CustomizeProcessForm customizeProcessForm = new Forms.CustomizeProcessForm(uIMollierProcess))
            {
                if (customizeProcessForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                if (Parent?.Parent?.Parent?.Parent is Forms.ManageMollierObjectsForm)
                {
                    Forms.ManageMollierObjectsForm mollierCustomizeObjectsForm = (Forms.ManageMollierObjectsForm)Parent.Parent.Parent.Parent;
                    MollierForm mollierForm = mollierCustomizeObjectsForm.MollierForm;

                    // mollierForm.RemoveProcess(uIMollierProcess);
                    mollierCustomizeObjectsForm.RemoveMollierProcess(uIMollierProcess);

                    uIMollierProcess.UIMollierAppearance.Color = customizeProcessForm.Color;
                    uIMollierProcess.UIMollierAppearance_Start.Label = customizeProcessForm.Start_Label;
                    uIMollierProcess.UIMollierAppearance.Label = customizeProcessForm.Process_Label;
                    uIMollierProcess.UIMollierAppearance_End.Label = customizeProcessForm.End_Label;

                    mollierForm.AddProcesses(new List<UIMollierProcess>() { uIMollierProcess });
                    mollierCustomizeObjectsForm.AddMollierProcess(uIMollierProcess);
                }
            }
        }
        private void removeButton_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure to delete this item ?", "Delete Confirmation",
                                                 MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.No)
            {
                return;
            }
            else
            {
                MollierProcessRemoved.Invoke(this, new MollierProcessRemovedEventArgs(uIMollierProcess));
            }
        }
    }
}
