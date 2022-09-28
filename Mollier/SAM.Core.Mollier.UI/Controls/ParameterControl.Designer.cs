namespace SAM.Core.Mollier.UI.Controls
{
    partial class ParameterControl
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
            this.parameterNameLabel = new System.Windows.Forms.Label();
            this.Parameter_Value = new System.Windows.Forms.TextBox();
            this.parameterUnitLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // parameterNameLabel
            // 
            this.parameterNameLabel.AutoSize = true;
            this.parameterNameLabel.Location = new System.Drawing.Point(3, 0);
            this.parameterNameLabel.Name = "parameterNameLabel";
            this.parameterNameLabel.Size = new System.Drawing.Size(106, 16);
            this.parameterNameLabel.TabIndex = 0;
            this.parameterNameLabel.Text = "parameterName";
            // 
            // Parameter_Value
            // 
            this.Parameter_Value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Parameter_Value.Location = new System.Drawing.Point(0, 19);
            this.Parameter_Value.Name = "Parameter_Value";
            this.Parameter_Value.Size = new System.Drawing.Size(150, 22);
            this.Parameter_Value.TabIndex = 1;
            this.Parameter_Value.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Parameter_Value_KeyPress);
            // 
            // parameterUnitLabel
            // 
            this.parameterUnitLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.parameterUnitLabel.AutoSize = true;
            this.parameterUnitLabel.Location = new System.Drawing.Point(156, 22);
            this.parameterUnitLabel.Name = "parameterUnitLabel";
            this.parameterUnitLabel.Size = new System.Drawing.Size(41, 16);
            this.parameterUnitLabel.TabIndex = 2;
            this.parameterUnitLabel.Text = "kg/kg";
            // 
            // ParameterControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.parameterUnitLabel);
            this.Controls.Add(this.Parameter_Value);
            this.Controls.Add(this.parameterNameLabel);
            this.Name = "ParameterControl";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Size = new System.Drawing.Size(200, 51);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label parameterNameLabel;
        private System.Windows.Forms.TextBox Parameter_Value;
        private System.Windows.Forms.Label parameterUnitLabel;
    }
}
