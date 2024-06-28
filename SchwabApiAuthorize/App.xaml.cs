using System.Configuration;
using System.Data;
using System.Windows;

namespace SchwabApiAuthorize
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // public static string[] commandLineArgs; 
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SchwabApiAuthorize.MainWindow.CommandLineArgs = e.Args;
        }
    }

}
