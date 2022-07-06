
namespace SAM.Core.Mollier.UI
{
    partial class MollierForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.wet_bulb_temperature_box = new System.Windows.Forms.CheckBox();
            this.density_box = new System.Windows.Forms.CheckBox();
            this.enthalpy_box = new System.Windows.Forms.CheckBox();
            this.specific_volume_box = new System.Windows.Forms.CheckBox();
            this.textBox_pressure = new System.Windows.Forms.TextBox();
            this.label_pressure = new System.Windows.Forms.Label();
            this.Add_new_point = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem_File = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_View = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Density = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Enthalpy = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SpecificVolume = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_WetBulbTemperature = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_ChartType = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Mollier = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Psychrometric = new System.Windows.Forms.ToolStripMenuItem();
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button2.Location = new System.Drawing.Point(921, 28);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(138, 27);
            this.button2.TabIndex = 18;
            this.button2.Text = "Pyschrometric Chart";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button1.Location = new System.Drawing.Point(807, 27);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(108, 28);
            this.button1.TabIndex = 17;
            this.button1.Text = "Mollier Chart";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // wet_bulb_temperature_box
            // 
            this.wet_bulb_temperature_box.AutoSize = true;
            this.wet_bulb_temperature_box.Checked = true;
            this.wet_bulb_temperature_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.wet_bulb_temperature_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.wet_bulb_temperature_box.Location = new System.Drawing.Point(520, 30);
            this.wet_bulb_temperature_box.Name = "wet_bulb_temperature_box";
            this.wet_bulb_temperature_box.Size = new System.Drawing.Size(163, 21);
            this.wet_bulb_temperature_box.TabIndex = 16;
            this.wet_bulb_temperature_box.Text = "wet bulb temperature";
            this.wet_bulb_temperature_box.UseVisualStyleBackColor = true;
            this.wet_bulb_temperature_box.CheckedChanged += new System.EventHandler(this.wet_bulb_temperature_box_CheckedChanged);
            // 
            // density_box
            // 
            this.density_box.AutoSize = true;
            this.density_box.Checked = true;
            this.density_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.density_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.density_box.Location = new System.Drawing.Point(163, 30);
            this.density_box.Name = "density_box";
            this.density_box.Size = new System.Drawing.Size(75, 21);
            this.density_box.TabIndex = 15;
            this.density_box.Text = "density";
            this.density_box.UseVisualStyleBackColor = true;
            this.density_box.CheckedChanged += new System.EventHandler(this.density_box_CheckedChanged);
            // 
            // enthalpy_box
            // 
            this.enthalpy_box.AutoSize = true;
            this.enthalpy_box.Checked = true;
            this.enthalpy_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.enthalpy_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.enthalpy_box.Location = new System.Drawing.Point(263, 30);
            this.enthalpy_box.Name = "enthalpy_box";
            this.enthalpy_box.Size = new System.Drawing.Size(84, 21);
            this.enthalpy_box.TabIndex = 14;
            this.enthalpy_box.Text = "enthalpy";
            this.enthalpy_box.UseVisualStyleBackColor = true;
            this.enthalpy_box.CheckedChanged += new System.EventHandler(this.enthalpy_box_CheckedChanged);
            // 
            // specific_volume_box
            // 
            this.specific_volume_box.AutoSize = true;
            this.specific_volume_box.Checked = true;
            this.specific_volume_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.specific_volume_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.specific_volume_box.Location = new System.Drawing.Point(373, 30);
            this.specific_volume_box.Name = "specific_volume_box";
            this.specific_volume_box.Size = new System.Drawing.Size(126, 21);
            this.specific_volume_box.TabIndex = 13;
            this.specific_volume_box.Text = "specific volume";
            this.specific_volume_box.UseVisualStyleBackColor = true;
            this.specific_volume_box.CheckedChanged += new System.EventHandler(this.specific_volume_box_CheckedChanged);
            // 
            // textBox_pressure
            // 
            this.textBox_pressure.Location = new System.Drawing.Point(75, 27);
            this.textBox_pressure.Name = "textBox_pressure";
            this.textBox_pressure.Size = new System.Drawing.Size(63, 22);
            this.textBox_pressure.TabIndex = 12;
            this.textBox_pressure.Text = "101325";
            this.textBox_pressure.TextChanged += new System.EventHandler(this.textBox_pressure_TextChanged);
            // 
            // label_pressure
            // 
            this.label_pressure.AutoSize = true;
            this.label_pressure.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_pressure.Location = new System.Drawing.Point(9, 30);
            this.label_pressure.Name = "label_pressure";
            this.label_pressure.Size = new System.Drawing.Size(64, 17);
            this.label_pressure.TabIndex = 11;
            this.label_pressure.Text = "pressure";
            // 
            // Add_new_point
            // 
            this.Add_new_point.Location = new System.Drawing.Point(679, 27);
            this.Add_new_point.Name = "Add_new_point";
            this.Add_new_point.Size = new System.Drawing.Size(108, 28);
            this.Add_new_point.TabIndex = 19;
            this.Add_new_point.Text = "Add Point";
            this.Add_new_point.UseVisualStyleBackColor = true;
            this.Add_new_point.Click += new System.EventHandler(this.Add_new_point_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_File,
            this.ToolStripMenuItem_View});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1071, 28);
            this.menuStrip1.TabIndex = 20;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ToolStripMenuItem_File
            // 
            this.ToolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Save});
            this.ToolStripMenuItem_File.Name = "ToolStripMenuItem_File";
            this.ToolStripMenuItem_File.Size = new System.Drawing.Size(46, 24);
            this.ToolStripMenuItem_File.Text = "File";
            // 
            // ToolStripMenuItem_Save
            // 
            this.ToolStripMenuItem_Save.Name = "ToolStripMenuItem_Save";
            this.ToolStripMenuItem_Save.Size = new System.Drawing.Size(123, 26);
            this.ToolStripMenuItem_Save.Text = "Save";
            this.ToolStripMenuItem_Save.Click += new System.EventHandler(this.ToolStripMenuItem_Save_Click);
            // 
            // ToolStripMenuItem_View
            // 
            this.ToolStripMenuItem_View.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Density,
            this.ToolStripMenuItem_Enthalpy,
            this.ToolStripMenuItem_SpecificVolume,
            this.ToolStripMenuItem_WetBulbTemperature,
            this.ToolStripMenuItem_ChartType});
            this.ToolStripMenuItem_View.Name = "ToolStripMenuItem_View";
            this.ToolStripMenuItem_View.Size = new System.Drawing.Size(55, 24);
            this.ToolStripMenuItem_View.Text = "View";
            // 
            // ToolStripMenuItem_Density
            // 
            this.ToolStripMenuItem_Density.Checked = true;
            this.ToolStripMenuItem_Density.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Density.Name = "ToolStripMenuItem_Density";
            this.ToolStripMenuItem_Density.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Density.Text = "Density";
            this.ToolStripMenuItem_Density.Click += new System.EventHandler(this.ToolStripMenuItem_Density_Click);
            // 
            // ToolStripMenuItem_Enthalpy
            // 
            this.ToolStripMenuItem_Enthalpy.Checked = true;
            this.ToolStripMenuItem_Enthalpy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Enthalpy.Name = "ToolStripMenuItem_Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Enthalpy.Text = "Enthalpy";
            // 
            // ToolStripMenuItem_SpecificVolume
            // 
            this.ToolStripMenuItem_SpecificVolume.Checked = true;
            this.ToolStripMenuItem_SpecificVolume.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_SpecificVolume.Name = "ToolStripMenuItem_SpecificVolume";
            this.ToolStripMenuItem_SpecificVolume.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_SpecificVolume.Text = "Specific Volume";
            // 
            // ToolStripMenuItem_WetBulbTemperature
            // 
            this.ToolStripMenuItem_WetBulbTemperature.Checked = true;
            this.ToolStripMenuItem_WetBulbTemperature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_WetBulbTemperature.Name = "ToolStripMenuItem_WetBulbTemperature";
            this.ToolStripMenuItem_WetBulbTemperature.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_WetBulbTemperature.Text = "Wet Bulb Temperature";
            // 
            // ToolStripMenuItem_ChartType
            // 
            this.ToolStripMenuItem_ChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChartToolStripMenuItem_Mollier,
            this.ChartToolStripMenuItem_Psychrometric});
            this.ToolStripMenuItem_ChartType.Name = "ToolStripMenuItem_ChartType";
            this.ToolStripMenuItem_ChartType.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_ChartType.Text = "Chart Type";
            // 
            // ChartToolStripMenuItem_Mollier
            // 
            this.ChartToolStripMenuItem_Mollier.Name = "ChartToolStripMenuItem_Mollier";
            this.ChartToolStripMenuItem_Mollier.Size = new System.Drawing.Size(184, 26);
            this.ChartToolStripMenuItem_Mollier.Text = "Mollier";
            // 
            // ChartToolStripMenuItem_Psychrometric
            // 
            this.ChartToolStripMenuItem_Psychrometric.Checked = true;
            this.ChartToolStripMenuItem_Psychrometric.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChartToolStripMenuItem_Psychrometric.Name = "ChartToolStripMenuItem_Psychrometric";
            this.ChartToolStripMenuItem_Psychrometric.Size = new System.Drawing.Size(184, 26);
            this.ChartToolStripMenuItem_Psychrometric.Text = "Psychrometric";
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MollierControl_Main.ChartType = SAM.Core.Mollier.ChartType.Mollier;
            this.MollierControl_Main.Density_line = true;
            this.MollierControl_Main.Enthalpy_line = true;
            this.MollierControl_Main.Location = new System.Drawing.Point(12, 61);
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Pressure = 101325D;
            this.MollierControl_Main.Size = new System.Drawing.Size(1047, 519);
            this.MollierControl_Main.Specific_volume_line = true;
            this.MollierControl_Main.TabIndex = 0;
            this.MollierControl_Main.Wet_bulb_temperature_line = true;
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1071, 592);
            this.Controls.Add(this.Add_new_point);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.wet_bulb_temperature_box);
            this.Controls.Add(this.density_box);
            this.Controls.Add(this.enthalpy_box);
            this.Controls.Add(this.specific_volume_box);
            this.Controls.Add(this.textBox_pressure);
            this.Controls.Add(this.label_pressure);
            this.Controls.Add(this.MollierControl_Main);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MollierForm";
            this.Text = "Mollier";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MollierForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.MollierControl MollierControl_Main;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox wet_bulb_temperature_box;
        private System.Windows.Forms.CheckBox density_box;
        private System.Windows.Forms.CheckBox enthalpy_box;
        private System.Windows.Forms.CheckBox specific_volume_box;
        private System.Windows.Forms.TextBox textBox_pressure;
        private System.Windows.Forms.Label label_pressure;
        private System.Windows.Forms.Button Add_new_point;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_File;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Save;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_View;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Density;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Enthalpy;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_SpecificVolume;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_WetBulbTemperature;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_ChartType;
        private System.Windows.Forms.ToolStripMenuItem ChartToolStripMenuItem_Mollier;
        private System.Windows.Forms.ToolStripMenuItem ChartToolStripMenuItem_Psychrometric;
    }
}

