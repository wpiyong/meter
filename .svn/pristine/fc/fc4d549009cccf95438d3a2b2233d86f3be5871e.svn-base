using gColor.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gColor.ViewModel
{
    class SettingsViewModel : ViewModelBase
    {
        Model.Direction _rotationDirection;
        int _numberOfSteps;
        double _motorVelocity, _motorContVelocity;
        string _motorPort;

        string _measurementFolder, _textFilePath, _calDataTextFilePath, _metrologyFilePath;
        int _measurementImageType;
        bool _saveMeasurement, _extractToTextFile, _extractCalDataToTextFile;
        bool _calculateColorAtStep;

        int _motorStepsPerRev;

        double _saturation, _gamma, _wbConvergence, _hue;
        uint _cropLeft, _cropTop, _cropWidth, _cropHeight;
        decimal _frameRate;
        int _wbAdjust;
        double _crossHairVerticalOffset, _crossHairHorizontalOffset;
        int _crossHairBrush;

        bool _wbInitialize;
        int _wbInitialRed, _wbInitialBlue;

        bool _autoOpenClose;

        int _time;
        double _temperature;

        double _shutterTimeMs, _shutterTimeDiffMs, _cameraTempDiffMs, _aDiffMs, _bDiffMs, _wBBlueDiff, _wBRedDiff;

        double _lconv, _aconv, _bconv, _lshift, _ashift, _bshift;

        Camera _connectedCamera;

        public bool WBInitialize
        {
            get { return _wbInitialize; }
            set
            {
                _wbInitialize = value;
                OnPropertyChanged("WBInitialize");
            }
        }
        public int WBInitialRed
        {
            get { return _wbInitialRed; }
            set
            {
                _wbInitialRed = value;
                OnPropertyChanged("WBInitialRed");
            }
        }
        public int WBInitialBlue
        {
            get { return _wbInitialBlue; }
            set
            {
                _wbInitialBlue = value;
                OnPropertyChanged("WBInitialBlue");
            }
        }

        public bool AutoOpenClose
        {
            get { return _autoOpenClose; }
            set
            {
                _autoOpenClose = value;
                OnPropertyChanged("AutoOpenClose");
            }
        }

        public string MeasurementFolder
        {
            get { return _measurementFolder; }
            set
            {
                _measurementFolder = value;
                OnPropertyChanged("MeasurementFolder");
            }
        }

        public string TextFilePath
        {
            get { return _textFilePath; }
            set
            {
                _textFilePath = value;
                OnPropertyChanged("TextFilePath");
            }
        }

        public string MetrologyFilePath
        {
            get { return _metrologyFilePath; }
            set
            {
                _metrologyFilePath = value;
                OnPropertyChanged("MetrologyFilePath");
            }
        }

        public string CalDataTextFilePath
        {
            get { return _calDataTextFilePath; }
            set
            {
                _calDataTextFilePath = value;
                OnPropertyChanged("CalDataTextFilePath");
            }
        }

        public int MeasurementFileExtension
        {
            get { return _measurementImageType; }
            set
            {
                _measurementImageType = value;
                OnPropertyChanged("MeasurementFileExtension");
            }
        }

        public bool SaveMeasurement
        {
            get { return _saveMeasurement; }
            set
            {
                _saveMeasurement = value;
                OnPropertyChanged("SaveMeasurement");
            }
        }

        public bool ExtractToTextFile
        {
            get { return _extractToTextFile; }
            set
            {
                _extractToTextFile = value;
                OnPropertyChanged("ExtractToTextFile");
            }
        }

        public bool ExtractCalDataToTextFile
        {
            get { return _extractCalDataToTextFile; }
            set
            {
                _extractCalDataToTextFile = value;
                OnPropertyChanged("ExtractCalDataToTextFile");
            }
        }

        public bool CalculateColorAtStep
        {
            get
            {
                return _calculateColorAtStep;
            }
            set
            {
                _calculateColorAtStep = value;
                OnPropertyChanged("CalculateColorAtStep");
            }
        }


        public bool IsClockwise
        {
            get
            {
                return _rotationDirection == Model.Direction.CW;
            }
            set
            {
                _rotationDirection = Model.Direction.CW;
                
                OnPropertyChanged("IsClockwise");
                OnPropertyChanged("IsCounterClockwise");                            
            }
        }

        public bool IsCounterClockwise
        {
            get
            {
                return _rotationDirection == Model.Direction.CCW;
            }
            set
            {
                _rotationDirection = Model.Direction.CCW;

                OnPropertyChanged("IsClockwise");
                OnPropertyChanged("IsCounterClockwise");                 
            }
        }

        public int NumberOfSteps
        {
            get
            {
                return _numberOfSteps;
            }
            set
            {
                try
                {
                    if ((value !=0 && 360 % value != 0) || value < 0 ||
                        value > 360) throw new Exception("");
                    _numberOfSteps = value;
                    OnPropertyChanged("NumberOfSteps");
                }
                catch (Exception /*e*/)
                {
                    App.LogEntry.AddEntry("Number of Steps not changed, value must be a factor of 360");
                }
            }
        }

        public double MotorVelocity
        {
            get
            {
                return _motorVelocity;
            }
            set
            {
                _motorVelocity = value;
                OnPropertyChanged("MotorVelocity");
            }
        }

        public double MotorContVelocity
        {
            get
            {
                return _motorContVelocity;
            }
            set
            {
                _motorContVelocity = value;
                OnPropertyChanged("MotorContVelocity");
            }
        }

        public string MotorPort
        {
            get
            {
                return _motorPort;
            }
            set
            {
                _motorPort = value;
                OnPropertyChanged("MotorPort");
            }
        }

        public int MotorStepsPerRev
        {
            get
            {
                return _motorStepsPerRev;
            }
            set
            {
                _motorStepsPerRev = value;
                OnPropertyChanged("MotorStepsPerRev");
            }
        }

        public double Gamma
        {
            get
            {
                return _gamma;
            }
            set
            {
                _gamma = value;
                OnPropertyChanged("Gamma");
            }
        }

        public double Saturation
        {
            get
            {
                return _saturation;
            }
            set
            {
                _saturation = value;
                OnPropertyChanged("Saturation");
            }
        }

        public double Hue
        {
            get
            {
                return _hue;
            }
            set
            {
                _hue = value;
                OnPropertyChanged("Hue");
            }
        }

        public double WBConvergence
        {
            get
            {
                return _wbConvergence;
            }
            set
            {
                _wbConvergence = value;
                OnPropertyChanged("WBConvergence");
            }
        }

        public int WBAdjust
        {
            get
            {
                return _wbAdjust;
            }
            set
            {
                if (value > 0 && value < 1000)
                    _wbAdjust = value;
                OnPropertyChanged("WBConvergence");
            }
        }

        public decimal FrameRate
        {
            get
            {
                return _frameRate;
            }
            set
            {
                if (value <= 30)
                    _frameRate = value;
                OnPropertyChanged("FrameRate");
            }
        }

        public uint CropTop
        {
            get
            {
                return _cropTop;
            }
            set
            {
                _cropTop = value;
                OnPropertyChanged("CropTop");
            }
        }
        public uint CropLeft
        {
            get
            {
                return _cropLeft;
            }
            set
            {
                _cropLeft = value;
                OnPropertyChanged("CropLeft");
            }
        }
        public uint CropWidth
        {
            get
            {
                return _cropWidth;
            }
            set
            {
                if (value > 0)
                    _cropWidth = value;
                OnPropertyChanged("CropWidth");
            }
        }
        public uint CropHeight
        {
            get
            {
                return _cropHeight;
            }
            set
            {
                if (value > 0)
                    _cropHeight = value;
                OnPropertyChanged("CropHeight");
            }
        }
        public double ImageWidth
        {
            get { return _connectedCamera == null ? 0 : _connectedCamera.ImageWidth ; }
        }
        public double ImageHeight
        {
            get { return _connectedCamera == null ? 0 : _connectedCamera.ImageHeight; }
        }
        public double CropImageWidth
        {
            get { return _connectedCamera == null ? 0 : _connectedCamera.CropImageWidth; }
        }
        public double CropImageHeight
        {
            get { return _connectedCamera == null ? 0 : _connectedCamera.CropImageHeight; }
        }

        public double CrossHairVerticalOffset
        {
            get
            {
                return _crossHairVerticalOffset;
            }
            set
            {
                _crossHairVerticalOffset = value;
                OnPropertyChanged("CrossHairVerticalOffset");
            }
        }

        public double CrossHairHorizontalOffset
        {
            get
            {
                return _crossHairHorizontalOffset;
            }
            set
            {
                _crossHairHorizontalOffset = value;
                OnPropertyChanged("CrossHairHorizontalOffset");
            }
        }

        public int CrossHairBrush
        {
            get
            {
                return _crossHairBrush; ;
            }
            set
            {
                _crossHairBrush = value;
                OnPropertyChanged("CrossHairBrush");
            }
        }

        public int Time
        {
            get { return _time; }
            set
            {
                _time = value;
                OnPropertyChanged("Time");
            }
        }
        public double Temperature
        {
            get { return _temperature; }
            set
            {
                _temperature = value;
                OnPropertyChanged("Temperature");
            }
        }

        public double ShutterTimeMs
        {
            get
            {
                return Math.Round(_shutterTimeMs, 2);
            }
            set
            {
                _shutterTimeMs = value;
                OnPropertyChanged("ShutterTimeMs");
            }
        }
        public double ShutterTimeDiffMs
        {
            get
            {
                return Math.Round(_shutterTimeDiffMs, 2);
            }
            set
            {
                _shutterTimeDiffMs = value;
                OnPropertyChanged("ShutterTimeDiffMs");
            }
        }
        public double WBRedDiff
        {
            get
            {
                return Math.Round(_wBRedDiff, 2);
            }
            set
            {
                _wBRedDiff = value;
                OnPropertyChanged("WBRedDiff");
            }
        }
        public double WBBlueDiff
        {
            get
            {
                return Math.Round(_wBBlueDiff, 2);
            }
            set
            {
                _wBBlueDiff = value;
                OnPropertyChanged("WBBlueDiff");
            }
        }
        public double CameraTempDiffMs
        {
            get
            {
                return Math.Round(_cameraTempDiffMs, 2);
            }
            set
            {
                _cameraTempDiffMs = value;
                OnPropertyChanged("CameraTempDiffMs");
            }
        }
        public double ADiffMs
        {
            get
            {
                return Math.Round(_aDiffMs, 2);
            }
            set
            {
                _aDiffMs = value;
                OnPropertyChanged("ADiffMs");
            }
        }
        public double BDiffMs
        {
            get
            {
                return Math.Round(_bDiffMs, 2);
            }
            set
            {
                _bDiffMs = value;
                OnPropertyChanged("BDiffMs");
            }
        }

        public double Lshift
        {
            get
            {
                return Math.Round(_lshift, 3);
            }
            set
            {
                _lshift = value;
                OnPropertyChanged("Lshift");
            }
        }
        public double Ashift
        {
            get
            {
                return Math.Round(_ashift, 3);
            }
            set
            {
                _ashift = value;
                OnPropertyChanged("Ashift");
            }
        }
        public double Bshift
        {
            get
            {
                return Math.Round(_bshift, 3);
            }
            set
            {
                _bshift = value;
                OnPropertyChanged("Bshift");
            }
        }
        public double Lconv
        {
            get
            {
                return Math.Round(_lconv, 3);
            }
            set
            {
                _lconv = value;
                OnPropertyChanged("Lconv");
            }
        }
        public double Aconv
        {
            get
            {
                return Math.Round(_aconv, 3);
            }
            set
            {
                _aconv = value;
                OnPropertyChanged("Aconv");
            }
        }
        public double Bconv
        {
            get
            {
                return Math.Round(_bconv, 3);
            }
            set
            {
                _bconv = value;
                OnPropertyChanged("Bconv");
            }
        }


        public DataTable TargetCalStoneList
        {
            get
            {
                return CalibrationStoneTable.CalStoneTable;
            }
        }
        int _selectedTargetCalStoneIndex;
        public int SelectedTargetCalStoneIndex
        {
            get
            {
                return _selectedTargetCalStoneIndex;
            }
            set
            {
                _selectedTargetCalStoneIndex = value;
                OnPropertyChanged("SelectedTargetCalStoneIndex");
            }
        }

        DataTable _selectedCalStoneList;
        int _selectedCalStoneIndex;
        public DataTable SelectedCalStoneList
        {
            get
            {
                return _selectedCalStoneList;
            }
            set
            {
                _selectedCalStoneList = value;
                OnPropertyChanged("SelectedCalStoneList");
            }
        }
        public int SelectedCalStoneIndex
        {
            get
            {
                return _selectedCalStoneIndex;
            }
            set
            {
                _selectedCalStoneIndex = value;
                OnPropertyChanged("SelectedCalStoneIndex");
            }
        }

        public SettingsViewModel()
        {
            base.DisplayName = "SettingsViewModel";
            _rotationDirection = (Model.Direction)Properties.Settings.Default.RotationDirection;
            _numberOfSteps = Properties.Settings.Default.NumberOfSteps;
            _motorVelocity = Properties.Settings.Default.MotorVelocity;
            _motorContVelocity = Properties.Settings.Default.MotorContinuousVelocity;
            _motorPort = Properties.Settings.Default.MotorPort;
            MotorStepsPerRev = Properties.Settings.Default.MotorStepsPerRev;
            SaveMeasurement = Properties.Settings.Default.SaveMeasuments;
            MeasurementFolder = Properties.Settings.Default.MeasurementsFolder;
            Saturation = (double)Properties.Settings.Default.Saturation;
            Hue = (double)Properties.Settings.Default.Hue;
            Gamma = (double)Properties.Settings.Default.Gamma;
            WBConvergence = (double)Properties.Settings.Default.WBConvergence;
            WBAdjust = Properties.Settings.Default.WBIncrement;
            FrameRate = Properties.Settings.Default.FrameRate;
            MeasurementFileExtension = Properties.Settings.Default.MeasurementsFileExtension.ToUpper() == ".BMP" ? 1 : 0;
            CalculateColorAtStep = Properties.Settings.Default.CalculateColorAtStep;
            CropLeft = Properties.Settings.Default.CropLeft;
            CropTop = Properties.Settings.Default.CropTop;
            CropWidth = Properties.Settings.Default.CropWidth;
            CropHeight = Properties.Settings.Default.CropHeight;
            CrossHairVerticalOffset = Properties.Settings.Default.CrossHairVerticalOffsetPercent;
            CrossHairHorizontalOffset = Properties.Settings.Default.CrossHairHorizontalOffsetPercent;
            CrossHairBrush = Properties.Settings.Default.CrossHairBrush ;
            ExtractToTextFile = Properties.Settings.Default.ExtractToTextFile;
            TextFilePath = Properties.Settings.Default.TextFilePath;
            MetrologyFilePath = Properties.Settings.Default.MetrologyFilePath;
            WBInitialize = Properties.Settings.Default.WBInitialize;
            WBInitialBlue = Properties.Settings.Default.WBInitializeBlue;
            WBInitialRed = Properties.Settings.Default.WBInitializeRed;
            Time = Properties.Settings.Default.Time;
            Temperature = Properties.Settings.Default.Temperature;
            ShutterTimeMs = Properties.Settings.Default.ShutterTime;
            ShutterTimeDiffMs = Properties.Settings.Default.ShutterTimeDiff;
            WBRedDiff = GlobalVariables.LightStableSettings.RedWBDiff;
            WBBlueDiff = GlobalVariables.LightStableSettings.BlueWBDiff;
            CameraTempDiffMs = Properties.Settings.Default.CameraTempDiff;
            ADiffMs = Properties.Settings.Default.ADiff;
            BDiffMs = Properties.Settings.Default.BDiff;
            AutoOpenClose = Properties.Settings.Default.AutoOpenClose;

            ExtractCalDataToTextFile = Properties.Settings.Default.ExtractCalDataToTextFile;
            CalDataTextFilePath = Properties.Settings.Default.CalDataTextFilePath;
            int[] selectedCalStoneArray;
            try
            {
                selectedCalStoneArray =
                    Properties.Settings.Default.DailyMonitorTargetList.Split(',').Select(s => Int32.Parse(s)).ToArray();
                if (selectedCalStoneArray.Length < 2)
                {
                    Properties.Settings.Default.DailyMonitorTargetList = String.Empty;
                    selectedCalStoneArray = new int[0];
                }
            }
            catch
            {
                selectedCalStoneArray = new int[0];
            }
            DataTable dt = new DataTable();
            dt.Columns.Add("Target");
            for (int i = 0; i < selectedCalStoneArray.Length; i++)
            {
                DataRow row = dt.NewRow();
                row["Target"] = TargetCalStoneList.Rows[selectedCalStoneArray[i]]["Target"];
                dt.Rows.Add(row);
            }
            SelectedCalStoneList = dt;

            Lconv = Properties.Settings.Default.LConv;
            Aconv = Properties.Settings.Default.AConv;
            Bconv = Properties.Settings.Default.BConv;
            Lshift = Properties.Settings.Default.Lshift;
            Ashift = Properties.Settings.Default.Ashift;
            Bshift = Properties.Settings.Default.Bshift;


            CommandClose = new RelayCommand(param => this.Close(param));
            CommandUpdateSettings = new RelayCommand(param => this.UpdateSettings(param));
            CommandMeasurementFolder = new RelayCommand(param => this.SelectMeasurementFolder());
            CommandTextFilePath = new RelayCommand(param => this.SelectTextFilePath());
            CommandMetrologyFilePath = new RelayCommand(param => this.SelectMetrologyFilePath());

            CommandCalDataTextFilePath = new RelayCommand(param => this.SelectCalDataTextFilePath());
            CommandAddTarget = new RelayCommand(param => this.AddTargetStone());
            CommandRemoveTarget = new RelayCommand(param => this.RemoveTargetStone());

            _connectedCamera = MainWindowViewModel.ControlVM.ConnectedCamera;

        }

        ~SettingsViewModel()
        {
            
        }

        public RelayCommand CommandUpdateSettings { get; set; }
        public RelayCommand CommandClose { get; set; }
        public RelayCommand CommandMeasurementFolder { get; set; }
        public RelayCommand CommandTextFilePath { get; set; }
        public RelayCommand CommandMetrologyFilePath { get; set; }

        public RelayCommand CommandCalDataTextFilePath { get; set; }
        public RelayCommand CommandAddTarget { get; set; }
        public RelayCommand CommandRemoveTarget { get; set; }



        void SelectMeasurementFolder()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                    new System.Windows.Forms.FolderBrowserDialog();

            if (MeasurementFolder != null)
                folderBrowserDialog.SelectedPath = MeasurementFolder;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MeasurementFolder = folderBrowserDialog.SelectedPath;
            }
        }

        void SelectTextFilePath()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                    new System.Windows.Forms.FolderBrowserDialog();

            if (TextFilePath != null)
                folderBrowserDialog.SelectedPath = TextFilePath;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextFilePath = folderBrowserDialog.SelectedPath;
            }
        }

        void SelectMetrologyFilePath()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                    new System.Windows.Forms.FolderBrowserDialog();

            if (MetrologyFilePath != null)
                folderBrowserDialog.SelectedPath = MetrologyFilePath;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MetrologyFilePath = folderBrowserDialog.SelectedPath;
            }
        }

        void SelectCalDataTextFilePath()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                    new System.Windows.Forms.FolderBrowserDialog();

            if (CalDataTextFilePath != null)
                folderBrowserDialog.SelectedPath = CalDataTextFilePath;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CalDataTextFilePath = folderBrowserDialog.SelectedPath;
            }
        }

        void AddTargetStone()
        {
            if (SelectedTargetCalStoneIndex >= 0)
            {
                var foundRows = SelectedCalStoneList.Select("Target = '" +
                                    TargetCalStoneList.Rows[SelectedTargetCalStoneIndex]["Target"] + "'");
                if (foundRows.Length == 0)
                {
                    DataRow row = SelectedCalStoneList.NewRow();
                    row["Target"] = TargetCalStoneList.Rows[SelectedTargetCalStoneIndex]["Target"];
                    SelectedCalStoneList.Rows.Add(row);
                }
            }
        }

        void RemoveTargetStone()
        {
            if (SelectedCalStoneIndex >= 0)
            {
                SelectedCalStoneList.Rows[SelectedCalStoneIndex].Delete();
            }
        }


        void UpdateSettings(object param)
        {
            bool crossHairRefresh = false;
            bool shiftValuesRefresh = false;

            if (Properties.Settings.Default.NumberOfSteps != _numberOfSteps)
            {
                App.LogEntry.AddEntry("Number of Steps changed from " + Properties.Settings.Default.NumberOfSteps
                    + " to " + _numberOfSteps);
                Properties.Settings.Default.NumberOfSteps = _numberOfSteps;
            }
            if (Properties.Settings.Default.RotationDirection != (int)_rotationDirection)
            {
                App.LogEntry.AddEntry("Updated Rotated Direction to "
                    + (_rotationDirection == Model.Direction.CW ? "Clockwise" : "Counter Clockwise"));
                Properties.Settings.Default.RotationDirection = (int)_rotationDirection;
            }
            if (Properties.Settings.Default.MotorVelocity != _motorVelocity)
            {
                try
                {
                    ST5.Velocity = _motorVelocity;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error setting Velocity");
                    return;
                }
                App.LogEntry.AddEntry("Motor Velocity changed from " + Properties.Settings.Default.MotorVelocity
                    + " to " + _motorVelocity);
                Properties.Settings.Default.MotorVelocity = _motorVelocity;
            }
            if (Properties.Settings.Default.MotorContinuousVelocity != _motorContVelocity)
            {
                try
                {
                    ST5.ContinuousVelocity = _motorContVelocity;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error setting Velocity");
                    return;
                }
                App.LogEntry.AddEntry("Motor Continuous Velocity changed from " + Properties.Settings.Default.MotorContinuousVelocity
                    + " to " + _motorContVelocity);
                Properties.Settings.Default.MotorContinuousVelocity = _motorContVelocity;
            }
            if (Properties.Settings.Default.MotorPort != _motorPort)
            {
                App.LogEntry.AddEntry("Motor Port changed from " + Properties.Settings.Default.MotorPort
                    + " to " + _motorPort);
                Properties.Settings.Default.MotorPort = _motorPort;
            }
            if (Properties.Settings.Default.MotorStepsPerRev != _motorStepsPerRev)
            {
                if (_motorStepsPerRev <= 400 || _motorStepsPerRev > 30000)
                {
                    App.LogEntry.AddEntry("Motor Steps/Rev not changed, range is from 400 to 30000");
                }
                else
                {
                    ST5.StepsPerRev = _motorStepsPerRev;
                    App.LogEntry.AddEntry("Motor Steps/Rev changed from " + Properties.Settings.Default.MotorStepsPerRev
                        + " to " + _motorStepsPerRev);
                    Properties.Settings.Default.MotorStepsPerRev = _motorStepsPerRev;
                }
            }

            if (Properties.Settings.Default.SaveMeasuments != _saveMeasurement)
            {
                App.LogEntry.AddEntry("Save Measurements changed from " + Properties.Settings.Default.SaveMeasuments
                    + " to " + _saveMeasurement);
                Properties.Settings.Default.SaveMeasuments = _saveMeasurement;
            }
            if (Properties.Settings.Default.ExtractToTextFile != _extractToTextFile)
            {
                App.LogEntry.AddEntry("Extract to Text file changed from " + Properties.Settings.Default.ExtractToTextFile
                    + " to " + _extractToTextFile);
                Properties.Settings.Default.ExtractToTextFile = _extractToTextFile;
            }
            if (Properties.Settings.Default.ExtractCalDataToTextFile != _extractCalDataToTextFile)
            {
                App.LogEntry.AddEntry("Extract Cal Data to Text file changed from " + Properties.Settings.Default.ExtractCalDataToTextFile
                    + " to " + _extractCalDataToTextFile);
                Properties.Settings.Default.ExtractCalDataToTextFile = _extractCalDataToTextFile;
            }
            if (Properties.Settings.Default.CalculateColorAtStep != _calculateColorAtStep)
            {
                App.LogEntry.AddEntry("Calculate Color at step changed from " + Properties.Settings.Default.CalculateColorAtStep
                    + " to " + _calculateColorAtStep);
                Properties.Settings.Default.CalculateColorAtStep = _calculateColorAtStep;
            }
            if (Properties.Settings.Default.MeasurementsFolder != _measurementFolder)
            {
                App.LogEntry.AddEntry("Measurements Folder changed from " + Properties.Settings.Default.MeasurementsFolder
                    + " to " + _measurementFolder);
                Properties.Settings.Default.MeasurementsFolder = _measurementFolder;
            }
            if (_measurementImageType != (Properties.Settings.Default.MeasurementsFileExtension.ToUpper() == ".BMP" ? 1 : 0))
            {
                string ext = (_measurementImageType == 0 ? ".JPG" : ".BMP");
                App.LogEntry.AddEntry("Measurement images file extension changed from " + Properties.Settings.Default.MeasurementsFileExtension
                    + " to " + ext);
                Properties.Settings.Default.MeasurementsFileExtension = ext;
            }

            if (Properties.Settings.Default.TextFilePath != _textFilePath)
            {
                App.LogEntry.AddEntry("Text file path changed from " + Properties.Settings.Default.TextFilePath
                    + " to " + _textFilePath);
                Properties.Settings.Default.TextFilePath = _textFilePath;
            }

            if (Properties.Settings.Default.MetrologyFilePath != _metrologyFilePath)
            {
                App.LogEntry.AddEntry("Metrology file path changed from " + Properties.Settings.Default.MetrologyFilePath
                    + " to " + _metrologyFilePath);
                Properties.Settings.Default.MetrologyFilePath = _metrologyFilePath;
            }

            if (Properties.Settings.Default.CalDataTextFilePath != _calDataTextFilePath)
            {
                App.LogEntry.AddEntry("Cal Data Text file path changed from " + Properties.Settings.Default.CalDataTextFilePath
                    + " to " + _calDataTextFilePath);
                Properties.Settings.Default.CalDataTextFilePath = _calDataTextFilePath;
            }

            if ((double)Properties.Settings.Default.Saturation != _saturation)
            {
                App.LogEntry.AddEntry("Saturation changed from " + Properties.Settings.Default.Saturation
                    + " to " + _saturation);
                Properties.Settings.Default.Saturation = (decimal)_saturation;
            }
            if ((double)Properties.Settings.Default.Hue != _hue)
            {
                App.LogEntry.AddEntry("Hue changed from " + Properties.Settings.Default.Hue
                    + " to " + _hue);
                Properties.Settings.Default.Hue = _hue;
            }
            if ((double)Properties.Settings.Default.Gamma != _gamma)
            {
                App.LogEntry.AddEntry("Gamma changed from " + Properties.Settings.Default.Gamma
                    + " to " + _gamma);
                Properties.Settings.Default.Gamma = (decimal)_gamma;
            }
            if ((double)Properties.Settings.Default.WBConvergence != _wbConvergence)
            {
                App.LogEntry.AddEntry("White Balance Convergence changed from " + Properties.Settings.Default.WBConvergence
                    + " to " + _wbConvergence);
                Properties.Settings.Default.WBConvergence = (decimal)_wbConvergence;
            }
            if (Properties.Settings.Default.WBIncrement != _wbAdjust)
            {
                App.LogEntry.AddEntry("White Balance Adjust changed from " + Properties.Settings.Default.WBIncrement
                    + " to " + _wbAdjust);
                Properties.Settings.Default.WBIncrement = _wbAdjust;
            }
            if (Properties.Settings.Default.FrameRate != _frameRate)
            {
                App.LogEntry.AddEntry("FrameRate changed from " + Properties.Settings.Default.FrameRate
                    + " to " + _frameRate);
                Properties.Settings.Default.FrameRate = _frameRate;
            }

            if (Properties.Settings.Default.WBInitialize != _wbInitialize)
            {
                App.LogEntry.AddEntry("WB Initialize changed from " + Properties.Settings.Default.WBInitialize
                    + " to " + _wbInitialize);
                Properties.Settings.Default.WBInitialize = _wbInitialize;
            }
            if (Properties.Settings.Default.WBInitializeBlue != _wbInitialBlue)
            {
                App.LogEntry.AddEntry("WB Initialize Blue changed from " + Properties.Settings.Default.WBInitializeBlue
                    + " to " + _wbInitialBlue);
                Properties.Settings.Default.WBInitializeBlue = _wbInitialBlue;
            }
            if (Properties.Settings.Default.WBInitializeRed != _wbInitialRed)
            {
                App.LogEntry.AddEntry("WB Initialize Red changed from " + Properties.Settings.Default.WBInitializeRed
                    + " to " + _wbInitialRed);
                Properties.Settings.Default.WBInitializeRed = _wbInitialRed;
            }

            if (Properties.Settings.Default.CropLeft != _cropLeft)
            {
                if (_cropLeft + _cropWidth > (_connectedCamera == null ? 0 : _connectedCamera.ImageWidth))
                {
                    App.LogEntry.AddEntry("Crop Left not changed, Crop Left + Crop Width must be less than " +
                        (_connectedCamera == null ? 0 : _connectedCamera.ImageWidth));
                }
                else
                {
                    App.LogEntry.AddEntry("Crop Left changed from " + Properties.Settings.Default.CropLeft
                        + " to " + _cropLeft);
                    Properties.Settings.Default.CropLeft = _cropLeft;
                }
            }
            if (Properties.Settings.Default.CropTop != _cropTop)
            {
                if (_cropTop + _cropHeight > (_connectedCamera == null ? 0 : _connectedCamera.ImageHeight))
                {
                    App.LogEntry.AddEntry("Crop Top not changed, Crop Top + Crop Height must be less than " +
                        (_connectedCamera == null ? 0 : _connectedCamera.ImageHeight));
                }
                else
                {
                    App.LogEntry.AddEntry("Crop Top changed from " + Properties.Settings.Default.CropTop
                        + " to " + _cropTop);
                    Properties.Settings.Default.CropTop = _cropTop;
                }
            }
            if (Properties.Settings.Default.CropWidth != _cropWidth)
            {
                if (_cropLeft + _cropWidth > (_connectedCamera == null ? 0 : _connectedCamera.ImageWidth))
                {
                    App.LogEntry.AddEntry("Crop Width not changed, Crop Left + Crop Width must be less than " +
                        (_connectedCamera == null ? 0 : _connectedCamera.ImageWidth));
                }
                else
                {
                    App.LogEntry.AddEntry("Crop Width changed from " + Properties.Settings.Default.CropWidth
                        + " to " + _cropWidth);
                    Properties.Settings.Default.CropWidth = _cropWidth;
                }
            }
            if (Properties.Settings.Default.CropHeight != _cropHeight)
            {
                if (_cropTop + _cropHeight > (_connectedCamera == null ? 0 : _connectedCamera.ImageHeight))
                {
                    App.LogEntry.AddEntry("Crop Height not changed, Crop Top + Crop Height must be less than " +
                        (_connectedCamera == null ? 0 : _connectedCamera.ImageHeight));
                }
                else
                {
                    App.LogEntry.AddEntry("Crop Height changed from " + Properties.Settings.Default.CropHeight
                        + " to " + _cropHeight);
                    Properties.Settings.Default.CropHeight = _cropHeight;
                }
            }

            if (Properties.Settings.Default.CrossHairHorizontalOffsetPercent != _crossHairHorizontalOffset)
            {
                if (Math.Abs(_crossHairHorizontalOffset) > 100)
                {
                    App.LogEntry.AddEntry("Cross Hair Horizontal Offset Percent not changed, must be less than 100");
                }
                else
                {
                    App.LogEntry.AddEntry("Cross Hair Horizontal Offset Percent changed from " + Properties.Settings.Default.CrossHairHorizontalOffsetPercent
                        + " to " + _crossHairHorizontalOffset);
                    Properties.Settings.Default.CrossHairHorizontalOffsetPercent = _crossHairHorizontalOffset;
                    crossHairRefresh = true;
                }
            }
            if (Properties.Settings.Default.CrossHairVerticalOffsetPercent != _crossHairVerticalOffset)
            {
                if (Math.Abs(_crossHairVerticalOffset) > 100)
                {
                    App.LogEntry.AddEntry("Cross Hair Vertical Offset Percent not changed, must be less than 100");
                }
                else
                {
                    App.LogEntry.AddEntry("Cross Hair Vertical Offset Percent changed from " + Properties.Settings.Default.CrossHairVerticalOffsetPercent
                        + " to " + _crossHairVerticalOffset);
                    Properties.Settings.Default.CrossHairVerticalOffsetPercent = _crossHairVerticalOffset;
                    crossHairRefresh = true;
                }
            }
            if (Properties.Settings.Default.CrossHairBrush != _crossHairBrush)
            {
                App.LogEntry.AddEntry("Cross Hair Brush changed from " + Properties.Settings.Default.CrossHairBrush
                    + " to " + _crossHairBrush);
                Properties.Settings.Default.CrossHairBrush = _crossHairBrush;
                crossHairRefresh = true;
            }
            if (Properties.Settings.Default.Time != _time)
            {
                App.LogEntry.AddEntry("Time changed from " + Properties.Settings.Default.Time
                    + " to " + _time);
                Properties.Settings.Default.Time = _time;
            }
            if (Properties.Settings.Default.Temperature != _temperature)
            {
                App.LogEntry.AddEntry("Temperature changed from " + Properties.Settings.Default.Temperature
                    + " to " + _temperature);
                Properties.Settings.Default.Temperature = _temperature;
            }

            if (Properties.Settings.Default.ShutterTime != _shutterTimeMs)
            {
                App.LogEntry.AddEntry("ShutterTime changed from " + Properties.Settings.Default.ShutterTime
                    + " to " + _shutterTimeMs);
                Properties.Settings.Default.ShutterTime = _shutterTimeMs;
            }
            if (Properties.Settings.Default.ShutterTimeDiff != _shutterTimeDiffMs)
            {
                App.LogEntry.AddEntry("ShutterTime diff changed from " + Properties.Settings.Default.ShutterTimeDiff
                    + " to " + _shutterTimeDiffMs);
                Properties.Settings.Default.ShutterTimeDiff = _shutterTimeDiffMs;
            }
            if (GlobalVariables.LightStableSettings.BlueWBDiff != _wBBlueDiff)
            {
                App.LogEntry.AddEntry("WB Blue diff changed from " + GlobalVariables.LightStableSettings.BlueWBDiff
                    + " to " + _wBBlueDiff);
                GlobalVariables.LightStableSettings.BlueWBDiff = _wBBlueDiff;
                GlobalVariables.LightStableSettings.Save();
            }
            if (GlobalVariables.LightStableSettings.RedWBDiff != _wBRedDiff)
            {
                App.LogEntry.AddEntry("WB Red diff changed from " + GlobalVariables.LightStableSettings.RedWBDiff
                    + " to " + _wBRedDiff);
                GlobalVariables.LightStableSettings.RedWBDiff = _wBRedDiff;
                GlobalVariables.LightStableSettings.Save();
            }
            if (Properties.Settings.Default.CameraTempDiff != _cameraTempDiffMs)
            {
                App.LogEntry.AddEntry("CameraTemp diff changed from " + Properties.Settings.Default.CameraTempDiff
                    + " to " + _cameraTempDiffMs);
                Properties.Settings.Default.CameraTempDiff = _cameraTempDiffMs;
            }
            if (Properties.Settings.Default.ADiff != _aDiffMs)
            {
                App.LogEntry.AddEntry("A diff changed from " + Properties.Settings.Default.ADiff
                    + " to " + _aDiffMs);
                Properties.Settings.Default.ADiff = _aDiffMs;
            }
            if (Properties.Settings.Default.BDiff != _bDiffMs)
            {
                App.LogEntry.AddEntry("B diff changed from " + Properties.Settings.Default.BDiff
                    + " to " + _bDiffMs);
                Properties.Settings.Default.BDiff = _bDiffMs;
            }

            if (SelectedCalStoneList.Rows.Count == 0 || SelectedCalStoneList.Rows.Count >= 2)
            {
                List<int> dailyMonitorTargetList = new List<int>();
                foreach (DataRow row in SelectedCalStoneList.Rows)
                {
                    var found = TargetCalStoneList.Select("Target = '" +
                                    row["Target"] + "'");
                    dailyMonitorTargetList.Add(TargetCalStoneList.Rows.IndexOf(found[0]));
                }
                int[] dailyMonitorTargetArray = dailyMonitorTargetList.ToArray();
                string dailyMonitorTargets = String.Join(",", dailyMonitorTargetArray.Select(i => i.ToString()).ToArray());
                if (Properties.Settings.Default.DailyMonitorTargetList != dailyMonitorTargets)
                {
                    App.LogEntry.AddEntry("DailyMonitorTargetList changed from " + Properties.Settings.Default.DailyMonitorTargetList
                        + " to " + dailyMonitorTargets);
                    Properties.Settings.Default.DailyMonitorTargetList = dailyMonitorTargets;
                    shiftValuesRefresh = true;
                }
            }

            if (Properties.Settings.Default.LConv != _lconv)
            {
                App.LogEntry.AddEntry("L conv changed from " + Properties.Settings.Default.LConv
                    + " to " + _lconv);
                Properties.Settings.Default.LConv = _lconv;
                shiftValuesRefresh = true;
            }
            if (Properties.Settings.Default.AConv != _aconv)
            {
                App.LogEntry.AddEntry("A conv changed from " + Properties.Settings.Default.AConv
                    + " to " + _aconv);
                Properties.Settings.Default.AConv = _aconv;
                shiftValuesRefresh = true;
            }
            if (Properties.Settings.Default.BConv != _bconv)
            {
                App.LogEntry.AddEntry("B conv changed from " + Properties.Settings.Default.BConv
                    + " to " + _bconv);
                Properties.Settings.Default.BConv = _bconv;
                shiftValuesRefresh = true;
            }
            if (Properties.Settings.Default.Lshift != _lshift)
            {
                App.LogEntry.AddEntry("L shift changed from " + Properties.Settings.Default.Lshift
                    + " to " + _lshift);
                Properties.Settings.Default.Lshift = _lshift;
                shiftValuesRefresh = true;
            }
            if (Properties.Settings.Default.Ashift != _ashift)
            {
                App.LogEntry.AddEntry("A shift changed from " + Properties.Settings.Default.Ashift
                    + " to " + _ashift);
                Properties.Settings.Default.Ashift = _ashift;
                shiftValuesRefresh = true;
            }
            if (Properties.Settings.Default.Bshift != _bshift)
            {
                App.LogEntry.AddEntry("B shift changed from " + Properties.Settings.Default.Bshift
                    + " to " + _bshift);
                Properties.Settings.Default.Bshift = _bshift;
                shiftValuesRefresh = true;
            }

            if (Properties.Settings.Default.AutoOpenClose != _autoOpenClose)
            {
                App.LogEntry.AddEntry("Auto Open/Close changed from " + Properties.Settings.Default.AutoOpenClose
                    + " to " + _autoOpenClose);
                Properties.Settings.Default.AutoOpenClose = _autoOpenClose;
            }

            UpdateBoundaryTableHash((Window)param);

            if (crossHairRefresh)
                MainWindowViewModel.ControlVM.RefreshCrossHair();

            if (shiftValuesRefresh)
                MainWindowViewModel.ControlVM.InitializeReferenceStoneCalibrationTable();

            AppSettings.Save();

            ((Window)param).Close();
        }

        void UpdateBoundaryTableHash(Window window)
        {
            object obj = window.FindName("pwdHash");
            if (obj is System.Windows.Controls.PasswordBox)
            {
                System.Windows.Controls.PasswordBox pw = obj as System.Windows.Controls.PasswordBox;
                string decrypt = StringCipher.Decrypt(Properties.Settings.Default.BoundaryHash);
                if ( pw.Password.Length > 0 && decrypt != pw.Password )
                {
                    string encrypt = StringCipher.Encrypt(pw.Password);
                    Properties.Settings.Default.BoundaryHash = encrypt;
                    App.LogEntry.AddEntry("Boundary Hash updated");
                }
                
            }

        }

        void Close(object param)
        {
            ((Window)param).Close();
        }

    }
}
