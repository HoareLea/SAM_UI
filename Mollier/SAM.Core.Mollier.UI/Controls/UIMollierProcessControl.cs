using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierProcessControl : UserControl
    {
        private UIMollierProcess uIMollierProcess;
        private MollierControlSettings mollierControlSettings;

        public event MollierProcessRemovedEventHandler MollierProcessRemoved;
       // Ẇ  litera TODO;
        public UIMollierProcessControl()
        {
            InitializeComponent();
        }
        public UIMollierProcessControl(UIMollierProcess mollierProcess, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierProcess = mollierProcess;
            this.mollierControlSettings = mollierControlSettings;
            fillValues();
        }
        private void fillValues()
        {

            double airflow = 0; // TODO: przeniesc to do manageMollierObjects form i zrobic eventy ze jak sie zmieni to tu zmien itd

            if(uIMollierProcess == null)
            {
                return;
            }

            processNameLabel.Text = uIMollierProcess.FullProcessName();
            if (uIMollierProcess.UIMollierAppearance_Start?.Label != null && uIMollierProcess.UIMollierAppearance_Start?.Label != "")
            {
                processStartLabel.Text = uIMollierProcess.UIMollierAppearance_Start?.Label;
            }
            if(uIMollierProcess.UIMollierAppearance_End?.Label != null && uIMollierProcess.UIMollierAppearance_End?.Label != "")
            {
                processEndLabel.Text = uIMollierProcess.UIMollierAppearance_End?.Label;
            }

            MollierPoint start = uIMollierProcess.Start;
            MollierPoint end = uIMollierProcess.End;
            if(!double.IsNaN(start.DryBulbTemperature))
            {
                start_DryBulbTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(start.DryBulbTemperature, 0.01));
            }
            if (!double.IsNaN(end.DryBulbTemperature))
            {
                end_DryBulbTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(end.DryBulbTemperature, 0.01));
            }
            if (!double.IsNaN(start.HumidityRatio))
            {
                start_HumidityRatio.Text = String.Format("{0:0.00}", Core.Query.Round(start.HumidityRatio, 0.01));
            }
            if (!double.IsNaN(end.HumidityRatio))
            {
                end_HumidityRatio.Text = String.Format("{0:0.00}", Core.Query.Round(end.HumidityRatio, 0.01));
            }
            if (!double.IsNaN(start.RelativeHumidity))
            {
                start_RelativeHumidity.Text = String.Format("{0:0.00}", Core.Query.Round(start.RelativeHumidity, 0.01));
            }
            if (!double.IsNaN(end.RelativeHumidity))
            {
                end_RelativeHumidity.Text = String.Format("{0:0.00}", Core.Query.Round(end.RelativeHumidity, 0.01));
            }
            if (!double.IsNaN(start.WetBulbTemperature()))
            {
                start_WetBulbTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(start.WetBulbTemperature(), 0.01));
            }
            if (!double.IsNaN(end.WetBulbTemperature()))
            {
                end_WetBulbTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(end.WetBulbTemperature(), 0.01));
            }
            if (!double.IsNaN(start.DewPointTemperature()))
            {
                start_DewPointTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(start.DewPointTemperature(), 0.01));
            }
            if (!double.IsNaN(end.DewPointTemperature()))
            {
                end_DewPointTemperature.Text = String.Format("{0:0.00}", Core.Query.Round(end.DewPointTemperature(), 0.01));
            }
            if (!double.IsNaN(start.SpecificVolume()))
            {
                start_SpecificVolume.Text = String.Format("{0:0.00}", Core.Query.Round(start.SpecificVolume(), 0.01));
            }
            if (!double.IsNaN(end.SpecificVolume()))
            {
                end_SpecificVolume.Text = String.Format("{0:0.00}", Core.Query.Round(end.SpecificVolume(), 0.01));
            }
            if (!double.IsNaN(start.Enthalpy))
            {
                start_Enthalpy.Text = String.Format("{0:0.00}", Core.Query.Round(start.Enthalpy / 1000, 0.01));
            }
            if (!double.IsNaN(end.Enthalpy))
            {
                end_Enthalpy.Text = String.Format("{0:0.00}", Core.Query.Round(end.Enthalpy / 1000, 0.01));
            }
            if (!double.IsNaN(start.Density()))
            {
                start_Density.Text = String.Format("{0:0.00}", Core.Query.Round(start.Density(), 0.01));
            }
            if (!double.IsNaN(end.Enthalpy))
            {
                end_Density.Text = String.Format("{0:0.00}", Core.Query.Round(end.Density(), 0.01));
            }

            double massFlow = Mollier.Query.MassFlow(airflow, end.Density());
            if(!double.IsNaN(massFlow))
            {
                end_MassFlow.Text = String.Format("{0:0.00}", Core.Query.Round(massFlow, 0.01));
            }

            double enthalpyDifference = start.Enthalpy - end.Enthalpy;
            double totalLoad = Mollier.Query.TotalLoad(end, enthalpyDifference, airflow);
            if (!double.IsNaN(totalLoad))
            {
                end_TotalLoad.Text = String.Format("{0:0.00}", Core.Query.Round(totalLoad, 0.01));

                //end_TotalLoad.Font = 


            }

            double temperatureDifference = start.DryBulbTemperature - end.DryBulbTemperature;
            double sensibleLoad = Mollier.Query.SensibleLoad(end, temperatureDifference, airflow);
            if (!double.IsNaN(sensibleLoad))
            {
                end_SensibleLoad.Text = String.Format("{0:0.00}", Core.Query.Round(sensibleLoad, 0.01));
            }

            double humidityRatioDifference = start.HumidityRatio - end.HumidityRatio;
            double latentLoad = Mollier.Query.LatentLoad(end, humidityRatioDifference, airflow);
            if (!double.IsNaN(latentLoad))
            {
                end_LatentLoad.Text = String.Format("{0:0.00}", Core.Query.Round(latentLoad, 0.01));
            }
        }

        private void ProcessCustomizeControl_Load(object sender, EventArgs e)
        {
          /*  string startLabel = uIMollierProcess.UIMollierAppearance_Start.Label;
            string endLabel = uIMollierProcess.UIMollierAppearance_End.Label;
            int maxNameLength = 5;

            processNameLabel.Text = uIMollierProcess.ChartDataType.ToString();
            end_HumidityRatio.Text = startLabel == string.Empty || startLabel == null ? "-" : startLabel.Substring(0, System.Math.Min(startLabel.Length, maxNameLength));
            start_RelativeHumidity.Text = endLabel == string.Empty || endLabel == null ? "-" : endLabel.Substring(0, System.Math.Min(endLabel.Length, maxNameLength));
            
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                start_DryBulbTemperature.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.HumidityRatio * 1000, 2));
                start_HumidityRatio.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.DryBulbTemperature, 2));
                start_DewPointTemperature.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.HumidityRatio * 1000, 2));
                start_SpecificVolume.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.DryBulbTemperature, 2));
            }
            else
            {
                start_DryBulbTemperature.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.DryBulbTemperature, 2));
                start_HumidityRatio.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.Start.HumidityRatio * 1000, 2));
                start_DewPointTemperature.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.DryBulbTemperature, 2));
                start_SpecificVolume.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierProcess.End.HumidityRatio * 1000, 2));
            }*/
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
                  //  MollierForm mollierForm = mollierCustomizeObjectsForm.MollierForm;

                    // mollierForm.RemoveProcess(uIMollierProcess);
                  //  mollierCustomizeObjectsForm.RemoveMollierProcess(uIMollierProcess);

                    uIMollierProcess.UIMollierAppearance.Color = customizeProcessForm.Color;
                    uIMollierProcess.UIMollierAppearance_Start.Label = customizeProcessForm.Start_Label;
                    uIMollierProcess.UIMollierAppearance.Label = customizeProcessForm.Process_Label;
                    uIMollierProcess.UIMollierAppearance_End.Label = customizeProcessForm.End_Label;

                    //mollierForm.AddProcesses(new List<UIMollierProcess>() { uIMollierProcess });
                   // mollierCustomizeObjectsForm.AddMollierProcess(uIMollierProcess);
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
