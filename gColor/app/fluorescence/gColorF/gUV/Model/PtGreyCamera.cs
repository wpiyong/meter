//using FlyCapture2Managed;
//using FlyCapture2Managed.Gui;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using gUV.View;


namespace gUV.Model
{
    enum TriggerType
    {
        Software,
        Hardware, 
        Unknown
    };

    enum TriggerMode
    {
        Off,
        On
    };

    enum StreamBufferCountMode
    {
        Auto,
        Manual
    };

    public struct PtGreyCameraImage
    {
        public ManagedImage raw;
        public ManagedImage converted;
        public IManagedCamera cam;
    }

    public class PtGreyCamera : Camera
    {
        // todo: spinnaker 
        //CameraControlDialog m_ctldlg;
        ManagedSystem system = null;
        IManagedCamera camera;
        INodeMap nodeMap = null;
        string sensorType = Properties.Settings.Default.Sensor;

        SafeQueue<BitmapSource> imagesQueue;
        AutoResetEvent stopCameraEvent;
        volatile bool _imageCaptureThreadRunning;
        volatile bool _dropFrames = true;

        double _normalShutterTime;
        double captRate;
        DateTime prevTime = DateTime.Now;

        public override event EventHandler<ImageEventArgs> ImageChanged;

        protected virtual void RaiseImageChangedEvent(ImageEventArgs eventArgs)
        {
            ImageChanged?.Invoke(this, eventArgs);
        }

        public override double CaptRate
        {
            get
            {
                //todo: need to check the result
                return captRate;
            }
        }

        public override double Framerate
        {
            get
            {
                //todo: need to check the result
                return Convert.ToDouble(GetPropertyValue("FrameRate"));
            }
            set
            {
                SetProprtyAutomaticSetting("FrameRate", false);
                SetAbsolutePropertyValue("FrameRate", Convert.ToString(value));
            }
        }

        public override bool Connect()
        {
            bool result = false;
            try
            {
                // todo: spinnaker
                //CameraSelectionDialog m_selDlg = new CameraSelectionDialog();
                //if (true)//m_selDlg.ShowModal())
                //{
                //    //todo: spinnaker
                //    //ManagedPGRGuid[] guids = m_selDlg.GetSelectedCameraGuids();
                //    //if (guids.Length == 0)
                //    //{
                //    //    //MessageBox.Show("Please select a camera", "No camera selected");
                //    //    return false;
                //    //}

                //    camera = new ManagedCamera();
                //    //m_ctldlg = new CameraControlDialog();
                //    camera.Connect(guids[0]);

                //    CameraInfo ci = camera.GetCameraInfo();
                //    SerialNumber = ci.serialNumber;
                //    FirmwareVersion = ci.firmwareVersion;

                //    stopCameraEvent = new AutoResetEvent(false);
                //    _imageCaptureThreadRunning = false;

                //    //initialise settings
                //    InitializeSettings();
                //    InitializeSettingsWB();

                //    result = true;
                //}
                system = new ManagedSystem();
                IList<IManagedCamera> camList = system.GetCameras();

                if (camList.Count != 1)
                {
                    int count = camList.Count;
                    foreach (IManagedCamera mc in camList)
                        mc.Dispose();

                    // Clear camera list before releasing system
                    camList.Clear();

                    // Release system
                    system.Dispose();
                    throw new Exception("Only one camera should be connected, but found " + count);
                }

                camera = camList[0];
                // Initialize camera
                camera.Init();

                SerialNumber = Convert.ToUInt32(camera.DeviceSerialNumber);
                FirmwareVersion = camera.DeviceFirmwareVersion;

                stopCameraEvent = new AutoResetEvent(false);
                _imageCaptureThreadRunning = false;

                // Retrieve GenICam nodemap
                nodeMap = camera.GetNodeMap();

                //initialise settings
                InitializeSettings();
                InitializeSettingsWB();

                result = true;
            }
            catch (Exception /*ex*/)
            {
                //App.LogEntry.AddEntry("Failed to Connect to Point Grey Camera : " + ex.Message);
                result = false;
            }

            return result;
        }

        public override void StartCapture(BackgroundWorker bw)
        {
            ManagedImage m_image = new ManagedImage();
            ManagedImage m_converted = new ManagedImage();

            StartImageCaptureThread();

            PtGreyCameraImage helper = new PtGreyCameraImage();
            helper.converted = m_converted;
            helper.raw = m_image;
            helper.cam = camera;

            bw.RunWorkerAsync(helper);
        }

        void StartImageCaptureThread()
        {
            StopImageCaptureThread();

            imagesQueue = new SafeQueue<BitmapSource>();
            Task.Run(() => QueueImages(camera));

            while (!_imageCaptureThreadRunning)
                Thread.Sleep(20);
        }

        void StopImageCaptureThread()
        {
            if (!_imageCaptureThreadRunning) 
                return;

            stopCameraEvent.Set();
            int count = 0;
            while (_imageCaptureThreadRunning)
            {
                Thread.Sleep(100);
                count++;
                if(count > 50)
                {
                    break;
                }
            }
                

            Thread.Sleep(200);//time for camera to stop streaming
        }

        public override void RestartCapture()
        {
            if (!_imageCaptureThreadRunning)
                StartImageCaptureThread();
        }

        void QueueImages(IManagedCamera cam)
        {
            //ManagedImage managedImage = new ManagedImage();
            ManagedImage managedImageConverted = new ManagedImage();
            Debug.WriteLine("Camera capture starting");

            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                //camera.StartCapture();
                cam.BeginAcquisition();
                _imageCaptureThreadRunning = true;

                while (true)
                {
                    try
                    {
                        //todo: spinnaker way
                        //cam.RetrieveBuffer(managedImage);
                        IManagedImage managedImage = cam.GetNextImage();

                        DateTime currTime = DateTime.Now;

                        TimeSpan ts = currTime - prevTime;
                        prevTime = currTime;
                        captRate = 1000.0 / (int)ts.TotalMilliseconds;
                        captRate = Math.Truncate(captRate * 100) / 100;

                        if (managedImage.IsIncomplete)
                        {
                            Debug.WriteLine("Image incomplete with image status {0}...", managedImage.ImageStatus);
                        }
                        else
                        { 
                            managedImage.ConvertToBitmapSource(PixelFormatEnums.BGR8, managedImageConverted);
                            BitmapSource image = managedImageConverted.bitmapsource.Clone();
                            image.Freeze();
                            RaiseImageChangedEvent(new ImageEventArgs(image));
                            if (!imagesQueue.TryEnqueue(image, 200, _dropFrames))
                            {
                                Debug.WriteLine("Failed to queue");
                            }
                        }
                        managedImage.Release();
                    }
                    catch (SpinnakerException fex)
                    {
                        //todo: need to check the error code ImageConsistencyError could not been found in spinnaker
                        if (fex.ErrorCode != Error.SPINNAKER_ERR_TIMEOUT) // && fex.ErrorCode != ErrorType.ImageConsistencyError)
                            throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        throw;
                    }

                    if (stopCameraEvent.WaitOne(50))
                        break;

                }

                //camera.StopCapture();
                cam.EndAcquisition();

                Debug.WriteLine("Camera capture stopped" );
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                                App.LogEntry.AddEntry("Camera capture thread exception:" + ex.Message)));
            }
            finally
            {
                _imageCaptureThreadRunning = false;
            }
        }

        public override BitmapSource GetImage(DoWorkEventArgs e)
        {
            BitmapSource source = null;
            BitmapSource deQueueSource = null;

            int timer = 0;
            while (!imagesQueue.TryDequeue(out deQueueSource, 100))
            {
                Thread.Sleep(100);
                if (timer++ > 150)
                    throw new ApplicationException("Timed out waiting for image");
            }

            if (deQueueSource == null)
                throw new ApplicationException("Bad queue data");


            if ((Properties.Settings.Default.CropHeight == 0 && Properties.Settings.Default.CropWidth == 0) ||
                 (Properties.Settings.Default.CropHeight + Properties.Settings.Default.CropTop > deQueueSource.Height) ||
                 (Properties.Settings.Default.CropWidth + Properties.Settings.Default.CropLeft > deQueueSource.Width)
                )
            {
                source = deQueueSource;
                CropImageWidth = 0;
                CropImageHeight = 0;
            }
            else
            {
                source = new CroppedBitmap(deQueueSource,
                    new System.Windows.Int32Rect((int)Properties.Settings.Default.CropLeft,
                                                    (int)Properties.Settings.Default.CropTop,
                                                    (int)(Properties.Settings.Default.CropWidth == 0 ?
                                                            deQueueSource.Width : Properties.Settings.Default.CropWidth),
                                                    (int)(Properties.Settings.Default.CropHeight == 0 ?
                                                            deQueueSource.Height : Properties.Settings.Default.CropHeight)));
                CropImageWidth = source.Width;
                CropImageHeight = source.Height;
            }
            source.Freeze();
            ImageWidth = deQueueSource.Width;
            ImageHeight = deQueueSource.Height;
            
            return source;
        }


        public override void DisConnect()
        {
            try
            {
                if (camera != null)
                {
                    StopImageCaptureThread();
                    camera.EndAcquisition();
                    camera.DeInit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PtGrey Disconnect : " + ex.Message);
            }
            finally
            {

            }
        }

        public override void EditCameraSettings()
        {
            //bool restartCapture = _imageCaptureThreadRunning;

            //try
            //{
            //    StopImageCaptureThread();

            //    //todo: implement PropertyGridControl
            //    //if (m_ctldlg.IsVisible())
            //    //{
            //    //    m_ctldlg.Disconnect();
            //    //    m_ctldlg.Hide();
            //    //}
            //    //else
            //    //{
            //    //    m_ctldlg.Connect(camera);
            //    //    m_ctldlg.Show();
            //    //}
            //}
            //finally
            //{
            //    if (restartCapture)
            //        StartImageCaptureThread();
            //}

            CameraSettings camDlg = new CameraSettings(camera);
            camDlg.ShowDialog();
        }


        void SetCameraVideoModeAndFrameRate()
        {
            bool restartCapture = _imageCaptureThreadRunning;
            StopImageCaptureThread();

            //try
            //{
            //    if (FirmwareVersion[0] == '2')
            //    {
            //        const Mode k_fmt7Mode = Mode.Mode1;
            //        const PixelFormat k_fmt7PixelFormat = PixelFormat.PixelFormatRgb8;

            //        Format7ImageSettings fmt7ImageSettings = new Format7ImageSettings();
            //        fmt7ImageSettings.mode = k_fmt7Mode;
            //        fmt7ImageSettings.offsetX = 80;// 560;
            //        fmt7ImageSettings.offsetY = 60;// 420;
            //        fmt7ImageSettings.width = 800;
            //        fmt7ImageSettings.height = 600;
            //        fmt7ImageSettings.pixelFormat = k_fmt7PixelFormat;

            //        // Validate the settings to make sure that they are valid
            //        bool settingsValid = false;
            //        Format7PacketInfo fmt7PacketInfo = camera.ValidateFormat7Settings(
            //            fmt7ImageSettings,
            //            ref settingsValid);

            //        if (settingsValid != true)
            //        {
            //            // Settings are not valid
            //            throw new Exception("Invalid resolution settings");
            //        }

            //        //Format7ImageSettings fmt7CamImageSettings = new Format7ImageSettings();
            //        //uint currPacketSize = 0;
            //        //float percentage = 0.0f;

            //        //camera.GetFormat7Configuration(fmt7CamImageSettings, ref currPacketSize, ref percentage);

            //        ////bool supported = false;
            //        ////Format7Info fmt7Info = camera.GetFormat7Info(k_fmt7Mode, ref supported);
            //        //if (fmt7ImageSettings.pixelFormat == fmt7CamImageSettings.pixelFormat
            //        //    && fmt7ImageSettings.mode == fmt7CamImageSettings.mode)
            //        //{
            //        //    if (currPacketSize >= fmt7PacketInfo.maxBytesPerPacket)
            //        //        currPacketSize = fmt7PacketInfo.maxBytesPerPacket;
            //        //}
            //        //else
            //        //    currPacketSize = fmt7PacketInfo.maxBytesPerPacket;

            //        // Set the settings to the camera
            //        camera.SetFormat7Configuration(
            //           fmt7ImageSettings,
            //           6256);//8648);
            //    }
            //    else
            //    {
            //        camera.SetVideoModeAndFrameRate(VideoMode.VideoMode800x600Rgb, FrameRate.FrameRate30);
            //    }
            //}
            //catch (SpinnakerException /*ex*/)
            //{
            //    throw;
            //}

            try
            {
                //camera.SetVideoModeAndFrameRate(newVideoMode, newFrameRate);
                // Set acquisition mode to continuous
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (node retrieval). Aborting...\n");
                    restartCapture = false;
                }

                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionMode.IsReadable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    restartCapture = false;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                // set framerate 30
                SetProprtyEnabledSetting("FrameRate", true);
                SetProprtyAutomaticSetting("FrameRate", false);
                SetAbsolutePropertyValue("FrameRate", Properties.Settings.Default.FrameRate.ToString());
            }
            catch (SpinnakerException /*ex*/)
            {
                throw;
            }

            //if (!SetFrameRate((double)Properties.Settings.Default.FrameRate)) // set frame rate 30 frame per second
            //{
            //    restartCapture = false;
            //}

            if (restartCapture)
                StartImageCaptureThread();
        }

        public bool SetFrameRate(double value)
        {
            bool result = false;

            try
            {
                camera.AcquisitionFrameRate.Value = value;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        //void SetAbsolutePropertyValue(PropertyType property, float newValue)
        //{
        //    CameraProperty camProp = camera.GetProperty(property);
        //    CameraPropertyInfo propInfo = camera.GetPropertyInfo(property);

        //    if (!camProp.autoManualMode && propInfo.manualSupported && propInfo.absValSupported)
        //    {
        //        float difference = camProp.absValue - newValue;
        //        if (difference != 0)
        //        {
        //            // The brightness abs register sometimes starts drifting
        //            // due to a rounding error between the camera and the
        //            // actual value being held by the adjustment. To prevent
        //            // this, only apply the change to the camera if the
        //            // difference is greater than a specified amount.

        //            // Check if the difference is greater than 0.005f. 
        //            if (property != PropertyType.Brightness ||
        //                Math.Abs(difference) > 0.005f)
        //            {
        //                camProp.absControl = true;
        //                camProp.absValue = newValue;
        //                camera.SetProperty(camProp);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        throw new ApplicationException("Trying to set a property that cannot be adjusted");
        //    }
        //}
        public void SetAbsolutePropertyValue(string property, string newValue)
        {
            try
            {
                if (property == "Hue")
                {
                    IFloat hue = nodeMap.GetNode<IFloat>("Hue");
                    hue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Gamma")
                {
                    IFloat gamma = nodeMap.GetNode<IFloat>("Gamma");
                    gamma.Value = Convert.ToDouble(newValue);
                }
                else if (property == "OffsetX")
                {
                    IInteger x = nodeMap.GetNode<IInteger>("OffsetX");
                    x.Value = Convert.ToInt32(newValue);
                }
                else if (property == "OffsetY")
                {
                    IInteger y = nodeMap.GetNode<IInteger>("OffsetY");
                    y.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Width")
                {
                    IInteger width = nodeMap.GetNode<IInteger>("Width");
                    width.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Height")
                {
                    IInteger height = nodeMap.GetNode<IInteger>("Height");
                    height.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Gain")
                {
                    IEnum gainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                    gainAuto.Value = "Off";

                    IFloat gainValue = nodeMap.GetNode<IFloat>("Gain");
                    gainValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Saturation")
                {
                    IEnum saturationAuto = nodeMap.GetNode<IEnum>("SaturationAuto");
                    saturationAuto.Value = "Off";

                    IFloat saturationValue = nodeMap.GetNode<IFloat>("Saturation");
                    saturationValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Binning")
                {
                    IInteger binningValue = nodeMap.GetNode<IInteger>("BinningVertical");
                    binningValue.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Shutter")
                {
                    IFloat shutterValue = nodeMap.GetNode<IFloat>("ExposureTime");
                    shutterValue.Value = Convert.ToDouble(newValue) * 1000;
                }
                else if (property == "FrameRate")
                {
                    IEnum frameRateAuto = nodeMap.GetNode<IEnum>("AcquisitionFrameRateAuto");
                    frameRateAuto.Value = "Off";

                    IFloat frameRateValue = nodeMap.GetNode<IFloat>("AcquisitionFrameRate");
                    frameRateValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "PixelFormat")
                {
                    IEnum pixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    IEnumEntry pixelFormatItem = pixelFormat.GetEntryByName(newValue);

                    if (pixelFormatItem?.IsReadable == true)
                    {
                        pixelFormat.Value = pixelFormatItem.Symbolic;
                    }
                }
                else if (property == "VideoMode")
                {
                    IEnum acquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                    if (acquisitionMode?.IsWritable == true)
                    {
                        IEnumEntry acquisitionModeItem = acquisitionMode.GetEntryByName(newValue);
                        if (acquisitionModeItem?.IsReadable == true)
                        {
                            acquisitionMode.Value = acquisitionModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "BinningControl")
                {
                    IEnum acquisitionMode = nodeMap.GetNode<IEnum>("BinningControl");
                    if (acquisitionMode?.IsWritable == true)
                    {
                        IEnumEntry acquisitionModeItem = acquisitionMode.GetEntryByName(newValue);
                        if (acquisitionModeItem?.IsReadable == true)
                        {
                            acquisitionMode.Value = acquisitionModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "ShutterMode")
                {
                    IEnum exposureMode = nodeMap.GetNode<IEnum>("ExposureMode");
                    if (exposureMode?.IsWritable == true)
                    {
                        IEnumEntry exposureModeItem = exposureMode.GetEntryByName(newValue);
                        if (exposureModeItem?.IsReadable == true)
                        {
                            exposureMode.Value = exposureModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "StreamBufferMode")
                {
                    INodeMap nodeMapStream = camera.GetTLStreamNodeMap();
                    IEnum bufferMode = nodeMapStream.GetNode<IEnum>("StreamBufferHandlingMode");
                    if (bufferMode?.IsWritable == true)
                    {
                        IEnumEntry bufferModeItem = bufferMode.GetEntryByName(newValue);
                        if (bufferModeItem?.IsReadable == true)
                        {
                            bufferMode.Value = bufferModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "ExposureCompensation")
                {
                    IFloat expoCompensation = nodeMap.GetNode<IFloat>("pgrExposureCompensation");
                    expoCompensation.Value = Convert.ToDouble(newValue);
                }
                else
                {
                    Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property + " not implemented.");
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property + " exceptoin: " + e.Message);
            }

        }


        //public float GetPropertyValue(PropertyType property, bool absolute, bool valueB = false)
        //{
        //    CameraProperty camProp = camera.GetProperty(property);

        //    return (absolute ? camProp.absValue : (!valueB ? camProp.valueA : camProp.valueB));
        //}
        public string GetPropertyValue(string property, bool valueB = false)
        {
            if (property == "Shutter")
            {
                IFloat node = nodeMap.GetNode<IFloat>("ExposureTime");
                return node.Value.ToString();
            }
            else if (property == "DeviceTemperature")
            {
                IFloat node = nodeMap.GetNode<IFloat>("DeviceTemperature");
                return node.Value.ToString();
            }
            else if (property == "WidthMax")
            {
                IInteger node = nodeMap.GetNode<IInteger>("WidthMax");
                return node.Value.ToString();
            }
            else if (property == "HeightMax")
            {
                IInteger node = nodeMap.GetNode<IInteger>("HeightMax");
                return node.Value.ToString();
            }
            else if (property == "FrameRate")
            {
                IFloat node = nodeMap.GetNode<IFloat>("AcquisitionFrameRate");
                return node.Value.ToString();
            }
            else
            {
                IEnum node = nodeMap.GetNode<IEnum>(property);
                return node.Value.ToString();
            }
        }


        //public override void AdjustWhiteBalance(int increment, bool valueB, ref uint oldValue)
        //{
        //    PropertyType property = PropertyType.WhiteBalance;

        //    CameraProperty camProp = camera.GetProperty(property);
        //    CameraPropertyInfo propInfo = camera.GetPropertyInfo(property);

        //    camProp.absControl = false;
        //    if (!valueB)
        //    {
        //        oldValue = camProp.valueA;

        //        if (increment > 0)
        //            camProp.valueA = camProp.valueA + (uint)increment;
        //        else
        //            camProp.valueA = camProp.valueA - (uint)(-1 * increment);
        //    }
        //    else
        //    {
        //        oldValue = camProp.valueB;

        //        if (increment > 0)
        //            camProp.valueB = camProp.valueB + (uint)increment;
        //        else
        //            camProp.valueB = camProp.valueB - (uint)(-1 * increment);
        //    }
        //    camera.SetProperty(camProp);
        //}
        public override void AdjustWhiteBalance(double increment, bool valueB, ref double oldValue)
        {
            // TODO: increment value need to check for spinnaker, before is 1 unit step for increase/decrease
            oldValue = valueB ? GetWhiteBalanceBlue() : GetWhiteBalanceRed();

            if (valueB)
            {
                SetWhiteBalanceBlue(oldValue + increment);
                if (GetWhiteBalanceBlue() == oldValue)
                    SetWhiteBalanceBlue(oldValue + increment * 1.5);
            }
            else
            {
                SetWhiteBalanceRed(oldValue + increment);
                if (GetWhiteBalanceRed() == oldValue)
                    SetWhiteBalanceRed(oldValue + increment * 1.5);
            }
        }

        //uint SetWhiteBalance(int newValue, bool valueB)
        //{
        //    uint oldValue;
        //    PropertyType property = PropertyType.WhiteBalance;

        //    CameraProperty camProp = camera.GetProperty(property);
        //    CameraPropertyInfo propInfo = camera.GetPropertyInfo(property);

        //    camProp.absControl = false;
        //    if (!valueB)//red
        //    {
        //        oldValue = camProp.valueA;
        //        camProp.valueA = (uint)newValue;
        //    }
        //    else
        //    {
        //        oldValue = camProp.valueB;
        //        camProp.valueB = (uint)newValue;
        //    }
        //    camera.SetProperty(camProp);
        //    return oldValue;

        //}
        public double SetWhiteBalance(double newValue, bool valueB)
        {
            double oldValue;

            IEnum balanceWhiteAuto = nodeMap.GetNode<IEnum>("BalanceWhiteAuto");
            balanceWhiteAuto.Value = "Off";
            IEnum balanceRatioSelector = nodeMap.GetNode<IEnum>("BalanceRatioSelector");
            if (valueB)
            {
                oldValue = GetWhiteBalanceBlue();
                SetWhiteBalanceBlue(newValue);
            }
            else
            {
                oldValue = GetWhiteBalanceRed();
                SetWhiteBalanceRed(newValue);
            }

            return oldValue;
        }


        //void SetProprtyAutomaticSetting(PropertyType property, bool automatic)
        //{
        //    CameraProperty camProp = camera.GetProperty(property);
        //    if (camProp.autoManualMode != automatic) 
        //    {
        //        camProp.autoManualMode = automatic;
        //        camera.SetProperty(camProp);
        //    }
        //}
        public void SetProprtyAutomaticSetting(string property, bool automatic)
        {
            try
            {
                if (property == "Gain")
                {
                    IEnum gainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        gainAuto.Value = "Continuous";
                    }
                    else
                    {
                        gainAuto.Value = "Off";
                    }
                }
                else if (property == "Shutter")
                {
                    IEnum exposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        exposureAuto.Value = "Continuous";
                    }
                    else
                    {
                        exposureAuto.Value = "Off";
                    }
                }
                else if (property == "Sharpness")
                {
                    IEnum sharpnessAuto = nodeMap.GetNode<IEnum>("SharpnessAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        sharpnessAuto.Value = "Continuous";
                    }
                    else
                    {
                        sharpnessAuto.Value = "Off";
                    }
                }
                else if (property == "FrameRate")
                {
                    IEnum framerateAuto = nodeMap.GetNode<IEnum>("AcquisitionFrameRateAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        framerateAuto.Value = "Continuous";
                    }
                    else
                    {
                        framerateAuto.Value = "Off";
                    }
                }
                else if (property == "Saturation")
                {
                    IEnum saturationAuto = nodeMap.GetNode<IEnum>("SaturationAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        saturationAuto.Value = "Continuous";
                    }
                    else
                    {
                        saturationAuto.Value = "Off";
                    }
                }
                else if (property == "WhiteBalance")
                {
                    IEnum whiteBalanceAuto = nodeMap.GetNode<IEnum>("BalanceWhiteAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        IEnumEntry iBalanceWhiteAutoModeContinuous = whiteBalanceAuto.GetEntryByName("Continuous");
                        if (iBalanceWhiteAutoModeContinuous?.IsReadable == true)
                        {
                            whiteBalanceAuto.Value = iBalanceWhiteAutoModeContinuous.Symbolic;
                        }
                    }
                    else
                    {
                        whiteBalanceAuto.Value = "Off";
                    }
                }
                else if (property == "ExposureCompensationAuto")
                {
                    IEnum expoCompAuto = nodeMap.GetNode<IEnum>("pgrExposureCompensationAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        IEnumEntry iExpoCompAutoModeContinuous = expoCompAuto.GetEntryByName("Continuous");
                        if (iExpoCompAutoModeContinuous?.IsReadable == true)
                        {
                            expoCompAuto.Value = iExpoCompAutoModeContinuous.Symbolic;
                        }
                    }
                    else
                    {
                        expoCompAuto.Value = "Off";
                    }
                }
                else
                {
                    Debug.WriteLine("Error: SetPropertyAutomaticSetting for " + property + " not implemented.");
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetPropertyAutomaticSetting for " + property + " exceptoin: " + e.Message);
            }
        }

        //void SetProprtyEnabledSetting(PropertyType property, bool enabled)
        //{
        //    CameraProperty camProp = camera.GetProperty(property);
        //    camProp.onOff = enabled;
        //    camera.SetProperty(camProp);
        //}
        public void SetProprtyEnabledSetting(string property, bool enabled)
        {
            try
            {
                BoolNode boolNode;
                if (property == "Gamma")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("GammaEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Sharpness")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SharpnessEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Hue")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("HueEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Saturation")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SaturationEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "FrameRate")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("AcquisitionFrameRateEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else
                {
                    Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not implemented", property);
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetProprtyEnabledSetting " + property + " exceptoin: " + e.Message);
            }
        }

        // spinnaker does not have onepush function
        //void SetProprtyOnePush(PropertyType property, bool onePush)
        //{
        //    CameraProperty camProp = camera.GetProperty(property);

        //    camProp.onePush = onePush;
        //    camera.SetProperty(camProp);
        //}


        public override void GetInitializationPropertyValues(Dictionary<CAMERA_PROPERTY, double> properties)
        {
            foreach (var key in properties.Keys.ToList())
            {
                switch (key)
                {
                    case CAMERA_PROPERTY.Shutter:
                        properties[key] = Math.Round(Convert.ToDouble(GetPropertyValue("Shutter")), 3);
                        break;
                    case CAMERA_PROPERTY.Temperature:
                        properties[key] = (Convert.ToDouble(GetPropertyValue("DeviceTemperature")));// / 10.0) - 273.15;
                        break;
                    case CAMERA_PROPERTY.WhiteBalanceRed:
                        properties[key] = GetWhiteBalanceRed();
                        break;
                    case CAMERA_PROPERTY.WhiteBalanceBlue:
                        properties[key] = GetWhiteBalanceBlue();
                        break;
                }
            }
        }

        void InitializeSettings()
        {
            bool restartCapture = _imageCaptureThreadRunning;
            StopImageCaptureThread();

            //todo: pixel format RGB8 must be set first otherwise, hue, saturation, gamma settings may not work.
            SetAbsolutePropertyValue("PixelFormat", "RGB8");
            SetAbsolutePropertyValue("VideoMode", "Continuous");
            configTrigger(TriggerMode.Off, TriggerType.Unknown);
            int width = 0, height = 0;
            if (sensorType == "CCD")
            {
                //width = 1920;
                //height = 1440;
                SetAbsolutePropertyValue("Binning", "1");
                width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                height = Convert.ToInt32(GetPropertyValue("HeightMax"));
            }
            else if (sensorType == "CMOS")
            {
                // set binning
                SetAbsolutePropertyValue("Binning", "2");
                width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                height = Convert.ToInt32(GetPropertyValue("HeightMax"));

                SetAbsolutePropertyValue("BinningControl", "Average");
            }
            SetAbsolutePropertyValue("OffsetX", "0");
            SetAbsolutePropertyValue("OffsetY", "0");
            SetAbsolutePropertyValue("Width", width.ToString());// 1920
            SetAbsolutePropertyValue("Height", height.ToString());//1440
            Console.WriteLine("image size: {0}x{1}", width, height);


            //first reset to default settings
            //camera.RestoreFromMemoryChannel(0);
            DefaultSettings();

            SetProprtyAutomaticSetting("ExposureCompensationAuto", false);
            SetAbsolutePropertyValue("ExposureCompensation", "2.0");

            SetProprtyAutomaticSetting("FrameRate", false);
            SetCameraVideoModeAndFrameRate();
            SetProprtyAutomaticSetting("Shutter", false);
            SetAbsolutePropertyValue("Shutter", "17.0");
            SetProprtyAutomaticSetting("WhiteBalance", false);
            SetProprtyAutomaticSetting("Gain", true);
            //SetAbsolutePropertyValue("Gain", "3.5");
            //todo: onepush
            //SetProprtyOnePush(PropertyType.WhiteBalance, true);
            Thread.Sleep(2000);


            SetProprtyAutomaticSetting("Saturation", false);
            // does not have hue auto setting
            //SetProprtyAutomaticSetting("Hue", false);
            // gamma does not have automatic setting 
            //SetProprtyAutomaticSetting("Gamma", false);
            SetProprtyEnabledSetting("Gamma", true);

            SetAbsolutePropertyValue("Saturation", Convert.ToString(Properties.Settings.Default.Saturation));
            SetAbsolutePropertyValue("Gamma", Convert.ToString(Properties.Settings.Default.Gamma));

            SetProprtyEnabledSetting("Hue", true);
            SetAbsolutePropertyValue("Hue", Convert.ToString(Properties.Settings.Default.Hue));

            //todo: implementing spinnaker grabtimeout 
            //FC2Config config = new FC2Config();
            //config = camera.GetConfiguration();
            //config.grabTimeout = 200;
            //camera.SetConfiguration(config);
            SetStreamBufferCount(1); // test only
            SetAbsolutePropertyValue("StreamBufferMode", "NewestOnly");

            if (restartCapture)
            {
                StartImageCaptureThread();
            }

        }
        //private void InitializeSettings()
        //{
        //    bool restartCapture = _imageCaptureThreadRunning;
        //    StopImageCaptureThread();

        //    //first reset to default settings
        //    DefaultSettings();

        //    // hue, gamma, and saturation setting controled by pixel format RGB8 will enable those settings
        //    //TODO: spinnaker
        //    //camera.SetCameraVideoModeAndFrameRate(VideoMode.VideoMode800x600Rgb,
        //    //    FrameRate.FrameRate30);
        //    SetAbsolutePropertyValue("PixelFormat", "RGB8");
        //    SetAbsolutePropertyValue("VideoMode", "Continuous");
        //    SetAbsolutePropertyValue("Width", "800");
        //    SetAbsolutePropertyValue("Height", "600");

        //    SetProprtyAutomaticSetting("Shutter", true);

        //    SetAbsolutePropertyValue("Gain", "0");

        //    SetProprtyEnabledSetting("AcquisitionFrameRate", true);
        //    SetAbsolutePropertyValue("FrameRate", App.Settings.FrameRate.ToString());

        //    SetProprtyEnabledSetting("Saturation", true);
        //    SetAbsolutePropertyValue("Saturation", App.Settings.Saturation.ToString());

        //    SetProprtyEnabledSetting("Gamma", true);
        //    SetAbsolutePropertyValue("Gamma", App.Settings.Gamma.ToString());

        //    SetProprtyEnabledSetting("Hue", true);
        //    SetAbsolutePropertyValue("Hue", App.Settings.Hue.ToString());
        //}

        bool configTrigger(TriggerMode triggerMode, TriggerType triggerType)
        {
            bool result = false;
            if(triggerMode == TriggerMode.Off)
            {
                IEnum triMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (triMode == null || !triMode.IsWritable)
                {
                    Console.WriteLine("configTrigger: Unable to disable trigger mode (enum retrieval). Aborting...");
                    return false;
                }

                IEnumEntry iTriggerModeOff = triMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("configTrigger: Unable to disable trigger mode (entry retrieval). Aborting...");
                    return false;
                }
                triMode.Value = iTriggerModeOff.Value;
            }
            else if(triggerMode == TriggerMode.On)
            {
                IEnum triMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (triMode == null || !triMode.IsWritable)
                {
                    Console.WriteLine("configTrigger: Unable to enable trigger mode (enum retrieval). Aborting...");
                    return false;
                }

                IEnumEntry iTriggerModeOn = triMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("configTrigger: Unable to enable trigger mode (entry retrieval). Aborting...");
                    return false;
                }
                triMode.Value = iTriggerModeOn.Value;

                IEnum triggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (triggerType == TriggerType.Software)
                {
                    // Set trigger mode to software
                    IEnumEntry iTriggerSourceSoftware = triggerSource.GetEntryByName("Software");
                    if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set software trigger mode (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSource.Value = iTriggerSourceSoftware.Value;

                    Console.WriteLine("configTrigger: Trigger source set to software...");
                }
                else if (triggerType == TriggerType.Hardware)
                {
                    // Set trigger mode to hardware ('Line0')
                    IEnumEntry iTriggerSourceHardware = triggerSource.GetEntryByName("Line0");
                    if (iTriggerSourceHardware == null || !iTriggerSourceHardware.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set hardware trigger mode (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSource.Value = iTriggerSourceHardware.Value;

                    Console.WriteLine("configTrigger: Trigger source set to hardware...");
                } else
                {
                    Console.WriteLine("configTrigger: Trigger source Unknown");
                    return false;
                }

                {
                    IEnum triggerSelector = nodeMap.GetNode<IEnum>("TriggerSelector");
                    IEnumEntry iTriggerSelector = triggerSelector.GetEntryByName("FrameStart");
                    if (iTriggerSelector == null || !iTriggerSelector.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set trigger selector (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSelector.Value = iTriggerSelector.Value;
                }
                {
                    IEnum triggerActivation = nodeMap.GetNode<IEnum>("TriggerActivation");
                    triggerActivation.Value = "RisingEdge";
                    IEnumEntry iTriggerActivation = triggerActivation.GetEntryByName("RisingEdge");
                    if (iTriggerActivation == null || !iTriggerActivation.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set trigger activation (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerActivation.Value = iTriggerActivation.Value;
                }
            }
            return result;
        }

        public override void BufferFrames(bool onOff)
        {
            bool restartCapture = _imageCaptureThreadRunning;
            if (onOff)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("buffer frames start ON")));
            } else
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("buffer frames start OFF")));
            }
            StopImageCaptureThread();

            // todo: enable chunckdata adding framecounter
            //EmbeddedImageInfo embeddedInfo = new EmbeddedImageInfo();
            //embeddedInfo = camera.GetEmbeddedImageInfo();

            //todo: spinnaker
            //FC2Config config = new FC2Config();
            //config = camera.GetConfiguration();

            //TriggerMode triggerMode = camera.GetTriggerMode();

            if (onOff)
            {
                if (!Hemisphere.EnableDisableTrigger(false)) //disable timer trigger
                    throw new ApplicationException("Could not disable external timer trigger");
                if (_imageCaptureThreadRunning)
                {
                    Console.WriteLine("bufferframe TRUE, image capture thread still running");
                }
                //enable chunck data fremecounter
                EnableChunkData("FrameCounter");

                // triggle mode to hardware trigger
                configTrigger(TriggerMode.On, TriggerType.Hardware);
                //triggerMode.onOff = true;
                //triggerMode.mode = 0;
                //triggerMode.parameter = 0;
                //triggerMode.polarity = 1; //rising edge

                //todo: spinnaker
                //config.grabMode = GrabMode.BufferFrames;
                //config.numBuffers = 88;
                SetStreamBufferCount(88);
                SetAbsolutePropertyValue("StreamBufferMode", "OldestFirst");
                ////embeddedInfo.frameCounter.onOff = true;

                ////camera.SetTriggerMode(triggerMode);
                //camera.SetConfiguration(config);
                ////camera.SetEmbeddedImageInfo(embeddedInfo);

                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Camera buffer frames on")));

            }
            else
            {
                /////triggerMode.onOff = false;
                //configTrigger(TriggerMode.Off, TriggerType.Unknown);
                SetAbsolutePropertyValue("StreamBufferMode", "NewestOnly");
                //todo: spinnaker
                //config.grabMode = GrabMode.DropFrames;
                ////embeddedInfo.frameCounter.onOff = false;
                //todo: disable chunkdata item framecounter
                if (_imageCaptureThreadRunning)
                {
                    Console.WriteLine("bufferframe FALSE, image capture thread still running");
                    Thread.Sleep(200);
                }
                if (_imageCaptureThreadRunning)
                {
                    Console.WriteLine("bufferframe FALSE, image capture thread still running after 200ms");
                    Thread.Sleep(200);
                }
                EnableChunkData("FrameCounter", false);
                
                ////camera.SetEmbeddedImageInfo(embeddedInfo);
                //camera.SetConfiguration(config);
                ////camera.SetTriggerMode(triggerMode);
                // todo: need to check the buffer need for dropframes
                SetStreamBufferCount(1);
                if (!Hemisphere.EnableDisableTrigger(true))
                    throw new ApplicationException("Could not enable external timer trigger");

                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Camera buffer frames off")));
            }

            if (!imagesQueue.TryClear(1000))
                throw new ApplicationException("Could not empty queue");

            _dropFrames = !onOff;

            if (restartCapture)
            {
                StartImageCaptureThread();
            }

        }

        bool SetStreamBufferCount(long count, StreamBufferCountMode mode = StreamBufferCountMode.Manual )
        {
            try
            {
                //StreamDefaultBufferCount is the number of images to buffer on PC
                //default is 10
                INodeMap sNodeMap = camera.GetTLStreamNodeMap();

                // todo: stream buffer count mode to manual (Auto)
                IEnum streamBufferCountMode = sNodeMap.GetNode<IEnum>("StreamBufferCountMode");
                if (streamBufferCountMode == null || !streamBufferCountMode.IsWritable)
                {
                    return false;
                }

                if (mode == StreamBufferCountMode.Manual)
                {
                    IEnumEntry streamBufferCountModeManual = streamBufferCountMode.GetEntryByName("Manual");
                    if (streamBufferCountModeManual == null || !streamBufferCountModeManual.IsReadable)
                    {
                        return false;
                    }
                    streamBufferCountMode.Value = streamBufferCountModeManual.Value;

                    IInteger streamNode = sNodeMap.GetNode<IInteger>("StreamDefaultBufferCount");
                    if (streamNode == null || !streamNode.IsWritable)
                    {
                        return false;
                    }

                    streamNode.Value = count;
                } else if(mode == StreamBufferCountMode.Auto)
                {
                    IEnumEntry streamBufferCountModeAuto = streamBufferCountMode.GetEntryByName("Auto");
                    if (streamBufferCountModeAuto == null || !streamBufferCountModeAuto.IsReadable)
                    {
                        return false;
                    }
                    streamBufferCountMode.Value = streamBufferCountModeAuto.Value;
                } else
                {
                    return false;
                }

            }
            catch
            {
                return false;
            }

            return true;
        }

        bool EnableChunkData(string item, bool enable=true)
        {
            bool result = true;

            try
            {
                IBool iChunkModeActive = nodeMap.GetNode<IBool>("ChunkModeActive");
                if (iChunkModeActive == null || !iChunkModeActive.IsWritable)
                {
                    Console.WriteLine("Cannot active chunk mode. Aborting...");
                    return false;
                }

                iChunkModeActive.Value = true;

                IEnum iChunkSelector = nodeMap.GetNode<IEnum>("ChunkSelector");
                if (iChunkSelector == null || !iChunkSelector.IsReadable)
                {
                    Console.WriteLine("Chunk selector not available. Aborting...");
                    return false;
                }

                bool itemFound = false;
                if(item == "FrameCounter")
                {
                    itemFound = true;
                } else if(item == "ExposureTime")
                {
                    itemFound = true;
                }
                else if (item == "Timestamp")
                {
                    itemFound = true;
                }

                if (itemFound)
                {
                    IEnumEntry itemEntry = iChunkSelector.GetEntryByName(item);
                    if (!itemEntry.IsAvailable || !itemEntry.IsReadable)
                    {

                    }
                    else
                    {
                        iChunkSelector.Value = itemEntry.Value;
                        IBool iChunkEnable = nodeMap.GetNode<IBool>("ChunkEnable");
                        if (iChunkEnable?.IsWritable == true)
                        {
                            iChunkEnable.Value = enable;
                        }
                    }

                    result = true;
                } else
                {
                    Console.WriteLine("EnableChunkData: {0} was not implemented", item);
                    result = false;
                }

            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                result = false;
            }

            return result;
        }

        //void InitializeSettingsWB()
        //{
        //    if (Properties.Settings.Default.WBInitialize)
        //    {
        //        SetWhiteBalance(Properties.Settings.Default.WBInitializeRed, false);
        //        SetWhiteBalance(Properties.Settings.Default.WBInitializeBlue, true);
        //    }
        //}
        private void InitializeSettingsWB()
        {
            if (Properties.Settings.Default.WBInitialize)
            {
                SetProprtyAutomaticSetting("WhiteBalance", false);
                SetWhiteBalance(Properties.Settings.Default.WBInitializeRed, false);
                SetWhiteBalance(Properties.Settings.Default.WBInitializeBlue, true);
            }
        }

        public override void InitCalibrationSettings()
        {
            InitializeSettings();
        }
         
        public override void ResetSettings()
        {
            //SetCameraVideoModeAndFrameRate();
            InitializeSettingsWB();

			//Sharpness, Saturation, FrameRate, W.B.(Red) Auto = ON
            SetProprtyAutomaticSetting("Sharpness", false);
            SetProprtyAutomaticSetting("Saturation", false);
            //SetProprtyAutomaticSetting(FlyCapture2Managed.PropertyType.Hue, true);
            SetProprtyAutomaticSetting("FrameRate", false);
            SetProprtyAutomaticSetting("WhiteBalance", true);
        }

        //Hiroshi debug
        static bool _firstCalib = true;

        public override void DefaultSettings()
        {
            SetProprtyAutomaticSetting("Sharpness", false);
            SetProprtyAutomaticSetting("Saturation", false);
            //SetProprtyAutomaticSetting("Hue", false);
            SetProprtyAutomaticSetting("FrameRate", false);
            SetProprtyAutomaticSetting("WhiteBalance", false);

            SetAbsolutePropertyValue("Saturation", Convert.ToString(Properties.Settings.Default.Saturation));
            SetAbsolutePropertyValue("Hue", Convert.ToString(Properties.Settings.Default.Hue));
        
            // Hiroshi add 2014/4/24 Adjust the Blue and Red gain for initial stage of R>G and B<G 
            if (_firstCalib==true)
            {
                //uint oldValue = 0;
                //AdjustWhiteBalance(-2, false, ref oldValue); // -2 for red
                //AdjustWhiteBalance(-2, true, ref oldValue); // -2 for blue
                _firstCalib =false;
            }

        }

        public override void Calibrate(double R, double G, double B)
        {
            // todo: need to test the adjustment
            double oldValue = 0;

            if (R - G > (double)Properties.Settings.Default.WBConvergence)
            {
                AdjustWhiteBalance(-0.004, false, ref oldValue);
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Decremented WB Red from " + oldValue)));
            }
            else if (G - R > (double)Properties.Settings.Default.WBConvergence)
            {
                AdjustWhiteBalance(0.004, false, ref oldValue);
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Incremented WB Red from " + oldValue)));
            }

            if (B - G > (double)Properties.Settings.Default.WBConvergence)
            {
                AdjustWhiteBalance(-0.004, true, ref oldValue);
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Decremented WB Blue from " + oldValue)));
            }
            else if (G - B > (double)Properties.Settings.Default.WBConvergence)
            {
                AdjustWhiteBalance(0.004, true, ref oldValue);
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Incremented WB Blue from " + oldValue)));
            }

        }

        //Hiroshi add for debug
        public override void Finish_calibration()
        {
            //todo: need to implement spinnaker verison
            _normalShutterTime = Convert.ToDouble(GetPropertyValue("Shutter")) / 1000;

            //TriggerMode triggerMode = camera.GetTriggerMode();
            //triggerMode.onOff = true;
            //triggerMode.mode = 0;
            //triggerMode.parameter = 0;
            //triggerMode.polarity = 1; //rising edge
            //camera.SetTriggerMode(triggerMode);
            configTrigger(TriggerMode.On, TriggerType.Hardware);
        }

        public override void InitFluorescenceSettings(int set)
        {
            float shutter = 0, gain = 0;

            switch (set)
            {
                case 0:
                    shutter = (float)GlobalVariables.fluorescenceSettings.FShutterTime;
                    gain = (float)GlobalVariables.fluorescenceSettings.Gain;
                    break;
                case 1:
                    shutter = (float)GlobalVariables.fluorescenceSettings.LowFShutterTime;
                    gain = (float)GlobalVariables.fluorescenceSettings.LowGain;
                    break;
                default:
                    return;
            }

            SetProprtyAutomaticSetting("Shutter", false);
            SetAbsolutePropertyValue("Shutter", Convert.ToString(shutter));

            SetProprtyAutomaticSetting("Gain", false);
            SetAbsolutePropertyValue("Gain", Convert.ToString(gain));
        }

        public override void RestoreNormalSettings()
        {
            
            SetProprtyAutomaticSetting("Shutter", false);
            SetAbsolutePropertyValue("Shutter", Convert.ToString(_normalShutterTime));

            SetProprtyAutomaticSetting("Gain", true);
            //SetAbsolutePropertyValue("Gain", "3.5");
        }

        public bool SetWhiteBalanceRed(double wbRed)
        {
            bool result = false;

            try
            {
                camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
                camera.BalanceRatio.Value = wbRed;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetWhiteBalanceRed()
        {
            camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
            return (double)camera.BalanceRatio.Value;
        }

        public bool SetWhiteBalanceBlue(double wbBlue)
        {
            bool result = false;

            try
            {
                camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
                camera.BalanceRatio.Value = wbBlue;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        public double GetWhiteBalanceBlue()
        {
            camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
            return (double)camera.BalanceRatio.Value;
        }
        //public bool ReadRegister(uint addr, out uint value)
        //{
        //    value = 0;

        //    try
        //    {
        //        value = camera.ReadRegister(addr);
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        //public bool WriteRegister(uint addr, uint value)
        //{
        //    try
        //    {
        //        uint temp = 0;
        //        if (ReadRegister(addr, out  temp))
        //        {
        //            if (temp != value)
        //                camera.WriteRegister(addr, value);

        //            return true;
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
    }

    class SafeQueue<T>
    {
        // A queue that is protected by Monitor.
        private Queue<T> m_inputQueue = new Queue<T>();

        // Try to add an element to the queue: Add the element to the queue 
        // only if the lock becomes available during the specified time
        // interval.
        public bool TryEnqueue(T qValue, int waitTime, bool dropFrames)
        {
            bool res = false;

            // Request the lock.
            if (Monitor.TryEnter(m_inputQueue, waitTime))
            {
                try
                {
                    if (dropFrames)
                    {
                        m_inputQueue.Clear();
                        m_inputQueue.Enqueue(qValue);
                        res = true;
                    }
                    else if (m_inputQueue.Count < 100)
                    {
                        m_inputQueue.Enqueue(qValue);
                        res = true;
                    }
                }
                finally
                {
                    // Ensure that the lock is released.
                    Monitor.Exit(m_inputQueue);
                }
                
            }

            return res;
        }

        // Lock the queue and dequeue an element.
        public bool TryDequeue(out T retval, int waitTime)
        {
            bool res = false;
            retval = (T)(object)null;

            // Request the lock, and block until it is obtained.
            if (Monitor.TryEnter(m_inputQueue, waitTime))
            {
                try
                {
                    if (m_inputQueue.Count > 0)
                    {
                        // When the lock is obtained, dequeue an element.
                        retval = m_inputQueue.Dequeue();
                        res = true;
                    }

                }
                finally
                {
                    // Ensure that the lock is released.
                    Monitor.Exit(m_inputQueue);
                }
                
            }

            return res;
            
        }


        public bool TryClear(int waitTime)
        {
            bool res = false;
            
            // Request the lock, and block until it is obtained.
            if (Monitor.TryEnter(m_inputQueue, waitTime))
            {
                try
                {
                    m_inputQueue.Clear();
                    res = true;
                }
                finally
                {
                    // Ensure that the lock is released.
                    Monitor.Exit(m_inputQueue);
                }

            }

            return res;

        }
    }


}
