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
            this.PointColor_Button.Location = new System.Drawing.Point(141, 12);
            this.PointColor_Button.Name = "PointColor_Button";
            this.PointColor_Button.Size = new System.Drawing.Size(88, 23);
            this.PointColor_Button.TabIndex = 1;
            this.PointColor_Button.UseVisualStyleBackColor = true;
            this.PointColor_Button.Click += new System.EventHandler(this.PointColor_Button_Click);
            // 
            // UIMollierPointControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PointLabel_TextBox);
            this.Controls.Add(this.PointLabel_Label);
            this.Controls.Add(this.PointColor_Label);
            this.Controls.Add(this.PointColor_Button);
            this.Name = "UIMollierPointControl";
            this.Size = new System.Drawing.Size(242, 85);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox PointLabel_TextBox;
        private System.Windows.Forms.Label PointLabel_Label;
        private System.Windows.Forms.Label PointColor_Label;
        private System.Windows.Forms.Button PointColor_Button;
    }
}
