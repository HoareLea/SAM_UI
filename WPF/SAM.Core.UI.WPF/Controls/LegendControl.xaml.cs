using System;
using System.Collections.Generic;
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
        public bool Sort { get; set; } = true;
        public LegendItem UndefinedLegendItem { get; set; }

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
                    List<Tuple<LegendItem, double>> tuples = new List<Tuple<LegendItem, double>>();
                    foreach(LegendItem legendItem in legendItems)
                    {
                        if(!Core.Query.TryConvert(legendItem.Text, out double value) || double.IsNaN(value))
                        {
                            tuples = null;
                            break;
                        }

                        tuples.Add(new Tuple<LegendItem, double>(legendItem, value));
                    }
                    
                    if(tuples == null || tuples.Count == 0)
                    {
                        legendItems.Sort((x, y) => x.Text.CompareTo(y.Text));
                    }
                    else
                    {
                        tuples.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                        legendItems = tuples.ConvertAll(x => x.Item1);
                    }
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

        private Legend GetLegend()
        {
            Legend result = legend == null ? new Legend(textBox_Name.Text) : new Legend(textBox_Name.Text, legend);

            result.Visible = true;
            if (checkBox_Visible.IsChecked != null && checkBox_Visible.IsChecked.HasValue)
            {
                result.Visible = checkBox_Visible.IsChecked.Value;
            }
            List<LegendItem> legendItems = new List<LegendItem>();
            foreach(UIElement uIElement in stackPanel.Children)
            {
                StackPanel stackPanel_Temp = uIElement as StackPanel;
                if (stackPanel_Temp == null)
                {
                    continue;
                }

                LegendItem legendItem = stackPanel_Temp.Tag as LegendItem;
                if(legendItem == null)
                {
                    continue;
                }

                legendItems.Add(legendItem);
            }

            legendItems.ForEach(x => result.Update(x.Color, x.Text));

            return result;
        }

        private int Add(LegendItem legendItem)
        {
            if(legendItem == null)
            {
                return -1;
            }

            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, IsEnabled = legendItem.Editable};

            TextBox textBox = new TextBox() { IsReadOnly = true, Width = 20, Background = new SolidColorBrush(legendItem.Color.ToMedia()), TextWrapping = TextWrapping.WrapWithOverflow };
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(new Label() { Content = legendItem.Text });
            stackPanel.Tag = legendItem;

            if(legendItem.Editable)
            {
                stackPanel.MouseLeftButtonUp += StackPanel_MouseLeftButtonUp;
                textBox.MouseLeftButtonUp += TextBox_MouseLeftButtonUp;
            }

            return this.stackPanel.Children.Add(stackPanel);
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

        private void TextBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UpdateColor((sender as TextBox).Parent as StackPanel);
        }

        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UpdateColor(sender as StackPanel);
        }
    }
}
