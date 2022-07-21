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
    public partial class BuiltInVisibilitySettingControl : UserControl
    {
        private BuiltInVisibilitySetting builtInVisibilitySetting;

        public BuiltInVisibilitySettingControl()
        {
            InitializeComponent();
        }

        public BuiltInVisibilitySettingControl(BuiltInVisibilitySetting builtInVisibilitySetting)
        {
            InitializeComponent();

            this.builtInVisibilitySetting = builtInVisibilitySetting;
        }

        private void Button_Color_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if(builtInVisibilitySetting != null)
                {
                    colorDialog.Color = builtInVisibilitySetting.Color;
                }

                if(colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                Button_Color.BackColor = colorDialog.Color;
            }
        }

        private void BuiltInVisibilitySettingControl_Load(object sender, EventArgs e)
        {

            Enum.GetValues(typeof(ChartDataType)).Cast<ChartDataType>().ToList().ForEach(x => ComboBox_ChartDataType.Items.Add(x));
            Enum.GetValues(typeof(ChartParameterType)).Cast<ChartParameterType>().ToList().ForEach(x => ComboBox_ChartParameterType.Items.Add(x));

            BuiltInVisibilitySetting = builtInVisibilitySetting;
        }

        public BuiltInVisibilitySetting BuiltInVisibilitySetting
        {
            get
            {
                if(builtInVisibilitySetting == null)
                {
                    return null;
                }

                BuiltInVisibilitySetting result = new BuiltInVisibilitySetting(builtInVisibilitySetting);
                result.ChartDataType = (ChartDataType)ComboBox_ChartDataType.SelectedItem;
                result.ChartParameterType = (ChartParameterType)ComboBox_ChartParameterType.SelectedItem;
                result.Color = Button_Color.BackColor;

                return result;
            }

            set
            {
                this.builtInVisibilitySetting = value;

                if (builtInVisibilitySetting != null)
                {
                    ComboBox_ChartDataType.SelectedItem = builtInVisibilitySetting.ChartDataType;
                    ComboBox_ChartParameterType.SelectedItem = builtInVisibilitySetting.ChartParameterType;
                    Button_Color.BackColor = builtInVisibilitySetting.Color;
                }
            }
        }
    }
}
