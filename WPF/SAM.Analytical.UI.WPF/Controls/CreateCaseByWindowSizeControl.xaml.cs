using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSize.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeControl : UserControl
    {
        public CreateCaseByWindowSizeControl()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        public IEnumerable<WindowSizeCase>? WindowSizeCases
        {
            get
            {
                if (DataContext is not MainViewModel mainViewModel)
                {
                    return null;
                }

                List<WindowSizeCase> result = [];
                foreach(WindowSizeCase windowSizeCase in mainViewModel.Items)
                {
                    result.Add(windowSizeCase);
                }

                return result;
            }

            set
            {
                if (DataContext is not MainViewModel mainViewModel)
                {
                    return;
                }

                mainViewModel.Items.Clear();

                if (value == null)
                {
                    return;
                }

                foreach(WindowSizeCase windowSizeCase in value)
                {
                    mainViewModel.Items.Add(windowSizeCase);
                }
            }
        }
    }

    public class WindowSizeCase : INotifyPropertyChanged
    {
        private double apertureScaleFactor;

        public double ApertureScaleFactor
        {
            get
            {
                return apertureScaleFactor; 
            }

            set
            {
                apertureScaleFactor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApertureScaleFactor)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class MainViewModel
    {
        public ObservableCollection<WindowSizeCase> Items { get; set; }

        public MainViewModel()
        {
            Items = new ObservableCollection<WindowSizeCase>
            {
                new() { ApertureScaleFactor = 0.8 }
            };
        }
    }
}
