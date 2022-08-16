using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm : Form
    {
        public MollierProcessForm()
        {
            InitializeComponent();
        }
        public MollierProcess GetMollierProcess(double pressure)
        {
            MollierProcess mollierProcess = null;
            if (!Core.Query.TryConvert(TextBox_Temperature.Text, out double temperature))
            {
                return null;
            }

            if (!Core.Query.TryConvert(TextBox_HumidityRatio.Text, out double humidtyRatio))
            {
                return null;
            }
            if(!Core.Query.TryConvert(ComboBox_ChooseProcess.Text, out double process))
            {
                return null;
            }
            //mollierProcess.Start = new MollierPoint(temperature, humidtyRatio, pressure);
            return mollierProcess;
        }

    }
}
