namespace SAM.Core.Mollier.UI.Forms
{
    partial class CustomizeProcessForm
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
            this.customizeProcessControl1 = new SAM.Core.Mollier.UI.Controls.CustomizeProcessControl();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.OK_Button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // customizeProcessControl1
            // 
            this.customizeProcessControl1.Location = new System.Drawing.Point(12, 12);
            this.customizeProcessControl1.Name = "customizeProcessControl1";
            this.customizeProcessControl1.Size = new System.Drawing.Size(253, 146);
            this.customizeProcessControl1.TabIndex = 0;
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Location = new System.Drawing.Point(224, 159);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(102, 31);
            this.Cancel_Button.TabIndex = 2;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // OK_Button
            // 
            this.OK_Button.Location = new System.Drawing.Point(116, 159);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(102, 31);
            this.OK_Button.TabIndex = 3;
            this.OK_Button.Text = "OK";
            this.OK_Button.UseVisualStyleBackColor = true;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // CustomizeProcessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(338, 202);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.customizeProcessControl1);
            this.Name = "CustomizeProcessForm";
            this.Text = "CustomizeForm";
            this.Load += new System.EventHandler(this.CustomizeForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.CustomizeProcessControl customizeProcessControl1;
        private System.Windows.Forms.Button Cancel_Button;
        private System.Windows.Forms.Button OK_Button;
    }
}