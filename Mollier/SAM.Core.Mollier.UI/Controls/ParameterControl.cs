// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class ParameterControl : UserControl
    {
        public event EventHandler ValueHanged;

        private Units.UnitType unitType = Units.UnitType.Undefined;
        private string text = null;

        public ParameterControl()
        {
            InitializeComponent();

            TextBox_Value.KeyPress += new KeyPressEventHandler(Windows.EventHandler.ControlText_NumberOnly);
        }
        
        public ParameterControl(ProcessParameterType processParameterType)
        {
            InitializeComponent();

            SetProcessParameterType(processParameterType);

            TextBox_Value.KeyPress += new KeyPressEventHandler(Windows.EventHandler.ControlText_NumberOnly);
        }
        
        private void SetProcessParameterType(ProcessParameterType processParameterType)
        {
            if (processParameterType != ProcessParameterType.Undefined)
            {
                Label_Name.Text = processParameterType.Description();
                Label_Unit.Text = Units.Query.Abbreviation(Query.DefaultUnitType(processParameterType));

                unitType = Units.UnitType.Undefined;
                text = null;
            }
            else
            {
                Label_Name.Text = text;
                Label_Unit.Text = Units.Query.Abbreviation(unitType);
            }
        }

        public Units.UnitType UnitType
        {
            get
            {
                ProcessParameterType processParameterType = ProcessParameterType;
                if(processParameterType != ProcessParameterType.Undefined)
                {
                    return Query.DefaultUnitType(processParameterType);
                }
                else
                {
                    return unitType;
                }
            }

            set
            {
                unitType = value;
                Label_Unit.Text = Units.Query.Abbreviation(unitType);
            }
        }

        public string Name
        {
            get
            {
                return Label_Name.Text;
            }

            set
            {
                text = value;
                Label_Name.Text = text;
            }

        }

        public ProcessParameterType ProcessParameterType
        {
            get
            {
                return Core.Query.Enum<ProcessParameterType>(Label_Name.Text);
            }

            set
            {
                SetProcessParameterType(value);
            }
        }
        
        public double Value
        {
            get
            {
                if(!Core.Query.TryConvert(TextBox_Value.Text, out double result))
                {
                    return double.NaN;
                }
                return result;
            }
            set
            {
                if(double.IsNaN(value))
                {
                    TextBox_Value.Text = null;
                }
                else
                {
                    TextBox_Value.Text = value.ToString();
                }
            }
        }

        public new bool Enabled
        {
            get
            {
                return TextBox_Value.Enabled;
            }

            set
            {
                TextBox_Value.Enabled = value;
            }
        }

        private void TextBox_Value_TextChanged(object sender, EventArgs e)
        {
            ValueHanged?.Invoke(sender, e);
        }
    }
}
