using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for LegendControl.xaml
    /// </summary>
    public partial class LegendDisplayControl : UserControl
    {
        private Legend legend;
        public bool Sort { get; set; } = true;
        public LegendItem UndefinedLegendItem { get; set; }


        public LegendDisplayControl()
        {
            InitializeComponent();
        }

        public Legend Legend
        {
            get
            {
                return new Legend(legend);
            }

            set
            {
                SetLegend(value);
            }
        }

        private void SetLegend(Legend legend)
        {
            this.legend = legend;

            label_Name.Content = null;
            stackPanel.Children.Clear();

            if (legend == null)
            {
                return;
            }


            label_Name.Content = legend.Name;

            List<LegendItem> legendItems = legend.LegendItems;
            if (legendItems != null)
            {
                if(Sort)
                {
                    legendItems.Sort((x, y) => x.Text.CompareTo(y.Text));
                }

                LegendItem legendItem = null;
                if(UndefinedLegendItem != null)
                {
                    int index = legendItems.FindIndex(x => x.Text == UndefinedLegendItem.Text && Core.Query.Equals(x.Color, UndefinedLegendItem.Color));
                    if(index != -1)
                    {
                        legendItem = legendItems[index];
                        legendItems.RemoveAt(index);
                    }
                }

                foreach (LegendItem legendItem_Temp in legendItems)
                {
                    Add(legendItem_Temp);
                }

                if(legendItem != null)
                {
                    Add(legendItem);
                }
            }
        }

        private void Add(LegendItem legendItem)
        {
            if(legendItem == null)
            {
                return;
            }

            string text = legendItem.Text != null && legendItem.Text.Contains("_") ? string.Format("_{0}", legendItem.Text) : legendItem.Text; //TODO: Temporary solution, first occurance of "_" symbol will be removed from Label Content??

            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new TextBox() { IsReadOnly = true, Width = 20, Background = new SolidColorBrush(legendItem.Color.ToMedia()), TextWrapping = System.Windows.TextWrapping.WrapWithOverflow});
            stackPanel.Children.Add(new Label() { Content = text });
            stackPanel.Tag = legendItem;

            this.stackPanel.Children.Add(stackPanel);
        }
    }
}
