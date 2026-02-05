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
using System.Windows.Shapes;

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

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public List<PanelType> ExcludedPanelTypes
        {
            get
            {
                return SolverControl_Main.ExcludedPanelTypes;
            }
        }

        public bool RemovePanelInternalEdges
        {
            get
            {
                return SolverControl_Main.RemovePanelInternalEdges;
            }
        }

        public bool FilterPanels
        {
            get
            {
                return SolverControl_Main.FilterPanels;
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

        public double BucketSize_SingleLevel
        {
            get
            {
                return SolverControl_Main.BucketSize_SingleLevel;
            }
        }

        public double BucketSize_MultipleLevel
        {
            get
            {
                return SolverControl_Main.BucketSize_MultipleLevel;
            }
        }

        public double Weight
        {     get
            {
                return SolverControl_Main.Weight;
            }
        }

        public double MaxExtension
        {
            get
            {
                return SolverControl_Main.MaxExtension;
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
    }
}
