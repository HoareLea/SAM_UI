using SAM.Core.UI.WPF;

namespace SAM.Analytical.UI.WPF
{
    public class VisualAnalyticalModel : VisualJSAMObject<AnalyticalModel>
    {
        public VisualAnalyticalModel(AnalyticalModel analyticalModel)
            :base(analyticalModel)
        {

        }

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                return jSAMObject;
            }
        }
    }
}
