// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI.Controls
{
    partial class RoomProcessControl
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
            this.label3 = new System.Windows.Forms.Label();
            this.mollierPointControl_Room = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.ParameterControl_SensibleLoadRatio = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.ParameterControl_LatentLoad = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.ParameterControl_SensibleLoad = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.ParameterControl_HumidityRatio_Start = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.ParameterControl_Airflow = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.MollierPointControl_Start = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.ParameterControl_HumidityRatio_Room = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.ParameterControl_Epsilon = new SAM.Core.Mollier.UI.Controls.ParameterControl();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(25, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(155, 20);
            this.label2.TabIndex = 14;
            this.label2.Text = "Air Start Conditions";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(254, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 20);
            this.label1.TabIndex = 15;
            this.label1.Text = "Room Data";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label3.Location = new System.Drawing.Point(406, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(163, 20);
            this.label3.TabIndex = 22;
            this.label3.Text = "Room Air Conditions";
            // 
            // mollierPointControl_Room
            // 
            this.mollierPointControl_Room.AutoSize = true;
            this.mollierPointControl_Room.Location = new System.Drawing.Point(401, 32);
            this.mollierPointControl_Room.Name = "mollierPointControl_Room";
            this.mollierPointControl_Room.Pressure = 101325D;
            this.mollierPointControl_Room.PressureEnabled = true;
            this.mollierPointControl_Room.PressureVisible = true;
            this.mollierPointControl_Room.SelectMollierPointVisible = false;
            this.mollierPointControl_Room.Size = new System.Drawing.Size(168, 220);
            this.mollierPointControl_Room.TabIndex = 8;
            // 
            // ParameterControl_SensibleLoadRatio
            // 
            this.ParameterControl_SensibleLoadRatio.Location = new System.Drawing.Point(227, 212);
            this.ParameterControl_SensibleLoadRatio.Name = "ParameterControl_SensibleLoadRatio";
            this.ParameterControl_SensibleLoadRatio.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_SensibleLoadRatio.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_SensibleLoadRatio.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_SensibleLoadRatio.TabIndex = 6;
            this.ParameterControl_SensibleLoadRatio.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_SensibleLoadRatio.Value = 0D;
            // 
            // ParameterControl_LatentLoad
            // 
            this.ParameterControl_LatentLoad.Location = new System.Drawing.Point(227, 156);
            this.ParameterControl_LatentLoad.Name = "ParameterControl_LatentLoad";
            this.ParameterControl_LatentLoad.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_LatentLoad.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_LatentLoad.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_LatentLoad.TabIndex = 5;
            this.ParameterControl_LatentLoad.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_LatentLoad.Value = 0D;
            // 
            // ParameterControl_SensibleLoad
            // 
            this.ParameterControl_SensibleLoad.Location = new System.Drawing.Point(227, 99);
            this.ParameterControl_SensibleLoad.Name = "ParameterControl_SensibleLoad";
            this.ParameterControl_SensibleLoad.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_SensibleLoad.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_SensibleLoad.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_SensibleLoad.TabIndex = 4;
            this.ParameterControl_SensibleLoad.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_SensibleLoad.Value = 0D;
            // 
            // ParameterControl_HumidityRatio_Start
            // 
            this.ParameterControl_HumidityRatio_Start.Location = new System.Drawing.Point(12, 233);
            this.ParameterControl_HumidityRatio_Start.Name = "ParameterControl_HumidityRatio_Start";
            this.ParameterControl_HumidityRatio_Start.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_HumidityRatio_Start.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_HumidityRatio_Start.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_HumidityRatio_Start.TabIndex = 2;
            this.ParameterControl_HumidityRatio_Start.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_HumidityRatio_Start.Value = 0D;
            // 
            // ParameterControl_Airflow
            // 
            this.ParameterControl_Airflow.Location = new System.Drawing.Point(227, 42);
            this.ParameterControl_Airflow.Name = "ParameterControl_Airflow";
            this.ParameterControl_Airflow.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_Airflow.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_Airflow.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_Airflow.TabIndex = 3;
            this.ParameterControl_Airflow.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_Airflow.Value = 0D;
            // 
            // MollierPointControl_Start
            // 
            this.MollierPointControl_Start.AutoSize = true;
            this.MollierPointControl_Start.Location = new System.Drawing.Point(12, 32);
            this.MollierPointControl_Start.Name = "MollierPointControl_Start";
            this.MollierPointControl_Start.Pressure = 101325D;
            this.MollierPointControl_Start.PressureEnabled = true;
            this.MollierPointControl_Start.PressureVisible = true;
            this.MollierPointControl_Start.SelectMollierPointVisible = false;
            this.MollierPointControl_Start.Size = new System.Drawing.Size(168, 220);
            this.MollierPointControl_Start.TabIndex = 1;
            // 
            // ParameterControl_HumidityRatio_Room
            // 
            this.ParameterControl_HumidityRatio_Room.Location = new System.Drawing.Point(401, 232);
            this.ParameterControl_HumidityRatio_Room.Name = "ParameterControl_HumidityRatio_Room";
            this.ParameterControl_HumidityRatio_Room.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_HumidityRatio_Room.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_HumidityRatio_Room.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_HumidityRatio_Room.TabIndex = 9;
            this.ParameterControl_HumidityRatio_Room.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_HumidityRatio_Room.Value = 0D;
            // 
            // ParameterControl_Epsilon
            // 
            this.ParameterControl_Epsilon.Location = new System.Drawing.Point(227, 269);
            this.ParameterControl_Epsilon.Name = "ParameterControl_Epsilon";
            this.ParameterControl_Epsilon.ProcessParameterType = SAM.Core.Mollier.UI.ProcessParameterType.Undefined;
            this.ParameterControl_Epsilon.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ParameterControl_Epsilon.Size = new System.Drawing.Size(168, 51);
            this.ParameterControl_Epsilon.TabIndex = 7;
            this.ParameterControl_Epsilon.UnitType = SAM.Units.UnitType.Undefined;
            this.ParameterControl_Epsilon.Value = 0D;
            // 
            // RoomProcessControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.Controls.Add(this.ParameterControl_Epsilon);
            this.Controls.Add(this.ParameterControl_HumidityRatio_Room);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.mollierPointControl_Room);
            this.Controls.Add(this.ParameterControl_SensibleLoadRatio);
            this.Controls.Add(this.ParameterControl_LatentLoad);
            this.Controls.Add(this.ParameterControl_SensibleLoad);
            this.Controls.Add(this.ParameterControl_HumidityRatio_Start);
            this.Controls.Add(this.ParameterControl_Airflow);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MollierPointControl_Start);
            this.Name = "RoomProcessControl";
            this.Size = new System.Drawing.Size(579, 323);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MollierPointControl MollierPointControl_Start;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private ParameterControl ParameterControl_Airflow;
        private ParameterControl ParameterControl_HumidityRatio_Start;
        private ParameterControl ParameterControl_SensibleLoad;
        private ParameterControl ParameterControl_LatentLoad;
        private ParameterControl ParameterControl_SensibleLoadRatio;
        private System.Windows.Forms.Label label3;
        private MollierPointControl mollierPointControl_Room;
        private ParameterControl ParameterControl_HumidityRatio_Room;
        private ParameterControl ParameterControl_Epsilon;
    }
}
