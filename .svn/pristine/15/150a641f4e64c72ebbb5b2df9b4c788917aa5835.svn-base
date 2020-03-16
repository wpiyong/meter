using gUV.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace gUV
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
            if (GlobalVariables.gColorAppSettings.Load() && GlobalVariables.fluorescenceSettings.Load()
                && GlobalVariables.spectrumSettings.Load())
            {
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
            else
            {
               MessageBox.Show("An error occurred before the application could start.\n\nCould not load settings. ",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                Shutdown();

            }
        }

        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(e.Exception.ToString());
            e.Handled = true;
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                if (gUV.Properties.Settings.Default.AutoOpenClose && gUV.Model.Hemisphere.HemisphereMotorConnected)
                {
                    System.Threading.Thread.Sleep(500); //time to give any other threads first execution
                    int delay = 12;
                    while (gUV.Model.Hemisphere.CommandActive && --delay > 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    if (delay > 0)
                        gUV.Model.Hemisphere.Close(null, null);
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

        public static Model.AppSettings gColorAppSettings = new Model.AppSettings();
        public static Model.FluorescenceSettings fluorescenceSettings = new Model.FluorescenceSettings();
        public static Model.SpectrumSettings spectrumSettings = new Model.SpectrumSettings();

        public static gUV.Model.Endpoints endpoints;
    }
}
