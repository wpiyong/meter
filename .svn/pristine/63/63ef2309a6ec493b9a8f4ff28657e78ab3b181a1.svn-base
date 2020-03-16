using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace gColor.Model
{
    public enum Direction
    {
        CW = 0,
        CCW
    };

    public enum SERVO_MOTOR_MODEL
    {
        ST5Q = 22,
        ST5Plus = 26
    };
    
    public static class ST5
    {
        static Timer _statusTimer = null;
        static SerialPort _serialPort = null;
        static bool _motorEnabled = false;
        static int _stepsPerRevolution = Properties.Settings.Default.MotorStepsPerRev;
        static double _velocity;
        static double _continuousVelocity;
        static bool _driveBusy = false;
        static bool _driveConnected = false;
        static Object _statusLock = new Object();
        static SERVO_MOTOR_MODEL _model = SERVO_MOTOR_MODEL.ST5Q;


        public static event EventHandler DriveNotReady;
        public static event EventHandler DriveReady;

        public static int StepsPerRev
        {
            get
            {
                return _stepsPerRevolution;
            }
            set
            {
                _stepsPerRevolution = value;
            }
        }
        
        public static bool Connect(string interfaceName, int numRetries = 1)
        {
            for (int i = 0; i < numRetries; i++)
            {
                try
                {
                    App.LogEntry.AddEntry("Trying to connect to Stepper Motor");

                    if (_serialPort != null)
                        _serialPort.Close();

                    if (_statusTimer != null)
                        _statusTimer.Stop();

                    _serialPort = new System.IO.Ports.SerialPort(interfaceName, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
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
                    System.Threading.Thread.Sleep(100);
                    _serialPort.DiscardInBuffer();

                    //make sure in SCL mode
                    _serialPort.WriteLine("PM");
                    if (_serialPort.ReadLine().Substring(3) != "2")
                    {
                        _serialPort.WriteLine("PM2");
                        CheckAck();
                        App.LogEntry.AddEntry("ST5 : Please power cycle the Motor Drive and try connecting again", true);
                        return false;
                    }

                    _serialPort.WriteLine("PR5");
                    CheckAck();
                    _serialPort.WriteLine("SKD"); //Stop and Kill 
                    CheckAck();
                    _serialPort.WriteLine("MV");
                    string mvRead = _serialPort.ReadLine();
                    try
                    {
                        _model = (SERVO_MOTOR_MODEL)Convert.ToInt32(mvRead.Substring(4, 3)); ;
                    }
                    catch (Exception /*ex*/)
                    {
                        App.LogEntry.AddEntry("Unsupported Motor Drive", true);
                        return false;
                    }
                    App.LogEntry.AddEntry("ST5 Model/Revision: " + mvRead);
                    _serialPort.WriteLine("OP");
                    App.LogEntry.AddEntry("ST5 Option Board: " + _serialPort.ReadLine());

                    _serialPort.WriteLine("SC");
                    string response = _serialPort.ReadLine().Substring(3);
                    App.LogEntry.AddEntry("ST5 Status: 0x" + response);
                    _motorEnabled = (Convert.ToInt32(response.Substring(3, 1)) % 2) == 1;
                    _driveBusy = (Convert.ToInt32(response.Substring(2, 1)) % 2) != 0;

                    Velocity = Properties.Settings.Default.MotorVelocity;
                    ContinuousVelocity = Properties.Settings.Default.MotorContinuousVelocity;

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    ResetPosition();

                    _statusTimer = new Timer();
                    _statusTimer.Interval = 50;
                    _statusTimer.AutoReset = false;
                    _statusTimer.Elapsed += new ElapsedEventHandler(_statusTimer_Elapsed);
                    _driveConnected = true;

                    App.LogEntry.AddEntry("Connected to Stepper Motor");

                    break;
                }
                catch (Exception ex)
                {
                    if (i == numRetries - 1)
                        App.LogEntry.AddEntry("Could not connect to Stepper Motor : " + ex.Message, true);
                    else
                        App.LogEntry.AddEntry("Could not connect to Stepper Motor : " + ex.Message);

                    _driveConnected = false;
                    System.Threading.Thread.Sleep(500);
                }
            }

            return _driveConnected;
        }

        static void CheckAck()
        {
            var response = _serialPort.ReadLine();
            if (!response.Equals("%") && !response.Equals("*"))
            {
                throw new ApplicationException("The command was not acknowledged - please try again momentarily.");
            }
        }

        public static void ResetPosition()
        {
            //Apparently we have no encoder so this command is not necessary
            //if (_model == SERVO_MOTOR_MODEL.ST5Q)
            //{
            //    _serialPort.WriteLine("EP0");
            //    CheckAck();
            //}

            _serialPort.WriteLine("SP0");
            CheckAck();
        }



        public static void RotateByAngle(int noOfDegrees, Direction _direction)
        {
            int newPosition = (_stepsPerRevolution * noOfDegrees) / 360;

            if (!_motorEnabled)
                EnableMotor();

            if (_direction == Direction.CCW)
                newPosition *= -1;

            try
            {
                _serialPort.WriteLine("FL" + Convert.ToString(newPosition));
                CheckAck();
                //wait for motor to start
                do
                {
                    _serialPort.WriteLine("SC");
                    string response = _serialPort.ReadLine();
                    if (response.Length == 7)
                    {
                        response = response.Substring(3);
                        _driveBusy = (Convert.ToInt32(response.Substring(2, 1)) % 2) != 0;
                    }
                } while (!_driveBusy);
                OnDriveNotReady(null);//signal motor started
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry("ST5 RotateByAngle Failed : " + ex.Message);
                return;
            }

            _statusTimer.Start();
        }

        public static void ContinuousMotion(Direction _direction)
        {
            int dir = 1;

            if (!_motorEnabled)
                EnableMotor();

            if (_direction == Direction.CCW)
                dir = -1;

            try
            {
                _serialPort.WriteLine("DI" + dir.ToString());
                CheckAck();
                _serialPort.WriteLine("CJ");
                CheckAck();
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry("ST5 ContinuousMotion Failed : " + ex.Message);
                return;
            }

            _statusTimer.Start();
        }

        public static void StopMotor()
        {           
            try
            {
                lock (_statusLock)
                {
                    _serialPort.WriteLine("SKD");
                    CheckAck();
                }
            }
            catch(Exception ex)
            {
                App.LogEntry.AddEntry("ST5 StopMotor : " + ex.Message);
            }
        }
        

        public static void DisableMotor()
        {
            _serialPort.WriteLine("MD");
            try
            {
                CheckAck();
            }
            catch
            {
                throw;
            }
            _motorEnabled = false;
        }

        public static void EnableMotor()
        {
            _serialPort.WriteLine("ME");
            try
            {
                CheckAck();
            }
            catch
            {
                throw;
            }
            _motorEnabled = true;
        }

        public static bool DriveBusy
        {
            get
            {
                return _driveBusy;
            }
        }

        public static bool MotorConnected
        {
            get
            {
                return _driveConnected;
            }
        }
        

        
        public static double Velocity
        {
            get
            {
                _serialPort.WriteLine("VE");
                string response = _serialPort.ReadLine();
                if (response.Length > 3)
                    _velocity = Convert.ToDouble(response.Substring(3));
                else
                    _velocity = -1;
                return (_velocity);
            }

            set
            {
                if (Math.Abs(value) > 3)
                {
                    throw new ApplicationException("Velocity must not exceed 3 revolutions per second");
                }

                _serialPort.WriteLine("VE" + value.ToString());
                try
                {
                    CheckAck();
                }
                catch
                {
                    throw;
                }
                _velocity = value;
            }
        }

        public static double ContinuousVelocity
        {
            get
            {
                _serialPort.WriteLine("JS");
                string response = _serialPort.ReadLine();
                if (response.Length > 3)
                    _continuousVelocity = Convert.ToDouble(response.Substring(3));
                else
                    _continuousVelocity = -1;
                return (_continuousVelocity);
            }

            set
            {
                if (Math.Abs(value) > 3)
                {
                    throw new ApplicationException("Continuous velocity must not exceed 3 revolutions per second");
                }

                _serialPort.WriteLine("JS" + value.ToString());
                try
                {
                    CheckAck();
                }
                catch
                {
                    throw;
                }
                _continuousVelocity = value;
            }
        }

                

        static void _statusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int responseCode = 1;
            
            try
            {
                string response = "";
                lock (_statusLock)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.WriteLine("SC");
                    response = _serialPort.ReadLine();
                }
                if (response.Length == 7)
                {
                    response = response.Substring(3);
                    responseCode = (Convert.ToInt32(response.Substring(2, 1)) % 2);
                }
            }
            catch(Exception /*ex*/)
            {
                // if the check fails, we will try again
                _statusTimer.Start();
                return;
            }

            if (responseCode != 0)
            {
                if (!_driveBusy)
                {
                    _driveBusy = true;
                    OnDriveNotReady(null);
                }
                _statusTimer.Start();
            }
            else
            {
                if (_driveBusy)
                {
                    //when polling very rapidly, the timer elapsed will overlap
                    //so don't keep indicating the motor has stopped
                    _driveBusy = false;
                    OnDriveReady(null);
                }
            }
        }

        static void OnDriveNotReady(EventArgs e)
        {
            if (DriveNotReady != null)
                DriveNotReady(null, e);
        }

        static void OnDriveReady(EventArgs e)
        {
            if (DriveReady != null)
                DriveReady(null, e);
        }
        
    }
}
