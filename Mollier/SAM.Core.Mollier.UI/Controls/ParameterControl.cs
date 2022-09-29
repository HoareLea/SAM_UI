using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class ParameterControl : UserControl
    {
        public ParameterControl()
        {
            InitializeComponent();
        }
        public ParameterControl(ProcessParameterType processParameterType)
        {
            InitializeComponent();
            Refresh(processParameterType);
        }
        public void Refresh(ProcessParameterType processParameterType)
        {
            parameterNameLabel.Text = processParameterType.Description();
            parameterUnitLabel.Text = Units.Query.Abbreviation(Query.DefaultUnitType(processParameterType));
        }
        public ProcessParameterType ProcessParameterType
        {
            get
            {
                return Core.Query.Enum<ProcessParameterType>(parameterNameLabel.Text);
            }
        }
        public double Value
        {
            get
            {
                double result = double.NaN;
                Core.Query.TryConvert(Parameter_Value.Text, out result);
                return result;
            }
            set
            {
                Parameter_Value.Text = value.ToString();
            }
        }

        private void Parameter_Value_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }
        private void valueSecure(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }
        }
    }
}
