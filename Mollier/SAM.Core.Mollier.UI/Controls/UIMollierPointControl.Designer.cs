namespace SAM.Core.Mollier.UI.Controls
{
    partial class UIMollierPointControl
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
            this.PointLabel_TextBox = new System.Windows.Forms.TextBox();
            this.PointLabel_Label = new System.Windows.Forms.Label();
            this.PointColor_Label = new System.Windows.Forms.Label();
            this.PointColor_Button = new System.Windows.Forms.Button();
            this.Label_LabelColor = new System.Windows.Forms.Label();
            this.LabelColor_Button = new System.Windows.Forms.Button();
            this.Button_PointClear = new System.Windows.Forms.Button();
            this.Button_LabelClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PointLabel_TextBox
            // 
            this.PointLabel_TextBox.Location = new System.Drawing.Point(141, 44);
            this.PointLabel_TextBox.Name = "PointLabel_TextBox";
            this.PointLabel_TextBox.Size = new System.Drawing.Size(88, 22);
            this.PointLabel_TextBox.TabIndex = 2;
            // 
            // PointLabel_Label
            // 
            this.PointLabel_Label.AutoSize = true;
            this.PointLabel_Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.PointLabel_Label.Location = new System.Drawing.Point(7, 42);
            this.PointLabel_Label.Name = "PointLabel_Label";
            this.PointLabel_Label.Size = new System.Drawing.Size(93, 20);
            this.PointLabel_Label.TabIndex = 16;
            this.PointLabel_Label.Text = "Point Label";
            // 
            // PointColor_Label
            // 
            this.PointColor_Label.AutoSize = true;
            this.PointColor_Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.PointColor_Label.Location = new System.Drawing.Point(7, 12);
            this.PointColor_Label.Name = "PointColor_Label";
            this.PointColor_Label.Size = new System.Drawing.Size(89, 20);
            this.PointColor_Label.TabIndex = 15;
            this.PointColor_Label.Text = "Point color";
            // 
            // PointColor_Button
            // 
            this.PointColor_Button.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PointColor_Button.Location = new System.Drawing.Point(141, 12);
            this.PointColor_Button.Name = "PointColor_Button";
            this.PointColor_Button.Size = new System.Drawing.Size(59, 23);
            this.PointColor_Button.TabIndex = 1;
            this.PointColor_Button.UseVisualStyleBackColor = true;
            this.PointColor_Button.Click += new System.EventHandler(this.PointColor_Button_Click);
            // 
            // Label_LabelColor
            // 
            this.Label_LabelColor.AutoSize = true;
            this.Label_LabelColor.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.Label_LabelColor.Location = new System.Drawing.Point(7, 72);
            this.Label_LabelColor.Name = "Label_LabelColor";
            this.Label_LabelColor.Size = new System.Drawing.Size(92, 20);
            this.Label_LabelColor.TabIndex = 18;
            this.Label_LabelColor.Text = "Label color";
            // 
            // LabelColor_Button
            // 
            this.LabelColor_Button.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelColor_Button.Location = new System.Drawing.Point(141, 72);
            this.LabelColor_Button.Name = "LabelColor_Button";
            this.LabelColor_Button.Size = new System.Drawing.Size(59, 23);
            this.LabelColor_Button.TabIndex = 17;
            this.LabelColor_Button.UseVisualStyleBackColor = true;
            this.LabelColor_Button.Click += new System.EventHandler(this.LabelColor_Button_Click);
            // 
            // Button_PointClear
            // 
            this.Button_PointClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_PointClear.Location = new System.Drawing.Point(206, 12);
            this.Button_PointClear.Name = "Button_PointClear";
            this.Button_PointClear.Size = new System.Drawing.Size(23, 23);
            this.Button_PointClear.TabIndex = 19;
            this.Button_PointClear.Text = "c";
            this.Button_PointClear.UseVisualStyleBackColor = true;
            this.Button_PointClear.Click += new System.EventHandler(this.Button_PointClear_Click);
            // 
            // Button_LabelClear
            // 
            this.Button_LabelClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_LabelClear.Location = new System.Drawing.Point(206, 72);
            this.Button_LabelClear.Name = "Button_LabelClear";
            this.Button_LabelClear.Size = new System.Drawing.Size(23, 23);
            this.Button_LabelClear.TabIndex = 20;
            this.Button_LabelClear.Text = "c";
            this.Button_LabelClear.UseVisualStyleBackColor = true;
            this.Button_LabelClear.Click += new System.EventHandler(this.Button_LabelClear_Click);
            // 
            // UIMollierPointControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Button_LabelClear);
            this.Controls.Add(this.Button_PointClear);
            this.Controls.Add(this.Label_LabelColor);
            this.Controls.Add(this.LabelColor_Button);
            this.Controls.Add(this.PointLabel_TextBox);
            this.Controls.Add(this.PointLabel_Label);
            this.Controls.Add(this.PointColor_Label);
            this.Controls.Add(this.PointColor_Button);
            this.Name = "UIMollierPointControl";
            this.Size = new System.Drawing.Size(242, 106);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox PointLabel_TextBox;
        private System.Windows.Forms.Label PointLabel_Label;
        private System.Windows.Forms.Label PointColor_Label;
        private System.Windows.Forms.Button PointColor_Button;
        private System.Windows.Forms.Label Label_LabelColor;
        private System.Windows.Forms.Button LabelColor_Button;
        private System.Windows.Forms.Button Button_PointClear;
        private System.Windows.Forms.Button Button_LabelClear;
    }
}
