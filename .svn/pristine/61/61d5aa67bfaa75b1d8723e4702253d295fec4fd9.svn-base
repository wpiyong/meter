using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gColor.Model.Nikon
{
    #region enums
    public enum SniCamResult
    {
        SNI_OK = 0,
        SNICAM_OK = 0,
        SNICAM_EXCEPTION = -10000,
        SNICAM_TOO_MANY_CAMERAS,
        SNICAM_CAMERA_NOT_FOUND,
        SNICAM_CAMERA_ALREADY_OPEN,
        SNICAM_NOT_IMPLEMENTED,
        SNI_FEATURE_NOT_FOUND,
        SNI_ERROR_BUFFER_TOO_SMALL,
        SNI_FEATURE_INVALID_VALUE,
        SNI_INTERNAL_ERROR,
        SNI_ERROR_MEMORY_ALLOC_FAILED,
        SNI_INVALID_CAMERA_DEFINITION,
        SNI_FEATURE_READ_ONLY,
        SNI_THREAD_CREATION_FAILED,
        SNICAM_CAMERA_CLOSED,
        SNICAM_DIRECTSHOW_ERROR,
        SNICAM_ERROR_READING_CAMERA,
        SNICAM_GET_IMAGE_DISABLED,
        SNICAM_GET_IMAGE_STOPPED,
        SNICAM_SETEVENTFUNC_NOT_CALLED,
        SNICAM_SETIMAGEFUNC_NOT_CALLED,
        SNICAM_1394_ERROR,
        SNICAM_INVALID_NOT_IDLE,
        SNICAM_INVALID_WHEN_LIVE,
        SNICAM_INVALID_WHEN_TRIGGER,
        SNICAM_INVALID_WHEN_9SHOT,
        SNICAM_INVALID_WHEN_ROI,
        SNICAM_COMPOSE_ERROR,
        SNICAM_IOCTL_ERROR_CSR_RETRY,
        SNICAM_IOCTL_ERROR_FEATURE,
        SNICAM_IOCTL_ERROR_VENDOR,
        SNICAM_IOCTL_ERROR_PRESENCE,
        SNICAM_IOCTL_ERROR_ERROR_FLAG1,
        SNICAM_IOCTL_ERROR_ERROR_FLAG2,
        SNICAM_IOCTL_ERROR_COUNT_MAX,
        SNICAM_IOCTL_ERROR_CMD_FLAG,
        SNICAM_IOCTL_ERROR_PARAMETER,
        SNICAM_IOCTL_ERROR_DMA_SIZE,
        SNICAM_IOCTL_ERROR_DMA_MAX_SIZE,
        SNICAM_IOCTL_ERROR_COUNT_0,
        SNICAM_IOCTL_ERROR_ACCESS_CONTROL,
        SNICAM_IOCTL_ERROR_ISO_NO_IMAGE,
        SNICAM_IOCTL_ERROR_WR_EN,
        SNICAM_IOCTL_ERROR_HANDLE,
        SNICAM_IOCTL_ERROR_AE_UNDER,
        SNICAM_IOCTL_ERROR_AE_OVER,
        SNICAM_IOCTL_ERROR_SET_CSR,
        SNICAM_IOCTL_ERROR_GET_CSR,
        SNICAM_IOCTL_ERROR_WRITE_BLOCK,
        SNICAM_IOCTL_ERROR_READ_BLOCK,
        SNICAM_IOCTL_ERROR_ISO_PACKET,
        SNICAM_IOCTL_ERROR_ASYNC_PACKET,
        SNICAM_IOCTL_ERROR,
    }

    public enum ESniColorMode
    {
        sniRaw = 0,
        sniMono8,
        sniMono12,
        sniMono16,
        sniRgb24,
        sniRgb36,
        sniRgb48
    }

    public enum ESniAcquireState
    {
        sniIdle = 1,
        sniLive,
        sniCapture,
        sniFreeze,
        sniAbort
    }
    #endregion

    /// <summary>
    /// Wraps the functions in SniCam.dll in order to make them available
    /// for use by CLR languages.
    /// </summary>
    public static class DS_U3Wrapper
    {
        #region delegates
        public delegate SniCamResult SniCamImageFuncDelegate(IntPtr callback_id, ref SniImageFormat format, IntPtr image, Int32 index);
        #endregion

        #region structures
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode, Size = 404)]
        public struct SniCameraInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public String camera_name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public String controller_name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public String firmware_version;
            public Int32 lib_id;
            public Int32 controller_id;
            public Int32 camera_id;
            public IntPtr handle;
            public Int32 status;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct SniFeatures
        {
            public Int32 count;
            public Int32 reserved;
            public SniFeature not_supported;
            public IntPtr feature;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct SniFeature
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string name;
            public Int32 id;
            public Int32 value;
            public SniFeatureState state;
            public Int32 default_value;
            public SniRangeInfo range_info;
            public SniEnumInfo enum_info;
            public double scale;
            public double offset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String unit;
            public IntPtr reserved1;
            public Int32 reserved2;
            public Int32 reserved3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SniFeatureState
        {
            public UInt32 bits;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SniRangeInfo
        {
            public Int32 lower;
            public Int32 higher;
            public Int32 increment;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct SniEnumInfo
        {
            public Int32 count;
            public IntPtr values;
            public IntPtr strings;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct SniImageFormat
        {
            public ESniColorMode mode;
            public Int32 bits;
            public Int32 channels;
            public Int32 width;
            public Int32 height;
            public Int32 frame_count;
            public IntPtr buf_size;
        }
        #endregion

        #region externalMethods
        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamDiscoverCameras(out IntPtr info, out Int32 count);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamOpenCamera(ref SniCameraInfo info, out IntPtr Camera);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamCloseCamera(IntPtr Camera);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamReleaseCamera(IntPtr Camera);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetFeatures(IntPtr Camera, out IntPtr pFeatures);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetNextImage(IntPtr Camera, IntPtr stop_event, out IntPtr format, out Int32 index, ref IntPtr image);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamSetImageCallback(IntPtr Camera, SniCamImageFuncDelegate CallbackFunction, IntPtr CallbackID);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetImageData(IntPtr Camera, IntPtr Image, UInt32 buf_size, ref IntPtr buffer);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamReleaseImage(IntPtr Camera, ref IntPtr Image);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetValue(IntPtr Camera, Int32 FeatureID, out Int32 FeatureValue);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamSetValue(IntPtr Camera, Int32 FeatureID, Int32 FeatureValue);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetDriverVersion(IntPtr Camera, StringBuilder Version);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamSetState(IntPtr Camera, Int32 FeatureID, SniFeatureState FeatureState);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamGetState(IntPtr Camera, Int32 FeatureID, out SniFeatureState FeatureState);

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamTerminate();

        [DllImport("SniCam.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        extern public static SniCamResult SniCamFlushSettings(IntPtr Camera);

        #endregion
    }
}
