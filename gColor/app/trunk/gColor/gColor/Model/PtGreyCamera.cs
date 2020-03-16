//using FlyCapture2Managed;
//using FlyCapture2Managed.Gui;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using gColor.View;

namespace gColor.Model
{
    public struct PtGreyCameraImage
    {
        public ManagedImage raw;
        public ManagedImage converted;
        public IManagedCamera cam;
    }

    public class PtGreyCamera : Camera
    {
        ManagedSystem system = null;
        IManagedCamera camera;
        INodeMap nodeMap = null;
        string sensorType = Properties.Settings.Default.Sensor;

        double balanceRatioBlue = -1.0;
        double balanceRatioRed = -1.0;

        public override bool Connect()
        {
            bool result = false;
            //try
            //{
            //    CameraSelectionDialog m_selDlg = new CameraSelectionDialog();
            //    if (m_selDlg.ShowModal())
            //    {
            //        ManagedPGRGuid[] guids = m_selDlg.GetSelectedCameraGuids();
            //        if (guids.Length == 0)
            //        {
            //            //MessageBox.Show("Please select a camera", "No camera selected");
            //            return false;
            //        }

            //        camera = new ManagedCamera();
            //        m_ctldlg = new CameraControlDialog();
            //        camera.Connect(guids[0]);

            //        //initialise settings
            //        InitializeSettings();
            //        InitializeSettingsWB();

            //        CameraInfo ci = camera.GetCameraInfo();
            //        SerialNumber = ci.serialNumber;

            //        result = true;
            //    }
            //}
            //catch (Exception /*ex*/)
            //{
            //    //App.LogEntry.AddEntry("Failed to Connect to Point Grey Camera : " + ex.Message);
            //    result = false;
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

            // Retrieve GenICam nodemap
            nodeMap = camera.GetNodeMap();

            SerialNumber = Convert.ToUInt32(camera.DeviceSerialNumber);

            //initialise settings
            try
            {
                InitializeSettings();
                InitializeSettingsWB();
                result = true;
            }
            catch(SpinnakerException ex)
            {
                result = false;
                Debug.WriteLine("PtGrey connect failed: " + ex.Message);
            }

            return result;
        }

        public override void StartCapture(BackgroundWorker bw)
        {
            ManagedImage m_image = new ManagedImage();
            ManagedImage m_converted = new ManagedImage();

            try
            {
                //camera.StopCapture();
                camera.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                if (!ex.Message.Contains("Camera is not started"))
                {
                    throw;
                }
            }

            //camera.StartCapture();
            camera.BeginAcquisition();

            PtGreyCameraImage helper = new PtGreyCameraImage();
            helper.converted = m_converted;
            helper.raw = m_image;
            helper.cam = camera;

            bw.RunWorkerAsync(helper);
        }

        
        public override BitmapSource GetImage(DoWorkEventArgs e)
        {
            PtGreyCameraImage helper = (PtGreyCameraImage)e.Argument;
            BitmapSource source;

            //helper.cam.RetrieveBuffer(helper.raw);
            //helper.raw.ConvertToBitmapSource(helper.converted);
            IManagedImage managedImage = helper.cam.GetNextImage();
            if (managedImage.IsIncomplete)
            {
                Console.WriteLine("GetNextImage Incompleted");
                return null;
            }
            managedImage.ConvertToBitmapSource(PixelFormatEnums.BGR8, helper.converted);
            if ((Properties.Settings.Default.CropHeight == 0 && Properties.Settings.Default.CropWidth == 0) ||
                 (Properties.Settings.Default.CropHeight + Properties.Settings.Default.CropTop > helper.converted.bitmapsource.Height) ||
                 (Properties.Settings.Default.CropWidth + Properties.Settings.Default.CropLeft > helper.converted.bitmapsource.Width)
                )
            {
                source = helper.converted.bitmapsource;
                CropImageWidth = 0;
                CropImageHeight = 0;
            }
            else
            {
                source = new CroppedBitmap(helper.converted.bitmapsource,
                    new System.Windows.Int32Rect((int)Properties.Settings.Default.CropLeft,
                                                    (int)Properties.Settings.Default.CropTop,
                                                    (int)(Properties.Settings.Default.CropWidth == 0 ?
                                                            helper.converted.bitmapsource.Width : Properties.Settings.Default.CropWidth),
                                                    (int)(Properties.Settings.Default.CropHeight == 0 ?
                                                            helper.converted.bitmapsource.Height : Properties.Settings.Default.CropHeight)));
                CropImageWidth = source.Width;
                CropImageHeight = source.Height;
            }
            source.Freeze();
            ImageWidth = helper.converted.bitmapsource.Width;
            ImageHeight = helper.converted.bitmapsource.Height;
            managedImage.Release();
            return source;
        }


        public override void DisConnect()
        {
            try
            {
                if (camera != null)
                {
                    //camera.StopCapture();
                    //camera.Disconnect();
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
            //todo: spinnaker PropertyGridControl
            CameraSettings camDlg = new CameraSettings(camera);
            camDlg.ShowDialog();
        }


        //void SetCameraVideoModeAndFrameRate(VideoMode newVideoMode, FrameRate newFrameRate)
        //{
        //    bool restartCapture = true;
        //    try
        //    {
        //        camera.StopCapture();
        //    }
        //    catch (FC2Exception ex)
        //    {
        //        if (ex.Type != ErrorType.IsochNotStarted)
        //        {
        //            throw;
        //        }
        //        else
        //            restartCapture = false;
        //    }

        //    try
        //    {
        //        camera.SetVideoModeAndFrameRate(newVideoMode, newFrameRate);
        //    }
        //    catch (FC2Exception /*ex*/)
        //    {
        //        throw;
        //    }

        //    if (restartCapture)
        //    {
        //        camera.StartCapture();
        //    }
        //}


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
            //first reset to default settings
            //camera.RestoreFromMemoryChannel(0);
            //DefaultSettings();

            //SetCameraVideoModeAndFrameRate(VideoMode.VideoMode800x600Rgb, FrameRate.FrameRate30);
            SetAbsolutePropertyValue("PixelFormat", "RGB8");
            SetAbsolutePropertyValue("VideoMode", "Continuous");
            int width = 0, height = 0;
            if (sensorType == "CCD")
            {
                //width = 1920;
                //height = 1440;
                width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                height = Convert.ToInt32(GetPropertyValue("HeightMax"));
            }
            else if (sensorType == "CMOS")
            {
                // set binning
                SetAbsolutePropertyValue("Binning", "2");
                width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                height = Convert.ToInt32(GetPropertyValue("HeightMax"));
            }
            SetAbsolutePropertyValue("Width", width.ToString());// 1920
            SetAbsolutePropertyValue("Height", height.ToString());//1440
            Console.WriteLine("image size: {0}x{1}", width, height);

            DefaultSettings();

            SetProprtyAutomaticSetting("ExposureCompensationAuto", false);
            SetAbsolutePropertyValue("ExposureCompensation", "2");

            SetProprtyAutomaticSetting("Gain", false);
            if(sensorType == "CCD")
            {
                SetAbsolutePropertyValue("Gain", "0");
            } else if(sensorType == "CMOS")
            {
                SetAbsolutePropertyValue("Gain", "10");
            }
            

            // framerate: first enable acquisition frame rate control, then frame rate auto and frame rate value
            SetProprtyEnabledSetting("FrameRate", true);
            SetProprtyAutomaticSetting("FrameRate", false);
            SetAbsolutePropertyValue("FrameRate", Convert.ToString(Properties.Settings.Default.FrameRate));

            // saturation property: enable first, then automatic can be controled
            SetProprtyEnabledSetting("Saturation", true);
            SetProprtyAutomaticSetting("Saturation", false);
            SetAbsolutePropertyValue("Saturation", Convert.ToString(Properties.Settings.Default.Saturation));

            SetProprtyAutomaticSetting("Shutter", true);

            // gamma does not have automatic setting, first enable, then set the value
            //SetProprtyAutomaticSetting("Gamma", false);
            SetProprtyEnabledSetting("Gamma", true);
            SetAbsolutePropertyValue("Gamma", Convert.ToString(Properties.Settings.Default.Gamma));

            // hue does not have automatic setting, first enable, then set the value
            //SetProprtyAutomaticSetting("Hue", false);
            SetProprtyEnabledSetting("Hue", true);
            SetAbsolutePropertyValue("Hue", Convert.ToString(Properties.Settings.Default.Hue));

            SetAbsolutePropertyValue("StreamBufferMode", "NewestOnly");
        }

        void InitializeSettingsWB()
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
            // Hirosh modify
            //SetProprtyAutomaticSetting(FlyCapture2Managed.PropertyType.Shutter, false);
            //SetProprtyAutomaticSetting(FlyCapture2Managed.PropertyType.Shutter, true);

            //Standard Video Mode = 800x600; FrameRate = 30
            //SetCameraVideoModeAndFrameRate(FlyCapture2Managed.VideoMode.VideoMode800x600Rgb, FlyCapture2Managed.FrameRate.FrameRate30);
            //SetAbsolutePropertyValue("PixelFormat", "RGB8");
            //SetAbsolutePropertyValue("VideoMode", "Continuous");
            //SetAbsolutePropertyValue("Width", "1920");
            //SetAbsolutePropertyValue("Height", "1440");

            SetProprtyAutomaticSetting("Shutter", true);
            InitializeSettingsWB();

            //Sharpness, Saturation, FrameRate, W.B.(Red) Auto = ON
            SetProprtyAutomaticSetting("Sharpness", false);
            SetProprtyAutomaticSetting("Saturation", false);
            //SetProprtyAutomaticSetting(FlyCapture2Managed.PropertyType.Hue, true);
            SetProprtyAutomaticSetting("FrameRate", false);
            SetProprtyAutomaticSetting("WhiteBalance", true);

            //Gamma = 1
            //SetAbsolutePropertyValue(FlyCapture2Managed.PropertyType.Gamma,
               // (float)Properties.Settings.Default.Gamma);
        }


        public override void DefaultSettings()
        {
            //SetProprtyAutomaticSetting(FlyCapture2Managed.PropertyType.Shutter, true);
            
            //Sharpness, Saturation, Gain, FrameRate, W.B.(Red) Auto = OFF
            SetProprtyAutomaticSetting("Sharpness", false);
            SetProprtyAutomaticSetting("Saturation", false);
            //SetProprtyAutomaticSetting("Hue", false); // hue does not have automatic setting 
            SetProprtyAutomaticSetting("FrameRate", false);
            SetProprtyAutomaticSetting("WhiteBalance", false);

            SetAbsolutePropertyValue("Saturation", Convert.ToString(Properties.Settings.Default.Saturation));
            SetAbsolutePropertyValue("Hue", Convert.ToString(Properties.Settings.Default.Hue));
            SetAbsolutePropertyValue("FrameRate", Convert.ToString(Properties.Settings.Default.FrameRate));

            //W.B. (Red) One Push Click
            //SetProprtyOnePush(FlyCapture2Managed.PropertyType.WhiteBalance, true);
        
        }

        public override void Calibrate(double R, double G, double B)
        {

           

            double oldValue = 0.0;

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
            SetProprtyAutomaticSetting("Shutter", false);
        }

        public bool SetWhiteBalanceRed(double wbRed)
        {
            bool result = false;

            try
            {
                //camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
                //camera.BalanceRatio.Value = wbRed;
                //result = true;

                IEnum balanceWhiteAuto = nodeMap.GetNode<IEnum>("BalanceWhiteAuto");
                balanceWhiteAuto.Value = "Off";

                IEnum balanceRatioSelector = nodeMap.GetNode<IEnum>("BalanceRatioSelector");
                balanceRatioSelector.Value = "Red";

                IFloat balanceRatio = nodeMap.GetNode<IFloat>("BalanceRatio");
                balanceRatio.Value = wbRed;

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
                //camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
                //camera.BalanceRatio.Value = wbBlue;
                //result = true;

                IEnum balanceWhiteAuto = nodeMap.GetNode<IEnum>("BalanceWhiteAuto");
                balanceWhiteAuto.Value = "Off";

                IEnum balanceRatioSelector = nodeMap.GetNode<IEnum>("BalanceRatioSelector");
                balanceRatioSelector.Value = "Blue";

                IFloat balanceRatio = nodeMap.GetNode<IFloat>("BalanceRatio");
                balanceRatio.Value = wbBlue;

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

    }
}
