﻿using System.Collections.Generic;
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
                foreach (LegendItem legendItem in legendItems)
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

            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new TextBox() { IsReadOnly = true, Width = 20, Background = new SolidColorBrush(legendItem.Color.ToMedia()), TextWrapping = System.Windows.TextWrapping.WrapWithOverflow});
            stackPanel.Children.Add(new Label() { Content = legendItem.Text });
            stackPanel.Tag = legendItem;

            this.stackPanel.Children.Add(stackPanel);
        }
    }
}