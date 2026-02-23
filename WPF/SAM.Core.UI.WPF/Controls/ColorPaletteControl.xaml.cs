using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ColorPaletteControl.xaml
    /// </summary>
    public partial class ColorPaletteControl : System.Windows.Controls.UserControl
    {
        public ColorPaletteControl()
        {
            InitializeComponent();

            ApplyCustomHighlightStyle(ListBox_Main);
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Add(colorDialog.Color);
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            if(ListBox_Main.SelectedItems == null || ListBox_Main.SelectedItems.Count == 0)
            {
                return;
            }

            object[] itemsToRemove = new object[ListBox_Main.SelectedItems.Count];
            ListBox_Main.SelectedItems.CopyTo(itemsToRemove, 0);

            foreach (object item in itemsToRemove)
            {
                ListBox_Main.Items.Remove(item);
            }
        }

        private ListBoxItem Add(System.Drawing.Color color)
        {
            ListBoxItem result = new() 
            { 
                Background = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), 
                Height = 32 
            };

            MenuItem menuItem = new()
            {
                Name = "MenuItem_Edit",
                Header = "Edit",
                Tag = result
            };

            menuItem.Click += OpenMenuItem_Edit_Click;

            ContextMenu contextMenu = new();
            contextMenu.Items.Add(menuItem);


            result.ContextMenu = contextMenu;

            int index = ListBox_Main.Items.Add(result);

            return result;
        }

        public List<System.Drawing.Color> Colors
        {
            get
            {
                if(ListBox_Main.Items == null)
                {
                    return null;
                }

                List<System.Drawing.Color> result = [];

                foreach(ListBoxItem listBoxItem in ListBox_Main.Items)
                {
                    Color color = (listBoxItem.Background as SolidColorBrush).Color;

                    result.Add(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
                }

                return result;
            }

            set
            {
                ListBox_Main.Items.Clear();
                if (value == null)
                {
                    return;
                }

                foreach(System.Drawing.Color color in value)
                {
                    Add(color);
                }
            }
        }

        private void OpenMenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            if((sender as MenuItem).Tag is not ListBoxItem listBoxItem)
            {
                return;
            }

            Color color = (listBoxItem.Background as SolidColorBrush).Color;

            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            if (colorDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            listBoxItem.Background = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
        }

        public void ApplyCustomHighlightStyle(System.Windows.Controls.ListBox listBox)
        {
            Style itemStyle = new Style(typeof(ListBoxItem));

            // 1. Create a Template for ListBoxItem
            ControlTemplate template = new ControlTemplate(typeof(ListBoxItem));

            // 2. Define the visual tree for the template: a Border containing a ContentPresenter
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "ItemBorder";
            // Bind the background to the ListBoxItem's Background property so your colors stay visible
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Transparent); // Default transparent border

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);

            borderFactory.AppendChild(contentPresenter);
            template.VisualTree = borderFactory;

            // 3. Add Triggers for Selection
            Trigger selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            // When selected, change ONLY the border color, not the background
            selectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.DeepSkyBlue, "ItemBorder"));

            // Optional: Add a subtle hover effect
            Trigger hoverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.LightGray, "ItemBorder"));

            template.Triggers.Add(selectedTrigger);
            template.Triggers.Add(hoverTrigger);

            // 4. Assign the template to the style
            itemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty, template));

            // Apply the style to the ListBox
            listBox.ItemContainerStyle = itemStyle;
        }
    }
}
