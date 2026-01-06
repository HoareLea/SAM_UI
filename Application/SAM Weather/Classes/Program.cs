using System;
using System.Windows.Forms;

namespace SAM.Weather.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Windows.Forms.WeatherDataForm());
        }
    }
}
