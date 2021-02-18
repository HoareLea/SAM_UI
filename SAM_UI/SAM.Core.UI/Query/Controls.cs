using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static List<T> Controls<T>(this Form form) where T : Control
        {
            List<Control> controls = Controls(form);
            if (controls == null)
                return null;

            return controls.FindAll(x => x is T).Cast<T>().ToList();

        }
        
        public static List<Control> Controls(this Form form)
        {
            Control.ControlCollection controls = form?.Controls;
            if (controls == null)
                return null;

            List<Control> result = new List<Control>();
            foreach (Control control in controls)
                Controls(control, ref result);

            return result;
        }

        private static void Controls(Control control, ref List<Control> controls)
        {
            if (controls == null)
                controls = new List<Control>();

            Control.ControlCollection controls_Temp = control?.Controls;
            if (controls_Temp == null || controls_Temp.Count == 0)
                return;

            foreach (Control control_Temp in controls_Temp)
            {
                controls.Add(control_Temp);
                Controls(control_Temp, ref controls);
            }
        }
    }
}