using gUV.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gUV.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            base.DisplayName = "MainWindowViewModel";
            ControlVM = new ControlViewModel();
            MenuVM = new MenuViewModel();

            ControlVM.LogEntryVM.AddEntry("Application Started");
        }

        public static ControlViewModel ControlVM { get; set; }
        public MenuViewModel MenuVM { get; set; }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            ControlVM.Dispose();
            MenuVM.Dispose();
            App.Current.Shutdown();
        }
    }
}
