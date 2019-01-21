using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCV_BSC_dll_x64;
using System.Drawing;
using Utilities_BSC_dll_x64;
using ZXing;
using Emgu.CV;
using Emgu.CV.Structure;
using OpenCV_BSC_dll_x64.Windows;
using CameraToImage_dll_x64;
using System.Media;
using System.ComponentModel;
using System.Globalization;

namespace ImageProcessing_BSC_WPF
{
    public enum ErrorCode
    {
        Normal,
        No_picture_found,
        No_object_image,
        SearchSURF_Fail,
        SearchFFT_Fail,
        SearchColor_Fail
    }

    public enum DecoderEngine
    {
        Zxing,
        Cortex
    }

    public enum MLFolders
    {
        /// <summary>
        /// @"\Modules\MachineLearning"
        /// </summary>
        [Description(@"\Modules\MachineLearning")]
        MLRoot,

        /// <summary>
        /// @"\Modules\MachineLearning\CNTK"
        /// </summary>
        [Description(@"\Modules\MachineLearning\CNTK")]
        ML_CNTK,

        /// <summary>
        /// @"\Modules\MachineLearning\CNTK\TrainedModels"
        /// </summary>
        [Description(@"\Modules\MachineLearning\CNTK\TrainedModels")]
        ML_CNTK_model,

        /// <summary>
        /// @"\Modules\MachineLearning\YOLO"
        /// </summary>
        [Description(@"\Modules\MachineLearning\YOLO")]
        ML_YOLO,

        /// <summary>
        /// @"\Modules\MachineLearning\YOLO\backup", this is to save temp training file
        /// </summary>
        [Description(@"\Modules\MachineLearning\YOLO\backup")]
        ML_YOLO_backup,

        /// <summary>
        /// @"\Modules\MachineLearning\YOLO\model", files to be used in detection
        /// </summary>
        [Description(@"\Modules\MachineLearning\YOLO\model")]
        ML_YOLO_model,

        /// <summary>
        /// @"\Modules\MachineLearning\YOLO\data", used to train a model
        /// </summary>
        [Description(@"\Modules\MachineLearning\YOLO\data")]
        ML_YOLO_data,

        /// <summary>
        /// @"\Modules\MachineLearning\YOLO\data\img"
        /// </summary>
        [Description(@"\Modules\MachineLearning\YOLO\data\img")]
        ML_YOLO_data_img,
    }

    public static class Func
    {
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            string description = null;

            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (descriptionAttributes.Length > 0)
                        {
                            // we're only getting the first description we find
                            // others will be ignored
                            description = ((DescriptionAttribute)descriptionAttributes[0]).Description;
                        }

                        break;
                    }
                }
            }

            return description;
        }
    }

    public class GV
    {
        // Machine Learning
        public static string[] ML_Folders = new string[20];

        public static SoundPlayer CaptureSound = new SoundPlayer(System.Environment.CurrentDirectory + @"\Resources\camera-shutter-click-03.wav");

        public static DecoderEngine mDecoderEngine = DecoderEngine.Zxing;

        public const int PB_WIDTH_RATIO = 4;
        public const int PB_HEIGHT_RATIO = 3;

        public static bool _camConnectAtStartup;
        public static camType _camSelected;

        public static bool IsCameraConnected = false;
        public static bool IsPictureLoaded = false;

        public static int imgWidth = 0, imgHeight = 0;
        public static double _zoomFactor = 0;
        public static ErrorCode _err;

        public static bool _findMinSwitch;
        public static bool _decodeSwitch;                       //turn on code decoding.
        public static bool _OCRSwitch;                          //turn on OCR decoding.
        public static bool _MLSwitch;                           //turn on machine learning.
        public static bool _motionDetectSwitch;                 //turn on motion detection.
        public static bool _checkBoundry;                       //turn on boundry check.
        public static bool _fitEllipse;                         //turn on ellipse fitting.

        // Color detection, multi points select
        public static int _remainColorPoints = 3;               //total interest points in color detection
        public static int _colorRegionSize = 10;                //pixels
        
        public static Image<Bgr, byte> imgOriginal;
        public static Image<Bgr, byte> imgOriginal_pure;
        public static Image<Bgr, byte> imgOriginal_save;
        public static Image<Bgr, byte> imgProcessed;
        public static Image<Bgr, byte> OCROutputImg;
        public static Image<Bgr, byte> object_img = null;
        public static Image<Bgr, byte> ref_img = null;

        public static CameraConnection mCamera = new CameraConnection();
        public static CameraToImage_dll_x64.Windows.Conversion mConvert;

        public static Setting mSetting = new Setting();  //This will load the newest setting
    }
}
