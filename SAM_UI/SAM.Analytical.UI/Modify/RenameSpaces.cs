using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static Dictionary<Space, string> RenameSpaces(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, string name, RenameSpaceOption renameSpaceOption)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = RenameSpaces(adjacencyCluster, spaces, name, renameSpaceOption);
            if(result == null || result.Count == 0)
            {
                return result;
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(result.Keys));
            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, Position position, int count)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = RenameSpaces(adjacencyCluster, spaces, position, count);
            if (result == null || result.Count == 0)
            {
                return result;
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(result.Keys));
            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, Position position, string text)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = RenameSpaces(adjacencyCluster, spaces, position, text);
            if (result == null || result.Count == 0)
            {
                return result;
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(result.Keys));
            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces, string text_Old, string text_New)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = RenameSpaces(adjacencyCluster, spaces, text_Old, text_New);
            if (result == null || result.Count == 0)
            {
                return result;
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster), new AnalyticalModelModification(result.Keys));
            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, string name, RenameSpaceOption renameSpaceOption)
        {
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = new Dictionary<Space, string>();
            foreach (Space space in spaces)
            {
                string newName = adjacencyCluster.RenameSpace(space, name, renameSpaceOption);
                if (newName != null)
                {
                    result[space] = newName;
                }
            }

            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, Position position, int count)
        {
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = new Dictionary<Space, string>();
            foreach (Space space in spaces)
            {
                string newName = adjacencyCluster.RenameSpace(space, position, count);
                if (newName != null)
                {
                    result[space] = newName;
                }
            }

            return result;
        }

        public static Dictionary<Space, string> RenameSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, Position position, string text)
        {
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = new Dictionary<Space, string>();
            foreach (Space space in spaces)
            {
                string newName = adjacencyCluster.RenameSpace(space, position, text);
                if (newName != null)
                {
                    result[space] = newName;
                }
            }
            return result;
        }
        
        public static Dictionary<Space, string> RenameSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, string text_Old, string text_New)
        {
            if (adjacencyCluster == null)
            {
                return null;
            }

            Dictionary<Space, string> result = new Dictionary<Space, string>();
            foreach (Space space in spaces)
            {
                string newName = adjacencyCluster.RenameSpace(space, text_Old, text_New);
                if (newName != null)
                {
                    result[space] = newName;
                }
            }
            return result;
        }

    }
}