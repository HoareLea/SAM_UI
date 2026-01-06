// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI.Controls
{
    partial class UIMollierProcessPointControl
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
            this.Button_Color_Clear = new System.Windows.Forms.Button();
            this.Button_Color = new System.Windows.Forms.Button();
            this.Button_Vector2D_Clear = new System.Windows.Forms.Button();
            this.Button_Vector2D = new System.Windows.Forms.Button();
            this.Label_Name = new System.Windows.Forms.Label();
            this.TextBox_Label = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Button_Color_Clear
            // 
            this.Button_Color_Clear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Color_Clear.Location = new System.Drawing.Point(449, 3);
            this.Button_Color_Clear.Name = "Button_Color_Clear";
            this.Button_Color_Clear.Size = new System.Drawing.Size(30, 23);
            this.Button_Color_Clear.TabIndex = 15;
            this.Button_Color_Clear.Text = "c";
            this.Button_Color_Clear.UseVisualStyleBackColor = true;
            this.Button_Color_Clear.Click += new System.EventHandler(this.Button_Color_Clear_Click);
            // 
            // Button_Color
            // 
            this.Button_Color.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Color.Location = new System.Drawing.Point(368, 3);
            this.Button_Color.Name = "Button_Color";
            this.Button_Color.Size = new System.Drawing.Size(75, 23);
            this.Button_Color.TabIndex = 14;
            this.Button_Color.UseVisualStyleBackColor = true;
            this.Button_Color.Click += new System.EventHandler(this.Button_Color_Click);
            // 
            // Button_Vector2D_Clear
            // 
            this.Button_Vector2D_Clear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Vector2D_Clear.Location = new System.Drawing.Point(314, 3);
            this.Button_Vector2D_Clear.Name = "Button_Vector2D_Clear";
            this.Button_Vector2D_Clear.Size = new System.Drawing.Size(30, 23);
            this.Button_Vector2D_Clear.TabIndex = 13;
            this.Button_Vector2D_Clear.Text = "c";
            this.Button_Vector2D_Clear.UseVisualStyleBackColor = true;
            this.Button_Vector2D_Clear.Click += new System.EventHandler(this.Button_Vector2D_Clear_Click);
            // 
            // Button_Vector2D
            // 
            this.Button_Vector2D.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Vector2D.Location = new System.Drawing.Point(233, 3);
            this.Button_Vector2D.Name = "Button_Vector2D";
            this.Button_Vector2D.Size = new System.Drawing.Size(75, 23);
            this.Button_Vector2D.TabIndex = 12;
            this.Button_Vector2D.UseVisualStyleBackColor = true;
            this.Button_Vector2D.Click += new System.EventHandler(this.Button_Vector2D_Click);
            // 
            // Label_Name
            // 
            this.Label_Name.AutoSize = true;
            this.Label_Name.Location = new System.Drawing.Point(2, 6);
            this.Label_Name.Name = "Label_Name";
            this.Label_Name.Size = new System.Drawing.Size(41, 16);
            this.Label_Name.TabIndex = 11;
            this.Label_Name.Text = "Label";
            // 
            // TextBox_Label
            // 
            this.TextBox_Label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_Label.Location = new System.Drawing.Point(98, 3);
            this.TextBox_Label.Name = "TextBox_Label";
            this.TextBox_Label.Size = new System.Drawing.Size(111, 22);
            this.TextBox_Label.TabIndex = 10;
            // 
            // UIMollierProcessPointControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Button_Color_Clear);
            this.Controls.Add(this.Button_Color);
            this.Controls.Add(this.Button_Vector2D_Clear);
            this.Controls.Add(this.Button_Vector2D);
            this.Controls.Add(this.Label_Name);
            this.Controls.Add(this.TextBox_Label);
            this.Name = "UIMollierProcessPointControl";
            this.Size = new System.Drawing.Size(486, 30);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Button_Color_Clear;
        private System.Windows.Forms.Button Button_Color;
        private System.Windows.Forms.Button Button_Vector2D_Clear;
        private System.Windows.Forms.Button Button_Vector2D;
        private System.Windows.Forms.Label Label_Name;
        private System.Windows.Forms.TextBox TextBox_Label;
    }
}
