using SAM.Geometry.Mollier;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class PointManageControl : UserControl
    {
        private UIMollierPoint uIMollierPoint;
        private MollierControlSettings mollierControlSettings;
        public PointManageControl()
        {
            InitializeComponent();
        }

        public PointManageControl(UIMollierPoint uIMollierPoint, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierPoint = uIMollierPoint;
            this.mollierControlSettings = mollierControlSettings;
        }

        private void PointManageControl_Load(object sender, EventArgs e)
        {
            string pointLabel = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance).Label;
            int maxNameLength = 5;

            label_Name.Text = pointLabel == string.Empty || pointLabel == null ? "-" : pointLabel.Substring(0, System.Math.Min(pointLabel.Length, maxNameLength));

            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                label_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierPoint.HumidityRatio * 1000, 2));
                label_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierPoint.DryBulbTemperature, 2));
            }
            else
            {
                label_X.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierPoint.DryBulbTemperature, 2));
                label_Y.Text = String.Format("{0:0.00}", System.Math.Round(uIMollierPoint.HumidityRatio * 1000, 2));
            }
        }


        private void settingsButton_Click(object sender, EventArgs e)
        {
    
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
                if (Parent?.Parent?.Parent?.Parent is Forms.UIMollierObjectsForm)
                {
                    Forms.UIMollierObjectsForm mollierCustomizeObjectsForm = (Forms.UIMollierObjectsForm)Parent.Parent.Parent.Parent;
                  //  MollierForm mollierForm = mollierCustomizeObjectsForm.MollierForm;

                    //mollierForm.RemovePoint(uIMollierPoint);
                    //mollierCustomizeObjectsForm.RemoveMollierPoint(uIMollierPoint);
                }
            }
        }
    }
}
