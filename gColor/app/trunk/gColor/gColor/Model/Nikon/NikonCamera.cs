using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gColor.Model.Nikon
{
    public struct NikonCameraImage
    {
        public DS_U3Wrapper.SniCameraInfo camera;
        public IntPtr stop;
        public IntPtr format;
        public Int32 index;
        public IntPtr image;
    }

    public enum Features
    {
        Brightness,
        CameraType,
        CaptureColorMode,
        ColorMode,
        Format,
        AcquireState,
        ExposureBias,
        ExposureMode,
        Gain,
        Hue,
        Presets,
        Saturation,
        Shutter,
        Sharpness,
        WhiteBalance, //W
        WhiteBalanceBlue,
        WhiteBalanceRed
    }

    public enum AcquisitionState
    {
        Idle = 1,
        Live,
        Capture,
        Freeze,
        Abort
    }


    public enum ColourMode
    {
        Mono8 = 1,
        RGB24 = 4,
        RGB48 = 6
    }

    public enum ExposureBias
    {
        AE1,
        AE2,
        AE3,
        AE4,
        AE5,
        AE6,
        AE7,
        AE8,
        AE9,
        AE10,
        AE11,
        AE12,
        AE13
    }

    

    public enum FrameSize
    {
        FullFrame = 0,
        FullFrameROI = 2,
        Binningx2 = 3,
        Binningx4 = 4,
        HalfFrame = 5,
        HalfFrameROI = 6
    }

    public enum MeteringArea
    {
        Large = 1,
        Medium,
        Small
    }

    public enum MeteringMode
    {
        Average = 1,
        Peak
    }

    public enum Resolution
    {
        QSXGA = 34, //2560x1920
        SXGA = 33, //1280x960
        VGA = 32, //640x480
        QVGA = 31 //320x240
    }
    
    public enum NikonFeatureState
    {
        sniNoState = 0x00000000,        // unknown state (default value)
        sniAutoState = 0x00000001,    // switch the feature to automatic
        sniOnState = 0x00000002,   // switch the feature on
        sniOnePushState = 0x00000004, // start one-push operation
    }


    public class NikonCamera : Camera
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);


        DS_U3Wrapper.SniCameraInfo _camera;
        Dictionary<Features, DS_U3Wrapper.SniFeature> _features;

        

        public override bool Connect()
        {
            bool result = false;

            try
            {
                //discover camera
                _camera = new DS_U3Wrapper.SniCameraInfo();
                Int32 cameraCount = 0;
                IntPtr unmanagedCameraInfoAddr = IntPtr.Zero;
                SniCamResult sResult = DS_U3Wrapper.SniCamDiscoverCameras(out unmanagedCameraInfoAddr, out cameraCount);

                if (sResult != SniCamResult.SNI_OK)
                    throw new Exception("Could not connect to the DS-U3 successfully");
                if (cameraCount == 0)
                    throw new Exception("DS-U3 not available or no cameras connected.");

                // Marshal the data at the returned pointer into a managed object
                _camera = (DS_U3Wrapper.SniCameraInfo)Marshal.PtrToStructure(unmanagedCameraInfoAddr, typeof(DS_U3Wrapper.SniCameraInfo));

                unmanagedCameraInfoAddr = IntPtr.Zero;

                //open camera
                IntPtr cameraHandle = IntPtr.Zero;
                sResult = DS_U3Wrapper.SniCamOpenCamera(ref _camera, out cameraHandle);

                if (sResult != SniCamResult.SNI_OK)
                    throw new Exception("Could not open Camera Handle");

                _camera.handle = cameraHandle;

                // Need to check status of camera. According to camera logs,
                // if the Nikon software finds the camera in a status other
                // than available, it requests the driver version, then closes
                // and opens the camera. Not sure why, but prevents the controller
                // from simply preventing communication with the camera.
                if (_camera.status != 0)
                {
                    StringBuilder version = new StringBuilder(34);
                    DS_U3Wrapper.SniCamGetDriverVersion(_camera.handle, version);
                    DS_U3Wrapper.SniCamCloseCamera(_camera.handle);
                    _camera.handle = IntPtr.Zero;
                    sResult = DS_U3Wrapper.SniCamOpenCamera(ref _camera, out cameraHandle);

                    if (sResult != SniCamResult.SNI_OK)
                        throw new Exception("Could not open Camera Handle");

                    _camera.handle = cameraHandle;
                }


                _features = new Dictionary<Features, DS_U3Wrapper.SniFeature>();

                IntPtr p;
                
                sResult = DS_U3Wrapper.SniCamGetFeatures(_camera.handle, out p);
                if (sResult != SniCamResult.SNI_OK)
                    throw new Exception("Could not get Camera features");

                int numFeatures = Marshal.ReadInt32(p);
                //move to first feature element
                p = (IntPtr)(p.ToInt32() + Marshal.SizeOf(typeof(DS_U3Wrapper.SniFeatures)) -
                            Marshal.SizeOf(typeof(IntPtr)));
                                
                // Setup a mapping from feature name to feature ID number, in order to more easily 
                // access features by name.
                for (int i = 0; i < numFeatures; i++)
                {
                    DS_U3Wrapper.SniFeature feature = (DS_U3Wrapper.SniFeature)Marshal.PtrToStructure(p,
                                                            typeof(DS_U3Wrapper.SniFeature));

                    foreach (Features featureName in Enum.GetValues(typeof(Features)))
                    {
                        if (featureName.ToString().Equals(feature.name))
                            _features.Add(featureName, feature);
                    }

                    //move to next feature element
                    p = (IntPtr)(p.ToInt32() + Marshal.SizeOf(typeof(DS_U3Wrapper.SniFeature)));
                }


                //initialise settings
                InitializeSettings();

                result = true;
            }
            catch (Exception /*ex*/)
            {
                if (_camera.handle != IntPtr.Zero)
                {
                    DS_U3Wrapper.SniCamCloseCamera(_camera.handle);
                    _camera.handle = IntPtr.Zero;
                }
                result = false;
            }
                        

            return result;
        }

        public override void EditCameraSettings()
        {
            var nikonSettings = new ViewModel.NikonSettingsViewModel(this);
            var settingsWindow = new View.NikonSettings();
            settingsWindow.DataContext = nikonSettings;
            settingsWindow.ShowDialog();
        }

        void InitializeSettings()
        {
            AcquireState = AcquisitionState.Live;
            ColorMode = (int)ColourMode.RGB24;
            Format = 100532; //640x480 thinning 1/2
            Presets = 0;

            ExposureModeProperty = 0; //auto exposure

            Gain = 10;
            if (Properties.Settings.Default.Saturation > 50)
                Saturation = 50;
            else if (Properties.Settings.Default.Saturation < -50)
                Saturation = -50;
            else
                Saturation = (int)Properties.Settings.Default.Saturation;

            if (Properties.Settings.Default.Hue > 50)
                Hue = 50;
            else if (Properties.Settings.Default.Hue < -50)
                Hue = -50;
            else
                Hue = (int)Properties.Settings.Default.Hue;
        }

        public override void InitCalibrationSettings()
        {
            InitializeSettings();
        }

        public override void ResetSettings()
        {
            ExposureModeProperty = 4; // manual shutter

            ColorMode = (int)ColourMode.RGB24;
            Format = 100532; //640x480 thinning 1/2

        }

        public override void DefaultSettings()
        {
            ExecuteWhiteBalance();
        }

        public override void Calibrate(double R, double G, double B)
        {
            int oldValue = 0;
            int adjust = Properties.Settings.Default.WBIncrement;

            if (R - G > (double)Properties.Settings.Default.WBConvergence)
            {
                oldValue = WBRed;
                WBRed -= adjust;
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Decremented WB Red from " + oldValue)));
            }
            else if (G - R > (double)Properties.Settings.Default.WBConvergence)
            {
                oldValue = WBRed;
                WBRed += adjust;
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Incremented WB Red from " + oldValue)));
            }

            if (B - G > (double)Properties.Settings.Default.WBConvergence)
            {
                oldValue = WBBlue;
                WBBlue -= adjust;
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Decremented WB Blue from " + oldValue)));
            }
            else if (G - B > (double)Properties.Settings.Default.WBConvergence)
            {
                oldValue = WBBlue;
                WBBlue += adjust;
                Application.Current.Dispatcher.Invoke((Action)(() =>
                    App.LogEntry.AddEntry("Calibration Mode : Incremented WB Blue from " + oldValue)));
            }
        }

        public override void GetInitializationPropertyValues(Dictionary<CAMERA_PROPERTY, double> properties)
        {
            foreach (var key in properties.Keys.ToList())
            {
                switch (key)
                {
                    case CAMERA_PROPERTY.Shutter:
                        properties[key] = ExposureTimeMS;
                        break;
                    case CAMERA_PROPERTY.Temperature:
                        properties[key] = 0;
                        break;
                    case CAMERA_PROPERTY.WhiteBalanceRed:
                        properties[key] = WBRed;
                        break;
                    case CAMERA_PROPERTY.WhiteBalanceBlue:
                        properties[key] = WBBlue;
                        break;
                }
            }
        }

        public override void StartCapture(BackgroundWorker bw)
        {
            NikonCameraImage helper = new NikonCameraImage();
            Int32 index = 0;
            helper.camera = _camera;
            helper.index = index;
            bw.RunWorkerAsync(helper);
        }

        public override BitmapSource GetImage(DoWorkEventArgs e)
        {
            NikonCameraImage helper = (NikonCameraImage)e.Argument;
            BitmapSource source = null, orginalSource = null;
            
            try
            {
                SniCamResult sResult = DS_U3Wrapper.SniCamGetNextImage(helper.camera.handle,
                    helper.stop, out helper.format, out helper.index, ref helper.image);

                if (sResult < SniCamResult.SNI_OK)
                    throw new Exception("SniCamGetNextImage returned " + sResult);

                DS_U3Wrapper.SniImageFormat format = (DS_U3Wrapper.SniImageFormat)Marshal.PtrToStructure(
                    helper.format, typeof(DS_U3Wrapper.SniImageFormat));

                //Allocate space for image buffer
                IntPtr buffer = IntPtr.Zero;
                byte[] managedBuffer;
                try
                {
                    buffer = Marshal.AllocHGlobal(2 * format.buf_size.ToInt32());
                    sResult = DS_U3Wrapper.SniCamGetImageData(helper.camera.handle,
                        helper.image, (uint)(2 * format.buf_size.ToInt32()), ref buffer);
                    if (sResult != SniCamResult.SNI_OK)
                    {
                        DS_U3Wrapper.SniCamReleaseImage(helper.camera.handle, ref helper.image);
                        throw new Exception("SniCamGetImageData returned " + sResult);
                    }

                    if (format.mode == ESniColorMode.sniRgb24)
                    {
                        managedBuffer = new byte[format.buf_size.ToInt32()];
                        Marshal.Copy(buffer, managedBuffer, 0, format.buf_size.ToInt32());

                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(format.width, format.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
                                             new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                             System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                        Marshal.Copy(managedBuffer, 0, bmpData.Scan0, managedBuffer.Length);
                        bmp.UnlockBits(bmpData);

                        IntPtr hBitmap = bmp.GetHbitmap();
                        BitmapSizeOptions sizeOptions = BitmapSizeOptions.FromEmptyOptions();
                        try
                        {
                            orginalSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero,
                                System.Windows.Int32Rect.Empty, sizeOptions);
                        }
                        finally
                        {
                            DeleteObject(hBitmap);
                        }

                        if ((Properties.Settings.Default.CropHeight == 0 && Properties.Settings.Default.CropWidth == 0) ||
                             (Properties.Settings.Default.CropHeight + Properties.Settings.Default.CropTop > orginalSource.Height) ||
                             (Properties.Settings.Default.CropWidth + Properties.Settings.Default.CropLeft > orginalSource.Width)
                            )
                        {
                            source = orginalSource;
                            CropImageWidth = 0;
                            CropImageHeight = 0;
                        }
                        else
                        {
                            source = new CroppedBitmap(orginalSource,
                                new System.Windows.Int32Rect((int)Properties.Settings.Default.CropLeft,
                                                                (int)Properties.Settings.Default.CropTop,
                                                                (int)(Properties.Settings.Default.CropWidth == 0 ?
                                                                        orginalSource.Width : Properties.Settings.Default.CropWidth),
                                                                (int)(Properties.Settings.Default.CropHeight == 0 ?
                                                                        orginalSource.Height : Properties.Settings.Default.CropHeight)));
                            CropImageWidth = source.Width;
                            CropImageHeight = source.Height;
                        }
                        source.Freeze();
                        ImageWidth = orginalSource.Width;
                        ImageHeight = orginalSource.Height;
                                                                        
                        sResult = DS_U3Wrapper.SniCamReleaseImage(helper.camera.handle, ref helper.image);
                        if (sResult != SniCamResult.SNI_OK)
                            throw new Exception("SniCamReleaseImage returned " + sResult);
                    }
                    else
                    {
                        App.LogEntry.AddEntry("Nikon Camera is not in ESniColorMode.sniRgb24 - please reset it.");
                    }
                }
                catch (Exception /*ex*/)
                {
                    throw;
                }
                finally
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = IntPtr.Zero;
                    }
                }

            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry("Failed to get image: " + ex.Message);
            }
            finally
            {
            }

            return source;            
        }

        public override void DisConnect()
        {
            if (_camera.handle != IntPtr.Zero)
            {
                SniCamResult sResult = DS_U3Wrapper.SniCamReleaseCamera(_camera.handle);
                sResult = DS_U3Wrapper.SniCamTerminate();
                _camera.handle = IntPtr.Zero;
            }

            //unload the SniCam.dll
            //IntPtr nikonDll = LoadLibrary("SniCam.dll");
            //FreeLibrary(nikonDll);
            //FreeLibrary(nikonDll);

        }

        public void ExecuteWhiteBalance()
        {
            OnePush(_features[Features.WhiteBalance].id);
        }

        bool OnePush(int featureId)
        {
            bool result = false;
            DS_U3Wrapper.SniFeatureState state = new DS_U3Wrapper.SniFeatureState();
            DS_U3Wrapper.SniCamGetState(_camera.handle, featureId, out state);
            if ( (state.bits & 0x10000000U) != 0)
            {
                state.bits |= (uint)NikonFeatureState.sniOnePushState;
                SniCamResult res = DS_U3Wrapper.SniCamSetState(_camera.handle, featureId, state);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set OnePush, Error Code: " + res, "Error");
                }
                else
                    result = true;
            }

            return result;
        }

        bool AutoManual(int featureId, bool auto)
        {
            bool result = false;
            DS_U3Wrapper.SniFeatureState state = new DS_U3Wrapper.SniFeatureState();
            DS_U3Wrapper.SniCamGetState(_camera.handle, featureId, out state);

            if (auto)
            {
                if ((state.bits & 0x02000000U) != 0)
                {
                    state.bits |= (uint)NikonFeatureState.sniAutoState;
                    SniCamResult res = DS_U3Wrapper.SniCamSetState(_camera.handle, featureId, state);
                    if (res != SniCamResult.SNI_OK)
                    {
                        MessageBox.Show("Failed to set state, Error Code: " + res, "Error");
                    }
                    else
                        result = true;
                }
            }
            else
            {
                if ((state.bits & 0x01000000U) != 0)
                {
                    state.bits &= ~((uint)NikonFeatureState.sniAutoState);
                    SniCamResult res = DS_U3Wrapper.SniCamSetState(_camera.handle, featureId, state);
                    if (res != SniCamResult.SNI_OK)
                    {
                        MessageBox.Show("Failed to set state, Error Code: " + res, "Error");
                    }
                    else
                        result = true;
                }
            }

            return result;
        }

        #region Properties

        public int ExposureModeProperty
        {
            get
            {
                int _exposureMode;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.ExposureMode].id, out _exposureMode);
                return _exposureMode;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.ExposureMode].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set WB Red, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public int WBRed
        {
            get
            {
                int _wbRed;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.WhiteBalanceRed].id, out _wbRed);
                return _wbRed;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.WhiteBalanceRed].id,
                                                                value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set WB Red, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public int WBBlue
        {
            get
            {
                int _wbBlue;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.WhiteBalanceBlue].id, out _wbBlue);
                return _wbBlue;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.WhiteBalanceBlue].id,
                                                                value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set WB Blue, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public int Saturation
        {
            get
            {
                int _saturation;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Saturation].id, out _saturation);
                return _saturation;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Saturation].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Saturation, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public int Hue
        {
            get
            {
                int _hue;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Hue].id, out _hue);
                return _hue;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Hue].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Hue, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public int Gain
        {
            get
            {
                int _gain;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Gain].id, out _gain);
                return _gain;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Gain].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Gain, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);

            }
        }

        public int ExposureTimeMS
        {
            get
            {
                int _shutter;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Shutter].id, out _shutter);
                return _shutter / 100;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Shutter].id, value * 100);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Exposure Time, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }
        
        public int Format
        {
            get
            {
                int _format;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Format].id, out _format);
                return _format;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Format].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Format, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }


        public int ColorMode
        {
            get
            {
                int _colorMode;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.ColorMode].id, out _colorMode);
                return _colorMode;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.ColorMode].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set ColorMode, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }

        public AcquisitionState AcquireState
        {
            get
            {
                int value;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.AcquireState].id, out value);
                return (AcquisitionState)value;
            }
            set
            {
                DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.AcquireState].id, (int)value);
            }
        }

        public int Presets
        {
            get
            {
                int _presets;
                DS_U3Wrapper.SniCamGetValue(_camera.handle, _features[Features.Presets].id, out _presets);
                return _presets;
            }
            set
            {
                SniCamResult res = DS_U3Wrapper.SniCamSetValue(_camera.handle, _features[Features.Presets].id, value);
                if (res != SniCamResult.SNI_OK)
                {
                    MessageBox.Show("Failed to set Scene Mode, Error Code: " + res, "Error");
                }
                else
                    DS_U3Wrapper.SniCamFlushSettings(_camera.handle);
            }
        }
        
        #endregion

    }
}
