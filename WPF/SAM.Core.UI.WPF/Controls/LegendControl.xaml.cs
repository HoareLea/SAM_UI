using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                foreach(LegendItem legendItem in legendItems)
                {
                    Add(legendItem);
                }
            }
        }

        private Legend GetLegend()
        {
            Legend result = legend == null ? new Legend(textBox_Name.Text) : new Legend(legend);
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

        private void Add(LegendItem legendItem)
        {
            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            TextBox textBox = new TextBox() { IsReadOnly = true, Width = 20, Background = new SolidColorBrush(legendItem.Color.ToMedia()), TextWrapping = System.Windows.TextWrapping.WrapWithOverflow };
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(new Label() { Content = legendItem.Text });
            stackPanel.Tag = legendItem;

            stackPanel.MouseLeftButtonUp += StackPanel_MouseLeftButtonUp;
            textBox.MouseLeftButtonUp += TextBox_MouseLeftButtonUp;

            this.stackPanel.Children.Add(stackPanel);
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
