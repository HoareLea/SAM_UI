using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditNCMDatas(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            List<Tuple<Space, NCMData>> tuples = new List<Tuple<Space, NCMData>>();
            foreach (Space space in spaces)
            {
                if(space == null)
                {
                    continue;
                }

                Space space_Temp = adjacencyCluster.GetObject<Space>(space.Guid);
                if(space_Temp == null)
                {
                    continue;
                }

                InternalCondition internalCondition = space_Temp.InternalCondition;
                if(internalCondition == null)
                {
                    MessageBox.Show(string.Format("Cannot assign NCM. Space {0} is missing Internal Condition.", string.IsNullOrWhiteSpace(space_Temp.Name) ? "???" : space_Temp.Name));
                    return;
                }

                if(!internalCondition.TryGetValue(InternalConditionParameter.NCMData, out NCMData nCMData))
                {
                    continue;
                }

                tuples.Add(new Tuple<Space, NCMData>(space_Temp, nCMData));
            }

            if(tuples == null || tuples.Count == 0)
            {
                return;
            }

            NCMDataWindow nCMDataWindow = new NCMDataWindow();
            nCMDataWindow.NCMDatas = tuples.ConvertAll(x => x.Item2);

            bool? dialogResult = nCMDataWindow.ShowDialog();
            if(dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            List<NCMData> nCMDatas = nCMDataWindow.NCMDatas;
            if(nCMDatas == null || tuples.Count != nCMDatas.Count)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = new List<Core.SAMObject>();
            for (int i = 0; i < tuples.Count; i++)
            {
                NCMData nCMData = nCMDatas[i];

                Tuple<Space, NCMData> tuple = tuples[i];

                Space space = tuple.Item1;

                InternalCondition internalCondition = space.InternalCondition;

                internalCondition.SetValue(InternalConditionParameter.NCMData, nCMData == null ? null : new NCMData(nCMData));

                space.InternalCondition = internalCondition;

                adjacencyCluster.AddObject(space);
                sAMObjects.Add(space);
            }

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new AnalyticalModelModification(sAMObjects));

        }
    }
}