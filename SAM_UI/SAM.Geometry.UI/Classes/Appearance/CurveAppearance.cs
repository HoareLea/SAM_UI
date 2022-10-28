using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace SAM.Geometry.UI
{
    public class CurveAppearance : PointAppearance
    {
        public CurveAppearance(Color color, double thickness) 
            : base(color, thickness)
        {
        }

        public CurveAppearance(JObject jObject)
            : base(jObject)
        {

        }

        public CurveAppearance(CurveAppearance curveAppearance)
            : base(curveAppearance)
        {

        }
    }
}
