using SAM.Core.Mollier.UI.Controls;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static List<Control> Controls(this IEnumerable<ProcessParameterType> processParameterTypes)
        {
            if(processParameterTypes == null)
            {
                return null;
            }
            List<Control> result = new List<Control>();
            foreach(ProcessParameterType processParameterType in processParameterTypes)
            {
                ParameterControl parameterControl = new ParameterControl(processParameterType);
                result.Add(parameterControl);
            }
            return result;
        }
    }
}
