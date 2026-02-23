using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for LegendSettingsControl.xaml
    /// </summary>
    public partial class LegendControl : UserControl
    {
        private Legend legend;
        
        public LegendControl()
        {
            InitializeComponent();
        }

        public LegendControl(Legend legend)
        {
            InitializeComponent();

            SetLegend(legend);
        }

        public Legend Legend
        {
            get
            {
                return GetLegend();
            }
            set
            {
                SetLegend(value);
            }
        }

        public bool Sort { get; set; } = true;
        
        public LegendItem UndefinedLegendItem { get; set; }
        
        private int Add(LegendItem legendItem)
        {
            if (legendItem == null)
            {
                return -1;
            }

            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, IsEnabled = legendItem.Editable };

            string text = legendItem.Text != null && legendItem.Text.Contains("_") ? string.Format("_{0}", legendItem.Text) : legendItem.Text; //TODO: Temporary solution, first occurance of "_" symbol will be removed from Label Content??

            TextBox textBox = new TextBox() { IsReadOnly = true, Width = 20, Background = new SolidColorBrush(legendItem.Color.ToMedia()), TextWrapping = TextWrapping.WrapWithOverflow };
            stackPanel.Children.Add(textBox);

            Label label = new Label() { Content = text };
            stackPanel.Children.Add(label);

            stackPanel.Tag = legendItem;

            if (legendItem.Editable)
            {
                textBox.Cursor = Cursors.Hand;
                label.Cursor = Cursors.Hand;

                textBox.MouseDoubleClick += Control_DoubleClick;
                label.MouseDoubleClick += Control_DoubleClick;
            }

            return this.stackPanel.Children.Add(stackPanel);
        }

        private void Button_GradientColor_Click(object sender, RoutedEventArgs e)
        {
            if(legend?.LegendItems is not List<LegendItem> legendItems || legendItems.Count < 2)
            {
                return;
            }

            if (Sort)
            {
                UI.Modify.Sort(legendItems);
            }

            ColorGradientWindow colorGradientWindow = new ColorGradientWindow();
            colorGradientWindow.Low = legendItems.First().Color;
            colorGradientWindow.High = legendItems.Last().Color;
            if(legendItems.Count > 2)
            {
                colorGradientWindow.Mid = legendItems[legendItems.Count / 2].Color;
                colorGradientWindow.HasMid = true;
            }

            bool? dialogResult = colorGradientWindow.ShowDialog();
            if(dialogResult is null || !dialogResult.HasValue)
            {
                return;
            }

            System.Drawing.Color low = colorGradientWindow.Low;
            System.Drawing.Color high = colorGradientWindow.High;
            System.Drawing.Color mid = colorGradientWindow.HasMid ? colorGradientWindow.Mid : System.Drawing.Color.FromArgb((low.R + high.R)/2, (low.G + high.G) / 2, (low.B + high.B) / 2);

            List<System.Drawing.Color> colors = ColorGradientInterpolationGenerator.GenerateDiverging(low, mid, high, legendItems.Count);
            if(colors is null || colors.Count != legendItems.Count)
            {
                return;
            }

            for(int i =0; i < legendItems.Count; i++)
            {
                legendItems[i] = new LegendItem(colors[i], legendItems[i].Text);
            }

            Legend legend_New = new Legend(legend.Name, legendItems);

            SetLegend(legend_New);
        }

        private void Button_PaletteColor_Click(object sender, RoutedEventArgs e)
        {
            if (legend?.LegendItems is not List<LegendItem> legendItems || legendItems.Count < 2)
            {
                return;
            }

            if(Sort)
            {
                UI.Modify.Sort(legendItems);
            }

            ColorPaletteWindow colorPaletteWindow = new ColorPaletteWindow();
            colorPaletteWindow.Colors = legendItems.ConvertAll(x => x.Color);

            bool? dialogResult = colorPaletteWindow.ShowDialog();
            if (dialogResult is null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            List<System.Drawing.Color> colors = colorPaletteWindow.Colors;

            int colorCount = colors.Count;
            List<(double pos, System.Drawing.Color)> values = [];
            for (int i = 0; i < colorCount; i++ )
            {
                values.Add(((double)i / (double)colorCount, colors[i]));
            }

            colors = ColorPaletteGenerator.GetSequentialFromStops(legendItems.Count, values);

            for (int i = 0; i < legendItems.Count; i++)
            {
                legendItems[i] = new LegendItem(colors[i], legendItems[i].Text);
            }

            Legend legend_New = new Legend(legend.Name, legendItems);

            SetLegend(legend_New);

        }

        private void Control_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            UpdateColor((sender as Control).Parent as StackPanel);
        }

        private Legend GetLegend()
        {
            Legend result = legend == null ? new Legend(textBox_Name.Text) : new Legend(textBox_Name.Text, legend);

            result.Visible = true;
            if (checkBox_Visible.IsChecked != null && checkBox_Visible.IsChecked.HasValue)
            {
                result.Visible = checkBox_Visible.IsChecked.Value;
            }
            List<LegendItem> legendItems = new List<LegendItem>();
            foreach (UIElement uIElement in stackPanel.Children)
            {
                StackPanel stackPanel_Temp = uIElement as StackPanel;
                if (stackPanel_Temp == null)
                {
                    continue;
                }

                LegendItem legendItem = stackPanel_Temp.Tag as LegendItem;
                if (legendItem == null)
                {
                    continue;
                }

                legendItems.Add(legendItem);
            }

            legendItems.ForEach(x => result.Update(x.Color, x.Text));

            return result;
        }

        private void SetLegend(Legend legend)
        {
            this.legend = legend;

            textBox_Name.Text = null;
            checkBox_Visible.IsChecked = true;
            stackPanel.Children.Clear();

            if(legend == null)
            {
                return;
            }

            textBox_Name.Text = legend.Name;
            checkBox_Visible.IsChecked = legend.Visible;

            List<LegendItem> legendItems = legend.LegendItems;
            if(legendItems != null)
            {
                LegendItem undefinedlegendItem = null;
                if (UndefinedLegendItem != null)
                {
                    int undefinedLegendIndex = legendItems.FindIndex(x => x.Text == UndefinedLegendItem.Text && x.Color == UndefinedLegendItem.Color);
                    if(undefinedLegendIndex != -1)
                    {
                        undefinedlegendItem = legendItems[undefinedLegendIndex];
                        legendItems.RemoveAt(undefinedLegendIndex);
                    }
                }

                if (Sort)
                {
                    UI.Modify.Sort(legendItems);
                }

                foreach (LegendItem legendItem_Temp in legendItems)
                {
                    Add(legendItem_Temp);
                }

                if (undefinedlegendItem != null)
                {
                    Add(undefinedlegendItem);
                }
            }
        }
        
        private void UpdateColor(StackPanel stackPanel)
        {
            if(stackPanel == null)
            {
                return;
            }

            LegendItem legendItem = stackPanel.Tag as LegendItem;
            if (legendItem == null)
            {
                return;
            }

            System.Drawing.Color color = System.Drawing.Color.Empty;
            using (System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog())
            {
                colorDialog.Color = legendItem.Color;

                if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                color = colorDialog.Color;
            }


            if (color == System.Drawing.Color.Empty)
            {
                return;
            }

            stackPanel.Tag = legend?.Update(color, legendItem.Text);
            SetLegend(legend);
        }
    }
}
