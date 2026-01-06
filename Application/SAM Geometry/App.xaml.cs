// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM_Geometry_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {


            // Create main application window, starting minimized if specified
            SAM.Geometry.UI.WPF.GeometryWindow geometryWindow = new SAM.Geometry.UI.WPF.GeometryWindow();

            geometryWindow.Show();
        }
    }
}
