using SAM.Analytical.UI.WPF.Windows;
using SAM.Core;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {

            StartupOptions startupOptions = Core.Create.StartupOptions(e.Args);

            // Create main application window, starting minimized if specified
            AnalyticalWindow mainWindow = new AnalyticalWindow(startupOptions);
            mainWindow.Show();
        }
    }
}
