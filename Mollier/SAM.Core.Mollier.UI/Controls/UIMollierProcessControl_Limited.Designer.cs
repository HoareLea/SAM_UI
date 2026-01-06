// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI.Controls
{
    partial class UIMollierProcessControl_Limited
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
            this.processColorDialog = new System.Windows.Forms.ColorDialog();
            this.ProcessColor_button = new System.Windows.Forms.Button();
            this.ProcessColor_Label = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.ProcessLabel_Value = new System.Windows.Forms.TextBox();
            this.EndLabel_Value = new System.Windows.Forms.TextBox();
            this.StartLabel_Value = new System.Windows.Forms.TextBox();
            this.Button_Clear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ProcessColor_button
            // 
            this.ProcessColor_button.Location = new System.Drawing.Point(143, 11);
            this.ProcessColor_button.Name = "ProcessColor_button";
            this.ProcessColor_button.Size = new System.Drawing.Size(59, 23);
            this.ProcessColor_button.TabIndex = 1;
            this.ProcessColor_button.UseVisualStyleBackColor = true;
            this.ProcessColor_button.Click += new System.EventHandler(this.ProcessColor_button_Click);
            // 
            // ProcessColor_Label
            // 
            this.ProcessColor_Label.AutoSize = true;
            this.ProcessColor_Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.ProcessColor_Label.Location = new System.Drawing.Point(9, 11);
            this.ProcessColor_Label.Name = "ProcessColor_Label";
            this.ProcessColor_Label.Size = new System.Drawing.Size(113, 20);
            this.ProcessColor_Label.TabIndex = 1;
            this.ProcessColor_Label.Text = "Process color";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(9, 103);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "End Label";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label3.Location = new System.Drawing.Point(9, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(117, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Process Label";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label4.Location = new System.Drawing.Point(9, 41);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 20);
            this.label4.TabIndex = 6;
            this.label4.Text = "Start Label";
            // 
            // ProcessLabel_Value
            // 
            this.ProcessLabel_Value.Location = new System.Drawing.Point(143, 71);
            this.ProcessLabel_Value.Name = "ProcessLabel_Value";
            this.ProcessLabel_Value.Size = new System.Drawing.Size(88, 22);
            this.ProcessLabel_Value.TabIndex = 3;
            // 
            // EndLabel_Value
            // 
            this.EndLabel_Value.Location = new System.Drawing.Point(143, 103);
            this.EndLabel_Value.Name = "EndLabel_Value";
            this.EndLabel_Value.Size = new System.Drawing.Size(88, 22);
            this.EndLabel_Value.TabIndex = 4;
            // 
            // StartLabel_Value
            // 
            this.StartLabel_Value.Location = new System.Drawing.Point(143, 43);
            this.StartLabel_Value.Name = "StartLabel_Value";
            this.StartLabel_Value.Size = new System.Drawing.Size(88, 22);
            this.StartLabel_Value.TabIndex = 2;
            // 
            // Button_Clear
            // 
            this.Button_Clear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Clear.Location = new System.Drawing.Point(208, 11);
            this.Button_Clear.Name = "Button_Clear";
            this.Button_Clear.Size = new System.Drawing.Size(23, 23);
            this.Button_Clear.TabIndex = 7;
            this.Button_Clear.Text = "c";
            this.Button_Clear.UseVisualStyleBackColor = true;
            this.Button_Clear.Click += new System.EventHandler(this.Button_Clear_Click);
            // 
            // UIMollierProcessControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Button_Clear);
            this.Controls.Add(this.StartLabel_Value);
            this.Controls.Add(this.EndLabel_Value);
            this.Controls.Add(this.ProcessLabel_Value);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ProcessColor_Label);
            this.Controls.Add(this.ProcessColor_button);
            this.Name = "UIMollierProcessControl";
            this.Size = new System.Drawing.Size(253, 146);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ColorDialog processColorDialog;
        private System.Windows.Forms.Button ProcessColor_button;
        private System.Windows.Forms.Label ProcessColor_Label;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox ProcessLabel_Value;
        private System.Windows.Forms.TextBox EndLabel_Value;
        private System.Windows.Forms.TextBox StartLabel_Value;
        private System.Windows.Forms.Button Button_Clear;
    }
}
