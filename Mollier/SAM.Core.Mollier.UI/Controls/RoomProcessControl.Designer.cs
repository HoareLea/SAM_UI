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
            this.MollierPointControl_Start = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
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
            this.mollierPointControl_Room.TabIndex = 21;
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
            this.MollierPointControl_Start.TabIndex = 0;
            // 
            // RoomProcessControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.mollierPointControl_Room);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MollierPointControl_Start);
            this.Name = "RoomProcessControl";
            this.Size = new System.Drawing.Size(579, 310);
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
