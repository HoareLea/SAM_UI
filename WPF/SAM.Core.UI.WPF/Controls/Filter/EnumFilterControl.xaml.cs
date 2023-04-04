using System;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for EnumFilterControl.xaml
    /// </summary>
    public partial class EnumFilterControl : UserControl, IFilterControl
    {
        private UIEnumFilter uIEnumFilter;

        public event FilterChangedEventHandler FilterChanged;

        public event FilterRemovingEventHandler FilterRemoving;

        public EnumFilterControl()
        {
            InitializeComponent();
        }

        public EnumFilterControl(UIEnumFilter uIEnumFilter)
        {
            InitializeComponent();

            UIEnumFilter = uIEnumFilter;
        }

        public UIEnumFilter UIEnumFilter
        {
            get
            {
                return GetUIEnumFilter();
            }

            set
            {
                SetUIEnumFilter(value);
            }
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UIEnumFilter;
            }
        }

        private UIEnumFilter GetUIEnumFilter(bool updated = true)
        {
            UIEnumFilter result = uIEnumFilter;
            if(result == null)
            {
                return null;
            }

            result = new UIEnumFilter(result);
            if (!updated)
            {
                return result;
            }

            IEnumFilter enumFilter = result.Filter;
            if(enumFilter == null)
            {
                return null;
            }

            enumFilter.Inverted = checkBox_Inverted.IsChecked != null && checkBox_Inverted.IsChecked.HasValue && checkBox_Inverted.IsChecked.Value;

            Type type = enumFilter.GetType()?.BaseType.GenericTypeArguments?.FirstOrDefault();
            if (type == null)
            {
                return result;
            }
            
            if(Core.Query.TryGetEnum(comboBox_Enum.Text, type, out Enum @enum))
            {
                enumFilter.Enum = @enum;
            }

            return result;
        }

        private void SetUIEnumFilter(UIEnumFilter uIEnumFilter)
        {
            this.uIEnumFilter = uIEnumFilter;

            if (uIEnumFilter == null)
            {
                return;
            }

            groupBox_Main.Header = uIEnumFilter.Name;

            IEnumFilter enumFilter = uIEnumFilter.Filter;
            if (enumFilter == null)
            {
                return;
            }

            checkBox_Inverted.IsChecked = enumFilter.Inverted;

            Type type = enumFilter.GetType()?.BaseType.GenericTypeArguments?.FirstOrDefault();
            if (type == null)
            {
                return;
            }

            Modify.Reload(comboBox_Enum, type);

            comboBox_Enum.SelectedItem = Core.Query.Description((enumFilter as dynamic).Enum);
        }

        private void checkBox_Inverted_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIEnumFilter));
        }

        private void comboBox_Enum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIEnumFilter));
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu = new ContextMenu();

            MenuItem menuItem = null;

            menuItem = new MenuItem();
            menuItem.Name = "MenuItem_Remove";
            menuItem.Header = "Remove";
            menuItem.Click += MenuItem_Remove_Click;
            ContextMenu.Items.Add(menuItem);
        }

        private void MenuItem_Remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
        }
    }
}
