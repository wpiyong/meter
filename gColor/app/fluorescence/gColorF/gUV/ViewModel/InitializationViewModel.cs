using ClassOpenCV;
using gUV.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace gUV.ViewModel
{
    class InitializationViewModel : ViewModelBase
    {
        double _shutter, _cameraTemp, _wbRed, _wbBlue, _bValue;

        DispatcherTimer _dispatcherTimer;
        Camera _camera;
        ControlViewModel _controlViewModel;
        bool _enableReadyButton;

        //Hiroshi add for light source stabilization
        double _L, _a, _b;
        double _shutterTarget = 2.0;
        List<double> _shutterList = new List<double>();
        List<double> _cameraTempList = new List<double>();
        List<int> _wbRedList = new List<int>();
        List<int> _wbBlueList = new List<int>();
        List<double> _LList = new List<double>();
        List<double> _aList = new List<double>();
        List<double> _bList = new List<double>();
        DateTime _startTime;
        int _numTicks = 0;

        public InitializationViewModel(Camera c, ControlViewModel cvm)
        {
            base.DisplayName = "InitializationViewModel";
            _camera = c;
            _controlViewModel = cvm;
            _enableReadyButton = false;

            SetReadings();

            CommandOK = new RelayCommand(param => this.Ready(param), cc => { return _enableReadyButton; });

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += new EventHandler(_dispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Start();

            _startTime = DateTime.Now;
        }

        public RelayCommand CommandOK { get; set; }

        public string Shutter
        {
            get { return _shutter.ToString() + " ms"; }
        }

        public string CameraTemp
        {
            get { return _cameraTemp.ToString() + " °C"; }
        }

        public Visibility BVisibility
        {
            get 
            {
                return Properties.Settings.Default.WBInitialize ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public string WBRedLabel
        {
            get { return Properties.Settings.Default.WBInitialize ? "R: " : "W.B. (red): "; }
        }
        public string WBBlueLabel
        {
            get { return Properties.Settings.Default.WBInitialize ? "G: " : "W.B. (blue): "; }
        }

        public string WBRed
        {
            get { return _wbRed.ToString("N3"); }
        }

        public string WBBlue
        {
            get { return _wbBlue.ToString("N3"); }
        }

        public string BValue
        {
            get { return _bValue.ToString("N3"); }
        }

        void SetReadings()
        {
            Dictionary<CAMERA_PROPERTY, double> readings = new Dictionary<CAMERA_PROPERTY, double>();

            readings.Add(CAMERA_PROPERTY.Shutter, 0);
            readings.Add(CAMERA_PROPERTY.Temperature, 0);
            readings.Add(CAMERA_PROPERTY.WhiteBalanceRed, 0);
            readings.Add(CAMERA_PROPERTY.WhiteBalanceBlue, 0);

            _camera.GetInitializationPropertyValues(readings);

            _shutter = readings[CAMERA_PROPERTY.Shutter];
            _cameraTemp = readings[CAMERA_PROPERTY.Temperature];
            _wbRed = readings[CAMERA_PROPERTY.WhiteBalanceRed];
            _wbBlue = readings[CAMERA_PROPERTY.WhiteBalanceBlue];

            //BitmapSource bmpSource = _controlViewModel.CameraImage;
            //if (Properties.Settings.Default.WBInitialize)
            //{
            //    _bValue = 0;
            //    _wbRed = 0;
            //    _wbBlue = 0;
            //    if (bmpSource != null)
            //    {
            //        System.Drawing.Bitmap img_Bmp = _controlViewModel.GetBitmap(bmpSource);
            //        ImageProcessing.calcBGR_wholeimage(ref img_Bmp, ref _bValue, ref _wbBlue, ref _wbRed);
            //        img_Bmp.Dispose();
            //    }
            //}

            //OnPropertyChanged("Shutter");
            OnPropertyChanged("CameraTemp");
            //OnPropertyChanged("WBRed");
            //OnPropertyChanged("WBBlue");
            //OnPropertyChanged("BValue");


            ////////////////////////////////////////////
            //Hiroshi add for light source stabilization
            ////////////////////////////////////////////
            
            //if (bmpSource != null)
            //{
            //    System.Drawing.Bitmap img_Bmp = _controlViewModel.GetBitmap(bmpSource);
            //    ImageProcessing.calcLab_ROI(ref img_Bmp, ref _L, ref _a, ref _b);
            //    img_Bmp.Dispose();
            //    _LList.Add(_L);
            //    _aList.Add(_a);
            //    _bList.Add(_b);
            //}

            //_shutterList.Add(_shutter);
            _cameraTempList.Add(_cameraTemp);
            //_wbRedList.Add((int)_wbRed);
            //_wbBlueList.Add((int)_wbBlue);
            //_LList.Add(_L);
            //_aList.Add(_a);
            //_bList.Add(_b);

            if (_cameraTempList.Count > 60)
            {
                //_shutterList.RemoveAt(0);
                _cameraTempList.RemoveAt(0);
                //_wbRedList.RemoveAt(0);
                //_wbBlueList.RemoveAt(0);
                //_LList.RemoveAt(0);
                //_aList.RemoveAt(0);
                //_bList.RemoveAt(0);

                //if (Math.Abs(_shutterList.Max() - _shutterList.Min()) < Properties.Settings.Default.ShutterTimeDiff
                //    && Math.Abs(_shutter - Properties.Settings.Default.ShutterTime) < _shutterTarget
                //    && Math.Abs(_cameraTempList.Max() - _cameraTempList.Min()) < Properties.Settings.Default.CameraTempDiff
                //    && _wbRedList.Min() == _wbRedList.Max()
                //    && _wbBlueList.Min() == _wbBlueList.Max()
                //    && Math.Abs(_aList.Max() - _aList.Min()) < Properties.Settings.Default.ADiff
                //    && Math.Abs(_bList.Max() - _bList.Min()) < Properties.Settings.Default.BDiff)
                if (Math.Abs(_cameraTempList.Max() - _cameraTempList.Min()) 
                    < Properties.Settings.Default.CameraTempDiff)
                {
                    _enableReadyButton = true;
                }
                else
                {
                    _enableReadyButton = false;
                }
            }
        
        }

        private void _dispatcherTimer_Tick(object sender, EventArgs e)
        {

            DateTime now = DateTime.Now;
            TimeSpan timeSpan = now.Subtract(_startTime);
            string timeText = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            string shutterText = "";

            //if (timeSpan.TotalMinutes > 10)
            //{
            //    if (_shutter - Properties.Settings.Default.ShutterTime > _shutterTarget)
            //    {
            //        shutterText = "\nIncrease the voltage of power suply";
            //    }
            //    if (_shutter - Properties.Settings.Default.ShutterTime < - _shutterTarget)
            //    {
            //        shutterText = "\nDecrease the voltage of power suply";
            //    }
            //}


            if (_enableReadyButton)
            {
                if (_cameraTempList.Count > Properties.Settings.Default.Time - 1)
                {
                    //if (Math.Abs(_shutterList.Max() - _shutterList.Min()) >= Properties.Settings.Default.ShutterTimeDiff)    
                    //timeText += "\nshutter=" + _shutter.ToString() + ", " + _shutterList.Max().ToString() + ", " + _shutterList.Min().ToString();
                    //if (Math.Abs(_cameraTempList.Max() - _cameraTempList.Min()) >= Properties.Settings.Default.Temperature)
                    timeText += "\ntemp=" + _cameraTemp.ToString() + ", " + _cameraTempList.Max().ToString() + ", " + _cameraTempList.Min().ToString();
                    //if (Math.Abs(_aList.Max() - _aList.Min()) >= Properties.Settings.Default.ADiff)
                    //timeText += "\na=" + string.Format("{0:0.00}", _a) + ", " + string.Format("{0:0.00}", _aList.Max()) + ", " + string.Format("{0:0.00}", _aList.Min());
                    //if (Math.Abs(_bList.Max() - _bList.Min()) >= Properties.Settings.Default.BDiff)
                    //timeText += "\nb=" + string.Format("{0:0.00}", _b) + ", " + string.Format("{0:0.00}", _bList.Max()) + ", " + string.Format("{0:0.00}", _bList.Min());
                }

                timeText += "\nInitialization OK";
            }
            else
            {
                if (_cameraTempList.Count > Properties.Settings.Default.Time - 1)
                {
                    //if (Math.Abs(_shutterList.Max() - _shutterList.Min()) >= Properties.Settings.Default.ShutterTimeDiff)    
                    //timeText += "\nshutter=" + _shutter.ToString() + ", " + _shutterList.Max().ToString() + ", " + _shutterList.Min().ToString();
                    //if (Math.Abs(_cameraTempList.Max() - _cameraTempList.Min()) >= Properties.Settings.Default.Temperature)
                    timeText += "\ntemp=" + _cameraTemp.ToString() + ", " + _cameraTempList.Max().ToString() + ", " + _cameraTempList.Min().ToString();
                    //if (Math.Abs(_aList.Max() - _aList.Min()) >= Properties.Settings.Default.ADiff)
                    //timeText += "\na=" + string.Format("{0:0.00}", _a) + ", " + string.Format("{0:0.00}", _aList.Max()) + ", " + string.Format("{0:0.00}", _aList.Min());
                    //if (Math.Abs(_bList.Max() - _bList.Min()) >= Properties.Settings.Default.BDiff)
                    //timeText += "\nb=" + string.Format("{0:0.00}", _b) + ", " + string.Format("{0:0.00}", _bList.Max()) + ", " + string.Format("{0:0.00}", _bList.Min());
                }
            }

#if !DEBUG
            Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Time: " + timeText, true)));
#else
            if ((_numTicks++ % 10) == 0)
                Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Time: " + timeText)));
#endif

            SetReadings();
            // Forcing the CommandManager to raise the RequerySuggested event
            Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
        }

        void Ready(object param)
        {
            _dispatcherTimer.Stop();
            ((Window)param).Close();
        }
    }
}
