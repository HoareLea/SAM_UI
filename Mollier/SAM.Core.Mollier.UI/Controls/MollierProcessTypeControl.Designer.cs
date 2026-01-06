// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Mollier.UI.Controls
{
    partial class MollierProcessTypeControl
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
            this.MollierProcessType_ComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // MollierProcessType_ComboBox
            // 
            this.MollierProcessType_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MollierProcessType_ComboBox.FormattingEnabled = true;
            this.MollierProcessType_ComboBox.Items.AddRange(new object[] {
            "Heating",
            "Cooling",
            "Heat Recovery",
            "Mixing",
            "Adiabatic Humidification",
            "Isothermal Humidification"});
            this.MollierProcessType_ComboBox.Location = new System.Drawing.Point(53, 13);
            this.MollierProcessType_ComboBox.Name = "MollierProcessType_ComboBox";
            this.MollierProcessType_ComboBox.Size = new System.Drawing.Size(262, 24);
            this.MollierProcessType_ComboBox.TabIndex = 0;
            this.MollierProcessType_ComboBox.SelectedIndexChanged += new System.EventHandler(this.MollierProcessType_ComboBox_SelectedIndexChanged);
            // 
            // MollierProcessTypeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MollierProcessType_ComboBox);
            this.Name = "MollierProcessTypeControl";
            this.Size = new System.Drawing.Size(405, 54);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox MollierProcessType_ComboBox;
    }
}
