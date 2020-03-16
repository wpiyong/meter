using gUV.View;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace gUV.ViewModel
{
    class MenuViewModel : ViewModelBase
    {
       
        public MenuViewModel()
        {
            base.DisplayName = "MenuViewModel";

            CommandAdmin = new RelayCommand(param => this.LogIn());
            CommandSettings = new RelayCommand(param => this.EditSettings());
            CommandCameraSettings = new RelayCommand(param => this.EditCameraSettings(),
                cc => { return MainWindowViewModel.ControlVM.Connected; });

            CommandAbout = new RelayCommand(param => this.HelpAbout());

            CommandLogOut = new RelayCommand(param => this.LogOut());

            CommandTest = new RelayCommand(param => this.Test());
        }

        public RelayCommand CommandAdmin { get; set; }
        public RelayCommand CommandSettings { get; set; }
        public RelayCommand CommandCameraSettings { get; set; }
        public RelayCommand CommandAbout { get; set; }
        public RelayCommand CommandLogOut { get; set; }
        public RelayCommand CommandTest { get; set; }

        public System.Windows.Visibility IsAdmin
        {
            get
            {
                return GlobalVariables.IsAdmin ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        public System.Windows.Visibility IsNotAdmin
        {
            get
            {
                return GlobalVariables.IsAdmin ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
        }

        void LogIn()
        {
            var login = new View.LogIn();
            login.ShowDialog();
            GlobalVariables.IsAdmin = login.Correct;
            OnPropertyChanged("IsAdmin");
            OnPropertyChanged("IsNotAdmin");
            MainWindowViewModel.ControlVM.UpdateIsAdminView();
        }

        void EditSettings()
        {
            Settings s = new Settings();
            s.ShowDialog();
        }

        void EditCameraSettings()
        {
            MainWindowViewModel.ControlVM.EditCameraSettings();
        }

        private void HelpAbout()
        {
            gUV.View.HelpAbout ha = new View.HelpAbout();
            ha.ShowDialog();
        }


        void LogOut()
        {
            GlobalVariables.IsAdmin = false;
            OnPropertyChanged("IsAdmin");
            OnPropertyChanged("IsNotAdmin");
            MainWindowViewModel.ControlVM.UpdateIsAdminView();
        }

        void Test()
        {
        }

    }
}
