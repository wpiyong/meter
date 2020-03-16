using gColor.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace gColor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static LogEntryViewModel LogEntry;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if DEBUG
            GlobalVariables.IsAdmin = true;
            string[] args = e.Args;
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "test":
                        GlobalVariables.InTestMode = true;
                        break;
                    default:
                        break;
                }
            }
#endif

            gColor.Model.AppSettings.Load();
            GlobalVariables.LightStableSettings.Load();

            var window = new MainWindow();

            // Create the ViewModel to which 
            // the main window binds.
            LogEntry = new LogEntryViewModel();
            var viewModel = new MainWindowViewModel();
            
            // Allow all controls in the window to 
            // bind to the ViewModel by setting the 
            // DataContext, which propagates down 
            // the element tree.
            window.DataContext = viewModel;
            window.Closing += viewModel.OnWindowClosing;

            window.Show();
        }

        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(e.Exception.ToString());
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                if (gColor.Properties.Settings.Default.AutoOpenClose && gColor.Model.Hemisphere.HemisphereMotorConnected)
                {
                    System.Threading.Thread.Sleep(500); //time to give any other threads first execution
                    int delay = 12;
                    while (gColor.Model.Hemisphere.CommandActive && --delay > 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    if (delay > 0)
                        gColor.Model.Hemisphere.Close(null, null);
                }
            }
            catch
            {
                
            }

            
        }
    }

    public class GlobalVariables
    {
        public static bool IsAdmin = false;
        public static bool InTestMode = false;

        static gColor.Model.LightStabilitySettings _lightStabilitySettings = new gColor.Model.LightStabilitySettings();
        public static gColor.Model.LightStabilitySettings LightStableSettings { get { return _lightStabilitySettings; } }

    }
}
