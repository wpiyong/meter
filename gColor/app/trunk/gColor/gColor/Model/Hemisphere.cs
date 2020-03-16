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

namespace gColor.Model
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

        static Hemisphere()
        {
            CommandActive = false;
            if (Properties.Settings.Default.AutoOpenClose && LoadHemisphereMotorProfile())
            {
                _motorConnectTimer = new Timer();
                _motorConnectTimer.Interval = 200;
                _motorConnectTimer.AutoReset = false;
                _motorConnectTimer.Elapsed += new ElapsedEventHandler(_motorConnectTimer_Elapsed);
                _motorConnectTimer.Start();
            }
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

                    _serialPort.WriteLine("BR");
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
        
    }
}
