// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


namespace SAM.Analytical.UI.Controls
{
    partial class SimulateControl
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
            this.TextBox_ProjectName = new System.Windows.Forms.TextBox();
            this.Label_ProjectName = new System.Windows.Forms.Label();
            this.CheckBox_UpdateConstructionLayersByPanelType = new System.Windows.Forms.CheckBox();
            this.CheckBox_UnmetHours = new System.Windows.Forms.CheckBox();
            this.Button_WeatherData = new System.Windows.Forms.Button();
            this.TextBox_WeatherData = new System.Windows.Forms.TextBox();
            this.Label_WeatherData = new System.Windows.Forms.Label();
            this.Button_OutputDirectory = new System.Windows.Forms.Button();
            this.TextBox_OutputDirectory = new System.Windows.Forms.TextBox();
            this.Label_OutputDirectory = new System.Windows.Forms.Label();
            this.CheckBox_RoomDataSheets = new System.Windows.Forms.CheckBox();
            this.CheckBox_FullYearSimulation = new System.Windows.Forms.CheckBox();
            this.Label_From = new System.Windows.Forms.Label();
            this.TextBox_From = new System.Windows.Forms.TextBox();
            this.TextBox_To = new System.Windows.Forms.TextBox();
            this.Label_To = new System.Windows.Forms.Label();
            this.CheckBox_Sizing = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ComboBoxControl_SolarCalculationMethod = new SAM.Core.Windows.ComboBoxControl();
            this.SuspendLayout();
            // 
            // TextBox_ProjectName
            // 
            this.TextBox_ProjectName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_ProjectName.Location = new System.Drawing.Point(3, 25);
            this.TextBox_ProjectName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TextBox_ProjectName.Name = "TextBox_ProjectName";
            this.TextBox_ProjectName.Size = new System.Drawing.Size(507, 24);
            this.TextBox_ProjectName.TabIndex = 24;
            this.TextBox_ProjectName.Text = "000006_SAM_AnalyticalModel";
            // 
            // Label_ProjectName
            // 
            this.Label_ProjectName.AutoSize = true;
            this.Label_ProjectName.Location = new System.Drawing.Point(3, 5);
            this.Label_ProjectName.Name = "Label_ProjectName";
            this.Label_ProjectName.Size = new System.Drawing.Size(107, 18);
            this.Label_ProjectName.TabIndex = 23;
            this.Label_ProjectName.Text = "Project Name :";
            // 
            // CheckBox_UpdateConstructionLayersByPanelType
            // 
            this.CheckBox_UpdateConstructionLayersByPanelType.AutoSize = true;
            this.CheckBox_UpdateConstructionLayersByPanelType.Checked = true;
            this.CheckBox_UpdateConstructionLayersByPanelType.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBox_UpdateConstructionLayersByPanelType.Enabled = false;
            this.CheckBox_UpdateConstructionLayersByPanelType.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.CheckBox_UpdateConstructionLayersByPanelType.Location = new System.Drawing.Point(3, 283);
            this.CheckBox_UpdateConstructionLayersByPanelType.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CheckBox_UpdateConstructionLayersByPanelType.Name = "CheckBox_UpdateConstructionLayersByPanelType";
            this.CheckBox_UpdateConstructionLayersByPanelType.Size = new System.Drawing.Size(367, 22);
            this.CheckBox_UpdateConstructionLayersByPanelType.TabIndex = 21;
            this.CheckBox_UpdateConstructionLayersByPanelType.Text = "Update Missing Construction Layers By Panel Type";
            this.CheckBox_UpdateConstructionLayersByPanelType.UseVisualStyleBackColor = true;
            // 
            // CheckBox_UnmetHours
            // 
            this.CheckBox_UnmetHours.AutoSize = true;
            this.CheckBox_UnmetHours.Location = new System.Drawing.Point(3, 373);
            this.CheckBox_UnmetHours.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CheckBox_UnmetHours.Name = "CheckBox_UnmetHours";
            this.CheckBox_UnmetHours.Size = new System.Drawing.Size(119, 22);
            this.CheckBox_UnmetHours.TabIndex = 22;
            this.CheckBox_UnmetHours.Text = "Unmet Hours";
            this.CheckBox_UnmetHours.UseVisualStyleBackColor = true;
            // 
            // Button_WeatherData
            // 
            this.Button_WeatherData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_WeatherData.Location = new System.Drawing.Point(470, 166);
            this.Button_WeatherData.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Button_WeatherData.Name = "Button_WeatherData";
            this.Button_WeatherData.Size = new System.Drawing.Size(40, 25);
            this.Button_WeatherData.TabIndex = 20;
            this.Button_WeatherData.Text = "...";
            this.Button_WeatherData.UseVisualStyleBackColor = true;
            this.Button_WeatherData.Click += new System.EventHandler(this.Button_WeatherData_Click);
            // 
            // TextBox_WeatherData
            // 
            this.TextBox_WeatherData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_WeatherData.Location = new System.Drawing.Point(2, 166);
            this.TextBox_WeatherData.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TextBox_WeatherData.Name = "TextBox_WeatherData";
            this.TextBox_WeatherData.ReadOnly = true;
            this.TextBox_WeatherData.Size = new System.Drawing.Size(461, 24);
            this.TextBox_WeatherData.TabIndex = 19;
            this.TextBox_WeatherData.Text = "POL_Zielona.Gora.124000_IMGW";
            // 
            // Label_WeatherData
            // 
            this.Label_WeatherData.AutoSize = true;
            this.Label_WeatherData.Location = new System.Drawing.Point(2, 144);
            this.Label_WeatherData.Name = "Label_WeatherData";
            this.Label_WeatherData.Size = new System.Drawing.Size(107, 18);
            this.Label_WeatherData.TabIndex = 18;
            this.Label_WeatherData.Text = "Weather Data :";
            // 
            // Button_OutputDirectory
            // 
            this.Button_OutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OutputDirectory.Location = new System.Drawing.Point(471, 83);
            this.Button_OutputDirectory.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Button_OutputDirectory.Name = "Button_OutputDirectory";
            this.Button_OutputDirectory.Size = new System.Drawing.Size(40, 25);
            this.Button_OutputDirectory.TabIndex = 17;
            this.Button_OutputDirectory.Text = "...";
            this.Button_OutputDirectory.UseVisualStyleBackColor = true;
            this.Button_OutputDirectory.Click += new System.EventHandler(this.Button_OutputDirectory_Click);
            // 
            // TextBox_OutputDirectory
            // 
            this.TextBox_OutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_OutputDirectory.Location = new System.Drawing.Point(3, 83);
            this.TextBox_OutputDirectory.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TextBox_OutputDirectory.Multiline = true;
            this.TextBox_OutputDirectory.Name = "TextBox_OutputDirectory";
            this.TextBox_OutputDirectory.Size = new System.Drawing.Size(460, 47);
            this.TextBox_OutputDirectory.TabIndex = 16;
            this.TextBox_OutputDirectory.Text = "C:\\Users\\DengusiakM\\Desktop\\SAM\\2023-01-04_SAM_UI\\000000ABCD\\ABC";
            // 
            // Label_OutputDirectory
            // 
            this.Label_OutputDirectory.AutoSize = true;
            this.Label_OutputDirectory.Location = new System.Drawing.Point(3, 61);
            this.Label_OutputDirectory.Name = "Label_OutputDirectory";
            this.Label_OutputDirectory.Size = new System.Drawing.Size(121, 18);
            this.Label_OutputDirectory.TabIndex = 15;
            this.Label_OutputDirectory.Text = "Output directory :";
            // 
            // CheckBox_RoomDataSheets
            // 
            this.CheckBox_RoomDataSheets.AutoSize = true;
            this.CheckBox_RoomDataSheets.Location = new System.Drawing.Point(3, 401);
            this.CheckBox_RoomDataSheets.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CheckBox_RoomDataSheets.Name = "CheckBox_RoomDataSheets";
            this.CheckBox_RoomDataSheets.Size = new System.Drawing.Size(237, 22);
            this.CheckBox_RoomDataSheets.TabIndex = 25;
            this.CheckBox_RoomDataSheets.Text = "Print Room Data Sheets (RDS)";
            this.CheckBox_RoomDataSheets.UseVisualStyleBackColor = true;
            // 
            // CheckBox_FullYearSimulation
            // 
            this.CheckBox_FullYearSimulation.AutoSize = true;
            this.CheckBox_FullYearSimulation.Location = new System.Drawing.Point(3, 342);
            this.CheckBox_FullYearSimulation.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CheckBox_FullYearSimulation.Name = "CheckBox_FullYearSimulation";
            this.CheckBox_FullYearSimulation.Size = new System.Drawing.Size(160, 22);
            this.CheckBox_FullYearSimulation.TabIndex = 26;
            this.CheckBox_FullYearSimulation.Text = "Full Year Simulation";
            this.CheckBox_FullYearSimulation.UseVisualStyleBackColor = true;
            this.CheckBox_FullYearSimulation.CheckedChanged += new System.EventHandler(this.CheckBox_FullYearSimulation_CheckedChanged);
            // 
            // Label_From
            // 
            this.Label_From.AutoSize = true;
            this.Label_From.Location = new System.Drawing.Point(190, 344);
            this.Label_From.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Label_From.Name = "Label_From";
            this.Label_From.Size = new System.Drawing.Size(48, 18);
            this.Label_From.TabIndex = 27;
            this.Label_From.Text = "From:";
            // 
            // TextBox_From
            // 
            this.TextBox_From.Location = new System.Drawing.Point(245, 341);
            this.TextBox_From.Margin = new System.Windows.Forms.Padding(5);
            this.TextBox_From.Name = "TextBox_From";
            this.TextBox_From.Size = new System.Drawing.Size(55, 24);
            this.TextBox_From.TabIndex = 28;
            this.TextBox_From.Text = "1";
            // 
            // TextBox_To
            // 
            this.TextBox_To.Location = new System.Drawing.Point(345, 341);
            this.TextBox_To.Margin = new System.Windows.Forms.Padding(5);
            this.TextBox_To.Name = "TextBox_To";
            this.TextBox_To.Size = new System.Drawing.Size(55, 24);
            this.TextBox_To.TabIndex = 30;
            this.TextBox_To.Text = "365";
            // 
            // Label_To
            // 
            this.Label_To.AutoSize = true;
            this.Label_To.Location = new System.Drawing.Point(310, 344);
            this.Label_To.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Label_To.Name = "Label_To";
            this.Label_To.Size = new System.Drawing.Size(30, 18);
            this.Label_To.TabIndex = 29;
            this.Label_To.Text = "To:";
            // 
            // CheckBox_Sizing
            // 
            this.CheckBox_Sizing.AutoSize = true;
            this.CheckBox_Sizing.Checked = true;
            this.CheckBox_Sizing.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBox_Sizing.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CheckBox_Sizing.Location = new System.Drawing.Point(3, 312);
            this.CheckBox_Sizing.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CheckBox_Sizing.Name = "CheckBox_Sizing";
            this.CheckBox_Sizing.Size = new System.Drawing.Size(70, 22);
            this.CheckBox_Sizing.TabIndex = 31;
            this.CheckBox_Sizing.Text = "Sizing";
            this.CheckBox_Sizing.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 438);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(196, 18);
            this.label1.TabIndex = 32;
            this.label1.Text = "* simulation using Tas EDSL";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // ComboBoxControl_SolarCalculationMethod
            // 
            this.ComboBoxControl_SolarCalculationMethod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_SolarCalculationMethod.Description = "Solar Calculation Method:";
            this.ComboBoxControl_SolarCalculationMethod.Location = new System.Drawing.Point(-1, 221);
            this.ComboBoxControl_SolarCalculationMethod.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ComboBoxControl_SolarCalculationMethod.Name = "ComboBoxControl_SolarCalculationMethod";
            this.ComboBoxControl_SolarCalculationMethod.Size = new System.Drawing.Size(465, 61);
            this.ComboBoxControl_SolarCalculationMethod.TabIndex = 14;
            this.ComboBoxControl_SolarCalculationMethod.Load += new System.EventHandler(this.ComboBoxControl_SolarCalculationMethod_Load);
            // 
            // SimulateControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CheckBox_Sizing);
            this.Controls.Add(this.TextBox_To);
            this.Controls.Add(this.Label_To);
            this.Controls.Add(this.TextBox_From);
            this.Controls.Add(this.Label_From);
            this.Controls.Add(this.CheckBox_FullYearSimulation);
            this.Controls.Add(this.CheckBox_RoomDataSheets);
            this.Controls.Add(this.TextBox_ProjectName);
            this.Controls.Add(this.Label_ProjectName);
            this.Controls.Add(this.CheckBox_UpdateConstructionLayersByPanelType);
            this.Controls.Add(this.CheckBox_UnmetHours);
            this.Controls.Add(this.Button_WeatherData);
            this.Controls.Add(this.TextBox_WeatherData);
            this.Controls.Add(this.Label_WeatherData);
            this.Controls.Add(this.Button_OutputDirectory);
            this.Controls.Add(this.TextBox_OutputDirectory);
            this.Controls.Add(this.Label_OutputDirectory);
            this.Controls.Add(this.ComboBoxControl_SolarCalculationMethod);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "SimulateControl";
            this.Size = new System.Drawing.Size(513, 559);
            this.Load += new System.EventHandler(this.SimulateControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextBox_ProjectName;
        private System.Windows.Forms.Label Label_ProjectName;
        private System.Windows.Forms.CheckBox CheckBox_UpdateConstructionLayersByPanelType;
        private System.Windows.Forms.CheckBox CheckBox_UnmetHours;
        private System.Windows.Forms.Button Button_WeatherData;
        private System.Windows.Forms.TextBox TextBox_WeatherData;
        private System.Windows.Forms.Label Label_WeatherData;
        private System.Windows.Forms.Button Button_OutputDirectory;
        private System.Windows.Forms.TextBox TextBox_OutputDirectory;
        private System.Windows.Forms.Label Label_OutputDirectory;
        private Core.Windows.ComboBoxControl ComboBoxControl_SolarCalculationMethod;
        private System.Windows.Forms.CheckBox CheckBox_RoomDataSheets;
        private System.Windows.Forms.CheckBox CheckBox_FullYearSimulation;
        private System.Windows.Forms.Label Label_From;
        private System.Windows.Forms.TextBox TextBox_From;
        private System.Windows.Forms.TextBox TextBox_To;
        private System.Windows.Forms.Label Label_To;
        private System.Windows.Forms.CheckBox CheckBox_Sizing;
        private System.Windows.Forms.Label label1;
    }
}
