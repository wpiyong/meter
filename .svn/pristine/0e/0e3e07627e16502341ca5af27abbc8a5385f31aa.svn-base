using gColor.Model.Nikon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace gColor.ViewModel
{
    public enum ExposureMode
    {
        Program,
        Focus,
        Shutter,
        Iso,
        Manual
    }


    class NikonSettingsViewModel : ViewModelBase
    {
        NikonCamera _camera;

        public RelayCommand CommandWhiteBalance { get; set; }
        public RelayCommand CommandClearSaturation { get; set; }
        public RelayCommand CommandClearHue { get; set; }
        public RelayCommand CommandClearWBRed { get; set; }
        public RelayCommand CommandClearWBBlue { get; set; }

        public NikonSettingsViewModel(NikonCamera c)
        {
            base.DisplayName = "NikonSettingsViewModel";
            _camera = c;

            CommandWhiteBalance = new RelayCommand(param => this.ExecuteWhiteBalance());
            CommandClearSaturation = new RelayCommand(param => this.ClearSaturation());
            CommandClearHue = new RelayCommand(param => this.ClearHue());
            CommandClearWBRed = new RelayCommand(param => this.ClearWBRed());
            CommandClearWBBlue = new RelayCommand(param => this.ClearWBBlue());
        }

        public int ExposureModeProperty
        {
            get
            {
                return _camera.ExposureModeProperty;
            }
            set
            {
                _camera.ExposureModeProperty = value;
                OnPropertyChanged("ExposureModeProperty");
            }
        }

        public bool ManualExposure
        {
            get
            {
                return ExposureModeProperty != (int)ExposureMode.Program;
            }
        }

        public bool AutoExposure
        {
            get
            {
                return ExposureModeProperty == (int)ExposureMode.Program;
            }
            set
            {
                ExposureModeProperty =  (ExposureModeProperty == (int)ExposureMode.Manual ? 
                                            (int)ExposureMode.Program : (int)ExposureMode.Manual);
                OnPropertyChanged("AutoExposure");
                OnPropertyChanged("ManualExposure");
                OnPropertyChanged("ExposureTime");
                OnPropertyChanged("Gain");
            }
        }


        public int WBRed
        {
            get 
            {
                return _camera.WBRed; 
            }
            set
            {
                _camera.WBRed = value;
                OnPropertyChanged("WBRed");
            }
        }

        public int WBBlue
        {
            get 
            {
                return _camera.WBBlue; 
            }
            set
            {
                _camera.WBBlue = value;
                OnPropertyChanged("WBBlue");
            }
        }

        public int Saturation
        {
            get 
            {
                return _camera.Saturation; 
            }
            set
            {
                _camera.Saturation = value;
                OnPropertyChanged("Saturation");
            }
        }

        public int Hue
        {
            get 
            {
                return _camera.Hue;
            }
            set
            {
                _camera.Hue = value;
                OnPropertyChanged("Hue");
            }
        }

        public int Gain
        {
            get 
            {
                return _camera.Gain; 
            }
            set
            {
                _camera.Gain = value;
                
                OnPropertyChanged("Gain");
            }
        }

       

        public int ExposureTime
        {
            get
            {
                return _camera.ExposureTimeMS; 
            }
            set
            {
                _camera.ExposureTimeMS = value;
                
                OnPropertyChanged("ExposureTime");
            }
        }


        void ClearSaturation()
        {
            Saturation = 0;
        }

        void ClearHue()
        {
            Hue = 0;
        }

        void ClearWBRed()
        {
            WBRed = 1500;
        }

        void ClearWBBlue()
        {
            WBBlue = 1700;
        }

        void ExecuteWhiteBalance()
        {
            _camera.ExecuteWhiteBalance();
            OnPropertyChanged("WBRed");
            OnPropertyChanged("WBBlue");
        }
    }
}
