
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
            this.ComboBoxControl_SolarCalculationMethod = new SAM.Core.Windows.ComboBoxControl();
            this.CheckBox_RoomDataSheets = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // TextBox_ProjectName
            // 
            this.TextBox_ProjectName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_ProjectName.Location = new System.Drawing.Point(3, 23);
            this.TextBox_ProjectName.Name = "TextBox_ProjectName";
            this.TextBox_ProjectName.Size = new System.Drawing.Size(383, 22);
            this.TextBox_ProjectName.TabIndex = 24;
            // 
            // Label_ProjectName
            // 
            this.Label_ProjectName.AutoSize = true;
            this.Label_ProjectName.Location = new System.Drawing.Point(3, 0);
            this.Label_ProjectName.Name = "Label_ProjectName";
            this.Label_ProjectName.Size = new System.Drawing.Size(97, 17);
            this.Label_ProjectName.TabIndex = 23;
            this.Label_ProjectName.Text = "Project Name:";
            // 
            // CheckBox_UpdateConstructionLayersByPanelType
            // 
            this.CheckBox_UpdateConstructionLayersByPanelType.AutoSize = true;
            this.CheckBox_UpdateConstructionLayersByPanelType.Checked = true;
            this.CheckBox_UpdateConstructionLayersByPanelType.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBox_UpdateConstructionLayersByPanelType.Location = new System.Drawing.Point(3, 295);
            this.CheckBox_UpdateConstructionLayersByPanelType.Name = "CheckBox_UpdateConstructionLayersByPanelType";
            this.CheckBox_UpdateConstructionLayersByPanelType.Size = new System.Drawing.Size(353, 21);
            this.CheckBox_UpdateConstructionLayersByPanelType.TabIndex = 21;
            this.CheckBox_UpdateConstructionLayersByPanelType.Text = "Update Missing Construction Layers By Panel Type";
            this.CheckBox_UpdateConstructionLayersByPanelType.UseVisualStyleBackColor = true;
            // 
            // CheckBox_UnmetHours
            // 
            this.CheckBox_UnmetHours.AutoSize = true;
            this.CheckBox_UnmetHours.Location = new System.Drawing.Point(3, 256);
            this.CheckBox_UnmetHours.Name = "CheckBox_UnmetHours";
            this.CheckBox_UnmetHours.Size = new System.Drawing.Size(113, 21);
            this.CheckBox_UnmetHours.TabIndex = 22;
            this.CheckBox_UnmetHours.Text = "Unmet Hours";
            this.CheckBox_UnmetHours.UseVisualStyleBackColor = true;
            // 
            // Button_WeatherData
            // 
            this.Button_WeatherData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_WeatherData.Location = new System.Drawing.Point(346, 152);
            this.Button_WeatherData.Name = "Button_WeatherData";
            this.Button_WeatherData.Size = new System.Drawing.Size(40, 23);
            this.Button_WeatherData.TabIndex = 20;
            this.Button_WeatherData.Text = "...";
            this.Button_WeatherData.UseVisualStyleBackColor = true;
            this.Button_WeatherData.Click += new System.EventHandler(this.Button_WeatherData_Click);
            // 
            // TextBox_WeatherData
            // 
            this.TextBox_WeatherData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_WeatherData.Location = new System.Drawing.Point(3, 153);
            this.TextBox_WeatherData.Name = "TextBox_WeatherData";
            this.TextBox_WeatherData.ReadOnly = true;
            this.TextBox_WeatherData.Size = new System.Drawing.Size(337, 22);
            this.TextBox_WeatherData.TabIndex = 19;
            // 
            // Label_WeatherData
            // 
            this.Label_WeatherData.AutoSize = true;
            this.Label_WeatherData.Location = new System.Drawing.Point(3, 130);
            this.Label_WeatherData.Name = "Label_WeatherData";
            this.Label_WeatherData.Size = new System.Drawing.Size(100, 17);
            this.Label_WeatherData.TabIndex = 18;
            this.Label_WeatherData.Text = "Weather Data:";
            // 
            // Button_OutputDirectory
            // 
            this.Button_OutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OutputDirectory.Location = new System.Drawing.Point(346, 88);
            this.Button_OutputDirectory.Name = "Button_OutputDirectory";
            this.Button_OutputDirectory.Size = new System.Drawing.Size(40, 23);
            this.Button_OutputDirectory.TabIndex = 17;
            this.Button_OutputDirectory.Text = "...";
            this.Button_OutputDirectory.UseVisualStyleBackColor = true;
            this.Button_OutputDirectory.Click += new System.EventHandler(this.Button_OutputDirectory_Click);
            // 
            // TextBox_OutputDirectory
            // 
            this.TextBox_OutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_OutputDirectory.Location = new System.Drawing.Point(3, 88);
            this.TextBox_OutputDirectory.Name = "TextBox_OutputDirectory";
            this.TextBox_OutputDirectory.Size = new System.Drawing.Size(337, 22);
            this.TextBox_OutputDirectory.TabIndex = 16;
            // 
            // Label_OutputDirectory
            // 
            this.Label_OutputDirectory.AutoSize = true;
            this.Label_OutputDirectory.Location = new System.Drawing.Point(3, 65);
            this.Label_OutputDirectory.Name = "Label_OutputDirectory";
            this.Label_OutputDirectory.Size = new System.Drawing.Size(114, 17);
            this.Label_OutputDirectory.TabIndex = 15;
            this.Label_OutputDirectory.Text = "Output directory:";
            // 
            // ComboBoxControl_SolarCalculationMethod
            // 
            this.ComboBoxControl_SolarCalculationMethod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_SolarCalculationMethod.Description = "Solar Calculation Method:";
            this.ComboBoxControl_SolarCalculationMethod.Location = new System.Drawing.Point(0, 195);
            this.ComboBoxControl_SolarCalculationMethod.Name = "ComboBoxControl_SolarCalculationMethod";
            this.ComboBoxControl_SolarCalculationMethod.Size = new System.Drawing.Size(386, 55);
            this.ComboBoxControl_SolarCalculationMethod.TabIndex = 14;
            // 
            // CheckBox_RoomDataSheets
            // 
            this.CheckBox_RoomDataSheets.AutoSize = true;
            this.CheckBox_RoomDataSheets.Location = new System.Drawing.Point(3, 335);
            this.CheckBox_RoomDataSheets.Name = "CheckBox_RoomDataSheets";
            this.CheckBox_RoomDataSheets.Size = new System.Drawing.Size(225, 21);
            this.CheckBox_RoomDataSheets.TabIndex = 25;
            this.CheckBox_RoomDataSheets.Text = "Print Room Data Sheets (RDS)";
            this.CheckBox_RoomDataSheets.UseVisualStyleBackColor = true;
            // 
            // SimulateControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            this.Name = "SimulateControl";
            this.Size = new System.Drawing.Size(389, 365);
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
    }
}
