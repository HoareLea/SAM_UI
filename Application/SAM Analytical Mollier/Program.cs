﻿using System;
using System.Windows.Forms;

namespace SAM.Analytical.Mollier.UI
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
            Application.Run(new Core.Mollier.UI.MollierForm());
        }
    }
}
