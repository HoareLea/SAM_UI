using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static Series AddMollierPoints(this Chart chart, IEnumerable<UIMollierPoint> uIMollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings == null || chart == null)
            {
                return null;
            }

            Series result = chart.Series.ToList().Find(x => x.Name == "MollierPoints");
            if(result == null)
            {
                result = chart.Series.Add("MollierPoints");
            }

            ChartType chartType = mollierControlSettings.ChartType;

            throw new System.NotImplementedException();

            return result;
        }
    }
}
