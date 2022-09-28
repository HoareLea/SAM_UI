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
            this.firstParameter_Value = new System.Windows.Forms.TextBox();
            this.secondParameter_ComboBox = new System.Windows.Forms.ComboBox();
            this.secondParameter_Value = new System.Windows.Forms.TextBox();
            this.firstParameter_ComboBox = new System.Windows.Forms.ComboBox();
            this.pressureTextBox = new System.Windows.Forms.TextBox();
            this.pressureLabel = new System.Windows.Forms.Label();
            this.firstUnitLabel = new System.Windows.Forms.Label();
            this.secondUnitLabel = new System.Windows.Forms.Label();
            this.pressureUnitLabel = new System.Windows.Forms.Label();
            this.ChoosePoint_Button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // firstParameter_Value
            // 
            this.firstParameter_Value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.firstParameter_Value.Location = new System.Drawing.Point(0, 96);
            this.firstParameter_Value.Name = "firstParameter_Value";
            this.firstParameter_Value.Size = new System.Drawing.Size(142, 22);
            this.firstParameter_Value.TabIndex = 0;
            this.firstParameter_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.firstParameter_Value.TextChanged += new System.EventHandler(this.firstParameter_Value_TextChanged);
            this.firstParameter_Value.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.firstParameter_Value_KeyPress);
            // 
            // secondParameter_ComboBox
            // 
            this.secondParameter_ComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.secondParameter_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.secondParameter_ComboBox.FormattingEnabled = true;
            this.secondParameter_ComboBox.Location = new System.Drawing.Point(0, 137);
            this.secondParameter_ComboBox.Name = "secondParameter_ComboBox";
            this.secondParameter_ComboBox.Size = new System.Drawing.Size(189, 24);
            this.secondParameter_ComboBox.TabIndex = 1;
            this.secondParameter_ComboBox.SelectedIndexChanged += new System.EventHandler(this.secondParameter_ComboBox_SelectedIndexChanged);
            // 
            // secondParameter_Value
            // 
            this.secondParameter_Value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.secondParameter_Value.Location = new System.Drawing.Point(0, 164);
            this.secondParameter_Value.Name = "secondParameter_Value";
            this.secondParameter_Value.Size = new System.Drawing.Size(142, 22);
            this.secondParameter_Value.TabIndex = 2;
            this.secondParameter_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.secondParameter_Value.TextChanged += new System.EventHandler(this.secondParameter_Value_TextChanged);
            this.secondParameter_Value.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.secondParameter_Value_KeyPress);
            // 
            // firstParameter_ComboBox
            // 
            this.firstParameter_ComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.firstParameter_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.firstParameter_ComboBox.FormattingEnabled = true;
            this.firstParameter_ComboBox.Location = new System.Drawing.Point(0, 69);
            this.firstParameter_ComboBox.Name = "firstParameter_ComboBox";
            this.firstParameter_ComboBox.Size = new System.Drawing.Size(189, 24);
            this.firstParameter_ComboBox.TabIndex = 3;
            this.firstParameter_ComboBox.SelectedIndexChanged += new System.EventHandler(this.firstParameter_ComboBox_SelectedIndexChanged);
            // 
            // pressureTextBox
            // 
            this.pressureTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pressureTextBox.Location = new System.Drawing.Point(3, 33);
            this.pressureTextBox.Name = "pressureTextBox";
            this.pressureTextBox.Size = new System.Drawing.Size(142, 22);
            this.pressureTextBox.TabIndex = 4;
            this.pressureTextBox.Text = "101325";
            this.pressureTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.pressureTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.pressureTextBox_KeyPress);
            // 
            // pressureLabel
            // 
            this.pressureLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pressureLabel.AutoSize = true;
            this.pressureLabel.Location = new System.Drawing.Point(0, 14);
            this.pressureLabel.Name = "pressureLabel";
            this.pressureLabel.Size = new System.Drawing.Size(61, 16);
            this.pressureLabel.TabIndex = 5;
            this.pressureLabel.Text = "Pressure";
            // 
            // firstUnitLabel
            // 
            this.firstUnitLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.firstUnitLabel.AutoSize = true;
            this.firstUnitLabel.Location = new System.Drawing.Point(148, 99);
            this.firstUnitLabel.Name = "firstUnitLabel";
            this.firstUnitLabel.Size = new System.Drawing.Size(41, 16);
            this.firstUnitLabel.TabIndex = 6;
            this.firstUnitLabel.Text = "kg/kg";
            // 
            // secondUnitLabel
            // 
            this.secondUnitLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.secondUnitLabel.AutoSize = true;
            this.secondUnitLabel.Location = new System.Drawing.Point(148, 167);
            this.secondUnitLabel.Name = "secondUnitLabel";
            this.secondUnitLabel.Size = new System.Drawing.Size(41, 16);
            this.secondUnitLabel.TabIndex = 7;
            this.secondUnitLabel.Text = "kg/kg";
            // 
            // pressureUnitLabel
            // 
            this.pressureUnitLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pressureUnitLabel.AutoSize = true;
            this.pressureUnitLabel.Location = new System.Drawing.Point(151, 36);
            this.pressureUnitLabel.Name = "pressureUnitLabel";
            this.pressureUnitLabel.Size = new System.Drawing.Size(24, 16);
            this.pressureUnitLabel.TabIndex = 8;
            this.pressureUnitLabel.Text = "Pa";
            // 
            // ChoosePoint_Button
            // 
            this.ChoosePoint_Button.Location = new System.Drawing.Point(177, 3);
            this.ChoosePoint_Button.Name = "ChoosePoint_Button";
            this.ChoosePoint_Button.Size = new System.Drawing.Size(25, 23);
            this.ChoosePoint_Button.TabIndex = 9;
            this.ChoosePoint_Button.Text = "...";
            this.ChoosePoint_Button.UseVisualStyleBackColor = true;
            this.ChoosePoint_Button.Visible = false;
            this.ChoosePoint_Button.Click += new System.EventHandler(this.ChoosePoint_Button_Click);
            // 
            // MollierPointControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.ChoosePoint_Button);
            this.Controls.Add(this.pressureUnitLabel);
            this.Controls.Add(this.secondUnitLabel);
            this.Controls.Add(this.firstUnitLabel);
            this.Controls.Add(this.pressureLabel);
            this.Controls.Add(this.pressureTextBox);
            this.Controls.Add(this.firstParameter_ComboBox);
            this.Controls.Add(this.secondParameter_Value);
            this.Controls.Add(this.secondParameter_ComboBox);
            this.Controls.Add(this.firstParameter_Value);
            this.Name = "MollierPointControl";
            this.Size = new System.Drawing.Size(205, 227);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox firstParameter_Value;
        private System.Windows.Forms.ComboBox secondParameter_ComboBox;
        private System.Windows.Forms.TextBox secondParameter_Value;
        private System.Windows.Forms.ComboBox firstParameter_ComboBox;
        private System.Windows.Forms.TextBox pressureTextBox;
        private System.Windows.Forms.Label pressureLabel;
        private System.Windows.Forms.Label firstUnitLabel;
        private System.Windows.Forms.Label secondUnitLabel;
        private System.Windows.Forms.Label pressureUnitLabel;
        private System.Windows.Forms.Button ChoosePoint_Button;
    }
}
