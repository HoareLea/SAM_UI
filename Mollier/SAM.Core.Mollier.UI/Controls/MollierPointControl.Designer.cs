namespace SAM.Core.Mollier.UI.Controls
{
    partial class MollierPointControl
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
            this.ComboBox_SecondParameter = new System.Windows.Forms.ComboBox();
            this.ComboBox_FirstParameter = new System.Windows.Forms.ComboBox();
            this.Label_Pressure = new System.Windows.Forms.Label();
            this.Label_FirstParameterUnit = new System.Windows.Forms.Label();
            this.Label_SecondParameterUnit = new System.Windows.Forms.Label();
            this.Label_PressureUnit = new System.Windows.Forms.Label();
            this.Button_SelectMollierPoint = new System.Windows.Forms.Button();
            this.NumberBoxControl_SecondParameter = new SAM.Core.Windows.NumberBoxControl();
            this.NumberBoxControl_FirstParameter = new SAM.Core.Windows.NumberBoxControl();
            this.NumberBoxControl_Pressure = new SAM.Core.Windows.NumberBoxControl();
            this.SuspendLayout();
            // 
            // ComboBox_SecondParameter
            // 
            this.ComboBox_SecondParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBox_SecondParameter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_SecondParameter.FormattingEnabled = true;
            this.ComboBox_SecondParameter.Location = new System.Drawing.Point(0, 137);
            this.ComboBox_SecondParameter.Name = "ComboBox_SecondParameter";
            this.ComboBox_SecondParameter.Size = new System.Drawing.Size(152, 24);
            this.ComboBox_SecondParameter.TabIndex = 1;
            this.ComboBox_SecondParameter.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SecondParameter_SelectedIndexChanged);
            this.ComboBox_SecondParameter.Click += new System.EventHandler(this.ComboBox_SecondParameter_Click);
            // 
            // ComboBox_FirstParameter
            // 
            this.ComboBox_FirstParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBox_FirstParameter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_FirstParameter.FormattingEnabled = true;
            this.ComboBox_FirstParameter.Location = new System.Drawing.Point(0, 69);
            this.ComboBox_FirstParameter.Name = "ComboBox_FirstParameter";
            this.ComboBox_FirstParameter.Size = new System.Drawing.Size(152, 24);
            this.ComboBox_FirstParameter.TabIndex = 3;
            this.ComboBox_FirstParameter.SelectedIndexChanged += new System.EventHandler(this.ComboBox_FirstParameter_SelectedIndexChanged);
            this.ComboBox_FirstParameter.Click += new System.EventHandler(this.ComboBox_FirstParameter_Click);
            // 
            // Label_Pressure
            // 
            this.Label_Pressure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_Pressure.AutoSize = true;
            this.Label_Pressure.Location = new System.Drawing.Point(3, 13);
            this.Label_Pressure.Name = "Label_Pressure";
            this.Label_Pressure.Size = new System.Drawing.Size(65, 17);
            this.Label_Pressure.TabIndex = 5;
            this.Label_Pressure.Text = "Pressure";
            // 
            // Label_FirstParameterUnit
            // 
            this.Label_FirstParameterUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_FirstParameterUnit.AutoSize = true;
            this.Label_FirstParameterUnit.Location = new System.Drawing.Point(110, 102);
            this.Label_FirstParameterUnit.Name = "Label_FirstParameterUnit";
            this.Label_FirstParameterUnit.Size = new System.Drawing.Size(42, 17);
            this.Label_FirstParameterUnit.TabIndex = 6;
            this.Label_FirstParameterUnit.Text = "kg/kg";
            this.Label_FirstParameterUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label_SecondParameterUnit
            // 
            this.Label_SecondParameterUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_SecondParameterUnit.AutoSize = true;
            this.Label_SecondParameterUnit.Location = new System.Drawing.Point(110, 170);
            this.Label_SecondParameterUnit.Name = "Label_SecondParameterUnit";
            this.Label_SecondParameterUnit.Size = new System.Drawing.Size(42, 17);
            this.Label_SecondParameterUnit.TabIndex = 7;
            this.Label_SecondParameterUnit.Text = "kg/kg";
            this.Label_SecondParameterUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label_PressureUnit
            // 
            this.Label_PressureUnit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_PressureUnit.AutoSize = true;
            this.Label_PressureUnit.Location = new System.Drawing.Point(113, 36);
            this.Label_PressureUnit.Name = "Label_PressureUnit";
            this.Label_PressureUnit.Size = new System.Drawing.Size(25, 17);
            this.Label_PressureUnit.TabIndex = 8;
            this.Label_PressureUnit.Text = "Pa";
            this.Label_PressureUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Button_SelectMollierPoint
            // 
            this.Button_SelectMollierPoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_SelectMollierPoint.Location = new System.Drawing.Point(120, 3);
            this.Button_SelectMollierPoint.Name = "Button_SelectMollierPoint";
            this.Button_SelectMollierPoint.Size = new System.Drawing.Size(25, 23);
            this.Button_SelectMollierPoint.TabIndex = 9;
            this.Button_SelectMollierPoint.Text = "...";
            this.Button_SelectMollierPoint.UseVisualStyleBackColor = true;
            this.Button_SelectMollierPoint.Visible = false;
            this.Button_SelectMollierPoint.Click += new System.EventHandler(this.Button_SelectMollierPoint_Click);
            // 
            // NumberBoxControl_SecondParameter
            // 
            this.NumberBoxControl_SecondParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NumberBoxControl_SecondParameter.Location = new System.Drawing.Point(0, 167);
            this.NumberBoxControl_SecondParameter.Name = "NumberBoxControl_SecondParameter";
            this.NumberBoxControl_SecondParameter.Size = new System.Drawing.Size(104, 22);
            this.NumberBoxControl_SecondParameter.String_NaN = "";
            this.NumberBoxControl_SecondParameter.String_NegativeInfinity = "";
            this.NumberBoxControl_SecondParameter.String_PositiveInfinity = "";
            this.NumberBoxControl_SecondParameter.TabIndex = 10;
            this.NumberBoxControl_SecondParameter.Tolerance = 0.001D;
            this.NumberBoxControl_SecondParameter.Value = double.NaN;
            // 
            // NumberBoxControl_FirstParameter
            // 
            this.NumberBoxControl_FirstParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NumberBoxControl_FirstParameter.Location = new System.Drawing.Point(0, 99);
            this.NumberBoxControl_FirstParameter.Name = "NumberBoxControl_FirstParameter";
            this.NumberBoxControl_FirstParameter.Size = new System.Drawing.Size(104, 22);
            this.NumberBoxControl_FirstParameter.String_NaN = "";
            this.NumberBoxControl_FirstParameter.String_NegativeInfinity = "";
            this.NumberBoxControl_FirstParameter.String_PositiveInfinity = "";
            this.NumberBoxControl_FirstParameter.TabIndex = 11;
            this.NumberBoxControl_FirstParameter.Tolerance = 0.001D;
            this.NumberBoxControl_FirstParameter.Value = double.NaN;
            // 
            // NumberBoxControl_Pressure
            // 
            this.NumberBoxControl_Pressure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NumberBoxControl_Pressure.Location = new System.Drawing.Point(3, 33);
            this.NumberBoxControl_Pressure.Name = "NumberBoxControl_Pressure";
            this.NumberBoxControl_Pressure.Size = new System.Drawing.Size(104, 22);
            this.NumberBoxControl_Pressure.String_NaN = "";
            this.NumberBoxControl_Pressure.String_NegativeInfinity = "";
            this.NumberBoxControl_Pressure.String_PositiveInfinity = "";
            this.NumberBoxControl_Pressure.TabIndex = 12;
            this.NumberBoxControl_Pressure.Tolerance = 0.001D;
            this.NumberBoxControl_Pressure.Value = double.NaN;
            // 
            // MollierPointControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.NumberBoxControl_Pressure);
            this.Controls.Add(this.NumberBoxControl_FirstParameter);
            this.Controls.Add(this.NumberBoxControl_SecondParameter);
            this.Controls.Add(this.Button_SelectMollierPoint);
            this.Controls.Add(this.Label_PressureUnit);
            this.Controls.Add(this.Label_SecondParameterUnit);
            this.Controls.Add(this.Label_FirstParameterUnit);
            this.Controls.Add(this.Label_Pressure);
            this.Controls.Add(this.ComboBox_FirstParameter);
            this.Controls.Add(this.ComboBox_SecondParameter);
            this.Name = "MollierPointControl";
            this.Size = new System.Drawing.Size(155, 200);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox ComboBox_SecondParameter;
        private System.Windows.Forms.ComboBox ComboBox_FirstParameter;
        private System.Windows.Forms.Label Label_Pressure;
        private System.Windows.Forms.Label Label_FirstParameterUnit;
        private System.Windows.Forms.Label Label_SecondParameterUnit;
        private System.Windows.Forms.Label Label_PressureUnit;
        private System.Windows.Forms.Button Button_SelectMollierPoint;
        private SAM.Core.Windows.NumberBoxControl NumberBoxControl_SecondParameter;
        private Windows.NumberBoxControl NumberBoxControl_FirstParameter;
        private Windows.NumberBoxControl NumberBoxControl_Pressure;
    }
}
