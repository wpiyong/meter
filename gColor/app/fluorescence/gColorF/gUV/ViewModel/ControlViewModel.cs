using gUV.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClassOpenCV;
using System.Data;


namespace gUV.ViewModel
{
    public class NoSizeDecorator : System.Windows.Controls.Decorator
    {
        protected override Size MeasureOverride(Size constraint)
        {
            // Ask for no space
            Child.Measure(new Size(0, 0));
            return new Size(0, 0);
        }
    }

    public static class SizeObserver
    {
        public static readonly DependencyProperty ObserveProperty = DependencyProperty.RegisterAttached(
            "Observe",
            typeof(bool),
            typeof(SizeObserver),
            new FrameworkPropertyMetadata(OnObserveChanged));

        public static readonly DependencyProperty ObservedWidthProperty = DependencyProperty.RegisterAttached(
            "ObservedWidth",
            typeof(double),
            typeof(SizeObserver));

        public static readonly DependencyProperty ObservedHeightProperty = DependencyProperty.RegisterAttached(
            "ObservedHeight",
            typeof(double),
            typeof(SizeObserver));

        public static bool GetObserve(FrameworkElement frameworkElement)
        {
            return (bool)frameworkElement.GetValue(ObserveProperty);
        }

        public static void SetObserve(FrameworkElement frameworkElement, bool observe)
        {
            frameworkElement.SetValue(ObserveProperty, observe);
        }

        public static double GetObservedWidth(FrameworkElement frameworkElement)
        {
            return (double)frameworkElement.GetValue(ObservedWidthProperty);
        }

        public static void SetObservedWidth(FrameworkElement frameworkElement, double observedWidth)
        {
            frameworkElement.SetValue(ObservedWidthProperty, observedWidth);
        }

        public static double GetObservedHeight(FrameworkElement frameworkElement)
        {
            return (double)frameworkElement.GetValue(ObservedHeightProperty);
        }

        public static void SetObservedHeight(FrameworkElement frameworkElement, double observedHeight)
        {
            frameworkElement.SetValue(ObservedHeightProperty, observedHeight);
        }

        private static void OnObserveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = (FrameworkElement)dependencyObject;

            if ((bool)e.NewValue)
            {
                frameworkElement.SizeChanged += OnFrameworkElementSizeChanged;
                UpdateObservedSizesForFrameworkElement(frameworkElement);
            }
            else
            {
                frameworkElement.SizeChanged -= OnFrameworkElementSizeChanged;
            }
        }

        private static void OnFrameworkElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateObservedSizesForFrameworkElement((FrameworkElement)sender);
        }

        private static void UpdateObservedSizesForFrameworkElement(FrameworkElement frameworkElement)
        {
            // WPF 4.0 onwards
            frameworkElement.SetCurrentValue(ObservedWidthProperty, frameworkElement.ActualWidth);
            frameworkElement.SetCurrentValue(ObservedHeightProperty, frameworkElement.ActualHeight);

            // WPF 3.5 and prior
            ////SetObservedWidth(frameworkElement, frameworkElement.ActualWidth);
            ////SetObservedHeight(frameworkElement, frameworkElement.ActualHeight);
        }
    }

    public enum CameraModel
    {
        PointGrey = 0,
        Nikon
    }

    class ControlViewModel : ViewModelBase
    {
        enum Mode
        {
            Regular = 0,
            ColorCalculation = 1,
            BackgroundCalculation,
            CalibrationAutoSetting,
            CalibrationCheck,
            CalibrationManualSetting,
            CalibrationAdjustWhiteBalance,
            CalibrationCollectBackground
        };

        int _calibrationStablizeCount = 0;
        int _whitebalanceStabilizeCount = 0;
        int _cameraModel;

        bool _connected = false;
        bool _firstCalibration = true;

        BitmapSource _cameraImage;
        double _imageContainerWidth, _imageContainerHeight;

        BackgroundWorker _bwImageRetriever;
        bool _keepRetrievingImages = false;

        int _rotationAngle;
        volatile int _numberOfRotations;
        volatile int _continuousImageCount;
        volatile bool _takeSingleMeasurement = false;
        volatile bool _takeContinuousMeasurement = false;
        volatile bool _takingBackgroundImages = false;
        volatile bool _takeImage = false;
        volatile bool _fluorescenceMeasurement = false;
        volatile bool _usedLowGain = false;

        volatile bool _ready = true;

        volatile bool _motorBusy = false;
        volatile Mode _mode = Mode.Regular;

        volatile bool _buttonPressed = false;

        int _lCalibrationRetryCount = 0;
        readonly int MAX_L_CAL_RETRY_COUNT = 10;
        uint _savedFlSetCurrent = 0;

        Dictionary<double, BitmapSource> _measurementImages;
        Dictionary<double, BitmapSource> _dustImages;
        Dictionary<double, BitmapSource> _fluorescenceMeasurementImages = new Dictionary<double, BitmapSource>();

        BitmapSource _backgroundImage = null;
        List<System.Drawing.Bitmap> _backgroundImageList = new List<System.Drawing.Bitmap>();
        List<BitmapSource> _backgroundImageSrcList = new List<BitmapSource>();

        Camera _camera = null;

        ManualResetEvent _mainBgWorkerDoneEvent = new ManualResetEvent(true);

        //Hiroshi Add for backround monitoring
        double _cameraTempBG;
        double _cameraTempMeasurement;
        double _shutterTimeBG;
        double _blue;
        double _red;
        bool _firstCalib = true;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        bool _tempChange = false;

        Stone _stone = null;
        List<Stone> _stoneMeasurements = null;

        int _uvIntensity;

        public LogEntryViewModel LogEntryVM { get { return App.LogEntry; } }

        public System.Windows.Visibility IsAdmin
        {
            get
            {
                return GlobalVariables.IsAdmin ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        const string _lockOutFileName = "adminfile";
        DateTime prevTime = DateTime.Now;
        bool updateFrameRate = true;

        public void UpdateIsAdminView()
        {
            OnPropertyChanged("IsAdmin");
            if (!GlobalVariables.IsAdmin) LogWindowWidth = new GridLength(0);
            OnPropertyChanged("GridSplitterWidth");

            if (GlobalVariables.IsAdmin && File.Exists(_lockOutFileName))
            {
                File.Delete(_lockOutFileName);
            }
        }


        bool IsHashCorrect
        {
            get
            {
#if DEBUG
                return true;
#else
                string hash = StringCipher.Decrypt(Properties.Settings.Default.BoundaryHash);
                return ImageProcessing.check_Hash_Boundary(hash, 2);
#endif
            }

        }

        GridLength _logWindowWidth;
        public GridLength LogWindowWidth
        {
            get
            {
                return _logWindowWidth;
            }
            set
            {
                _logWindowWidth = value;
                OnPropertyChanged("LogWindowWidth");
            }
        }

        public GridLength GridSplitterWidth
        {
            get
            {
#if DEBUG
                double width = 1;
                if (!GlobalVariables.IsAdmin) width = 0;
                return new GridLength(width);
#else
                double width = 0;
                if (GlobalVariables.IsAdmin) width = 1;
                return new GridLength(width);
#endif
            }
        }

        public int CameraModelProperty
        {
            get
            {
                return _cameraModel;
            }
            set
            {
                _cameraModel = value;
                OnPropertyChanged("CameraModelProperty");
            }
        }

        public Camera ConnectedCamera
        {
            get
            {
                return _camera;
            }
        }

        bool MeasurementActive
        {
            get { return (_takeSingleMeasurement || _takeContinuousMeasurement || _takingBackgroundImages); }
            set
            {
                _takeSingleMeasurement = false;
                _takeContinuousMeasurement = false;
                _takeImage = false;
                _fluorescenceMeasurement = false;
            }
        }

        string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        string _spectrumUserName;
        readonly string SPECTRUM_LOG_OUT = "NOT LOGGED IN";
        public string SpectrumUsername
        {
            get
            {
                return _spectrumUserName;
            }
            set
            {
                _spectrumUserName = value;
                OnPropertyChanged("SpectrumUsername");
                OnPropertyChanged("SpectrumAuthToolTip");
            }
        }

        public string SpectrumAuthToolTip
        {
            get
            {
                if (GlobalVariables.spectrumSettings.RootUrl.Length > 0)
                {
                    if (SpectrumUsername == SPECTRUM_LOG_OUT)
                        return "Click to log in";

                    return "Click to log out";
                }
                return "Not connected";
            }
        }

        public bool IsReady
        {
            get
            {
                return _ready;
            }
            set
            {
                if (value != _ready)
                {
                    _ready = value;
                    OnPropertyChanged("IsReady");
                }
                Busy = !_ready;
                Application.Current.Dispatcher.BeginInvoke(new Action(CommandManager.InvalidateRequerySuggested));
            }
        }

        public bool MotorBusy
        {
            get
            {
                return _motorBusy;
            }
            set
            {
                _motorBusy = value;
                OnPropertyChanged("MotorBusy");
                Application.Current.Dispatcher.BeginInvoke(new Action(CommandManager.InvalidateRequerySuggested));
            }
        }

        bool _busy;
        public bool Busy
        {
            get
            {
                return _busy;
            }
            set
            {
                _busy = value;
                OnPropertyChanged("Busy");
            }
        }

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

        BitmapSource _cachedCameraImage;
        object _cachedCameraImageLock = new object();
        BitmapSource CachedCameraImage
        {
            get
            {
                return _cachedCameraImage;
            }
            set
            {
                _cachedCameraImage = value;
            }
        }

        public Thickness CrossHairHorizontalOffset
        {
            get
            {
                double x = Properties.Settings.Default.CrossHairHorizontalOffsetPercent;
                if (x > 100) x = 100;
                if (x < -100) x = -100;
                x = (ImageContainerWidth * x / 100.0);
                return new Thickness(x, 0, 0, 0);
            }
        }

        public Thickness CrossHairVerticalOffset
        {
            get
            {
                double y = Properties.Settings.Default.CrossHairVerticalOffsetPercent;
                if (y > 100) y = 100;
                if (y < -100) y = -100;
                y = (ImageContainerHeight * y / 100.0);
                return new Thickness(0, y, 0, 0);
            }
        }

        public double CrossHairHorizontalPixelOffset
        {
            get
            {
                if (_camera == null) return 0;

                double x = Properties.Settings.Default.CrossHairHorizontalOffsetPercent;
                if (x > 100) x = 100;
                if (x < -100) x = -100;
                x = (x + 100) / 200.0;
                double imageWidth = _camera.CropImageWidth == 0 ? _camera.ImageWidth : _camera.CropImageWidth;

                return imageWidth * x;
            }
        }

        public double CrossHairVerticalPixelOffset
        {
            get
            {
                if (_camera == null) return 0;

                double y = Properties.Settings.Default.CrossHairVerticalOffsetPercent;
                if (y > 100) y = 100;
                if (y < -100) y = -100;
                y = (y + 100) / 200.0;
                double imageHeight = _camera.CropImageHeight == 0 ? _camera.ImageHeight : _camera.CropImageHeight;

                return imageHeight * y;
            }
        }


        public System.Windows.Media.Brush CrossHairBrush
        {
            get
            {
                int index = Properties.Settings.Default.CrossHairBrush;
                switch (index)
                {
                    case 1:
                        return System.Windows.Media.Brushes.White;
                    case 2:
                        return System.Windows.Media.Brushes.Orange;
                }
                return System.Windows.Media.Brushes.Black;
            }
        }

        public void RefreshCrossHair()
        {
            OnPropertyChanged("CrossHairHorizontalOffset");
            OnPropertyChanged("CrossHairVerticalOffset");
            OnPropertyChanged("CrossHairBrush");
        }

        public double ImageContainerWidth
        {
            get
            {
                return _imageContainerWidth;
            }
            set
            {
                _imageContainerWidth = value;
                OnPropertyChanged("ImageContainerWidth");
                OnPropertyChanged("CrossHairHorizontalOffset");
                OnPropertyChanged("CrossHairBrush");
            }
        }

        public double ImageContainerHeight
        {
            get
            {
                return _imageContainerHeight;
            }
            set
            {
                _imageContainerHeight = value;
                OnPropertyChanged("ImageContainerHeight");
                OnPropertyChanged("CrossHairVerticalOffset");
                OnPropertyChanged("CrossHairBrush");
            }
        }

        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged("Connected");
                OnPropertyChanged("NotConnected");
            }
        }

        public bool NotConnected
        {
            get { return !_connected; }
        }

        string _measureIconText, _measureIconImageSource, _measureIconToolTip;
        int _dailyMonitorTargetIndex = 0;
        public string MeasureIconText
        {
            get
            {
                return _measureIconText;
            }
            set
            {
                _measureIconText = value;
                OnPropertyChanged("MeasureIconText");
            }
        }
        public string MeasureIconImageSource
        {
            get
            {
                return _measureIconImageSource;
            }
            set
            {
                _measureIconImageSource = value;
                OnPropertyChanged("MeasureIconImageSource");
            }
        }
        public string MeasureIconToolTip
        {
            get
            {
                return _measureIconToolTip;
            }
            set
            {
                _measureIconToolTip = value;
                OnPropertyChanged("MeasureIconToolTip");
            }
        }

        bool _mainLight, _fluorescenceLight;
        public bool MainLight
        {
            get { return _mainLight; }
            set
            {
                bool res = false;
                if (Hemisphere.ArduinoVersion == 1)
                    res = LightControl.SetMainLightCurrent(value ? GlobalVariables.fluorescenceSettings.MainLightSetCurrent : 0);
                else if (Hemisphere.ArduinoVersion == 2)
                    res = Hemisphere.EnableDisableRingLight(value);

                if (res)
                {
                    _mainLight = value;
                    OnPropertyChanged("MainLight");
                }
            }
        }
        public bool FluorescenceLight
        {
            get { return _fluorescenceLight; }
            set
            {
                if (LightControl.SetFluorescenceLightCurrent(value ? GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent : 0))
                {
                    _fluorescenceLight = value;
                    OnPropertyChanged("FluorescenceLight");
                }
            }
        }
        bool _fluorescenceShutter;
        public bool FluorescenceShutter
        {
            get { return _fluorescenceShutter; }
            set
            {
                if (Hemisphere.UVcommand(value))
                {
                    _fluorescenceShutter = value;
                    OnPropertyChanged("FluorescenceShutter");
                }
            }
        }

        bool _dustDetectOn;
        public bool DustDetectOn
        {
            get { return _dustDetectOn; }
            set
            {
                if (value != _dustDetectOn)
                {
                    _dustDetectOn = value;
                    OnPropertyChanged("DustDetectOn");
                }
            }
        }
        bool _maskOn;
        public bool MaskOn
        {
            get { return _maskOn; }
            set
            {
                if (value != _maskOn)
                {
                    _maskOn = value;
                    OnPropertyChanged("MaskOn");
                }
            }
        }

        string _frameRate;
        public string FrameRate
        {
            get
            {
                return _frameRate;
            }
            set
            {
                if (value != _frameRate)
                {
                    _frameRate = value;
                    OnPropertyChanged("FrameRate");
                }
            }
        }

        public ControlViewModel()
        {
            base.DisplayName = "ControlViewModel";

            SplashScreen ss = new SplashScreen("splash_screen.jpg");
            ss.Show(false);

            CommandConnect = new RelayCommand(param => this.Connect(),
                cc =>
                {
                    return (Hemisphere.ArduinoConnected && !MeasurementActive && !MotorBusy && (_mode == Mode.Regular));
                });


            CommandCalibrate = new RelayCommand(param => this.Calibrate(),
                cc =>
                {
                    return (_connected && !MeasurementActive && !MotorBusy && (_mode == Mode.Regular)
                                && (!Properties.Settings.Default.AutoOpenClose || Hemisphere.IsOpen));
                });
            CommandMeasure = new RelayCommand(param => this.Measure(),
                cc =>
                {
                    return (_connected && !MeasurementActive && !MotorBusy && (_backgroundImage != null)
                                 && (!Properties.Settings.Default.AutoOpenClose || Hemisphere.IsOpen));
                });
            CommandTestMeasure = new RelayCommand(param => this.StartTestMeasure(),
                cc =>
                {
                    return (_connected && !MeasurementActive && !MotorBusy && (_backgroundImage != null)
                                 && (!Properties.Settings.Default.AutoOpenClose || Hemisphere.IsOpen));
                });
            CommandCancel = new RelayCommand(param => this.Cancel(),
                cc => { return (_connected && MeasurementActive); });

            CommandColor = new RelayCommand(param => this.Color(),
                cc => { return (_connected && !MeasurementActive && (_mode == Mode.Regular)); });
            CommandBackground = new RelayCommand(param => this.Background(),
                cc => { return (_connected && !MeasurementActive && (_mode == Mode.Regular)); });

            CommandOpenHemisphere = new RelayCommand(param => this.OpenHemisphereCommand(),
                cc => { return Hemisphere.HemisphereMotorConnected; });
            CommandCloseHemisphere = new RelayCommand(param => this.CloseHemisphereCommand(false),
                cc => { return Hemisphere.HemisphereMotorConnected; });

            CommandContinuousCCW = new RelayCommand(param => this.ContinuousCCW(),
                cc => { return !MeasurementActive && !MotorBusy; });
            CommandStepCCW = new RelayCommand(param => this.StepCCW(),
                cc => { return !MeasurementActive && !MotorBusy; });
            CommandStop = new RelayCommand(param => this.Stop(),
                cc => { return !MeasurementActive && MotorBusy; });
            CommandStepCW = new RelayCommand(param => this.StepCW(),
                cc => { return !MeasurementActive && !MotorBusy; });
            CommandContinuousCW = new RelayCommand(param => this.ContinuousCW(),
                cc => { return !MeasurementActive && !MotorBusy; });

            CommandSave = new RelayCommand(param => this.SaveDialog(),
                cc => { return _connected && !MeasurementActive && !ST5.DriveBusy; });

            CommandSpectrumAuth = new RelayCommand(param => this.SpectrumAuth(),
                cc => { return (!MeasurementActive && (_mode == Mode.Regular)); });

            MeasureIconImageSource = "..\\Images\\measurement.png";
            MeasureIconText = "";
            MeasureIconToolTip = "Measure";
            InitializeReferenceStoneCalibrationTable();

            DustDetectOn = false;
            MaskOn = false;

            SpectrumUsername = SPECTRUM_LOG_OUT;

#if DEBUG
            LogWindowWidth = new GridLength(1, GridUnitType.Star);
#else
            LogWindowWidth = new GridLength(0);
#endif

            CameraModelProperty = (int)CameraModel.PointGrey;

            var bwWebServiceEndpoints = new BackgroundWorker();
            bwWebServiceEndpoints.DoWork += new DoWorkEventHandler(MapWebServiceEndpoints_doWork);
            bwWebServiceEndpoints.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MapServiceEndpoints_WorkCompleted);
            bwWebServiceEndpoints.RunWorkerAsync();

            _bwImageRetriever = new BackgroundWorker();
            _bwImageRetriever.WorkerReportsProgress = true;
            _bwImageRetriever.DoWork += new DoWorkEventHandler(_bwImageRetriever_DoWork);
            _bwImageRetriever.ProgressChanged += new ProgressChangedEventHandler(_bwImageRetriever_ProgressChanged);

            BackgroundWorker bwConnectSerialPorts = new BackgroundWorker();
            bwConnectSerialPorts.DoWork += new DoWorkEventHandler(ConnectToSerialPorts);
            bwConnectSerialPorts.RunWorkerAsync();

            ST5.DriveReady += new EventHandler(ST5_BusyStateEvent);
            ST5.DriveNotReady += new EventHandler(ST5_BusyStateEvent);

            //Load OpenCvDlls so that they are in cache////////////////////
            ImageProcessingUtility.LoadMaskSettings();
            if (ImageProcessingUtility.DustDetectOn)
            {
                int dummyCount = -1;
                double area = -1;
                System.Drawing.Bitmap dummyBmp = new System.Drawing.Bitmap(1, 1);
                ImageProcessingUtility.ObjectDetector(dummyBmp, out dummyCount, out area);
                dummyBmp.Dispose();
            }
            ////////////////////////////////////////////////////////////////

            ss.Close(TimeSpan.FromSeconds(0.1));
        }

        public RelayCommand CommandConnect { get; set; }
        public RelayCommand CommandCalibrate { get; set; }
        public RelayCommand CommandMeasure { get; set; }
        public RelayCommand CommandTestMeasure { get; set; }
        public RelayCommand CommandCancel { get; set; }

        public RelayCommand CommandContinuousCCW { get; set; }
        public RelayCommand CommandStepCCW { get; set; }
        public RelayCommand CommandStop { get; set; }
        public RelayCommand CommandStepCW { get; set; }
        public RelayCommand CommandContinuousCW { get; set; }

        public RelayCommand CommandOpenHemisphere { get; set; }
        public RelayCommand CommandCloseHemisphere { get; set; }

        public RelayCommand CommandColor { get; set; }
        public RelayCommand CommandBackground { get; set; }

        public RelayCommand CommandSave { get; set; }

        public RelayCommand CommandSpectrumAuth { get; set; }


        void OpenHemisphereCommand()
        {
            BackgroundWorker bwOpen = new BackgroundWorker();
            bwOpen.DoWork += new DoWorkEventHandler(Hemisphere.Open);
            bwOpen.RunWorkerAsync();
            MainLight = false;
        }

        void CloseHemisphereCommand(bool turnOnMainLight = true)
        {
            BackgroundWorker bwClose = new BackgroundWorker();
            bwClose.DoWork += new DoWorkEventHandler(Hemisphere.Close);
            bwClose.RunWorkerAsync();
            MainLight = turnOnMainLight;
        }

        private void OnImageChanged(object sender, ImageEventArgs e)
        {
            CameraImage = e.image;
            DateTime currTime = DateTime.Now;
            double fRate;

            TimeSpan ts = currTime - prevTime;
            prevTime = currTime;
            fRate = 1000.0 / (int)ts.TotalMilliseconds;
            fRate = Math.Truncate(fRate * 100) / 100;

            if (updateFrameRate)
            {
                FrameRate = fRate.ToString() + "/" + _camera.CaptRate.ToString() + "/" + Properties.Settings.Default.FrameRate;
            }
            updateFrameRate = !updateFrameRate;

        }

        public void EditCameraSettings()
        {
            _camera.EditCameraSettings();
        }

        public void InitializeReferenceStoneCalibrationTable()
        {
            

            //force recalibration
            _dailyMonitorTargetIndex = 0;
            _backgroundImage = null;
            _backgroundImageList = new List<System.Drawing.Bitmap>();
            _backgroundImageSrcList.Clear();
        }

        

        void Connect()
        {
            if (!IsHashCorrect)
            {
                MessageBox.Show("Boundary table hash mismatch", "Contact Administrator");
                return;
            }

            if ((Properties.Settings.Default.AutoOpenClose && !Hemisphere.HemisphereMotorConnected) ||
                !Hemisphere.ArduinoConnected)
            {
                MessageBox.Show("Please wait for all devices to be connected", "Devices not connected");
                return;
            }

            if (Properties.Settings.Default.AutoOpenClose)
                CloseHemisphereCommand();

            //force recalibration
            _backgroundImage = null;
            _backgroundImageList = new List<System.Drawing.Bitmap>();

            if (_camera != null)
            {
                StopCamera();
                _camera = null;
            }

            Status = "Connecting...";

            switch (CameraModelProperty)
            {
                case (int)CameraModel.PointGrey:
                    _camera = new PtGreyCamera();
                    break;
                default:
                    MessageBox.Show("Bad camera type", "Please contact administrator");
                    return;
            }

            IsReady = false;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(ConnectCamera);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConnectCameraCompleted);
            bw.RunWorkerAsync();

        }

        void ConnectCamera(object sender, DoWorkEventArgs e)
        {
            e.Result = _camera.Connect();
        }

        void ConnectCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsReady = true;

            if ((bool)e.Result == true)
            {
                _camera.ImageChanged += OnImageChanged;
                _mainBgWorkerDoneEvent.Reset();
                _keepRetrievingImages = true;
                _camera.StartCapture(_bwImageRetriever);
                App.LogEntry.AddEntry("Started camera capture");
                Status = "Initializing";

                var initWindow = new View.Initialization();
                var initVm = new InitializationViewModel(_camera, this);
                initWindow.DataContext = initVm;
                initWindow.ShowDialog();

                FluorescenceShutter = false;
                FluorescenceLight = true;
                MainLight = false;

                if (Properties.Settings.Default.AutoOpenClose)
                    OpenHemisphereCommand();
                Connected = true;
                Status = "Connected";
            }
            else
            {
                App.LogEntry.AddEntry("Failed to Connect to Camera");
                Status = "";
            }

        }

        private void _bwImageRetriever_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (_keepRetrievingImages)
                {
                    try
                    {
                        BackgroundWorker worker = (BackgroundWorker)sender;

                        BitmapSource image = _camera.GetImage(e);
                        BitmapSource copy = image.Clone();
                        copy.Freeze();

                        if (IsReady)
                        {


                            if (_mode == Mode.Regular)
                            {
                                if (_takeSingleMeasurement)
                                {
                                    if (_takeImage)
                                    {
                                        _takeImage = false;

                                        //take image after motor has stopped
                                        image = _camera.GetImage(e);
                                        copy = image.Clone();
                                        copy.Freeze();

                                        double angle = _rotationAngle * _numberOfRotations;
                                        if (angle == 360)
                                            angle = 0;

                                        if (!_fluorescenceMeasurement)
                                            _measurementImages.Add(angle, copy);
                                        else
                                            _fluorescenceMeasurementImages.Add(angle, copy);

                                        if (Properties.Settings.Default.CalculateColorAtStep)
                                        {
                                            System.Drawing.Bitmap img_Bmp_diamond = GetBitmap(image);

                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                App.LogEntry.AddEntry("Analyzing Sample Image at angle " + angle)));

                                            List<System.Drawing.Bitmap> stepImage = new List<System.Drawing.Bitmap>();
                                            stepImage.Add(img_Bmp_diamond);
                                            double L = 0, C = 0, H = 0;
                                            string stepColor = ImageProcessing.GetColor_test2(ref stepImage, ref _backgroundImageList,
                                                ref L, ref C, ref H);
                                            foreach (var b in stepImage)
                                                b.Dispose();

                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                App.LogEntry.AddEntry("Color = " + stepColor + ",L = " + L +
                                                    ",C = " + C + ",H = " + H)));

                                        }

                                        if ((!_fluorescenceMeasurement &&
                                                (_numberOfRotations < Properties.Settings.Default.NumberOfSteps)) ||
                                                (_numberOfRotations < Properties.Settings.Default.NumberOfSteps))
                                        {
                                            ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                                        }
                                        else if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure && !_fluorescenceMeasurement)
                                        {
                                            MainLight = false;
                                            FluorescenceShutter = true;

                                            if (!FluorescenceLight || MainLight)
                                                throw new ApplicationException("Failed to set light");

                                            _camera.InitFluorescenceSettings(0);
                                            Thread.Sleep(500);
                                            _usedLowGain = false;

                                            _uvIntensity = Hemisphere.GetUVIntensity();
                                            if (_uvIntensity < GlobalVariables.fluorescenceSettings.UVWarningThreshold)
                                                throw new ApplicationException("UV Intensity low [" + _uvIntensity + "]");
                                            else if (_uvIntensity > GlobalVariables.fluorescenceSettings.UVWarningThresholdHigh)
                                                throw new ApplicationException("UV Intensity high [" + _uvIntensity + "]");

                                            if (Properties.Settings.Default.NumberOfSteps > 0)
                                            {
                                                _numberOfRotations = 0;
                                                _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                                                ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                                            }
                                            else
                                            {
                                                Thread.Sleep(200);//time for camera to change to fluorescence settings
                                                _continuousImageCount = 0;
                                                _takeSingleMeasurement = false;
                                                _camera.BufferFrames(true);
                                                _takeContinuousMeasurement = true;
                                                ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                                            }

                                            _fluorescenceMeasurement = true;
                                        }
                                        else
                                        {
                                            FluorescenceShutter = false;
                                            //MainLight = false;

                                            IsReady = false;
                                            Status = "Analyzing...";

                                            BackgroundWorker bw = new BackgroundWorker();
                                            bw.DoWork += new DoWorkEventHandler(SaveMeasurementImages);
                                            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveMeasurementImagesCompleted);
                                            bw.RunWorkerAsync();
                                        }
                                    }

                                }
                                else if (_takeContinuousMeasurement)
                                {
                                    if (((_continuousImageCount + 1) % 5) == 0)
                                    {
                                        if (!_fluorescenceMeasurement)
                                            _measurementImages.Add((_continuousImageCount - 4) / 5, copy);
                                        else
                                            _fluorescenceMeasurementImages.Add((_continuousImageCount - 4) / 5, copy);

                                        //if (Properties.Settings.Default.CalculateColorAtStep)
                                        //{
                                        //    System.Drawing.Bitmap img_Bmp_diamond = GetBitmap(image);

                                        //    Application.Current.Dispatcher.Invoke((Action)(() =>
                                        //        App.LogEntry.AddEntry("Analyzing Sample Image " + (_continuousImageCount - 4) / 5)));

                                        //    List<System.Drawing.Bitmap> stepImage = new List<System.Drawing.Bitmap>();
                                        //    stepImage.Add(img_Bmp_diamond);
                                        //    double L = 0, C = 0, H = 0;
                                        //    string stepColor = ImageProcessing.GetColor_test2(ref stepImage, ref _backgroundImageList,
                                        //        ref L, ref C, ref H);

                                        //    Application.Current.Dispatcher.Invoke((Action)(() =>
                                        //        App.LogEntry.AddEntry("Color = " + stepColor + ",L = " + L +
                                        //            ",C = " + C + ",H = " + H)));

                                        //}

                                        if (_continuousImageCount >= 99) //complete 0 to 99 = 100
                                        {
                                            _takeContinuousMeasurement = false;

                                            if (!GlobalVariables.fluorescenceSettings.FluorescenceMeasure || _fluorescenceMeasurement)
                                            {
                                                FluorescenceShutter = false;
                                                //MainLight = false;

                                                IsReady = false;
                                                Status = "Analyzing...";

                                                BackgroundWorker bw = new BackgroundWorker();
                                                bw.DoWork += new DoWorkEventHandler(SaveMeasurementImages);
                                                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveMeasurementImagesCompleted);
                                                bw.RunWorkerAsync();
                                            }
                                            else
                                            {
                                                MainLight = false;
                                                FluorescenceShutter = true;
                                                if (!FluorescenceLight || MainLight)
                                                    throw new ApplicationException("Failed to set light");

                                                _camera.InitFluorescenceSettings(0);
                                                Thread.Sleep(500);
                                                _usedLowGain = false;

                                                _uvIntensity = Hemisphere.GetUVIntensity();
                                                if (_uvIntensity < GlobalVariables.fluorescenceSettings.UVWarningThreshold)
                                                    throw new ApplicationException("UV Intensity low [" + _uvIntensity + "]");
                                                else if (_uvIntensity > GlobalVariables.fluorescenceSettings.UVWarningThresholdHigh)
                                                    throw new ApplicationException("UV Intensity high [" + _uvIntensity + "]");

                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Fluorescent Measurement Started")));

                                                if (Properties.Settings.Default.NumberOfSteps > 0)
                                                {
                                                    _camera.BufferFrames(false);
                                                    _takeImage = false; //will be set after first step
                                                    _takeSingleMeasurement = true;

                                                    _numberOfRotations = 0;
                                                    _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                                                    ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                                                }
                                                else
                                                {
                                                    //Thread.Sleep(200);//time for camera to change to fluorescence settings
                                                    _continuousImageCount = -1;
                                                    _takeContinuousMeasurement = true;
                                                    ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                                                }

                                                _fluorescenceMeasurement = true;
                                            }
                                        }
                                    }

                                    _continuousImageCount += 1;
                                    Console.WriteLine("image count: {0}", _continuousImageCount);
                                }
                            }
                            else
                            {
                                System.Drawing.Bitmap img_Bmp = null;
                                System.Drawing.Bitmap img_Bmp_copy = null;
                                if (_mode != Mode.CalibrationCollectBackground)
                                {
                                    img_Bmp = GetBitmap(image);
                                    img_Bmp_copy = (System.Drawing.Bitmap)img_Bmp.Clone();//GetBitmap(copy);
                                }

                                switch (_mode)
                                {
                                    case Mode.ColorCalculation:
                                        {
                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Analyzing Sample Image")));

                                            double imageWidth = 200;
                                            double imageHeight = 200;
                                            double imageLeft = (copy.Width / 2) - (imageWidth / 2);
                                            double imageTop = (copy.Height / 2) - (imageHeight / 2);

                                            _measurementImages = new Dictionary<double, BitmapSource>();
                                            _backgroundImageList = new List<System.Drawing.Bitmap>();
                                            _backgroundImageSrcList.Clear();

                                            BitmapSource croppedCenterImage = new CroppedBitmap(copy,
                                                                new System.Windows.Int32Rect((int)imageLeft, (int)imageTop,
                                                                    (int)imageWidth, (int)imageHeight));

                                            var tempCopy = croppedCenterImage.Clone();
                                            tempCopy.Freeze();

                                            _measurementImages.Add(0, tempCopy);

                                            BitmapSource croppedBottomLeftImage = new CroppedBitmap(copy,
                                                        new System.Windows.Int32Rect(0, (int)(copy.Height - imageHeight),
                                                            (int)imageWidth, (int)imageHeight));

                                            var tempCopy1 = croppedBottomLeftImage.Clone();
                                            tempCopy1.Freeze();

                                            _backgroundImageList.Add(GetBitmap(tempCopy1));

                                            IsReady = false;
                                            _mode = Mode.Regular;

                                            BackgroundWorker bw = new BackgroundWorker();
                                            bw.DoWork += new DoWorkEventHandler(SaveMeasurementImages);
                                            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveMeasurementImagesCompleted);
                                            bw.RunWorkerAsync();
                                        }
                                        break;
                                    case Mode.BackgroundCalculation:
                                        {
                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Analyzing Sample Image - calcBGR_wholeimage")));

                                            //double B = 0, G = 0, R = 0;
                                            string text = "";
                                            //ImageProcessing.calcBGR_wholeimage(ref img_Bmp, ref B, ref G, ref R);
                                            //text = "B = " + string.Format("{0:000.0}", B) + ", G = " + string.Format("{0:000.0}", G) + ", R = " + string.Format("{0:000.0}", R);

                                            string strMessage = "";
                                            List<System.Drawing.Bitmap> ImageList = new List<System.Drawing.Bitmap>();
                                            ImageList.Add(img_Bmp);
                                            if (ImageProcessing.check_NO_backgroundDust(ref ImageList, ref strMessage) == false)
                                            {
                                                text = "Check dust / scratch on stage";
                                            }
                                            else
                                            {
                                                text = "Stage condition: OK";
                                            }

                                            Application.Current.Dispatcher.Invoke((Action)(() => App.LogEntry.AddEntry(text, true)));

                                            if (Properties.Settings.Default.AutoOpenClose)
                                                OpenHemisphereCommand();

                                            _mode = Mode.Regular;
                                            IsReady = true;
                                        }
                                        break;
                                    case Mode.CalibrationAutoSetting:
                                        {
                                            if (_calibrationStablizeCount++ == 0)
                                            {
                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Calibration Mode : Initializing Calibration Settings...")));

                                                _camera.InitCalibrationSettings();//will reset GPIO pins

                                                //double check camera shutter controls
                                                FluorescenceShutter = false;
                                                MainLight = true;
                                            }
                                            else if (_calibrationStablizeCount == 3)
                                            {
                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Calibration Mode : Configuring Camera to Auto...")));

                                                _camera.ResetSettings();
                                            }
                                            else
                                            {
                                                if (_calibrationStablizeCount < 9)
                                                {
                                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                                        App.LogEntry.AddEntry("Calibration Mode : Auto Stabilizing...")));
                                                    //Thread.Sleep(1000);
                                                    Thread.Sleep(200);
                                                }
                                                else
                                                {
                                                    _calibrationStablizeCount = 0;
                                                    _mode = Mode.CalibrationManualSetting;
                                                }
                                            }
                                        }
                                        break;
                                    case Mode.CalibrationCheck:
                                        {
                                            //Get RGB background results
                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Calibration Mode: Checking Calibration...")));

                                            double B = 0, G = 0, R = 0;
                                            ImageProcessing.calcBGR_wholeimage(ref img_Bmp, ref B, ref G, ref R);

                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                App.LogEntry.AddEntry("B = " + B + ", G = " + G + ", R = " + R)));

                                            if (!GlobalVariables.fluorescenceSettings.EnableWBAdjustment ||
                                                (Math.Abs(R - G) <= (double)Properties.Settings.Default.WBConvergence &&
                                                    Math.Abs(B - G) <= (double)Properties.Settings.Default.WBConvergence))
                                            {
                                                ///////////////////////
                                                //Hiroshi add 5/23/2014
                                                ///////////////////////
                                                _camera.Finish_calibration();   //Shutter Auto off
                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                            App.LogEntry.AddEntry("Turn on Hardware Trigger!", true)));
                                                Thread.Sleep(500);
                                                ///////////////////////


                                                try
                                                {
                                                    _backgroundImage = null;
                                                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                                                    _backgroundImageSrcList.Clear();
                                                    _takingBackgroundImages = true;

                                                    if (!GlobalVariables.fluorescenceSettings.FluorescenceMeasure)
                                                    {
                                                        if (Properties.Settings.Default.NumberOfSteps > 0)
                                                        {
                                                            _numberOfRotations = 0;
                                                            _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                                                            ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                                                        }
                                                        else
                                                        {
                                                            //rotate 360 degrees and collect as many images as possible
                                                            ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                                                        }

                                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                                            App.LogEntry.AddEntry("Started collecting background images...", true)));
                                                    }

                                                    _mode = Mode.CalibrationCollectBackground;
                                                }
                                                catch (Exception /*exc*/)
                                                {
                                                    _backgroundImage = null;
                                                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                                                    _backgroundImageSrcList.Clear();
                                                    _takingBackgroundImages = false;
                                                    _calibrationStablizeCount = 0;
                                                    throw new ApplicationException("Error collecting background images, calibration failed!");
                                                }
                                            }
                                            else
                                                _mode = Mode.CalibrationManualSetting;
                                        }
                                        break;
                                    case Mode.CalibrationCollectBackground:
                                        {
                                            if (!GlobalVariables.fluorescenceSettings.FluorescenceMeasure && Properties.Settings.Default.NumberOfSteps == 0)
                                                _backgroundImageSrcList.Add(image);
                                                //_backgroundImageList.Add(img_Bmp_copy);

                                            if (!MotorBusy) //check for complete
                                            {
                                                for (int i = 0; i < _backgroundImageSrcList.Count; i++)
                                                {
                                                    System.Drawing.Bitmap bmp = GetBitmap(_backgroundImageSrcList[i]);
                                                    _backgroundImageList.Add(bmp);
                                                }
                                                _backgroundImageSrcList.Clear();
                                                if (_backgroundImageList.Count > 0)
                                                {
                                                    img_Bmp = _backgroundImageList[_backgroundImageList.Count - 1];
                                                    img_Bmp_copy = (System.Drawing.Bitmap)img_Bmp.Clone();
                                                }
                                                else
                                                {
                                                    img_Bmp = GetBitmap(image);
                                                    img_Bmp_copy = (System.Drawing.Bitmap)img_Bmp.Clone();
                                                }

                                                if (!GlobalVariables.fluorescenceSettings.FluorescenceMeasure && Properties.Settings.Default.NumberOfSteps > 0 &&
                                                        ++_numberOfRotations < Properties.Settings.Default.NumberOfSteps)
                                                {
                                                    _backgroundImageList.Add(img_Bmp_copy);
                                                    ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                                                }
                                                else
                                                {
                                                    if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure || Properties.Settings.Default.NumberOfSteps > 0)
                                                        _backgroundImageList.Add(img_Bmp_copy);

                                                    /////////////////////////////////////////////////////
                                                    // Hiroshi background check 2/20/2014
                                                    /////////////////////////////////////////////////////
                                                    string strMessage = "";
                                                    String text = "";

                                                    if (GlobalVariables.InTestMode ||
                                                        ImageProcessing.check_NO_backgroundDust(ref _backgroundImageList, ref strMessage) == true)
                                                    {
                                                        _backgroundImage = image.Clone();
                                                        _backgroundImage.Freeze();
                                                        _mode = Mode.Regular;
                                                        _takingBackgroundImages = false;


                                                        /////////////////////////////////////////////////////
                                                        //Hiroshi add for camera temp monitoring 5/07/2014
                                                        /////////////////////////////////////////////////////
                                                        _cameraTempBG = GetCameraTemp();

                                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                                        App.LogEntry.AddEntry("Camera temp=" + _cameraTempBG.ToString())));
                                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                                        App.LogEntry.AddEntry("Blue=" + GetWBBlue().ToString() + " Red=" + GetWBRed().ToString())));
                                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                                        App.LogEntry.AddEntry("BG image count=" + _backgroundImageList.Count.ToString())));

                                                        sw.Reset();
                                                        sw.Start();
                                                        _tempChange = false;

                                                        //Identify the sudden change of background

                                                        if (_firstCalib)
                                                        {
                                                            _shutterTimeBG = GetShutterTime();
                                                            _blue = GetWBBlue();
                                                            _red = GetWBRed();
                                                            _firstCalib = false;
                                                        }
                                                        else
                                                        {

                                                            //if (Math.Abs(_shutterTimeBG - GetShutterTime()) > Properties.Settings.Default.ShutterTimeDiff ||
                                                            //    Math.Abs(_blue - GetWBBlue()) > 3 ||
                                                            //    Math.Abs(_red - GetWBRed()) > 3)
                                                            //{
                                                            //    text = "[Warning: check light stability]";
                                                            //    //using (var f = File.OpenWrite(_lockOutFileName))
                                                            //    //{ }
                                                            //}

                                                            _shutterTimeBG = GetShutterTime();
                                                            _blue = GetWBBlue();
                                                            _red = GetWBRed();
                                                        }


                                                        //////////////////////////////////////////////////////

                                                        if (text.Length > 0)
                                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                                App.LogEntry.AddEntry(text, true)));
                                                        else
                                                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                                                    App.LogEntry.AddEntry("Calibration Complete!", true)));


                                                        IsReady = true;

                                                        if (Properties.Settings.Default.DailyMonitorTargetList.Length > 0 &&
                                                            _dailyMonitorTargetIndex < Properties.Settings.Default.DailyMonitorTargetList.Split(',').Length)
                                                        {
                                                            try
                                                            {
                                                                var targetStones = Properties.Settings.Default.DailyMonitorTargetList.Split(',');
                                                                MeasureIconText = CalibrationStoneTable.CalStoneTable.Rows[Convert.ToInt32(targetStones[_dailyMonitorTargetIndex])]["Target"].ToString();
                                                                MeasureIconImageSource = "..\\Images\\cal_stone.png";
                                                                MeasureIconToolTip = "Measure calibration stone";
                                                            }
                                                            catch
                                                            {
                                                                MeasureIconText = "";
                                                                MeasureIconImageSource = "..\\Images\\measurement.png";
                                                                MeasureIconToolTip = "Measure";
                                                                _dailyMonitorTargetIndex = 0;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            MeasureIconText = "";
                                                            MeasureIconImageSource = "..\\Images\\measurement.png";
                                                            MeasureIconToolTip = "Measure";
                                                        }

                                                        if (Properties.Settings.Default.AutoOpenClose)
                                                            OpenHemisphereCommand();

                                                        _firstCalibration = false;
                                                    }
                                                    else
                                                    {
                                                        _backgroundImage = null;
                                                        _backgroundImageList = new List<System.Drawing.Bitmap>();
                                                        _backgroundImageSrcList.Clear();
                                                        _takingBackgroundImages = false;
                                                        _calibrationStablizeCount = 0;
                                                        _firstCalibration = false;

                                                        throw new ApplicationException("Calibration Failed!  Dust on stage.");
                                                    }


                                                }
                                            }

                                        }
                                        break;
                                    case Mode.CalibrationManualSetting:
                                        {
                                            if (_calibrationStablizeCount++ == 0)
                                            {
                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("Calibration Mode : Configuring Camera calibration settings...")));

                                                _camera.DefaultSettings();
                                            }
                                            else
                                            {
                                                if (_calibrationStablizeCount < 50)
                                                {
                                                    _whitebalanceStabilizeCount = 0;
                                                    _mode = Mode.CalibrationAdjustWhiteBalance;
                                                }
                                                else
                                                {
                                                    _backgroundImage = null;
                                                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                                                    _backgroundImageSrcList.Clear();
                                                    _calibrationStablizeCount = 0;

                                                    throw new ApplicationException("Calibration Failed!");
                                                }
                                            }
                                        }
                                        break;
                                    case Mode.CalibrationAdjustWhiteBalance:
                                        {
                                            if (!GlobalVariables.fluorescenceSettings.EnableWBAdjustment)
                                            {
                                                _whitebalanceStabilizeCount = 0;
                                                _mode = Mode.CalibrationCheck;
                                            }
                                            else if (_whitebalanceStabilizeCount++ == 0)
                                            {
                                                //Get RGB background results
                                                double B = 0, G = 0, R = 0;
                                                ImageProcessing.calcBGR_wholeimage(ref img_Bmp, ref B, ref G, ref R);

                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                    App.LogEntry.AddEntry("B = " + B + ", G = " + G + ", R = " + R)));

                                                _camera.Calibrate(R, G, B);

                                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                            App.LogEntry.AddEntry("Delay wait for WB to take effect...")));
                                            }
                                            else
                                            {
                                                if (_whitebalanceStabilizeCount < 16)
                                                {
                                                    if (_whitebalanceStabilizeCount % 5 == 0)
                                                        Application.Current.Dispatcher.Invoke((Action)(() =>
                                                            App.LogEntry.AddEntry("Delay wait for WB to take effect...")));
                                                    Thread.Sleep(100);
                                                }
                                                else
                                                {
                                                    _whitebalanceStabilizeCount = 0;
                                                    _mode = Mode.CalibrationCheck;
                                                }
                                            }
                                        }
                                        break;

                                }

                            }


                            if (DustDetectOn)
                            {
                                System.Drawing.Bitmap src = GetBitmap(image);
                                int count = -1;
                                double area = -1;
                                System.Drawing.Bitmap dst = ImageProcessingUtility.ObjectDetector(src, out count, out area);
                                BitmapSource dst1 = image.Clone();
                                if (dst != null)
                                    dst1 = GetBitmapSource(dst).Clone();
                                dst1.Freeze();
                                lock (_cachedCameraImageLock)
                                {
                                    CachedCameraImage = dst1;
                                }
                                dst.Dispose();
                                src.Dispose();
                                worker.ReportProgress(0, dst1);
                            }
                            else if (MaskOn && ImageProcessingUtility.UseNewMask)
                            {
                                System.Drawing.Bitmap src = GetBitmap(image);
                                System.Drawing.Bitmap dst = ImageProcessingUtility.ObjectMask(src, -1, -1);
                                BitmapSource dst1 = image.Clone();
                                if (dst != null)
                                    dst1 = GetBitmapSource(dst).Clone();
                                dst1.Freeze();
                                lock (_cachedCameraImageLock)
                                {
                                    CachedCameraImage = dst1;
                                }
                                dst.Dispose();
                                src.Dispose();
                                worker.ReportProgress(0, dst1);
                            }
                            else
                            {
                                lock (_cachedCameraImageLock)
                                {
                                    CachedCameraImage = image.Clone();
                                    CachedCameraImage.Freeze();
                                }
                                worker.ReportProgress(0, image);
                            }

                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    catch (ApplicationException aex)
                    {
                        IsReady = true;
                        MeasurementActive = false;
                        _mode = Mode.Regular;
                        Status = "Connected";

                        FluorescenceShutter = false;
                        _camera.BufferFrames(false);

                        _camera.RestoreNormalSettings();
                        _camera.RestartCapture();

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Process error: " + aex.Message, true)));

                    }
                    catch (SpinnakerNET.SpinnakerException fex)
                    {
                        IsReady = true;
                        MeasurementActive = false;
                        _mode = Mode.Regular;
                        Status = "Connected";

                        FluorescenceShutter = false;
                        _camera.BufferFrames(false);

                        _camera.RestoreNormalSettings();
                        _camera.RestartCapture();

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Camera error: " + fex.Message, true)));
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Exception in background worker -> disconnect camera: " + ex.Message, true)));
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Exception in background worker: " + ex.ToString())));
                    }
                    finally
                    {
                        GC.Collect();
                    }
                }
            }
            finally
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Background worker exiting... " )));
                _mainBgWorkerDoneEvent.Set();
            }
        }

        private void _bwImageRetriever_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //CameraImage = (BitmapSource)e.UserState;
        }


        //double CheckFluourescence(BitmapSource image, BitmapSource fImage)
        //{
        //    double L = 0, C = 0, H = 0;
        //    string L_description = "", C_description = "", H_description = "", comment = "", instruction = "", version="";
        //    List<System.Drawing.Bitmap> imageList = new List<System.Drawing.Bitmap>();
        //    imageList.Add(GetBitmap(image));

        //    List<System.Drawing.Bitmap> fImageList = new List<System.Drawing.Bitmap>();
        //    fImageList.Add(GetBitmap(fImage));

        //    bool fluorescenceResult = ImageProcessing.GetPL_description(ref imageList, ref fImageList,
        //                        ref L, ref C, ref H, ref L_description,
        //                        ref C_description, ref H_description, ref comment, ref instruction, "", "", out version);

        //    foreach (var b in fImageList)
        //        b.Dispose();
        //    foreach (var b in imageList)
        //        b.Dispose();

        //    return L;
        //}

        void Calibrate()
        {
            if (File.Exists(_lockOutFileName))
            {
                MessageBox.Show("Light stability problem", "Contact Administrator");
                return;
            }

            FluorescenceShutter = false;
            MainLight = true;
            Thread.Sleep(500);

            if (Properties.Settings.Default.AutoOpenClose)
            {
                if (_buttonPressed)
                    return;
                else
                    _buttonPressed = true;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(Hemisphere.Close);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CalibrateWaitAfterFirst);
                bw.RunWorkerAsync();
            }
            else
                CalibrateContinue(null, null);
        }

        void CalibrateWaitAfterFirst(Object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if ((bool)e.Result == false)
                {
                    throw new Exception("Failed to close hemisphere");
                }
                else if (_firstCalibration)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += new DoWorkEventHandler(CalibrateSleep);
                    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CalibrateContinue);
                    bw.RunWorkerAsync();
                }
                else
                    CalibrateContinue(null, null);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry(ex.Message, true)));

                _buttonPressed = false;
            }
        }

        void CalibrateSleep(Object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(5000);
        }



        void CalibrateContinue(Object sender, RunWorkerCompletedEventArgs e)
        {
            List<System.Drawing.Bitmap> BGImageList = new List<System.Drawing.Bitmap>();
            try
            {
                /////////////////////////////////////////////////////
                //Dust monitoring
                /////////////////////////////////////////////////////
                string strMessage = "";
                System.Drawing.Bitmap img_Bmp = null;
                bool bgImageGood = false;
                for (int i = 0; i < 3; i++)
                {
                    Thread.Sleep(500);
                    lock (_cachedCameraImageLock)
                    {
                        img_Bmp = GetBitmap(CachedCameraImage);
                    }
                    BGImageList.Add(img_Bmp);


                    bgImageGood = ImageProcessing.check_NO_backgroundDust(ref BGImageList, ref strMessage);
                    if (bgImageGood)
                        break;
                    else
                    {
                        BGImageList.Clear();
                        img_Bmp.Dispose();
                    }
                }

                if (!GlobalVariables.InTestMode && bgImageGood == false)
                {
                    _backgroundImage = null;
                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                    _backgroundImageSrcList.Clear();
                    _takingBackgroundImages = false;
                    _mode = Mode.Regular;
                    _calibrationStablizeCount = 0;
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry(strMessage, true)));
                    IsReady = true;

                    if (Properties.Settings.Default.AutoOpenClose)
                        OpenHemisphereCommand();

                }
                else
                {
                    _lCalibrationRetryCount = 0;
                    _savedFlSetCurrent = GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent;
                    App.LogEntry.AddEntry("Calibration Started...", true);
                    Busy = true;
                    _calibrationStablizeCount = 0;
                    _mode = Mode.CalibrationAutoSetting;
                }
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry("Calibrate Exception : " + ex.Message);
            }
            finally
            {
                _buttonPressed = false;
                foreach (var b in BGImageList)
                    b.Dispose();
            }
        }


        ManualResetEvent testMeasureComplete = new ManualResetEvent(false);
        void StartTestMeasure()
        {
            GlobalVariables.InTestMode = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(TestMeasure_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TestMeasureComplete);
            bw.RunWorkerAsync();
        }

        void TestMeasure_DoWork(object sender, DoWorkEventArgs e)
        {
            int numLoops = 4000;
            for (int i = 1; i <= numLoops; i++)
            {
                testMeasureComplete.Reset();
                Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("TEST LOOP# : " + i + ", remaining: " + (numLoops - i) + " loops." )));
                if (_backgroundImage != null)
                    Measure();
                else
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Calibration lost...")));

                if (!testMeasureComplete.WaitOne(80000))
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("TEST LOOP# : " + i + " did not complete")));
                    break;
                }
                else
                {
                    //wait for hemisphere to fully open
                    while(!Hemisphere.IsOpen)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Waiting for hemisphere to open completely...")));
                        Thread.Sleep(500);
                    }
                }
            }
        }

        void TestMeasureComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            App.LogEntry.AddEntry("Testing complete");
            GlobalVariables.InTestMode = false;
        }

        void Measure()
        {
            if (File.Exists(_lockOutFileName))
            {
                MessageBox.Show("Light stability problem", "Contact Administrator");
                return;
            }

            _fluorescenceMeasurement = false;
            MainLight = true;
            Thread.Sleep(500);
            //var uvint = Hemisphere.GetUVIntensity();
            //if ( uvint !=0 )
            //{
            //    Application.Current.Dispatcher.Invoke((Action)(() =>
            //                App.LogEntry.AddEntry("uv intensity shoule be 0, got: " + uvint)));
            //    return;
            //}

            if (Properties.Settings.Default.AutoOpenClose)
            {
                if (_buttonPressed)
                    return;
                else
                    _buttonPressed = true;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(Hemisphere.Close);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MeasureContinue);
                bw.RunWorkerAsync();
            }
            else
                MeasureContinue(null, null);
        }

        void MeasureContinue(Object sender, RunWorkerCompletedEventArgs e)
        {
            System.Drawing.Bitmap img_Bmp = null;

            //var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (sender != null)
                {
                    if ((bool)e.Result == false)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Failed to close hemisphere", true)));
                        return;
                    }
                }

                if (!ST5.MotorConnected && !ST5.Connect())
                {
                    if (Properties.Settings.Default.AutoOpenClose)
                        OpenHemisphereCommand();
                    return;
                }

                
                if (!GlobalVariables.InTestMode)
                {
                    /////////////////////////////////////////////////////
                    //Temperature monitoring
                    /////////////////////////////////////////////////////
                    _cameraTempMeasurement = GetCameraTemp();
                    if (Math.Abs(_cameraTempMeasurement - _cameraTempBG) > 0.05) _tempChange = true;


                    //if (Math.Abs(_cameraTempMeasurement - _cameraTempBG) > 0.15)
                    if (Math.Abs(_cameraTempMeasurement - _cameraTempBG) > Convert.ToDouble(Properties.Settings.Default.Temperature))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Calibration again " + "Temp_BG=" + _cameraTempBG.ToString() + ", Temp_Measure=" + _cameraTempMeasurement.ToString(), true)));

                        _backgroundImage = null;
                        _backgroundImageList = new List<System.Drawing.Bitmap>();
                        _backgroundImageSrcList.Clear();
                        _takingBackgroundImages = false;
                        _mode = Mode.Regular;
                        _calibrationStablizeCount = 0;
                        IsReady = true;

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        return;
                    }

                    //if ((_tempChange == true && sw.ElapsedMilliseconds > 240000) || sw.ElapsedMilliseconds > 600000)
                    if (sw.ElapsedMilliseconds > Convert.ToDouble(Properties.Settings.Default.Time) * 1000)
                    {
                        sw.Stop();
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Calibration again " + "time[ms]=" + sw.ElapsedMilliseconds.ToString(), true)));

                        _backgroundImage = null;
                        _backgroundImageList = new List<System.Drawing.Bitmap>();
                        _backgroundImageSrcList.Clear();
                        _takingBackgroundImages = false;
                        _mode = Mode.Regular;
                        _calibrationStablizeCount = 0;
                        IsReady = true;

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        return;
                    }

                    //////////////////////////////////////////////////////

                    /////////////////////////////////////////////////////
                    //Diamond position check
                    /////////////////////////////////////////////////////
                    string comment = "";
                    try
                    {
                        int x, y;
                        x = (int)CrossHairHorizontalPixelOffset;
                        y = (int)CrossHairVerticalPixelOffset;
                        for (int retries = 0; retries < 3; retries++)
                        {
                            Thread.Sleep(500);
                            lock (_cachedCameraImageLock)
                            {
                                img_Bmp = GetBitmap(CachedCameraImage);
                            }

                            if (!ImageProcessing.check_diamond_position(ref img_Bmp, x, y, ref comment))
                            {
                                if (retries == 2)
                                    throw new Exception(comment);
                            }
                            else
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry(ex.Message, true)));
                        IsReady = true;

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        return;
                    }

                    /////////////////////////////////////////////////////

                    /////////////////////////////////////////////////////
                    //Dust monitoring
                    /////////////////////////////////////////////////////
                    //test object detection
                    int objectCount = -1;
                    double maskArea = -1;
                    if (ImageProcessingUtility.DustDetectOn)
                    {
                        ImageProcessingUtility.ObjectDetector(img_Bmp, out objectCount, out maskArea);
                        if (objectCount == -1)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Error processing stone - please contact adminstrator.", true)));
                            IsReady = true;

                            if (Properties.Settings.Default.AutoOpenClose)
                                OpenHemisphereCommand();

                            return;
                        }
                    }
                    bool noDust = ImageProcessing.check_NO_measurementDust(ref img_Bmp, ref comment);
                    //watch.Stop();
                    if (objectCount > 0 || !noDust)
                    {
                        bool objectDetectState = DustDetectOn;
                        DustDetectOn = true;

                        var result = MessageBox.Show("There may be dust on the stage.\n\nContinue measurement?",
                            "Multiple objects detected", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        DustDetectOn = objectDetectState;
                        if (result != MessageBoxResult.Yes)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Measurement cancelled by user", true)));
                            IsReady = true;

                            if (Properties.Settings.Default.AutoOpenClose)
                                OpenHemisphereCommand();

                            return;
                        }
                    }
                    ///////////////////////////////////////////////////////////////

                }

                //watch.Stop();
                
                /////////////////////////////////////////////////////

                _stone = null;
                _stoneMeasurements = new List<Stone>();
                if (GlobalVariables.InTestMode == false && MeasureIconText.Length == 0)
                {
                    var dialogControlNumberWindow = new gUV.View.StoneData();
                    var dialogStoneDataViewModel = new gUV.ViewModel.StoneDataViewModel();
                    dialogControlNumberWindow.DataContext = dialogStoneDataViewModel;
                    bool? dialogResult = dialogControlNumberWindow.ShowDialog();
                    if (dialogResult == true)
                        _stone = dialogStoneDataViewModel.Cassette;
                }
                if (GlobalVariables.InTestMode == true || _stone != null || MeasureIconText.Length > 0)
                {
                    ST5.ResetPosition();
                    _numberOfRotations = 0;
                    _continuousImageCount = 0;
                    _measurementImages = new Dictionary<double, BitmapSource>();
                    _fluorescenceMeasurementImages = new Dictionary<double, BitmapSource>();

                    if (MeasureIconText.Length == 0)
                    {
                        ImageProcessing.setLabAdjustment(
                            Properties.Settings.Default.LConv, Properties.Settings.Default.AConv, Properties.Settings.Default.BConv,
                            Properties.Settings.Default.Lshift, Properties.Settings.Default.Ashift, Properties.Settings.Default.Bshift);
                    }
                    else
                    {
                        ImageProcessing.setLabAdjustment(1.0, 1.0, 1.0, 0.0, 0.0, 0.0);
                    }

                    if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure)
                    {
                        FluorescenceShutter = false;
                        if (!FluorescenceLight)
                            FluorescenceLight = true;
                    }

                    if (Properties.Settings.Default.NumberOfSteps > 0)
                    {
                        _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                        ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                        App.LogEntry.AddEntry("Measurement Started");
                        _takeImage = false; //will be set after first step
                        _takeSingleMeasurement = true;
                    }
                    else
                    {
                        //rotate 360 degrees and collect 100 images controlled by motor trigger
                        Dictionary<CAMERA_PROPERTY, double> properties = new Dictionary<CAMERA_PROPERTY, double>();
                        properties.Add(CAMERA_PROPERTY.Shutter, 0);
                        _camera.GetInitializationPropertyValues(properties);
                        double maxTimePerFrame = 9.5 / Properties.Settings.Default.MotorContinuousVelocity; //95% limit
                        //double currentTimePerFrame = properties[CAMERA_PROPERTY.Shutter] / 1000 + (1000.0 / _camera.Framerate);
                        double currentTimePerFrame = (1000.0 / (float)Properties.Settings.Default.FrameRate);
                        if (currentTimePerFrame > maxTimePerFrame)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Time per frame (" + currentTimePerFrame + " ms) too long", true)));
                            IsReady = true;

                            if (Properties.Settings.Default.AutoOpenClose)
                                OpenHemisphereCommand();

                            return;
                        }
                        _camera.BufferFrames(true);
                        App.LogEntry.AddEntry("Continuous Measurement Started");
                        _takeContinuousMeasurement = true;
                        ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                    }

                    Busy = true;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Measurement not started by user", true)));
                    IsReady = true;

                    if (Properties.Settings.Default.AutoOpenClose)
                        OpenHemisphereCommand();
                }
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry("Measure Exception : " + ex.Message);
            }
            finally
            {
                _buttonPressed = false;
                if (img_Bmp != null)
                    img_Bmp.Dispose();
            }
        }

        void ST5_BusyStateEvent(object sender, EventArgs e)
        {
            MotorBusy = ST5.DriveBusy;
            if (_takeSingleMeasurement)
            {
                if (!ST5.DriveBusy)
                {
                    //completed step
                    _numberOfRotations++;
                    _takeImage = true;
                }
            }

        }



        public System.Drawing.Bitmap GetBitmap(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap tempBitmap, bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                tempBitmap = new System.Drawing.Bitmap(outStream);
            }
            bitmap = new System.Drawing.Bitmap(tempBitmap);
            tempBitmap.Dispose();
            return bitmap;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern Int32 DeleteObject(IntPtr hGDIObj);

        public BitmapSource GetBitmapSource(System.Drawing.Bitmap bmp)
        {
            IntPtr hBitmap = bmp.GetHbitmap();
            System.Windows.Media.Imaging.BitmapSource bs =
                System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  hBitmap,
                  IntPtr.Zero,
                  System.Windows.Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);
            bmp.Dispose();

            return bs;
        }

        private static System.Windows.Media.PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat sourceFormat)
        {
            switch (sourceFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return System.Windows.Media.PixelFormats.Bgr24;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return System.Windows.Media.PixelFormats.Bgra32;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return System.Windows.Media.PixelFormats.Bgr32;
            }

            return new System.Windows.Media.PixelFormat();
        }



        void Color()
        {
            if (Properties.Settings.Default.AutoOpenClose)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(Hemisphere.Close);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ColorContinue);
                bw.RunWorkerAsync();
            }
            else
                ColorContinue(null, null);

        }
        void ColorContinue(Object sender, RunWorkerCompletedEventArgs e)
        {
            _mode = Mode.ColorCalculation;
        }

        void Background()
        {
            if (Properties.Settings.Default.AutoOpenClose)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(Hemisphere.Close);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundContinue);
                bw.RunWorkerAsync();
            }
            else
                BackgroundContinue(null, null);
        }
        void BackgroundContinue(Object sender, RunWorkerCompletedEventArgs e)
        {
            _mode = Mode.BackgroundCalculation;
        }

        void Cancel()
        {
            if (MeasurementActive)
            {
                MeasurementActive = false;
                _takingBackgroundImages = false;
                _mode = Mode.Regular;
                _calibrationStablizeCount = 0;

                ST5.StopMotor();
                while (MotorBusy)
                    Thread.Sleep(10);

                //force recalibration
                _backgroundImage = null;
                _backgroundImageList = new List<System.Drawing.Bitmap>();
                _backgroundImageSrcList.Clear();
                IsReady = true;

                Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Measurement/Calibration Stopped", true)));

                if (Properties.Settings.Default.AutoOpenClose)
                    OpenHemisphereCommand();

            }

        }

        void ConnectToSerialPorts(object sender, DoWorkEventArgs e)
        {
            if (!LightControl.LightControlInit())
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Failed to connect to Light Controller", true)));
                return;
            }

            Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    ST5.Connect(3);
                }));
            MotorBusy = ST5.DriveBusy;

            int timeout = 0;
            if (Properties.Settings.Default.AutoOpenClose)
            {
                Hemisphere.StartConnectMotor();
                do
                {
                    Thread.Sleep(1000);
                    if (Hemisphere.HemisphereMotorConnected)
                        break;
                } while (timeout++ < 10);

                if (!Hemisphere.HemisphereMotorConnected)
                {
                    Hemisphere.CancelConnectMotor();
                    Thread.Sleep(500);
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Failed to connect to hemisphere", true)));
                }
            }

            timeout = 0;
            Hemisphere.StartConnectUVSensor();
            do
            {
                Thread.Sleep(1000);
                if (Hemisphere.ArduinoConnected)
                    break;
            } while (timeout++ < 7);

            
        }

        void ContinuousCCW()
        {
            if (!ST5.MotorConnected && !ST5.Connect())
            {
                return;
            }
            Hemisphere.EnableDisableTrigger(false);
            ST5.ContinuousMotion(Direction.CCW);
        }

        void StepCCW()
        {
            //if ((!ST5.MotorConnected && !ST5.Connect()) ||
            //    Properties.Settings.Default.NumberOfSteps == 0)
            //{
            //    return;
            //}

            //ST5.RotateByAngle(360 / Properties.Settings.Default.NumberOfSteps, Direction.CCW);
        }

        void Stop()
        {
            if (!ST5.MotorConnected)
            {
                return;
            }

            ST5.StopMotor();
            Hemisphere.EnableDisableTrigger(true);
        }

        void StepCW()
        {
            //if ((!ST5.MotorConnected && !ST5.Connect()) ||
            //    Properties.Settings.Default.NumberOfSteps == 0)
            //{
            //    return;
            //}

            //ST5.RotateByAngle(360 / Properties.Settings.Default.NumberOfSteps, Direction.CW);
        }

        void ContinuousCW()
        {
            if (!ST5.MotorConnected && !ST5.Connect())
            {
                return;
            }

            Hemisphere.EnableDisableTrigger(false);
            ST5.ContinuousMotion(Direction.CW);
        }

        void SaveDialog()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save current image";
            dlg.DefaultExt = "jpg";
            dlg.Filter = "Jpeg files|*.jpg|Bitmap files|*.bmp";
            if (dlg.ShowDialog() == true)
            {
                IsReady = false;
                App.LogEntry.AddEntry("Saving " + dlg.FileName);
                Status = "Saving Image...";

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(SaveImage);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveImageCompleted);
                bw.RunWorkerAsync(dlg.FileName);

            }
        }

        void SaveImage(object sender, DoWorkEventArgs e)
        {
            string filePath = (string)e.Argument;

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    string extension = Path.GetExtension(filePath);
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    if (extension.ToUpper() == ".BMP")
                        encoder = new BmpBitmapEncoder();
                    BitmapSource bs = null;
                    lock (_cachedCameraImageLock)
                    {
                        bs = CachedCameraImage;
                        bs.Freeze();   
                    }
                    
                    encoder.Frames.Add(BitmapFrame.Create(bs));
                    encoder.Save(fileStream);
                    e.Result = filePath + " saved!";
                }
            }
            catch (Exception ex)
            {
                e.Result = "Save Failed: " + ex.Message;
            }
            finally
            {

            }
        }

        void SaveImageCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsReady = true;

            App.LogEntry.AddEntry((string)e.Result);

            Status = "Connected";
        }


        void SaveMeasurementImages(object sender, DoWorkEventArgs e)
        {
            List<System.Drawing.Bitmap> imageList = new List<System.Drawing.Bitmap>();
            List<System.Drawing.Bitmap> fImageList = new List<System.Drawing.Bitmap>();

            try
            {
                _camera.BufferFrames(false);
                _camera.RestoreNormalSettings();

                string color = String.Empty;

                if (_measurementImages != null && _measurementImages.Count > 0)
                {
                    if (Properties.Settings.Default.SaveMeasuments && Properties.Settings.Default.MeasurementsFolder.Length > 0)
                    {
                        string rootDirectory = Properties.Settings.Default.MeasurementsFolder + @"\" +
                            (_stone != null ? (_stone.ControlNumber + @"\") : "")
                        + DateTime.Now.ToString("yyyy-MM-dd") + "_" + DateTime.Now.ToString("HH.mm.ss");

                        Directory.CreateDirectory(rootDirectory);

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Saving measurement images to " +
                                rootDirectory)));

                        Status = "Saving Measurement Images...";

                        foreach (KeyValuePair<double, BitmapSource> kvp in _measurementImages)
                        {
                            string filePath = rootDirectory + @"\"
                                + kvp.Key + Properties.Settings.Default.MeasurementsFileExtension;

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                BitmapEncoder encoder = new JpegBitmapEncoder();
                                if (Properties.Settings.Default.MeasurementsFileExtension.ToUpper() == ".BMP")
                                    encoder = new BmpBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(kvp.Value));
                                encoder.Save(fileStream);
                            }
                            imageList.Add(GetBitmap(kvp.Value));
                        }

                        if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure)
                        {
                            string fluorescenceDirectory = rootDirectory + @"\Fluorescence";
                            Directory.CreateDirectory(fluorescenceDirectory);

                            foreach (KeyValuePair<double, BitmapSource> kvp in _fluorescenceMeasurementImages)
                            {
                                string filePath = fluorescenceDirectory + @"\"
                                    + kvp.Key + Properties.Settings.Default.MeasurementsFileExtension;

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    BitmapEncoder encoder = new JpegBitmapEncoder();
                                    if (Properties.Settings.Default.MeasurementsFileExtension.ToUpper() == ".BMP")
                                        encoder = new BmpBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(kvp.Value));
                                    encoder.Save(fileStream);
                                }
                                fImageList.Add(GetBitmap(kvp.Value));
                            }
                        }

                        //save background image[s]
                        for (int i = 0; i < _backgroundImageList.Count; i++)
                        {
                            string bgFilePath = rootDirectory + @"\background" + i
                                    + Properties.Settings.Default.MeasurementsFileExtension;

                            if (Properties.Settings.Default.MeasurementsFileExtension.ToUpper() == ".BMP")
                                _backgroundImageList[i].Save(bgFilePath, System.Drawing.Imaging.ImageFormat.Bmp);
                            else
                                _backgroundImageList[i].Save(bgFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                    else //not saving images
                    {
                        foreach (KeyValuePair<double, BitmapSource> kvp in _measurementImages)
                        {
                            imageList.Add(GetBitmap(kvp.Value));
                        }
                        foreach (KeyValuePair<double, BitmapSource> kvp in _fluorescenceMeasurementImages)
                        {
                            fImageList.Add(GetBitmap(kvp.Value));
                        }
                    }

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                            App.LogEntry.AddEntry("Performing color analysis...")));


                    //store dust images////////////////////////////////////////
                    if (ImageProcessingUtility.DustDetectOn)
                    {
                        _dustImages = new Dictionary<double, BitmapSource>();
                        int div = _measurementImages.Count / 3;
                        if (Properties.Settings.Default.NumberOfSteps == 0)
                        {
                            _dustImages.Add(div, _measurementImages[div]);
                            _dustImages.Add(2 * div, _measurementImages[2 * div]);
                        }
                        else
                        {
                            int stepAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                            _dustImages.Add(stepAngle * div, _measurementImages[stepAngle * div]);
                            _dustImages.Add(stepAngle * div * 2, _measurementImages[stepAngle * div * 2]);
                        }
                    }
                    ///////////////////////////////////////////////////////////

                    ////////////////////////
                    //Hiroshi add 2/21/2014
                    ////////////////////////
                    double L = 0, a = 0, b = 0, C = 0, H = 0;
                    double mask_L = 0, mask_A = 0;
                    string L_description = "", C_description = "", H_description = "", comment = "", comment1 = "",
                        comment2 = "", comment3 = "", instruction = "", version = "";

                    if (_stone == null)
                        _stone = new Stone();

                    //Fluorescence ///////////////////////////////////////
                    if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure)
                    {
                        double L_override = -1.0;
                        if (imageList.Count != fImageList.Count)
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("WARNING: color image count does not match fluorescence image count")));

                        double gain = GlobalVariables.fluorescenceSettings.Gain;
                        if (_usedLowGain)
                        {
                            gain = GlobalVariables.fluorescenceSettings.LowGain;
                            L_override = _stoneMeasurements[0].LF;
                        }
                        double shutter = GetShutterTime();

                        L = 0; C = 0; H = 0;
                        L_description = ""; C_description = ""; H_description = ""; comment = "";
                        instruction = "";
                        bool checkMultiColor = false;

                        bool fluorescenceResult = ImageProcessing.GetPL_description(ref imageList, ref fImageList,
                                ref L, ref C, ref H, ref L_description,
                                ref C_description, ref H_description, ref comment, ref instruction,
                                gain.ToString(), shutter.ToString(), out version, L_override, out checkMultiColor);

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Fluorescence = " + C_description + ",L = " + L +
                            ",C = " + C + ",H = " + H + ", L_des = " + L_description +
                            ", C_des = " + C_description + ", H_des = " + H_description)));

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry(comment)));

                        if (fluorescenceResult)
                        {
                            _stone.CF = C;
                            _stone.LF = L;
                            _stone.HF = H;
                            _stone.HFDesc = H_description;
                            _stone.CFDesc = C_description;
                            _stone.LFDesc = L_description;
                            _stone.FComment = comment + ", " + GetCameraTemp().ToString() + ", "
                                    + GetWBBlue().ToString() + ", " + GetWBRed().ToString();
                            _stone.Instruction = instruction;
                            _stone.Version = version;

                            _stone.WarningMessage = "";
                            if (checkMultiColor)
                            {
                                bool isMultiColored = ImageProcessingUtility.IsMultiColorFluorescence(H_description, imageList,
                                    fImageList, GlobalVariables.fluorescenceSettings.MultiColorThresholdPercent);
                                _stone.WarningMessage = "Single color";
                                if (isMultiColored)
                                    _stone.WarningMessage = "Multi color";
                            }

                        }
                        
                        _stone.GoodColorResult = fluorescenceResult;

                    }

                    /////////////////////////////////////////////////////
                    //Hiroshi add for camera temp monitoring 5/07/2014
                    /////////////////////////////////////////////////////
                    _cameraTempMeasurement = GetCameraTemp();
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Camera temp=" + _cameraTempMeasurement.ToString() + " bgtemp=" + _cameraTempBG.ToString())));

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Blue=" + GetWBBlue().ToString() + " Red=" + GetWBRed().ToString())));

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Image count=" + imageList.Count.ToString())));


                    if (comment1 != "") comment1 = comment1 + "," + comment3 + ", " + GetCameraTemp().ToString() + ", " 
                        + _cameraTempBG.ToString() + ", " + GetShutterTime().ToString() + ", " + GetWBBlue().ToString() 
                        + ", " + GetWBRed().ToString();
                    if (comment2 != "") comment2 = comment2 + "," + comment3 + ", " + GetCameraTemp().ToString() + ", " 
                        + _cameraTempBG.ToString() + ", " + GetShutterTime().ToString() + ", " + GetWBBlue().ToString() 
                        + ", " + GetWBRed().ToString();

                    //////////////////////////////////////////////////////


                    //_stone.C = C;
                    //_stone.L = L;
                    //_stone.H = H;
                    //_stone.A = a;
                    //_stone.B = b;
                    //_stone.HDesc = H_description;
                    //_stone.CDesc = C_description;
                    //_stone.LDesc = L_description;
                    //_stone.Mask_L = mask_L;
                    //_stone.Mask_A = mask_A;
                    //_stone.Comment1 = comment1;
                    //_stone.Comment2 = comment2;
                    //_stone.Comment3 = comment3;
                    //_stone.GoodColorResult = colorResult;


                }

                e.Result = "Measurement complete!";

                if (color != String.Empty)
                {
                    e.Result += " The calculated color is " + color;
                }


            }
            catch (Exception ex)
            {
                if (_stone != null)
                    _stone.GoodColorResult = false;
                e.Result = "Measurement images save failed: " + ex.Message;
                _measurementImages = null;
                _fluorescenceMeasurementImages = new Dictionary<double, BitmapSource>();
            }
            finally
            {
                foreach (var i in imageList)
                    i.Dispose();
                foreach (var f in fImageList)
                    f.Dispose();
            }
            
        }

        //Hiroshi add for status monitoring
        double GetCameraTemp()
        {
            Dictionary<CAMERA_PROPERTY, double> readings = new Dictionary<CAMERA_PROPERTY, double>();
            readings.Add(CAMERA_PROPERTY.Temperature, 0);
            _camera.GetInitializationPropertyValues(readings);
            return readings[CAMERA_PROPERTY.Temperature];
        }

        double GetShutterTime()
        {
            Dictionary<CAMERA_PROPERTY, double> readings = new Dictionary<CAMERA_PROPERTY, double>();
            readings.Add(CAMERA_PROPERTY.Shutter, 0);
            _camera.GetInitializationPropertyValues(readings);
            return readings[CAMERA_PROPERTY.Shutter];
        }

        double GetWBRed()
        {
            Dictionary<CAMERA_PROPERTY, double> readings = new Dictionary<CAMERA_PROPERTY, double>();
            readings.Add(CAMERA_PROPERTY.WhiteBalanceRed, 0);
            _camera.GetInitializationPropertyValues(readings);
            return readings[CAMERA_PROPERTY.WhiteBalanceRed];
        }

        double GetWBBlue()
        {
            Dictionary<CAMERA_PROPERTY, double> readings = new Dictionary<CAMERA_PROPERTY, double>();
            readings.Add(CAMERA_PROPERTY.WhiteBalanceBlue, 0);
            _camera.GetInitializationPropertyValues(readings);
            return readings[CAMERA_PROPERTY.WhiteBalanceBlue];
        }

        void SaveMeasurementImagesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _stoneMeasurements.Add((Stone)_stone.Clone());

            if (MeasureIconText.Length > 0)
            {
                var targetStone = CalibrationStoneTable.CalStoneTable.Select("Target = '" + MeasureIconText + "'");
                double maxL = Convert.ToDouble(targetStone[0][1]);
                double minL = Convert.ToDouble(targetStone[0][2]);
                uint currentStep = Convert.ToUInt32(targetStone[0][3]);

                if ((++_lCalibrationRetryCount >= MAX_L_CAL_RETRY_COUNT) || (_stone.LF >= minL && _stone.LF <= maxL)) //within target
                {
                    SaveMeasurementImagesCompletedContinue(sender, e);
                    return;
                }
                else if (_stone.LF < minL)
                {
                    GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent += currentStep;
                    if (GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent > LightControl.FLUORESCENCE_MAX_CURRENT)
                        GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent = LightControl.FLUORESCENCE_MAX_CURRENT;
                }
                else if (_stone.LF > maxL)
                {
                    if (GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent > currentStep)
                        GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent -= currentStep;
                    else
                    {
                        FluorescenceShutter = false;
                        _camera.RestoreNormalSettings();
                        _camera.BufferFrames(false);

                        if (Properties.Settings.Default.AutoOpenClose)
                            OpenHemisphereCommand();

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry("L calibration failed, UV current too low", true);
                        }));

                        IsReady = true;
                        MeasurementActive = false;
                        Status = "Connected";
                        return;
                    }
                }

                //rotate and measure again 
                try
                {
                    FluorescenceLight = true;
                    ST5.ResetPosition();
                    _numberOfRotations = 0;
                    _continuousImageCount = 0;
                    _fluorescenceMeasurementImages = new Dictionary<double, BitmapSource>();

                    FluorescenceShutter = true;

                    if (!FluorescenceLight || MainLight)
                        throw new ApplicationException("Failed to set light");

                    _camera.InitFluorescenceSettings(0);
                    Thread.Sleep(500);
                    _usedLowGain = false;
                    _uvIntensity = Hemisphere.GetUVIntensity();

                    if (Properties.Settings.Default.NumberOfSteps > 0)
                    {
                        _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                        IsReady = true;
                        ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry("Fluorescence Measurement ReStarted");
                        }));
                        _takeImage = false; //will be set after first step
                        _takeSingleMeasurement = true;
                    }
                    else
                    {
                        //rotate 360 degrees and collect 100 images controlled by motor trigger
                        _camera.BufferFrames(true);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry("Fluorescence Continuous Calibration Measurement ReStarted with current: " +
                                GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent, true);
                        }));
                        _takeContinuousMeasurement = true;
                        IsReady = true;
                        ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                    }
                }
                catch (Exception ex)
                {
                    FluorescenceShutter = false;
                    _camera.RestoreNormalSettings();
                    _camera.BufferFrames(false);

                    if (Properties.Settings.Default.AutoOpenClose)
                        OpenHemisphereCommand();

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        App.LogEntry.AddEntry(ex.Message);
                    }));

                    IsReady = true;
                    MeasurementActive = false;
                    Status = "Connected";
                }


            }
            else if (_stone.LF < GlobalVariables.fluorescenceSettings.FLThreshold ||
                _stoneMeasurements.Count > 1)
            {
                SaveMeasurementImagesCompletedContinue(sender, e);
                return;
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                   {
                       App.LogEntry.AddEntry("FL saturated, measuring again.", true);
                   }));

                try
                {
                    ST5.ResetPosition();
                    _numberOfRotations = 0;
                    _continuousImageCount = 0;
                    _fluorescenceMeasurementImages = new Dictionary<double, BitmapSource>();

                    //MainLight = false;
                    FluorescenceShutter = true;

                    if (!FluorescenceLight || MainLight)
                        throw new ApplicationException("Failed to set light");

                    _camera.InitFluorescenceSettings(1);
                    Thread.Sleep(500);
                    _usedLowGain = true;
                    _uvIntensity = Hemisphere.GetUVIntensity();
                    if (_uvIntensity < GlobalVariables.fluorescenceSettings.UVWarningThreshold)
                        throw new ApplicationException("UV Intensity low [" + _uvIntensity + "]");
                    else if (_uvIntensity > GlobalVariables.fluorescenceSettings.UVWarningThresholdHigh)
                        throw new ApplicationException("UV Intensity high [" + _uvIntensity + "]");

                    if (Properties.Settings.Default.NumberOfSteps > 0)
                    {
                        _rotationAngle = 360 / Properties.Settings.Default.NumberOfSteps;
                        IsReady = true;
                        ST5.RotateByAngle(_rotationAngle, (Direction)Properties.Settings.Default.RotationDirection);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry("Fluorescence Measurement ReStarted");
                        }));
                        _takeImage = false; //will be set after first step
                        _takeSingleMeasurement = true;
                    }
                    else
                    {
                        //rotate 360 degrees and collect 100 images controlled by motor trigger
                        _camera.BufferFrames(true);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry("Fluorescence Continuous Measurement ReStarted");
                        }));
                        _takeContinuousMeasurement = true;
                        IsReady = true;
                        ST5.RotateByAngle(360, (Direction)Properties.Settings.Default.RotationDirection);
                    }
                }
                catch (Exception ex)
                {
                    FluorescenceShutter = false;
                    _camera.RestoreNormalSettings();
                    _camera.BufferFrames(false);

                    if (Properties.Settings.Default.AutoOpenClose)
                        OpenHemisphereCommand();

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        App.LogEntry.AddEntry(ex.Message);
                    }));

                    IsReady = true;
                    MeasurementActive = false;
                    Status = "Connected";
                }
            }
        }

        void SaveMeasurementImagesCompletedContinue(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Properties.Settings.Default.AutoOpenClose)
                OpenHemisphereCommand();

            foreach (Stone st in _stoneMeasurements)
            {
                if (st.GoodColorResult == false)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Image processing error", true)));

                    IsReady = true;
                    MeasurementActive = false;
                    Status = "Connected";
                    return;
                }
            }

            //Check dust in images///////////////////////////////////////////////////////////
            #region check_dust

            bool dustPresent = true;

            if (_dustImages != null && _dustImages.Count >= 2)
            {
                int objectCount = -1;
                double maskArea = -1;
                string unused = "";

                System.Drawing.Bitmap[] dBitmap = new System.Drawing.Bitmap[_dustImages.Count];
                int[] imageNumber = new int[_dustImages.Count];
                int count = 0;
                foreach (var kvp in _dustImages)
                {
                    dBitmap[count] = GetBitmap(kvp.Value);
                    imageNumber[count] = (int)kvp.Key;
                    count++;
                }

                var img = ImageProcessingUtility.ObjectDetector(dBitmap[0], out objectCount, out maskArea);
                bool noDust = ImageProcessing.check_NO_measurementDust(ref dBitmap[0], ref unused);
                bool userAccepted = false;

                if (!GlobalVariables.InTestMode && ((objectCount != 0) || !noDust) )
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        var verifyDustDlg = new gUV.View.VerifyDustDialog();
                        var verifyDustDlgVM = new VerifyDustDialogViewModel(imageNumber[0].ToString(), GetBitmapSource(img));
                        verifyDustDlg.DataContext = verifyDustDlgVM;
                        userAccepted = (bool)verifyDustDlg.ShowDialog();
                    }));
                }
                else
                    userAccepted = true;

                if (userAccepted)
                {
                    //user checked the image and accepted the image
                    //check second dust image
                    img = ImageProcessingUtility.ObjectDetector(dBitmap[1], out objectCount, out maskArea);
                    noDust = ImageProcessing.check_NO_measurementDust(ref dBitmap[1], ref unused);
                    if ( !GlobalVariables.InTestMode && ((objectCount != 0) || !noDust))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            var verifyDustDlg = new gUV.View.VerifyDustDialog();
                            var verifyDustDlgVM = new VerifyDustDialogViewModel(imageNumber[0].ToString(), GetBitmapSource(img));
                            verifyDustDlg.DataContext = verifyDustDlgVM;
                            userAccepted = (bool)verifyDustDlg.ShowDialog();
                        }));
                    }
                    else
                        userAccepted = true;
                }

                dustPresent = !userAccepted;

                foreach (var bmp in dBitmap)
                    bmp.Dispose();
            }
            else
                dustPresent = false;

            if (dustPresent)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("User rejected measurement due to dust!", true)));
            }

            #endregion
            /////////////////////////////////////////////////////////////////////////////////

            //save data only if no dust
            #region save_data
            string deviceName = ImageProcessing.getDevicename();
            string spectrumFileName = deviceName + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";

            if (!dustPresent)
            {
                if (MeasureIconText.Length > 0)
                {
                    _stone = _stoneMeasurements[0];

                    if (!GlobalVariables.InTestMode && Properties.Settings.Default.ExtractCalDataToTextFile &&
                        Properties.Settings.Default.CalDataTextFilePath.Length > 0)
                    {
                        //execute on main thread
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            _stone.ControlNumber = MeasureIconText;
                            //_stone.Save(Properties.Settings.Default.MetrologyFilePath, deviceName + "ColorimeterMetrology.csv");
                            //_stone.Save(Properties.Settings.Default.CalDataTextFilePath, spectrumFileName);

                            Stone.SaveFluorescenceData(Properties.Settings.Default.CalDataTextFilePath,
                                                            spectrumFileName, _stoneMeasurements, _uvIntensity);
                            Stone.SaveFluorescenceData(Properties.Settings.Default.MetrologyFilePath,
                                                            deviceName + "FluorescenceMetrology.csv", _stoneMeasurements, _uvIntensity);

                        }));
                    }

                    var targetStone = CalibrationStoneTable.CalStoneTable.Select("Target = '" + MeasureIconText + "'");
                    int index = -1;

                    try
                    {
                        var targetStones = Properties.Settings.Default.DailyMonitorTargetList.Split(',');
                        int currentIndex = CalibrationStoneTable.CalStoneTable.Rows.IndexOf(targetStone[0]);
                        index = Array.IndexOf(targetStones, currentIndex.ToString());
                        _dailyMonitorTargetIndex = index + 1;
                        
                        if (index < targetStones.Length - 1)
                        {
                            MeasureIconText = CalibrationStoneTable.CalStoneTable.Rows[Convert.ToInt32(targetStones[_dailyMonitorTargetIndex])]["Target"].ToString();
                            MeasureIconImageSource = "..\\Images\\cal_stone.png";
                            MeasureIconToolTip = "Measure calibration stone";
                        }
                        else
                        {
                            _dailyMonitorTargetIndex = 0; //cycle back to first calibration stone
                            MeasureIconText = CalibrationStoneTable.CalStoneTable.Rows[Convert.ToInt32(targetStones[_dailyMonitorTargetIndex])]["Target"].ToString();
                            MeasureIconImageSource = "..\\Images\\cal_stone.png";
                            MeasureIconToolTip = "Measure calibration stone";
                            if (_lCalibrationRetryCount >= MAX_L_CAL_RETRY_COUNT)
                            {
                                GlobalVariables.fluorescenceSettings.FluorescenceSetCurrent = _savedFlSetCurrent;
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    App.LogEntry.AddEntry("L calibration failed - too many retries", true);
                                }));
                            }
                            GlobalVariables.fluorescenceSettings.Save();
                            _lCalibrationRetryCount = 0;
                            FluorescenceLight = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            App.LogEntry.AddEntry(ex.Message);
                        }));
                    }


                }
                else if (!GlobalVariables.InTestMode && _stoneMeasurements.Count > 0 &&
                          (((Properties.Settings.Default.ExtractToTextFile)
                          ) || (GlobalVariables.spectrumSettings.RootUrl.Length > 0))
                         )
                {
                    //execute on main thread
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            Stone combined = (Stone)_stoneMeasurements[0].Clone();
                            if (_stoneMeasurements.Count > 1)
                            {
                                combined = (Stone)_stoneMeasurements[1].Clone();
                                combined.LF = _stoneMeasurements[0].LF;
                                combined.LFDesc = _stoneMeasurements[0].LFDesc;
                            }
                            var dialogSaveWindow = new gUV.View.SaveData();
                            var dialogSaveDataViewModel = new gUV.ViewModel.SaveDataViewModel(combined, _stoneMeasurements, _uvIntensity,
                                spectrumFileName, deviceName, ref _spectrumUserName);
                            dialogSaveWindow.DataContext = dialogSaveDataViewModel;
                            if (dialogSaveWindow.ShowDialog() == true)
                            {
                                if (dialogSaveDataViewModel.SpectrumMessage.Length > 0)
                                    App.LogEntry.AddEntry(dialogSaveDataViewModel.SpectrumMessage, true);
                                else
                                    App.LogEntry.AddEntry("Data Saved", true);
                            }
                            
                            if (dialogSaveDataViewModel.SpectrumUser.Length > 0)
                                SpectrumUsername = dialogSaveDataViewModel.SpectrumUser;
                            else
                                SpectrumUsername = SPECTRUM_LOG_OUT;
                        }));
                }
            }
            #endregion

            IsReady = true;
            MeasurementActive = false;
            Status = "Connected";

            #region check_calibration_again
            if (!GlobalVariables.InTestMode)
            {
                /////////////////////////////////////////////////////
                //Hiroshi add for Re-calibration warning
                /////////////////////////////////////////////////////
                if (Math.Abs(_cameraTempMeasurement - _cameraTempBG) > Convert.ToDouble(Properties.Settings.Default.Temperature))
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration again " + "Temp_BG=" + _cameraTempBG.ToString() + ", Temp_Measure=" + _cameraTempMeasurement.ToString(), true)));

                    _backgroundImage = null;
                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                    _backgroundImageSrcList.Clear();
                    _takingBackgroundImages = false;
                    _mode = Mode.Regular;
                    _calibrationStablizeCount = 0;
                    IsReady = true;
                }
                //if ((_tempChange == true && sw.ElapsedMilliseconds > 240000) || sw.ElapsedMilliseconds > 600000)
                else if (sw.ElapsedMilliseconds > (Convert.ToDouble(Properties.Settings.Default.Time) * 1000 - 20000))
                {
                    sw.Stop();
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration again " + "time[ms]=" + sw.ElapsedMilliseconds.ToString(), true)));

                    _backgroundImage = null;
                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                    _backgroundImageSrcList.Clear();
                    _takingBackgroundImages = false;
                    _mode = Mode.Regular;
                    _calibrationStablizeCount = 0;
                    IsReady = true;
                }
                /////////////////////////////////////////////////////
                //Hiroshi add for Re-calibration warning
                /////////////////////////////////////////////////////
                else if (Math.Abs(_cameraTempMeasurement - _cameraTempBG) > Convert.ToDouble(Properties.Settings.Default.Temperature))
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration again " + "Temp_BG=" + _cameraTempBG.ToString() + ", Temp_Measure=" + _cameraTempMeasurement.ToString(), true)));

                    _backgroundImage = null;
                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                    _backgroundImageSrcList.Clear();
                    _takingBackgroundImages = false;
                    _mode = Mode.Regular;
                    _calibrationStablizeCount = 0;
                    IsReady = true;

                }
                //if ((_tempChange == true && sw.ElapsedMilliseconds > 240000) || sw.ElapsedMilliseconds > 600000)
                else if (sw.ElapsedMilliseconds > Convert.ToDouble(Properties.Settings.Default.Time) * 1000)
                {
                    sw.Stop();
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration again " + "time[ms]=" + sw.ElapsedMilliseconds.ToString(), true)));

                    _backgroundImage = null;
                    _backgroundImageList = new List<System.Drawing.Bitmap>();
                    _backgroundImageSrcList.Clear();
                    _takingBackgroundImages = false;
                    _mode = Mode.Regular;
                    _calibrationStablizeCount = 0;
                    IsReady = true;

                }
            }
            #endregion

            Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry((string)e.Result)));

            testMeasureComplete.Set();

        }

        void StopCamera()
        {
            _keepRetrievingImages = false;
            _mainBgWorkerDoneEvent.WaitOne(5000);
            App.LogEntry.AddEntry("Stopped camera capture");
            _camera.DisConnect();
            App.LogEntry.AddEntry("Camera Disconnected");
        }

        void MapWebServiceEndpoints_doWork(object sender, DoWorkEventArgs e)
        {
            e.Result = null;

            if (GlobalVariables.spectrumSettings.RootUrl.Length == 0)
                return;

            if (GlobalVariables.endpoints.map_user_url == null || GlobalVariables.endpoints.map_user_url.Length == 0)
            {
                string code = null;
                string reason = null;

                e.Result = GiaSpectrum.GetEndpoints(out code, out reason, out GlobalVariables.endpoints);
            }
        }

        void MapServiceEndpoints_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null && (bool)e.Result == false)
            {
                MessageBox.Show("Error trying to retrieve web service end points.", "Program will exit");
                App.Current.Shutdown();
            }
        }

        void SpectrumAuth()
        {
            if (SpectrumUsername == SPECTRUM_LOG_OUT)
            {
                var spectrumLogin = new gUV.View.SpectrumLogin();
                bool? dialogResult = spectrumLogin.ShowDialog();
                if (dialogResult == true)
                {
                    string message;
                    string code;
                    if (GiaSpectrum.MapUser(GlobalVariables.endpoints.map_user_url, spectrumLogin.SpectrumUser, spectrumLogin.SpectrumDat,
                                ClassOpenCV.ImageProcessing.getDevicename(), out code, out message))
                    {
                        SpectrumUsername = spectrumLogin.SpectrumUser;
                    }
                    else
                    {
                        MessageBox.Show(message, "Spectrum login error");
                        SpectrumUsername = SPECTRUM_LOG_OUT;
                    }
                }
                
            }
            else
            {
                string message;
                string code;
                if (GiaSpectrum.Logout(GlobalVariables.endpoints.dissociate_user_url, ClassOpenCV.ImageProcessing.getDevicename(), 
                    out code, out message))
                    SpectrumUsername = SPECTRUM_LOG_OUT;
                else
                    MessageBox.Show(message, "Spectrum logout error");
            }
        }


        ~ControlViewModel()
        {
            _keepRetrievingImages = false;
            _mainBgWorkerDoneEvent.WaitOne(5000);
            CameraDispose(false);
        }


        private bool _disposed = false;

        protected override void OnDispose()
        {
            _keepRetrievingImages = false;
            _mainBgWorkerDoneEvent.WaitOne(5000);
            LightControl.Close();
            CameraDispose(true);
            GC.SuppressFinalize(this);
        }

        void CameraDispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_camera != null)
                        _camera.DisConnect();
                }
                _disposed = true;
            }
        }
    }

    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return new BitmapImage(new Uri((string)value, UriKind.Relative));
            }
            catch
            {
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
