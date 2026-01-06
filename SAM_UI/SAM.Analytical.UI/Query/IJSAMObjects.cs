using SAM.Core;
using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static List<TJSAMObject> IJSAMObjects<TJSAMObject>(this CaseSelection caseSelection, AnalyticalModel analyticalModel) where TJSAMObject : IJSAMObject
        {
            if(caseSelection is null || analyticalModel is null)
            {
                return null;
            }

            if(caseSelection is FilterSelection filterSelection)
            {
                return filterSelection.IJSAMObjects<TJSAMObject>(analyticalModel);
            }

            return Analytical.Query.IJSAMObjects<TJSAMObject>(caseSelection, analyticalModel);
        }

        public static List<TJSAMObject> IJSAMObjects<TJSAMObject>(this FilterSelection filterSelection, AnalyticalModel analyticalModel) where TJSAMObject : IJSAMObject
        {
            if (filterSelection is null || analyticalModel?.AdjacencyCluster is not AdjacencyCluster adjacencyCluster)
            {
                return null;
            }

            List<TJSAMObject> result = [];
            if (filterSelection.Filter is not IFilter filter)
            {
                return result;
            }

            if(filter is IUIFilter uIFilter)
            {
                filter = uIFilter.Transform();
            }

            Modify.AssignAdjacencyCluster(filter, adjacencyCluster);

            return adjacencyCluster.Filter<TJSAMObject>(filter, null);
        }
    }
}