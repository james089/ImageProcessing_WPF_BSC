using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraToImage_dll;
using OpenCV_BSC_dll;
using System.Drawing;
using Utilities_BSC_dll;
using ZXing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessing_BSC_WPF
{
    public enum ErrorCode
    {
        Normal,
        No_picture_found,
        No_object_image,
        SearchFFT_Fail,
        SearchColor_Fail
    }

    public static class GV
    {
        public static int _pictureBoxWidthRatio = 4;
        public static int _pictureBoxHeightRatio = 3;

        public static bool _camConnectAtStartup;
        public static camType _camSelected;
        public static previewFPS _previewFPS;

        public static bool _cameraConnected = false;
        public static bool _capturing = false;
        public static bool _pictureLoaded = false;

        public static int imgWidth = 0, imgHeight = 0;
        public static double _zoomFactor = 0;
        public static ErrorCode _err;

        public static bool _findMinSwitch;
        public static bool _findCenterSwitch;
        public static bool _decodeSwitch;                       //turn on code decoding.
        public static bool _OCRSwitch;                          //turn on OCR decoding.

        public static bool maxmized = false;
        public static Image<Bgr, byte> imgOriginal;
        public static Image<Bgr, byte> imgProcessed;
        public static Image<Bgr, byte> object_img = null;

        public static bool _colorInverse = false;

        public static Graphics mGraphics;

        public static CameraToImage_dll.CameraConnection mCamera;
        public static CameraToImage_dll.Conversion mConvert;

        public static OpenCV_BSC_dll.Setting mSetting = new Setting();

        public static MainWindow mMainWindow = null;

    }
}
