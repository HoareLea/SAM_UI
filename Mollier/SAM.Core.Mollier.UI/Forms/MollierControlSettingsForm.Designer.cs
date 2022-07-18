
namespace SAM.Core.Mollier.UI
{
    partial class MollierControlSettingsForm
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
            this.TabControl_Main = new System.Windows.Forms.TabControl();
            this.TabPage_View = new System.Windows.Forms.TabPage();
            this.TabPage_Ranges = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.HumidityRatioIntervalTextbox = new System.Windows.Forms.TextBox();
            this.HumidityRatioMaximumValueTextbox = new System.Windows.Forms.TextBox();
            this.HumidityRatioMinimumValueTextbox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.TemperatureIntervalTextbox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.TemperatureMaximumValueTextbox = new System.Windows.Forms.TextBox();
            this.TemperatureMinimumValueTextbox = new System.Windows.Forms.TextBox();
            this.Button_OK = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.Button_Apply = new System.Windows.Forms.Button();
            this.Button_ResetChart = new System.Windows.Forms.Button();
            this.TabControl_Main.SuspendLayout();
            this.TabPage_Ranges.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControl_Main
            // 
            this.TabControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TabControl_Main.Controls.Add(this.TabPage_View);
            this.TabControl_Main.Controls.Add(this.TabPage_Ranges);
            this.TabControl_Main.Location = new System.Drawing.Point(12, 12);
            this.TabControl_Main.Name = "TabControl_Main";
            this.TabControl_Main.SelectedIndex = 0;
            this.TabControl_Main.Size = new System.Drawing.Size(681, 395);
            this.TabControl_Main.TabIndex = 0;
            // 
            // TabPage_View
            // 
            this.TabPage_View.Location = new System.Drawing.Point(4, 25);
            this.TabPage_View.Name = "TabPage_View";
            this.TabPage_View.Padding = new System.Windows.Forms.Padding(3);
            this.TabPage_View.Size = new System.Drawing.Size(673, 366);
            this.TabPage_View.TabIndex = 0;
            this.TabPage_View.Text = "View";
            this.TabPage_View.UseVisualStyleBackColor = true;
            // 
            // TabPage_Ranges
            // 
            this.TabPage_Ranges.Controls.Add(this.label8);
            this.TabPage_Ranges.Controls.Add(this.label7);
            this.TabPage_Ranges.Controls.Add(this.label6);
            this.TabPage_Ranges.Controls.Add(this.label5);
            this.TabPage_Ranges.Controls.Add(this.HumidityRatioIntervalTextbox);
            this.TabPage_Ranges.Controls.Add(this.HumidityRatioMaximumValueTextbox);
            this.TabPage_Ranges.Controls.Add(this.HumidityRatioMinimumValueTextbox);
            this.TabPage_Ranges.Controls.Add(this.label4);
            this.TabPage_Ranges.Controls.Add(this.label3);
            this.TabPage_Ranges.Controls.Add(this.TemperatureIntervalTextbox);
            this.TabPage_Ranges.Controls.Add(this.label2);
            this.TabPage_Ranges.Controls.Add(this.label1);
            this.TabPage_Ranges.Controls.Add(this.TemperatureMaximumValueTextbox);
            this.TabPage_Ranges.Controls.Add(this.TemperatureMinimumValueTextbox);
            this.TabPage_Ranges.Location = new System.Drawing.Point(4, 25);
            this.TabPage_Ranges.Name = "TabPage_Ranges";
            this.TabPage_Ranges.Padding = new System.Windows.Forms.Padding(3);
            this.TabPage_Ranges.Size = new System.Drawing.Size(673, 366);
            this.TabPage_Ranges.TabIndex = 1;
            this.TabPage_Ranges.Text = "Ranges";
            this.TabPage_Ranges.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label8.Location = new System.Drawing.Point(403, 161);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(50, 16);
            this.label8.TabIndex = 47;
            this.label8.Text = "Interval";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(273, 161);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 16);
            this.label7.TabIndex = 46;
            this.label7.Text = "Maximum Value";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label6.Location = new System.Drawing.Point(169, 161);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(98, 16);
            this.label6.TabIndex = 45;
            this.label6.Text = "Minimum Value";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label5.Location = new System.Drawing.Point(253, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(119, 16);
            this.label5.TabIndex = 44;
            this.label5.Text = "HumidityRatio Axis";
            // 
            // HumidityRatioIntervalTextbox
            // 
            this.HumidityRatioIntervalTextbox.Location = new System.Drawing.Point(378, 180);
            this.HumidityRatioIntervalTextbox.Name = "HumidityRatioIntervalTextbox";
            this.HumidityRatioIntervalTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioIntervalTextbox.TabIndex = 43;
            this.HumidityRatioIntervalTextbox.Text = "5";
            // 
            // HumidityRatioMaximumValueTextbox
            // 
            this.HumidityRatioMaximumValueTextbox.Location = new System.Drawing.Point(273, 180);
            this.HumidityRatioMaximumValueTextbox.Name = "HumidityRatioMaximumValueTextbox";
            this.HumidityRatioMaximumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioMaximumValueTextbox.TabIndex = 42;
            this.HumidityRatioMaximumValueTextbox.Text = "35";
            // 
            // HumidityRatioMinimumValueTextbox
            // 
            this.HumidityRatioMinimumValueTextbox.Location = new System.Drawing.Point(168, 180);
            this.HumidityRatioMinimumValueTextbox.Name = "HumidityRatioMinimumValueTextbox";
            this.HumidityRatioMinimumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioMinimumValueTextbox.TabIndex = 41;
            this.HumidityRatioMinimumValueTextbox.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(397, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 16);
            this.label4.TabIndex = 40;
            this.label4.Text = "Interval";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(270, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 16);
            this.label3.TabIndex = 39;
            this.label3.Text = "Maximum Value";
            // 
            // TemperatureIntervalTextbox
            // 
            this.TemperatureIntervalTextbox.Location = new System.Drawing.Point(375, 68);
            this.TemperatureIntervalTextbox.Name = "TemperatureIntervalTextbox";
            this.TemperatureIntervalTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureIntervalTextbox.TabIndex = 38;
            this.TemperatureIntervalTextbox.Text = "5";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(168, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 16);
            this.label2.TabIndex = 37;
            this.label2.Text = "Minimum Value";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(253, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 16);
            this.label1.TabIndex = 36;
            this.label1.Text = "Temperature Axis";
            // 
            // TemperatureMaximumValueTextbox
            // 
            this.TemperatureMaximumValueTextbox.Location = new System.Drawing.Point(270, 68);
            this.TemperatureMaximumValueTextbox.Name = "TemperatureMaximumValueTextbox";
            this.TemperatureMaximumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureMaximumValueTextbox.TabIndex = 35;
            this.TemperatureMaximumValueTextbox.Text = "50";
            // 
            // TemperatureMinimumValueTextbox
            // 
            this.TemperatureMinimumValueTextbox.Location = new System.Drawing.Point(165, 68);
            this.TemperatureMinimumValueTextbox.Name = "TemperatureMinimumValueTextbox";
            this.TemperatureMinimumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureMinimumValueTextbox.TabIndex = 34;
            this.TemperatureMinimumValueTextbox.Text = "-20";
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(533, 413);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 28);
            this.Button_OK.TabIndex = 8;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            this.Button_OK.Click += new System.EventHandler(this.Button_OK_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button_Cancel.Location = new System.Drawing.Point(614, 413);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 28);
            this.Button_Cancel.TabIndex = 7;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // Button_Apply
            // 
            this.Button_Apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Apply.Location = new System.Drawing.Point(452, 413);
            this.Button_Apply.Name = "Button_Apply";
            this.Button_Apply.Size = new System.Drawing.Size(75, 28);
            this.Button_Apply.TabIndex = 8;
            this.Button_Apply.Text = "Apply";
            this.Button_Apply.UseVisualStyleBackColor = true;
            this.Button_Apply.Click += new System.EventHandler(this.Button_Apply_Click);
            // 
            // Button_ResetChart
            // 
            this.Button_ResetChart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_ResetChart.Location = new System.Drawing.Point(16, 413);
            this.Button_ResetChart.Name = "Button_ResetChart";
            this.Button_ResetChart.Size = new System.Drawing.Size(98, 28);
            this.Button_ResetChart.TabIndex = 9;
            this.Button_ResetChart.Text = "Reset Chart";
            this.Button_ResetChart.UseVisualStyleBackColor = true;
            this.Button_ResetChart.Click += new System.EventHandler(this.Button_ResetChart_Click);
            // 
            // MollierSettingsForm
            // 
            this.AcceptButton = this.Button_OK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.Button_Cancel;
            this.ClientSize = new System.Drawing.Size(705, 453);
            this.Controls.Add(this.Button_ResetChart);
            this.Controls.Add(this.Button_Apply);
            this.Controls.Add(this.Button_OK);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.TabControl_Main);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierSettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Mollier Settings";
            this.TabControl_Main.ResumeLayout(false);
            this.TabPage_Ranges.ResumeLayout(false);
            this.TabPage_Ranges.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl TabControl_Main;
        private System.Windows.Forms.TabPage TabPage_View;
        private System.Windows.Forms.TabPage TabPage_Ranges;
        private System.Windows.Forms.Button Button_OK;
        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TemperatureIntervalTextbox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TemperatureMaximumValueTextbox;
        private System.Windows.Forms.TextBox TemperatureMinimumValueTextbox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox HumidityRatioIntervalTextbox;
        private System.Windows.Forms.TextBox HumidityRatioMaximumValueTextbox;
        private System.Windows.Forms.TextBox HumidityRatioMinimumValueTextbox;
        private System.Windows.Forms.Button Button_Apply;
        private System.Windows.Forms.Button Button_ResetChart;
    }
}