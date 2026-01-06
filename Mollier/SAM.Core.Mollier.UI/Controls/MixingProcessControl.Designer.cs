// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI.Controls
{
    partial class MixingProcessControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.FirstAirflowControl = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.SecondAirflowControl = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.MollierPointControl_SecondPoint = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.MollierPointControl_FirstPoint = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(24, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(142, 20);
            this.label2.TabIndex = 14;
            this.label2.Text = "First Airflow Point";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(286, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(164, 20);
            this.label1.TabIndex = 15;
            this.label1.Text = "Second Airflow Point";
            // 
            // FirstAirflowControl
            // 
            this.FirstAirflowControl.Location = new System.Drawing.Point(4, 229);
            this.FirstAirflowControl.Name = "FirstAirflowControl";
            this.FirstAirflowControl.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.FirstAirflowControl.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.FirstAirflowControl.Size = new System.Drawing.Size(200, 51);
            this.FirstAirflowControl.TabIndex = 2;
            this.FirstAirflowControl.UnitType = SAM.Units.UnitType.Undefined;
            this.FirstAirflowControl.Value = 0D;
            // 
            // SecondAirflowControl
            // 
            this.SecondAirflowControl.Location = new System.Drawing.Point(272, 229);
            this.SecondAirflowControl.Name = "SecondAirflowControl";
            this.SecondAirflowControl.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.SecondAirflowControl.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.SecondAirflowControl.Size = new System.Drawing.Size(200, 51);
            this.SecondAirflowControl.TabIndex = 4;
            this.SecondAirflowControl.UnitType = SAM.Units.UnitType.Undefined;
            this.SecondAirflowControl.Value = 0D;
            // 
            // MollierPointControl_SecondPoint
            // 
            this.MollierPointControl_SecondPoint.AutoSize = true;
            this.MollierPointControl_SecondPoint.Location = new System.Drawing.Point(272, 27);
            this.MollierPointControl_SecondPoint.Name = "MollierPointControl_SecondPoint";
            this.MollierPointControl_SecondPoint.Pressure = 101325D;
            this.MollierPointControl_SecondPoint.PressureEnabled = true;
            this.MollierPointControl_SecondPoint.PressureVisible = true;
            this.MollierPointControl_SecondPoint.SelectMollierPointVisible = false;
            this.MollierPointControl_SecondPoint.Size = new System.Drawing.Size(205, 206);
            this.MollierPointControl_SecondPoint.TabIndex = 3;
            // 
            // MollierPointControl_FirstPoint
            // 
            this.MollierPointControl_FirstPoint.AutoSize = true;
            this.MollierPointControl_FirstPoint.Location = new System.Drawing.Point(0, 27);
            this.MollierPointControl_FirstPoint.Name = "MollierPointControl_FirstPoint";
            this.MollierPointControl_FirstPoint.Pressure = 101325D;
            this.MollierPointControl_FirstPoint.PressureEnabled = true;
            this.MollierPointControl_FirstPoint.PressureVisible = true;
            this.MollierPointControl_FirstPoint.SelectMollierPointVisible = false;
            this.MollierPointControl_FirstPoint.Size = new System.Drawing.Size(205, 206);
            this.MollierPointControl_FirstPoint.TabIndex = 1;
            // 
            // MixingProcessControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.FirstAirflowControl);
            this.Controls.Add(this.SecondAirflowControl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MollierPointControl_SecondPoint);
            this.Controls.Add(this.MollierPointControl_FirstPoint);
            this.Name = "MixingProcessControl";
            this.Size = new System.Drawing.Size(489, 295);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MollierPointControl MollierPointControl_FirstPoint;
        private MollierPointControl MollierPointControl_SecondPoint;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private ParameterControl SecondAirflowControl;
        private ParameterControl FirstAirflowControl;
    }
}
