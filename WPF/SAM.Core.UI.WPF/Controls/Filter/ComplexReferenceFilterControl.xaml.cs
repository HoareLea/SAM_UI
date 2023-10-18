using System.Windows;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for ComplexReferenceFilterControl.xaml
    /// </summary>
    public partial class ComplexReferenceFilterControl : UserControl, IFilterControl
    {
        private UIComplexReferenceFilter uIComplexReferenceFilter;

        public event FilterChangedEventHandler FilterChanged;
        public event FilterRemovingEventHandler FilterRemoving;

        public ComplexReferenceFilterControl()
        {
            InitializeComponent();
        }

        public ComplexReferenceFilterControl(UIComplexReferenceFilter uIComplexReferenceFilter)
        {
            InitializeComponent();

            UIComplexReferenceFilter = uIComplexReferenceFilter;
        }

        public IUIFilter UIFilter
        {
            get
            {
                return UIComplexReferenceFilter;
            }
        }

        public UIComplexReferenceFilter UIComplexReferenceFilter
        {
            get
            {
                return GetUIComplexReferenceFilter();
            }

            set
            {
                SetUIComplexReferenceFilter(value);
            }
        }

        private UIComplexReferenceFilter GetUIComplexReferenceFilter(bool updated = true)
        {
            UIComplexReferenceFilter result = uIComplexReferenceFilter;
            if (result == null)
            {
                return null;
            }

            result = new UIComplexReferenceFilter(result);
            if (!updated)
            {
                return result;
            }

            UIElementCollection uIElementCollection = StackPanel_Filter.Children;
            if (uIElementCollection == null || uIElementCollection.Count == 0)
            {
                return null;
            }

            UserControl userControl = uIElementCollection[0] as UserControl;
            if (userControl == null)
            {
                return null;
            }

            IComplexReference complexReference = Core.Convert.ComplexReference(TextBox_ComplexReference.Text);

            ComplexReferenceFilter complexReferenceFilter = result.Filter;
            if (userControl is NumberFilterControl)
            {
                NumberFilterControl numberFilterControl = (NumberFilterControl)userControl;
                INumberFilter numberFilter = numberFilterControl?.UINumberFilter?.Filter;
                if (numberFilter != null)
                {
                    complexReferenceFilter = new ComplexReferenceNumberFilter() { RelationCluster = complexReferenceFilter.RelationCluster, ComplexReference = complexReference, Inverted = numberFilter.Inverted, NumberComparisonType = numberFilter.NumberComparisonType, Value = numberFilter.Value };
                }
            }
            else if (userControl is TextFilterControl)
            {
                TextFilterControl textFilterControl = (TextFilterControl)userControl;
                ITextFilter textFilter = textFilterControl?.UITextFilter?.Filter;
                if (textFilter != null)
                {
                    complexReferenceFilter = new ComplexReferenceTextFilter() { RelationCluster = complexReferenceFilter.RelationCluster, ComplexReference = complexReference, Inverted = textFilter.Inverted, TextComparisonType = textFilter.TextComparisonType, Value = textFilter.Value };
                }
            }

            return new UIComplexReferenceFilter(result.Name, result.Type, complexReferenceFilter);
        }

        private void SetUIComplexReferenceFilter(UIComplexReferenceFilter uIComplexReferenceFilter)
        {
            this.uIComplexReferenceFilter = uIComplexReferenceFilter;
            if(uIComplexReferenceFilter == null)
            {
                return;
            }

            StackPanel_Filter.Children.Clear();

            ComplexReferenceFilter complexReferenceFilter = uIComplexReferenceFilter.Filter;

            TextBox_ComplexReference.TextChanged -= TextBox_ComplexReference_TextChanged;
            TextBox_ComplexReference.Text = complexReferenceFilter?.ComplexReference?.ToString();
            TextBox_ComplexReference.TextChanged += TextBox_ComplexReference_TextChanged;

            if (complexReferenceFilter is ComplexReferenceNumberFilter)
            {
                ComplexReferenceNumberFilter complexReferenceNumberFilter = complexReferenceFilter as ComplexReferenceNumberFilter;
                NumberFilterControl numberFilterControl = new NumberFilterControl(new UINumberFilter(uIComplexReferenceFilter.Name, uIComplexReferenceFilter.Type, complexReferenceNumberFilter));
                numberFilterControl.Values = complexReferenceNumberFilter.RelationCluster?.GetValues(complexReferenceNumberFilter.ComplexReference).ConvertAll(x => x?.ToString());
                numberFilterControl.FilterChanged += NumberFilterControl_FilterChanged;
                numberFilterControl.FilterRemoving += NumberFilterControl_FilterRemoving;

                StackPanel_Filter.Children.Add(numberFilterControl);
            }
            else if(complexReferenceFilter is ComplexReferenceTextFilter)
            {
                ComplexReferenceTextFilter complexReferenceTextFilter = complexReferenceFilter as ComplexReferenceTextFilter;
                TextFilterControl textFilterControl = new TextFilterControl(new UITextFilter(uIComplexReferenceFilter.Name, uIComplexReferenceFilter.Type, complexReferenceTextFilter));
                textFilterControl.Values = complexReferenceTextFilter.RelationCluster?.GetValues(complexReferenceTextFilter.ComplexReference).ConvertAll(x => x?.ToString());
                textFilterControl.FilterChanged += NumberFilterControl_FilterChanged;
                textFilterControl.FilterRemoving += NumberFilterControl_FilterRemoving;

                StackPanel_Filter.Children.Add(textFilterControl);
            }
            else
            {
                return;
            }
        }

        private void NumberFilterControl_FilterRemoving(object sender, FilterRemovingEventArgs e)
        {
            FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
        }

        private void NumberFilterControl_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
        }

        private void TextBox_ComplexReference_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ComplexReferenceFilter complexReferenceFilter = uIComplexReferenceFilter?.Filter;
            if (complexReferenceFilter != null)
            {
                RelationCluster relationCluster = complexReferenceFilter.RelationCluster;
                if (relationCluster != null)
                {
                    IComplexReference complexReference = Core.Convert.ComplexReference(TextBox_ComplexReference.Text);
                    if (complexReference != null)
                    {
                        IFilterControl filterControl = null;

                        UIElementCollection uIElementCollection = StackPanel_Filter.Children;
                        if (uIElementCollection != null && uIElementCollection.Count != 0)
                        {
                            filterControl = uIElementCollection[0] as IFilterControl;
                        }

                        ComplexReferenceFilter complexReferenceFilter_Default = UI.Query.DefaultComplexReferenceFilter(complexReference, relationCluster);
                        if (complexReferenceFilter_Default != null)
                        {
                            if (filterControl == null || complexReferenceFilter.GetType() != complexReferenceFilter_Default?.GetType())
                            {
                                UIComplexReferenceFilter = new UIComplexReferenceFilter(uIComplexReferenceFilter.Name, uIComplexReferenceFilter?.Type, complexReferenceFilter_Default);
                            }
                        }

                        if(filterControl is TextFilterControl)
                        {
                            ((TextFilterControl)filterControl).Values = complexReferenceFilter.RelationCluster?.GetValues(complexReference).ConvertAll(x => x?.ToString());
                        }

                        if (filterControl is NumberFilterControl)
                        {
                            ((NumberFilterControl)filterControl).Values = complexReferenceFilter.RelationCluster?.GetValues(complexReference).ConvertAll(x => x?.ToString());
                        }
                    }
                }
            }

             FilterChanged?.Invoke(this, new FilterChangedEventArgs(UIFilter));
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

        private void MenuItem_Remove_Click(object sender, RoutedEventArgs e)
        {
            FilterRemoving?.Invoke(this, new FilterRemovingEventArgs(this));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RelationClusterComplexReferenceWindow relationClusterComplexReferenceWindow = new RelationClusterComplexReferenceWindow();
            relationClusterComplexReferenceWindow.RelationCluster = UIComplexReferenceFilter?.Filter?.RelationCluster;
            relationClusterComplexReferenceWindow.Type = uIComplexReferenceFilter.Type;
            relationClusterComplexReferenceWindow.TypesEnabled = false;
            //relationClusterComplexReferenceWindow.Name

            bool? dialogResult = relationClusterComplexReferenceWindow.ShowDialog();

            if(dialogResult != null && dialogResult.HasValue && dialogResult.Value)
            {
                TextBox_ComplexReference.Text = relationClusterComplexReferenceWindow.ComplexReference.ToString();
            }
        }
    }
}
