using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// For mollierModel it gets all processes that are in groups
        /// and it regroups it 
        /// </summary>
        /// <param name="mollierModel">Mollier model</param>
        /// <returns>New List of mollier group</returns>
        public static List<IMollierGroup> RegroupProcesses(this MollierModel mollierModel)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<IMollierGroup> result = new List<IMollierGroup>();

            List<IMollierGroup> mollierGroups = mollierModel.GetMollierObjects<IMollierGroup>();
            List<IMollierProcess> processes = new List<IMollierProcess>();
            foreach(MollierGroup mollierGroup in mollierGroups)
            {
                processes.AddRange(mollierGroup.GetMollierProcesses());
            }
            result = Mollier.Query.Group(processes);

            mollierModel.Clear<IMollierGroup>();
            mollierModel.AddRange(result);

            return result;
        }
    }
}