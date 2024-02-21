using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class BuiltInVisibilitySettingControl : UserControl
    {
        private BuiltInVisibilitySetting builtInVisibilitySetting;
        public event EventHandler ColorChanged;

        public List<int> CustomColors { get; set; } = new List<int>();

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
                if(CustomColors != null)
                {
                    colorDialog.CustomColors = CustomColors.ToArray();
                }

                System.Drawing.Color color;

                if(builtInVisibilitySetting != null)
                {
                    colorDialog.Color = builtInVisibilitySetting.Color;
                }

                if(colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                if(colorDialog.CustomColors != null)
                {
                    CustomColors = colorDialog.CustomColors.ToList();
                }

                Button_Color.BackColor = colorDialog.Color;
                ColorChanged.Invoke(this, EventArgs.Empty);
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
                result.Visible = CheckBox_Visible.Checked;

                return result;
            }

            set
            {
                builtInVisibilitySetting = value;

                if (builtInVisibilitySetting != null)
                {
                    ComboBox_ChartDataType.SelectedItem = builtInVisibilitySetting.ChartDataType;
                    ComboBox_ChartParameterType.SelectedItem = builtInVisibilitySetting.ChartParameterType;
                    Button_Color.BackColor = builtInVisibilitySetting.Color;
                    CheckBox_Visible.Checked = builtInVisibilitySetting.Visible;
                }
            }
        }
    }
}
