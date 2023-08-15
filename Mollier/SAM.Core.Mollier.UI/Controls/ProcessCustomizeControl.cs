using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class ProcessCustomizeControl : UserControl
    {
        private UIMollierProcess uIMollierProcess;
        private MollierControlSettings mollierControlSettings;
        public ProcessCustomizeControl()
        {
            InitializeComponent();
        }

        public ProcessCustomizeControl(UIMollierProcess mollierProcess, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierProcess = mollierProcess;
            this.mollierControlSettings = mollierControlSettings;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void ProcessCustomizeControl_Load(object sender, EventArgs e)
        {
            processNameLabel.Text = uIMollierProcess.ChartDataType.ToString();

            if(mollierControlSettings.ChartType == ChartType.Mollier)
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

        private void removeButton_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure to delete this item ?",
                                                 "Delete Confirmation",
                                                 MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.No)
            {
                return;
            }
            else
            {
                if(Parent?.Parent?.Parent?.Parent is Forms.MollierCustomizeObjectsForm)
                {
                    Forms.MollierCustomizeObjectsForm mollierCustomizeObjectsForm = (Forms.MollierCustomizeObjectsForm)Parent.Parent.Parent.Parent;
                    MollierForm mollierForm = mollierCustomizeObjectsForm.MollierForm;

                    mollierForm.RemoveProcess(uIMollierProcess);
                    //mollierCustomizeObjectsForm.RemoveProcess(uIMollierProcess);
                }
            }
        }
    }
}
