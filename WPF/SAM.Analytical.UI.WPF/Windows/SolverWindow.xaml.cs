using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for SolverWindow.xaml
    /// </summary>
    public partial class SolverWindow : System.Windows.Window
    {
        public SolverWindow()
        {
            InitializeComponent();
        }

        public double BucketSize_MultipleLevel
        {
            get
            {
                return SolverControl_Main.BucketSize_MultipleLevel;
            }
        }

        public double BucketSize_SingleLevel
        {
            get
            {
                return SolverControl_Main.BucketSize_SingleLevel;
            }
        }

        public List<PanelType> ExcludedPanelTypes
        {
            get
            {
                return SolverControl_Main.ExcludedPanelTypes;
            }
        }

        public bool FilterPanels
        {
            get
            {
                return SolverControl_Main.FilterPanels;
            }
        }

        public double LevelOffset
        {
            get
            {
                return SolverControl_Main.LevelOffset;
            }
        }

        public List<double> Levels
        {
            get
            {
                return SolverControl_Main.Levels;
            }

            set
            {
                SolverControl_Main.Levels = value;
            }
        }

        public double MaxExtension
        {
            get
            {
                return SolverControl_Main.MaxExtension;
            }
        }

        public double MinArea
        {
            get
            {
                return SolverControl_Main.MinArea;
            }
        }

        public double MinThinnessRatio
        {
            get
            {
                return SolverControl_Main.MinThinnessRatio;
            }
        }

        public bool RemovePanelInternalEdges
        {
            get
            {
                return SolverControl_Main.RemovePanelInternalEdges;
            }
        }

        public bool RemoveUnusedSpaces
        {
            get
            {
                return SolverControl_Main.RemoveUnusedSpaces;
            }
        }

        public double Weight
        {
            get
            {
                return SolverControl_Main.Weight;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            List<double> levels = SolverControl_Main.Levels;


            if (levels is null || levels.Count < 2)
            {
                MessageBox.Show("Please select at least two levels");
                return;
            }
            
            DialogResult = true;
            Close();
        }

        private void SolverControl_Main_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
