
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
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.wet_bulb_temperature_box = new System.Windows.Forms.CheckBox();
            this.density_box = new System.Windows.Forms.CheckBox();
            this.enthalpy_box = new System.Windows.Forms.CheckBox();
            this.specific_volume_box = new System.Windows.Forms.CheckBox();
            this.textBox_pressure = new System.Windows.Forms.TextBox();
            this.label_pressure = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Density_line = true;
            this.MollierControl_Main.Location = new System.Drawing.Point(12, 55);
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Size = new System.Drawing.Size(1047, 525);
            this.MollierControl_Main.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button2.Location = new System.Drawing.Point(921, 22);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(138, 27);
            this.button2.TabIndex = 18;
            this.button2.Text = "Second Graph";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button1.Location = new System.Drawing.Point(807, 21);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(108, 28);
            this.button1.TabIndex = 17;
            this.button1.Text = "First Graph";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // wet_bulb_temperature_box
            // 
            this.wet_bulb_temperature_box.AutoSize = true;
            this.wet_bulb_temperature_box.Checked = true;
            this.wet_bulb_temperature_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.wet_bulb_temperature_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.wet_bulb_temperature_box.Location = new System.Drawing.Point(520, 24);
            this.wet_bulb_temperature_box.Name = "wet_bulb_temperature_box";
            this.wet_bulb_temperature_box.Size = new System.Drawing.Size(153, 20);
            this.wet_bulb_temperature_box.TabIndex = 16;
            this.wet_bulb_temperature_box.Text = "wet bulb temperature";
            this.wet_bulb_temperature_box.UseVisualStyleBackColor = true;
            // 
            // density_box
            // 
            this.density_box.AutoSize = true;
            this.density_box.Checked = true;
            this.density_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.density_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.density_box.Location = new System.Drawing.Point(163, 24);
            this.density_box.Name = "density_box";
            this.density_box.Size = new System.Drawing.Size(72, 20);
            this.density_box.TabIndex = 15;
            this.density_box.Text = "density";
            this.density_box.UseVisualStyleBackColor = true;
            // 
            // enthalpy_box
            // 
            this.enthalpy_box.AutoSize = true;
            this.enthalpy_box.Checked = true;
            this.enthalpy_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.enthalpy_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.enthalpy_box.Location = new System.Drawing.Point(263, 24);
            this.enthalpy_box.Name = "enthalpy_box";
            this.enthalpy_box.Size = new System.Drawing.Size(80, 20);
            this.enthalpy_box.TabIndex = 14;
            this.enthalpy_box.Text = "enthalpy";
            this.enthalpy_box.UseVisualStyleBackColor = true;
            // 
            // specific_volume_box
            // 
            this.specific_volume_box.AutoSize = true;
            this.specific_volume_box.Checked = true;
            this.specific_volume_box.CheckState = System.Windows.Forms.CheckState.Checked;
            this.specific_volume_box.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.specific_volume_box.Location = new System.Drawing.Point(373, 24);
            this.specific_volume_box.Name = "specific_volume_box";
            this.specific_volume_box.Size = new System.Drawing.Size(122, 20);
            this.specific_volume_box.TabIndex = 13;
            this.specific_volume_box.Text = "specific volume";
            this.specific_volume_box.UseVisualStyleBackColor = true;
            // 
            // textBox_pressure
            // 
            this.textBox_pressure.Location = new System.Drawing.Point(75, 21);
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
            this.label_pressure.Location = new System.Drawing.Point(9, 24);
            this.label_pressure.Name = "label_pressure";
            this.label_pressure.Size = new System.Drawing.Size(60, 16);
            this.label_pressure.TabIndex = 11;
            this.label_pressure.Text = "pressure";
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1071, 592);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.wet_bulb_temperature_box);
            this.Controls.Add(this.density_box);
            this.Controls.Add(this.enthalpy_box);
            this.Controls.Add(this.specific_volume_box);
            this.Controls.Add(this.textBox_pressure);
            this.Controls.Add(this.label_pressure);
            this.Controls.Add(this.MollierControl_Main);
            this.Name = "MollierForm";
            this.Text = "Mollier";
            this.Load += new System.EventHandler(this.MollierForm_Load);
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
    }
}

