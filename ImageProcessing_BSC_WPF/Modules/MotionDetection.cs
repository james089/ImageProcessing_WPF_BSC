using Emgu.CV;
using Emgu.CV.Structure;
using OpenCV_BSC_dll_x64.ObjectDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    public class MotionDetection
    {
        static bool isHistorySaved = false;
        static Image<Bgr, byte> imgOriginal_history;
        static DateTime startTime;

        public static bool checkMotion(Image<Bgr, byte> inputImage)
        {
            if (!isHistorySaved)
            {
                isHistorySaved = true;
                startTime = DateTime.Now;
                imgOriginal_history = inputImage;
            }
            if ((DateTime.Now - startTime).TotalSeconds > 1)
            {
                if (FFT.searchObject_FFT(inputImage, imgOriginal_history))   // means picture doesn't change 
                {
                    isHistorySaved = false;
                    return false;
                }
                else
                {
                    isHistorySaved = false;
                    return true;
                }
            }
            return false;
        }
    }
}
