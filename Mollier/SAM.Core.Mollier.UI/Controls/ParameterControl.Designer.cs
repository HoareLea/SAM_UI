// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
            this.Label_Name = new System.Windows.Forms.Label();
            this.TextBox_Value = new System.Windows.Forms.TextBox();
            this.Label_Unit = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Label_Name
            // 
            this.Label_Name.AutoSize = true;
            this.Label_Name.Location = new System.Drawing.Point(0, 0);
            this.Label_Name.Name = "Label_Name";
            this.Label_Name.Size = new System.Drawing.Size(110, 17);
            this.Label_Name.TabIndex = 0;
            this.Label_Name.Text = "parameterName";
            // 
            // TextBox_Value
            // 
            this.TextBox_Value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox_Value.Location = new System.Drawing.Point(0, 21);
            this.TextBox_Value.Name = "TextBox_Value";
            this.TextBox_Value.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TextBox_Value.Size = new System.Drawing.Size(150, 22);
            this.TextBox_Value.TabIndex = 1;
            this.TextBox_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.TextBox_Value.TextChanged += new System.EventHandler(this.TextBox_Value_TextChanged);
            // 
            // Label_Unit
            // 
            this.Label_Unit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Label_Unit.AutoSize = true;
            this.Label_Unit.Location = new System.Drawing.Point(153, 24);
            this.Label_Unit.Name = "Label_Unit";
            this.Label_Unit.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label_Unit.Size = new System.Drawing.Size(42, 17);
            this.Label_Unit.TabIndex = 2;
            this.Label_Unit.Text = "kg/kg";
            this.Label_Unit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ParameterControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.Label_Unit);
            this.Controls.Add(this.TextBox_Value);
            this.Controls.Add(this.Label_Name);
            this.Name = "ParameterControl";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Size = new System.Drawing.Size(200, 51);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label_Name;
        private System.Windows.Forms.TextBox TextBox_Value;
        private System.Windows.Forms.Label Label_Unit;
    }
}
