using SAM.Core;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for InternalConditionWindow.xaml
    /// </summary>
    public partial class InternalConditionWindow : System.Windows.Window
    {
        private UIAnalyticalModel uIAnalyticalModel;

        public InternalConditionWindow(UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces = null)
        {
            InitializeComponent();

            this.uIAnalyticalModel = uIAnalyticalModel;

            listBoxControl.SelectionMode = System.Windows.Controls.SelectionMode.Multiple;

            Load(spaces);
        }

        private void Load(IEnumerable<Space> spaces)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            internalConditionControl.AnalyticalModel = analyticalModel;

            List<Space> spaces_Temp = new List<Space>();

            if (spaces != null)
            {
                List<Space> spaces_AnalyticalModel = analyticalModel.GetSpaces();
                foreach (Space space in spaces)
                {
                    if (space == null)
                    {
                        continue;
                    }

                    int index = spaces_AnalyticalModel.FindIndex(x => x.Guid == space.Guid);
                    if (index == -1)
                    {
                        continue;
                    }

                    spaces_Temp.Add(spaces_AnalyticalModel[index]);
                }
            }
            else
            {
                spaces_Temp = analyticalModel.GetSpaces();
            }

            List<InternalConditionData> internalConditionDatas = spaces_Temp?.ConvertAll(x => new InternalConditionData(analyticalModel, x));

            listBoxControl.SelectionChanged += ListBoxControl_SelectionChanged;
            listBoxControl.SetValues(internalConditionDatas, x => x?.Space?.Name);
            listBoxControl.SelectAll();
        }

        public List<InternalConditionData> InternalConditionDatas
        {
            get
            {
                return internalConditionControl.InternalConditionDatas;
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return internalConditionControl.InternalConditionDatas?.ConvertAll(x => x?.Space);
            }
        }

        private void ListBoxControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            internalConditionControl.InternalConditionDatas =  listBoxControl.GetValues<InternalConditionData>(true);
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            DialogResult = true;
        }

        private void Apply()
        {
            List<InternalConditionData> internalConditionDatas = internalConditionControl.InternalConditionDatas;
            if(internalConditionDatas == null || internalConditionDatas.Count == 0)
            {
                return;
            }

            AnalyticalModel analyticalModel = internalConditionControl.AnalyticalModel;

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>(); 
            foreach (InternalConditionData internalConditionData in internalConditionDatas)
            {
                Space space = internalConditionData.Space;
                if(space == null)
                {
                    continue;
                }

                if(adjacencyCluster.AddObject(space))
                {
                    listBoxControl.UpdateValue(internalConditionData, x => x.Space.Guid.ToString());
                    sAMObjects.Add(space);
                }
            }

            internalConditionControl.InternalConditionDatas = listBoxControl.GetValues<InternalConditionData>(true);

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
            internalConditionControl.AnalyticalModel = analyticalModel;

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new AnalyticalModelModification(sAMObjects));
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }
    }
}
