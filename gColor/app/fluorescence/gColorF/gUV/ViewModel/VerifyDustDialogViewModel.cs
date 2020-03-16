using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace gUV.ViewModel
{
    class VerifyDustDialogViewModel : ViewModelBase
    {
        public VerifyDustDialogViewModel(string imageNumber, BitmapSource bmp_src)
        {
            base.DisplayName = "VerifyDustDialogViewModel";

            CommandCancel = new RelayCommand(param => this.Close(param));
            CommandOK = new RelayCommand(param => this.Continue(param));

            WindowTitle = "Verify Dust: Image #" + imageNumber;
            CameraImage = bmp_src;
            
        }

        public RelayCommand CommandCancel { get; set; }
        public RelayCommand CommandOK { get; set; }

        BitmapSource _cameraImage;
        public BitmapSource CameraImage
        {
            get
            {
                return _cameraImage;
            }
            set
            {
                _cameraImage = value;
                OnPropertyChanged("CameraImage");
            }
        }

        string _windowTitle;
        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value;
                OnPropertyChanged("WindowTitle");
            }
        }

        void Continue(object param)
        {
            ((Window)param).DialogResult = true;
        }

        void Close(object param)
        {
            ((Window)param).Close();
        }
    }
}
