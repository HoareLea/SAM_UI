namespace SAM.Core.Mollier.UI.Forms
{
    partial class MollierPointForm
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
            this.Label_HumidityRatio = new System.Windows.Forms.Label();
            this.Label_DryBulbTemperature = new System.Windows.Forms.Label();
            this.TextBox_HumidityRatio = new System.Windows.Forms.TextBox();
            this.TextBox_DryBulbTemperature = new System.Windows.Forms.TextBox();
            this.Button_OK = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Label_HumidityRatio
            // 
            this.Label_HumidityRatio.AutoSize = true;
            this.Label_HumidityRatio.Location = new System.Drawing.Point(219, 68);
            this.Label_HumidityRatio.Name = "Label_HumidityRatio";
            this.Label_HumidityRatio.Size = new System.Drawing.Size(132, 16);
            this.Label_HumidityRatio.TabIndex = 0;
            this.Label_HumidityRatio.Text = "Humidity Ratio [g/kg]";
            // 
            // Label_DryBulbTemperature
            // 
            this.Label_DryBulbTemperature.AutoSize = true;
            this.Label_DryBulbTemperature.Location = new System.Drawing.Point(27, 68);
            this.Label_DryBulbTemperature.Name = "Label_DryBulbTemperature";
            this.Label_DryBulbTemperature.Size = new System.Drawing.Size(163, 16);
            this.Label_DryBulbTemperature.TabIndex = 1;
            this.Label_DryBulbTemperature.Text = "Dry Bulb Temperature [°C]";
            // 
            // TextBox_HumidityRatio
            // 
            this.TextBox_HumidityRatio.Location = new System.Drawing.Point(242, 102);
            this.TextBox_HumidityRatio.Name = "TextBox_HumidityRatio";
            this.TextBox_HumidityRatio.Size = new System.Drawing.Size(75, 22);
            this.TextBox_HumidityRatio.TabIndex = 2;
            this.TextBox_HumidityRatio.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // TextBox_DryBulbTemperature
            // 
            this.TextBox_DryBulbTemperature.Location = new System.Drawing.Point(71, 102);
            this.TextBox_DryBulbTemperature.Name = "TextBox_DryBulbTemperature";
            this.TextBox_DryBulbTemperature.Size = new System.Drawing.Size(78, 22);
            this.TextBox_DryBulbTemperature.TabIndex = 3;
            this.TextBox_DryBulbTemperature.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(242, 202);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 23);
            this.Button_OK.TabIndex = 4;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            this.Button_OK.Click += new System.EventHandler(this.Button_OK_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button_Cancel.Location = new System.Drawing.Point(323, 202);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 23);
            this.Button_Cancel.TabIndex = 5;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // MollierPointForm
            // 
            this.AcceptButton = this.Button_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.Button_Cancel;
            this.ClientSize = new System.Drawing.Size(410, 237);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.Button_OK);
            this.Controls.Add(this.TextBox_DryBulbTemperature);
            this.Controls.Add(this.TextBox_HumidityRatio);
            this.Controls.Add(this.Label_DryBulbTemperature);
            this.Controls.Add(this.Label_HumidityRatio);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierPointForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Point";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label_HumidityRatio;
        private System.Windows.Forms.Label Label_DryBulbTemperature;
        private System.Windows.Forms.TextBox TextBox_HumidityRatio;
        private System.Windows.Forms.TextBox TextBox_DryBulbTemperature;
        private System.Windows.Forms.Button Button_OK;
        private System.Windows.Forms.Button Button_Cancel;
    }
}