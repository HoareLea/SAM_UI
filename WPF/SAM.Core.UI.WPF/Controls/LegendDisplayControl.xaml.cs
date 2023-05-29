using System;
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
            if (legendItems == null)
            {
                return;
            }

            if (Sort)
            {
                List<Tuple<double, LegendItem>> tuples_Double = new List<Tuple<double, LegendItem>>();
                List<Tuple<string, LegendItem>> tuples_String = new List<Tuple<string, LegendItem>>();
                foreach (LegendItem legendItem_Temp in legendItems)
                {
                    if(legendItem_Temp == null)
                    {
                        continue;
                    }

                    if(!Core.Query.TryConvert( legendItem_Temp.Text, out double value))
                    {
                        tuples_String.Add(new Tuple<string, LegendItem>(legendItem_Temp.Text, legendItem_Temp));
                    }

                    tuples_Double.Add(new Tuple<double, LegendItem>(value, legendItem_Temp));
                }

                double factor = 0;
                if(tuples_Double.Count > 0)
                {
                    factor = 1;
                    if(tuples_String.Count > 0)
                    {
                        factor = System.Convert.ToDouble(tuples_Double.Count) / System.Convert.ToDouble(tuples_String.Count);
                    }
                }

                if(factor < 0.8)
                {
                    legendItems.Sort((x, y) => x.Text.CompareTo(y.Text));
                }
                else
                {
                    tuples_Double.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                    legendItems = tuples_Double.ConvertAll(x => x.Item2);

                    tuples_String.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                    legendItems.AddRange(tuples_String.ConvertAll(x => x.Item2));
                }
            }

            LegendItem legendItem = null;
            if (UndefinedLegendItem != null)
            {
                int index = legendItems.FindIndex(x => x.Text == UndefinedLegendItem.Text && Core.Query.Equals(x.Color, UndefinedLegendItem.Color));
                if (index != -1)
                {
                    legendItem = legendItems[index];
                    legendItems.RemoveAt(index);
                }
            }

            foreach (LegendItem legendItem_Temp in legendItems)
            {
                Add(legendItem_Temp);
            }

            if (legendItem != null)
            {
                Add(legendItem);
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
