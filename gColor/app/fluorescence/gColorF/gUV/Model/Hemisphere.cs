﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;


namespace gUV.Model
{
    public static class Hemisphere
    {
        static bool _motorConnected = false;
        static SerialPort _serialPort = null;
        static Timer _inputStatusPollTimer, _motorConnectTimer;
        static Object _statusLock = new Object();
        static SERVO_MOTOR_MODEL _model = SERVO_MOTOR_MODEL.ST5Q;
        static byte _errorCount = 0;
        static List<string> _openMotorCommands = new List<string>();
        static List<string> _closeMotorCommands = new List<string>();

        static object _commandActive = new object();
        public static bool CommandActive { get; private set; }

        static Timer _uvIntensityMonitorConnectTimer;
        static SerialPort _uvSerialPort = null;
        static bool _uvControllerConnected = false;
        static SerialPort _arduinoSerialPort = null;
        static bool _arduinoConnected = false;
        static int _arduinoVersion = 0;

        static volatile bool _abortConectMotor = false;
        
        public static void StartConnectMotor()
        {
            if (LoadHemisphereMotorProfile())
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Connecting to hemisphere motor...")));

                _abortConectMotor = false;
                _motorConnectTimer = new Timer();
                _motorConnectTimer.Interval = 200;
                _motorConnectTimer.AutoReset = false;
                _motorConnectTimer.Elapsed += new ElapsedEventHandler(_motorConnectTimer_Elapsed);
                _motorConnectTimer.Start();
            }
            else
                Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Failed to load motor profile")));
        }

        public static void CancelConnectMotor()
        {
            _abortConectMotor = true;
        }

        public static void StartConnectUVSensor()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Connecting to UV monitor controller...")));

            _uvIntensityMonitorConnectTimer = new Timer();
            _uvIntensityMonitorConnectTimer.Interval = 1000;
            _uvIntensityMonitorConnectTimer.AutoReset = false;
            _uvIntensityMonitorConnectTimer.Elapsed += new ElapsedEventHandler(_uvIntensityMonitorConnectTimer_Elapsed);
            _uvIntensityMonitorConnectTimer.Start();
        }

        static bool LoadHemisphereMotorProfile()
        {
            bool result = false;
            try
            {
                _openMotorCommands = new List<string>();
                _closeMotorCommands = new List<string>();
                int state = 0;

                using (var reader = new StreamReader("HemisphereMotorProfile.txt"))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.ToUpper() == "OPENPROFILE")
                            state = 1;
                        else if (line.ToUpper() == "CLOSEPROFILE")
                            state = 2;
                        else if (line.Trim().Length > 0)
                        {
                            switch (state)
                            {
                                case 1:
                                    _openMotorCommands.Add(line);
                                    break;
                                case 2:
                                    _closeMotorCommands.Add(line);
                                    break;
                            }
                        }
                    }
                }


                if (_openMotorCommands.Count == 0 || _closeMotorCommands.Count == 0)
                {
                    throw new Exception("Bad HemisphereMotorProfile.txt format");
                }

                result = true;
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry(ex.Message, true);
                result = false;
            }

            return result;
        }


        public static bool UVcommand(bool open)
        {
            bool result = false;
            try
            {
                if (!_arduinoConnected)
                    throw new Exception("Arduino not connected");

                for (int i = 0; i < 3; i++)
                {
                    _arduinoSerialPort.DiscardOutBuffer();
                    _arduinoSerialPort.DiscardInBuffer();

                    byte[] _openShutter = new byte[] { (byte)'O' };
                    byte[] _closeShutter = new byte[] { (byte)'C' };

                    if (open)
                        _arduinoSerialPort.Write(_openShutter, 0, _openShutter.Length);
                else
                        _arduinoSerialPort.Write(_closeShutter, 0, _closeShutter.Length);

                    result = _arduinoSerialPort.ReadLine()[0] == 'A';

                    if (result)
                        break;
                    else
                        System.Diagnostics.Debug.WriteLine("no arduino ack");
                }

            }
            catch(Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() => App.LogEntry.AddEntry(ex.Message, true)));
            }

            return result;
        }

        public static void Open(Object sender, DoWorkEventArgs e)
        {
            if (!_motorConnected) return;
            if (IsOpen) return;

            if (e != null) e.Result = false;

            if (System.Threading.Monitor.TryEnter(_commandActive, 12000))
            {
            try
            {
                    CommandActive = true;
                foreach (var command in _openMotorCommands)
                {
                    _serialPort.WriteLine(command);
                }
                for (int i = 0; i < 50; i++)
                {
                    if (!IsOpen)
                        System.Threading.Thread.Sleep(200);
                    else
                        break;
                }
                if (e != null) e.Result = IsOpen;
            }
                catch (Exception ex)
            {
                App.LogEntry.AddEntry(ex.Message, true);
                throw;
            }
                finally
                {
                    System.Threading.Monitor.Exit(_commandActive);
                    CommandActive = false;
                }
            }
        }

        public static void Close(Object sender, DoWorkEventArgs e)
        {
            if (!_motorConnected) return;
            if (IsClosed) return;

            if (e != null) e.Result = false;

            if (System.Threading.Monitor.TryEnter(_commandActive, 12000))
            {
            try
            {
                    CommandActive = true;
                foreach (var command in _closeMotorCommands)
                {
                    _serialPort.WriteLine(command);
                }
                for (int i = 0; i < 50; i++)
                {
                    if (!IsClosed)
                        System.Threading.Thread.Sleep(200);
                    else
                        break;
                }
                if (e != null) e.Result = IsClosed;
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry(ex.Message, true);
                throw;
            }
                finally
                {
                    System.Threading.Monitor.Exit(_commandActive);
                    CommandActive = false;
                }
            }
        }

        static void _motorConnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            bool _hemiMotorFound = false;

            foreach (string port in ports)
            {
                try
                {
                    if (_serialPort != null)
                        _serialPort.Close();

                    _serialPort = new SerialPort(port, 19200, Parity.None, 8, StopBits.One);
                    _serialPort.DtrEnable = true;
                    _serialPort.NewLine = "\r";
                    _serialPort.Encoding = System.Text.Encoding.ASCII;
                    _serialPort.ReadTimeout = 2000;
                    _serialPort.WriteTimeout = 2000;

                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    //write some nulls
                    _serialPort.Write("\0");
                    _serialPort.Write("\0");
                    _serialPort.Write("\0");
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    _serialPort.WriteLine("MV");
                    string mvRead = _serialPort.ReadLine();
                    try
                    {
                        _model = (SERVO_MOTOR_MODEL)Convert.ToInt32(mvRead.Substring(4, 3)); ;
                    }
                    catch
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                            App.LogEntry.AddEntry("Unsupported Hemisphere Motor Drive", true)));
                    }

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Hemisphere  Model/Revision: " + mvRead)));
                    _serialPort.WriteLine("OP");
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Hemisphere Option Board: " + _serialPort.ReadLine())));

                    _serialPort.WriteLine("SC");
                    string response = _serialPort.ReadLine().Substring(3);
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                        App.LogEntry.AddEntry("Hemisphere Status: 0x" + response)));

                    _serialPort.WriteLine("BR");//baud rate 2 = 19200
                    response = _serialPort.ReadLine().Substring(3);

                    if (response == "2")
                    {
                        _hemiMotorFound = true;
                        break;
                    }
                }
                catch
                {
                    if (_serialPort != null)
                        _serialPort.Close();
                }

            }

            if (!_hemiMotorFound)
            {
                if (_serialPort != null)
                    _serialPort.Close();
                if (!_abortConectMotor)
                    _motorConnectTimer.Start();
                return;
            }

            _motorConnected = true;
            Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));

            string status = "";
            _serialPort.DiscardInBuffer();
            _serialPort.WriteLine("IS");
            status = _serialPort.ReadLine();
            var hState = 0;
            if (status.Length == 11)
            {
                status = status.Substring(3);
                hState = (Convert.ToByte(status.Substring(5, 3), 2));
            }

            _isOpen = (hState & 0x5) == 0x5;
            _isClosed = (hState & 0x6) == 0x6;

            _inputStatusPollTimer = new Timer();
            _inputStatusPollTimer.Interval = 200;
            _inputStatusPollTimer.AutoReset = false;
            _inputStatusPollTimer.Elapsed += new ElapsedEventHandler(_inputStatusPollTimer_Elapsed);
            _inputStatusPollTimer.Start();

            //close on connect
            Close(null, null);
        }

        public static bool HemisphereMotorConnected
        {
            get { return _motorConnected; }
        }
        public static bool ArduinoConnected
        {
            get { return _arduinoConnected; }
        }
        public static int ArduinoVersion
        {
            get { return _arduinoVersion; }
        }

        static volatile bool _isOpen, _isClosed;
        public static bool IsOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                if (_isOpen != value)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
                }

                _isOpen = value;
            }
        }

        public static bool IsClosed
        {
            get
            {
                return _isClosed;
            }
            set
            {
                if (_isClosed != value)
                    Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));

                _isClosed = value;
            }
        }


        static void _inputStatusPollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string response = "";
                lock (_statusLock)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.WriteLine("IS");
                    response = _serialPort.ReadLine();
                }
                
                byte hState = 0;
                if (response.Length == 11)
                {
                    response = response.Substring(3);
                    hState = (Convert.ToByte(response.Substring(6, 2), 2));
                }
                IsOpen = (hState & 0x1) == 0x1;
                IsClosed = (hState & 0x2) == 0x2;
                
                _inputStatusPollTimer.Start();
                _errorCount = 0;
            }
            catch 
            {
                if (_errorCount++ > 10) // 10 consecutive errors
                {
                    _errorCount = 0;
                    _motorConnected = false;
                    //_motorConnectTimer.Start();
                }
                else
                    _inputStatusPollTimer.Start();
            }
            
            
        }


        static void _uvIntensityMonitorConnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            _uvControllerConnected = false;

            foreach (string port in ports)
            {
                try
                {
                    if (_uvSerialPort != null)
                        _uvSerialPort.Close();

                    _uvSerialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                    _uvSerialPort.Encoding = System.Text.Encoding.ASCII;
                    _uvSerialPort.NewLine = "\r\n";
                    _uvSerialPort.DtrEnable = true;
                    _uvSerialPort.RtsEnable = true;
                    _uvSerialPort.ReadTimeout = 2000;
                    _uvSerialPort.WriteTimeout = 2000;

                    if (!_uvSerialPort.IsOpen)
                        _uvSerialPort.Open();

                    _uvSerialPort.DiscardInBuffer();
                    _uvSerialPort.DiscardOutBuffer();

                    byte[] buf = new byte[1];
                    buf[0] = (byte)'U';
                    _uvSerialPort.Write(buf, 0, 1);
                    
                    try
                    {
                        string id = _uvSerialPort.ReadLine();
                        if (id == "Micro")
                        {
                            _uvControllerConnected = true;
                            break;
                        }
                    }
                    catch(Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        if (_uvSerialPort != null)
                            _uvSerialPort.Close();
                    }

                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    if (_uvSerialPort != null)
                        _uvSerialPort.Close();
                }

            }

            if (!_uvControllerConnected)
            {
                Application.Current.Dispatcher.Invoke((Action)(() => 
                    App.LogEntry.AddEntry("Could not connect to UV monitor controller.", true)));
                if (_uvSerialPort != null)
                    _uvSerialPort.Close();
                //_uvIntensityMonitorConnectTimer.Start();
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Connected to UV monitor controller.")));
                _arduinoConnect();
            }

            
        }

        static void _arduinoConnect()
        {
            string[] ports = SerialPort.GetPortNames();
            _arduinoConnected = false;

            foreach (string port in ports)
            {
                try
                {
                    if (_arduinoSerialPort != null)
                        _arduinoSerialPort.Close();

                    _arduinoSerialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                    _arduinoSerialPort.Encoding = System.Text.Encoding.ASCII;
                    _arduinoSerialPort.NewLine = "\r\n";
                    _arduinoSerialPort.ReadTimeout = 2000;
                    _arduinoSerialPort.WriteTimeout = 2000;

                    if (!_arduinoSerialPort.IsOpen)
                        _arduinoSerialPort.Open();

                    _arduinoSerialPort.DiscardInBuffer();
                    _arduinoSerialPort.DiscardOutBuffer();

                    byte[] buf = new byte[1];
                    buf[0] = (byte)'U';
                    _arduinoSerialPort.Write(buf, 0, 1);

                    try
                    {
                        string id = _arduinoSerialPort.ReadLine();
                        if (id == "Arduino")
                        {
                            _arduinoConnected = true;
                            _arduinoVersion = 1;
                            break;
                        }
                        else if (id == "Arduino2")
                        {
                            if (!LightControl.SetMainLightCurrent(GlobalVariables.fluorescenceSettings.MainLightSetCurrent))
                                throw new Exception("Could not set LED driver channel 2 ");

                            _arduinoConnected = true;
                            _arduinoVersion = 2;
                            
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        if (_arduinoSerialPort != null)
                            _arduinoSerialPort.Close();
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    if (_arduinoSerialPort != null)
                        _arduinoSerialPort.Close();
                }

            }

            if (!_arduinoConnected)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Could not connect to Arduino controller.", true)));
                if (_arduinoSerialPort != null)
                    _arduinoSerialPort.Close();
            }
            else
            {                   
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Connected to Arduino controller.")));
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
            }

            
        }

        
        public static int GetUVIntensity()
        {
            int result = -1;
            try
            {
                if (!_uvControllerConnected)
                    throw new Exception("UV monitor not connected");

                _uvSerialPort.DiscardOutBuffer();
                _uvSerialPort.DiscardInBuffer();

                byte[] buf = new byte[1];
                buf[0] = (byte)'G';
                _uvSerialPort.Write(buf, 0, 1);

                result = Convert.ToUInt16(_uvSerialPort.ReadLine());

                Application.Current.Dispatcher.Invoke((Action)(() =>
                                                App.LogEntry.AddEntry("UV Intensity = " + result)));
                
            }
            catch(Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() => App.LogEntry.AddEntry(ex.Message, true)));
            }

            return result;
        }

        public static bool EnableDisableTrigger(bool enable)
        {
            bool result = false;
            try
            {
                if (!_arduinoConnected)
                    throw new Exception("Arduino not connected");

                for (int i = 0; i < 3; i++)
                {
                    _arduinoSerialPort.DiscardOutBuffer();
                    _arduinoSerialPort.DiscardInBuffer();

                    byte[] enableTrigger = new byte[] { (byte)'B'};
                    byte[] disableTrigger = new byte[] { (byte)'E' };

                    if (enable)
                        _arduinoSerialPort.Write(enableTrigger, 0, enableTrigger.Length);
                    else
                        _arduinoSerialPort.Write(disableTrigger, 0, disableTrigger.Length);

                    result = _arduinoSerialPort.ReadLine()[0] == 'A';

                    if (result)
                        break;
                }

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() => App.LogEntry.AddEntry(ex.Message, true)));
            }

            return result;
        }

        public static bool EnableDisableRingLight(bool enable)
        {
            bool result = false;
            try
            {
                if (!_arduinoConnected)
                    throw new Exception("Arduino not connected");

                for (int i = 0; i < 3; i++)
                {
                    _arduinoSerialPort.DiscardOutBuffer();
                    _arduinoSerialPort.DiscardInBuffer();

                    byte[] enableRingLight = new byte[] { (byte)'R' };
                    byte[] disableRingLight = new byte[] { (byte)'D' };

                    if (enable)
                        _arduinoSerialPort.Write(enableRingLight, 0, enableRingLight.Length);
                    else
                        _arduinoSerialPort.Write(disableRingLight, 0, disableRingLight.Length);

                    result = _arduinoSerialPort.ReadLine()[0] == 'A';

                    if (result)
                        break;
                }

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() => App.LogEntry.AddEntry(ex.Message, true)));
            }

            return result;
        }
    }
}
