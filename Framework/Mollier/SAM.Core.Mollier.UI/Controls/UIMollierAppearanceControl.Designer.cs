
namespace SAM.Core.Mollier.UI.Controls
{
    partial class UIMollierAppearanceControl
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
            this.Button_Color = new System.Windows.Forms.Button();
            this.Label_Color = new System.Windows.Forms.Label();
            this.Label_Label = new System.Windows.Forms.Label();
            this.TextBox_Label = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Button_Color
            // 
            this.Button_Color.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Color.Location = new System.Drawing.Point(54, 3);
            this.Button_Color.Name = "Button_Color";
            this.Button_Color.Size = new System.Drawing.Size(147, 23);
            this.Button_Color.TabIndex = 1;
            this.Button_Color.UseVisualStyleBackColor = true;
            this.Button_Color.Click += new System.EventHandler(this.Button_Color_Click);
            // 
            // Label_Color
            // 
            this.Label_Color.AutoSize = true;
            this.Label_Color.Location = new System.Drawing.Point(3, 6);
            this.Label_Color.Name = "Label_Color";
            this.Label_Color.Size = new System.Drawing.Size(42, 16);
            this.Label_Color.TabIndex = 1;
            this.Label_Color.Text = "Color:";
            // 
            // Label_Label
            // 
            this.Label_Label.AutoSize = true;
            this.Label_Label.Location = new System.Drawing.Point(3, 35);
            this.Label_Label.Name = "Label_Label";
            this.Label_Label.Size = new System.Drawing.Size(44, 16);
            this.Label_Label.TabIndex = 2;
            this.Label_Label.Text = "Label:";
            // 
            // TextBox_Label
            // 
            this.TextBox_Label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_Label.Location = new System.Drawing.Point(54, 32);
            this.TextBox_Label.Name = "TextBox_Label";
            this.TextBox_Label.Size = new System.Drawing.Size(147, 22);
            this.TextBox_Label.TabIndex = 2;
            // 
            // UIMollierAppearanceControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.Controls.Add(this.TextBox_Label);
            this.Controls.Add(this.Label_Label);
            this.Controls.Add(this.Label_Color);
            this.Controls.Add(this.Button_Color);
            this.Name = "UIMollierAppearanceControl";
            this.Size = new System.Drawing.Size(213, 60);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Button_Color;
        private System.Windows.Forms.Label Label_Color;
        private System.Windows.Forms.Label Label_Label;
        private System.Windows.Forms.TextBox TextBox_Label;
    }
}
