namespace SAM.Core.Mollier.UI.Controls
{
    partial class BuiltInVisibilitySettingControl
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
            this.ComboBox_ChartParameterType = new System.Windows.Forms.ComboBox();
            this.ComboBox_ChartDataType = new System.Windows.Forms.ComboBox();
            this.ColorDialog_Main = new System.Windows.Forms.ColorDialog();
            this.Button_Color = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ComboBox_ChartParameterType
            // 
            this.ComboBox_ChartParameterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_ChartParameterType.Enabled = false;
            this.ComboBox_ChartParameterType.FormattingEnabled = true;
            this.ComboBox_ChartParameterType.Location = new System.Drawing.Point(3, 8);
            this.ComboBox_ChartParameterType.Name = "ComboBox_ChartParameterType";
            this.ComboBox_ChartParameterType.Size = new System.Drawing.Size(139, 24);
            this.ComboBox_ChartParameterType.TabIndex = 0;
            // 
            // ComboBox_ChartDataType
            // 
            this.ComboBox_ChartDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_ChartDataType.Enabled = false;
            this.ComboBox_ChartDataType.FormattingEnabled = true;
            this.ComboBox_ChartDataType.Location = new System.Drawing.Point(148, 9);
            this.ComboBox_ChartDataType.Name = "ComboBox_ChartDataType";
            this.ComboBox_ChartDataType.Size = new System.Drawing.Size(226, 24);
            this.ComboBox_ChartDataType.TabIndex = 1;
            // 
            // Button_Color
            // 
            this.Button_Color.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Color.Location = new System.Drawing.Point(380, 10);
            this.Button_Color.Name = "Button_Color";
            this.Button_Color.Size = new System.Drawing.Size(208, 23);
            this.Button_Color.TabIndex = 2;
            this.Button_Color.UseVisualStyleBackColor = true;
            this.Button_Color.Click += new System.EventHandler(this.Button_Color_Click);
            // 
            // BuiltInVisibilitySettingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Button_Color);
            this.Controls.Add(this.ComboBox_ChartDataType);
            this.Controls.Add(this.ComboBox_ChartParameterType);
            this.Name = "BuiltInVisibilitySettingControl";
            this.Size = new System.Drawing.Size(613, 44);
            this.Load += new System.EventHandler(this.BuiltInVisibilitySettingControl_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox ComboBox_ChartParameterType;
        private System.Windows.Forms.ComboBox ComboBox_ChartDataType;
        private System.Windows.Forms.ColorDialog ColorDialog_Main;
        private System.Windows.Forms.Button Button_Color;
    }
}
