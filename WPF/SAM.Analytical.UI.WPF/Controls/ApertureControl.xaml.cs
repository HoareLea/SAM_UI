using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureControl.xaml
    /// </summary>
    public partial class ApertureControl : System.Windows.Controls.UserControl
    {
        private Brush background = null;

        private List<ApertureData> apertureDatas;

        public ApertureControl()
        {
            InitializeComponent();

            multipleValueComboBoxControl_ConstructionName.IsEditable = false;

            if (background == null)
            {
                background = button_Color.Background;
            }

            multipleValueComboBoxControl_DischargeCoefficient.TextChanged += MultipleValueComboBoxControl_DischargeCoefficient_TextChanged;
        }

        private void MultipleValueComboBoxControl_DischargeCoefficient_TextChanged(object sender, EventArgs e)
        {
            if(!multipleValueComboBoxControl_DischargeCoefficient.VarySet)
            {
                if(Core.Query.TryConvert(multipleValueComboBoxControl_DischargeCoefficient.Value, out double value) && !double.IsNaN(value))
                {
                    for (int i = 0; i < apertureDatas.Count; i++)
                    {
                        apertureDatas[i].Color = Analytical.Query.Color(ApertureType.Window, AperturePart.Pane, true);
                    }

                    SetColor(apertureDatas);
                }
            }
        }

        public List<Aperture> Apertures
        {
            get
            {
                return GetApertureDatas(true)?.ConvertAll(x => x.Aperture);
            }
            set
            {
                apertureDatas = value?.ConvertAll(x => new ApertureData(x));
                SetApertureDatas(apertureDatas);
            }
        }

        private void SetApertureDatas(IEnumerable<ApertureData> apertureDatas)
        {
            this.apertureDatas = apertureDatas == null ? null : new List<ApertureData>(apertureDatas);

            multipleValueComboBoxControl_ConstructionName.Values = this.apertureDatas.ConvertAll(x => x?.ApertureConstruction?.Name);
            multipleValueTextBoxControl_Area.Values = this.apertureDatas.ConvertAll(x => Core.Query.Round(x.Area, 0.01))?.Texts();
            multipleValueComboBoxControl_DischargeCoefficient.Values = this.apertureDatas.ConvertAll(x => x.DischargeCoefficient)?.Texts();
            multipleValueComboBoxControl_Function.Values = this.apertureDatas.ConvertAll(x => x.Function);
            multipleValueComboBoxControl_Description.Values = this.apertureDatas.ConvertAll(x => x.Description);

            SetColor(apertureDatas);
        }

        private List<ApertureData> GetApertureDatas(bool updated = true)
        {
            if(!updated)
            {
                return apertureDatas;
            }

            List<ApertureData> result = new List<ApertureData>();
            foreach(ApertureData apertureData in apertureDatas)
            {
                if(!multipleValueComboBoxControl_ConstructionName.VarySet)
                {
                    ApertureConstruction apertureConstruction = apertureDatas.Find(x => x.Name == multipleValueComboBoxControl_ConstructionName.Value)?.ApertureConstruction;
                    if(apertureConstruction != null)
                    {
                        apertureData.ApertureConstruction = apertureConstruction;
                    }
                }

                System.Drawing.Color color = GetColor(out bool vary);
                if(!vary)
                {
                    apertureData.Color = color;
                }

                if (!multipleValueComboBoxControl_DischargeCoefficient.VarySet)
                {
                    string value = multipleValueComboBoxControl_DischargeCoefficient.Value;
                    if (Core.Query.TryConvert(value, out double value_Temp))
                    {
                        apertureData.DischargeCoefficient = value_Temp;
                    }
                    else
                    {
                        apertureData.DischargeCoefficient = double.NaN;
                    }
                        
                }

                if (!multipleValueComboBoxControl_Function.VarySet)
                {
                    apertureData.Function = multipleValueComboBoxControl_Function.Value;
                }

                if (!multipleValueComboBoxControl_Description.VarySet)
                {
                    apertureData.Description = multipleValueComboBoxControl_Description.Value;
                }

                result.Add(apertureData);
            }

            return result;
        }

        private void SetColor(IEnumerable<ApertureData> apertureDatas)
        {
            button_Color.Content = string.Empty;

            if (apertureDatas == null || apertureDatas.Count() == 0)
            {
                button_Color.Background = background;
            }

            List<System.Drawing.Color> colors = apertureDatas?.ToList().ConvertAll(x => x.Color);
            if (colors == null || colors.Count == 0)
            {
                return;
            }

            if (Core.UI.Query.Vary(colors))
            {
                button_Color.Content = multipleValueComboBoxControl_ConstructionName.VaryText;
                button_Color.Background = background;
                return;
            }

            colors.Remove(System.Drawing.Color.Empty);
            if (colors == null || colors.Count == 0)
            {
                button_Color.Background = background;
                return;
            }

            System.Drawing.Color color = colors[0];

            button_Color.Background = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private System.Drawing.Color GetColor(out bool vary)
        {
            vary = false;
            if (!string.IsNullOrWhiteSpace(button_Color?.Content?.ToString()))
            {
                vary = true;
            }

            SolidColorBrush solidColorBrush = button_Color.Background as SolidColorBrush;
            if (solidColorBrush == null)
            {
                return System.Drawing.Color.Empty;
            }

            SolidColorBrush solidColorBrush_Background = background as SolidColorBrush;
            if (solidColorBrush_Background == null)
            {
                return System.Drawing.Color.Empty;
            }

            if (solidColorBrush.Color == solidColorBrush_Background.Color)
            {
                return System.Drawing.Color.Empty;
            }

            return System.Drawing.Color.FromArgb(solidColorBrush.Color.A, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B);
        }

        private void button_Color_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color color = System.Drawing.Color.Empty;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                Color color_Start = (button_Color.Background as SolidColorBrush).Color;

                colorDialog.Color = System.Drawing.Color.FromArgb(color_Start.A, color_Start.R, color_Start.G, color_Start.B);
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                color = colorDialog.Color;
            }

            for (int i = 0; i < apertureDatas.Count; i++)
            {
                apertureDatas[i].Color = color;
            }

            SetApertureDatas(apertureDatas);
        }
    }
}
