using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCasesControl.xaml
    /// </summary>
    public partial class CreateCasesControl : UserControl
    {
        private AnalyticalModel analyticalModel;

        public CreateCasesControl()
        {
            InitializeComponent();

            MenuItem menuItem;

            ContextMenu contextMenu = new ContextMenu();

            menuItem = new MenuItem() { Name = "MenuItem_Remove", Header = "Remove" };
            menuItem.Click += MenuItem_Remove_Click;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem() { Name = "MenuItem_Edit", Header = "Edit" };
            menuItem.Click += MenuItem_Edit_Click;
            contextMenu.Items.Add(menuItem);

            ListBox_Cases.ContextMenu = contextMenu;
        }

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                return analyticalModel;
            }

            set
            {
                analyticalModel = value;
            }
        }

        public List<Cases> Cases
        {
            get
            {
                List<Cases> result = [];

                foreach (object item in ListBox_Cases.Items)
                {
                    if (item is not ListBoxItem listBoxItem)
                    {
                        continue;
                    }

                    if (listBoxItem.Tag is not Cases cases)
                    {
                        continue;
                    }

                    result.Add(cases);
                }

                return result;
            }

            set
            {
                SetCases(value);
            }
        }
        private void Add(Cases cases, int index = -1)
        {
            if (cases is null)
            {
                return;
            }

            ListBoxItem listBoxItem = new()
            {
                Content = Query.Name(cases),
                Tag = cases
            };

            listBoxItem.MouseDoubleClick += ListBoxItem_MouseDoubleClick;

            if(index == -1)
            {
                ListBox_Cases.Items.Add(listBoxItem);
            }
            else
            {
                ListBox_Cases.Items[index] = listBoxItem;
            }
        }

        private void Add_ApertureConstructionCases(IEnumerable<ApertureConstructionCase>? apertureConstructionCases = null, int index = -1)
        {
            CreateCaseByApertureConstructionWindow createCaseByApertureConstructionWindow = new()
            {
                AnalyticalModel = analyticalModel
            };

            if (apertureConstructionCases is not null && apertureConstructionCases.Any())
            {
                createCaseByApertureConstructionWindow.ApertureConstructionCases = apertureConstructionCases;
            }

            bool? dialogResult = null;

            dialogResult = createCaseByApertureConstructionWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            if (createCaseByApertureConstructionWindow.ApertureConstructionCases is not IEnumerable<ApertureConstructionCase> apertureConstructionCases_Temp)
            {
                return;
            }

            Add(Analytical.Create.Cases(apertureConstructionCases_Temp), index);
        }

        private void Add_FinShadeCases(IEnumerable<FinShadeCase>? finShadeCases = null, int index = -1)
        {
            if (finShadeCases is null)
            {
                finShadeCases = [new FinShadeCase(false, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, null)];
            }

            CreateCaseByFinShadeWindow createCaseByWindowSizeWindow = new()
            {
                FinShadeCases = finShadeCases,
                AnalyticalModel = analyticalModel
            };

            bool? dialogResult = null;

            dialogResult = createCaseByWindowSizeWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            if (createCaseByWindowSizeWindow.FinShadeCases is not IEnumerable<FinShadeCase> finShadeCases_Temp)
            {
                return;
            }

            Add(Analytical.Create.Cases(finShadeCases_Temp), index);
        }

        private void Add_WeatherDataCases(IEnumerable<WeatherDataCase>? weatherDataCases = null, int index = -1)
        {
            CreateCaseByWeatherDataWindow createCaseByWeatherDataWindow = new();
            if(weatherDataCases is not null && weatherDataCases.Any())
            {
                createCaseByWeatherDataWindow.WeatherDataCases = weatherDataCases;
            }

            bool? dialogResult = null;

            dialogResult = createCaseByWeatherDataWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            if (createCaseByWeatherDataWindow.WeatherDataCases is not IEnumerable<WeatherDataCase> weatherDataCase_Temp)
            {
                return;
            }

            Add(Analytical.Create.Cases(weatherDataCase_Temp), index);
        }
        
        private void Add_WindowSizeCases(IEnumerable<WindowSizeCase>? windowSizeCases = null, int index = -1)
        {
            if (windowSizeCases is null)
            {
                windowSizeCases = [new WindowSizeCase(0.8, null)];
            }

            CreateCaseByWindowSizeWindow createCaseByWindowSizeWindow = new()
            {
                WindowSizeCases = windowSizeCases,
                AnalyticalModel = analyticalModel
            };

            bool? dialogResult = null;

            dialogResult = createCaseByWindowSizeWindow.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            if (createCaseByWindowSizeWindow.WindowSizeCases is not IEnumerable<WindowSizeCase> windowSizeCases_Temp)
            {
                return;
            }

            Add(Analytical.Create.Cases(windowSizeCases_Temp), index);
        }
        private void Button_CaseByApertureConstruction_Click(object sender, RoutedEventArgs e)
        {
            Add_ApertureConstructionCases();
        }

        private void Button_CaseByFinShade_Click(object sender, RoutedEventArgs e)
        {
            Add_FinShadeCases();
        }

        private void Button_CaseByWeatherData_Click(object sender, RoutedEventArgs e)
        {
            Add_WeatherDataCases();
        }

        private void Button_CaseByWindowSize_Click(object sender, RoutedEventArgs e)
        {
            Add_WindowSizeCases();
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            ListBox_Cases.Items.Clear();
        }

        private void Button_Down_Click(object sender, RoutedEventArgs e)
        {
            int index = ListBox_Cases.SelectedIndex;
            if (index >= 0 && index < ListBox_Cases.Items.Count - 1)
            {
                var itemToMoveDown = ListBox_Cases.Items[index];
                ListBox_Cases.Items.RemoveAt(index);
                ListBox_Cases.Items.Insert(index + 1, itemToMoveDown);
                ListBox_Cases.SelectedIndex = index + 1;
            }
        }

        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {
            string? path = null;
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            List<Cases> cases = Core.Convert.ToSAM<Cases>(path);
            if (cases == null)
            {
                return;
            }

            SetCases(cases);
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            List<Cases> cases = Cases;
            if (cases is null)
            {
                return;
            }

            string? path = null;

            using (System.Windows.Forms.SaveFileDialog saveFileDialog = new())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = "cases.json";
                if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                path = saveFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Core.Convert.ToFile(cases, path);
        }

        private void Button_Up_Click(object sender, RoutedEventArgs e)
        {
            int index = ListBox_Cases.SelectedIndex;
            if (index > 0)
            {
                var itemToMoveUp = ListBox_Cases.Items[index];
                ListBox_Cases.Items.RemoveAt(index);
                ListBox_Cases.Items.Insert(index - 1, itemToMoveUp);
                ListBox_Cases.SelectedIndex = index - 1;
            }
        }

        private void Edit()
        {
            Cases? cases = GetCases(out int index);
            if (cases is null)
            {
                return;
            }

            System.Type type = cases.BaseType;
            if (type is null)
            {
                return;
            }

            if (cases.GetCases<WindowSizeCase>() is IEnumerable<WindowSizeCase> windowSizeCases && windowSizeCases.Any())
            {
                Add_WindowSizeCases(windowSizeCases, index);
                return;
            }

            if (cases.GetCases<WeatherDataCase>() is IEnumerable<WeatherDataCase> weatherDataCases && weatherDataCases.Any())
            {
                Add_WeatherDataCases(weatherDataCases, index);
                return;
            }

            if (cases.GetCases<ApertureConstructionCase>() is IEnumerable<ApertureConstructionCase> apertureConstructionCase && apertureConstructionCase.Any())
            {
                Add_ApertureConstructionCases(apertureConstructionCase, index);
                return;
            }

            if (cases.GetCases<FinShadeCase>() is IEnumerable<FinShadeCase> finShadeCases && finShadeCases.Any())
            {
                Add_FinShadeCases(finShadeCases, index);
                return;
            }
        }

        private Cases? GetCases(out int index)
        {
            index = ListBox_Cases.SelectedIndex;
            if (index == -1)
            {
                return null;
            }

            return (ListBox_Cases.Items[index] as ListBoxItem)?.Tag as Cases;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Edit();
        }
        
        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }
        
        private void MenuItem_Remove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetCases(IEnumerable<Cases> cases)
        {
            ListBox_Cases.Items.Clear();
            if(cases is null)
            {
                return;
            }

            foreach(Cases cases_Temp in cases)
            {
                Add(cases_Temp);
            }   
        }
    }
}
