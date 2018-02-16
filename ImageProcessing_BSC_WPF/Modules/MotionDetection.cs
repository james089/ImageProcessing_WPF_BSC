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
        static Image<Bgr, byte> imgOriginal_history;
        static DateTime startTime;

        public static bool checkMotion(Image<Bgr, byte> inputImage)
        {
            if (imgOriginal_history == null)
            {
                startTime = DateTime.Now;
                imgOriginal_history = inputImage;
            }
            if ((DateTime.Now - startTime).TotalSeconds > 2)
            {
                startTime = DateTime.Now;
                if (FFT.searchObject_FFT(inputImage, imgOriginal_history))   // means picture doesn't change 
                {
                    return false;
                }
                else
                {
                    imgOriginal_history = inputImage;
                    return true;
                }
            }
            return false;
        }
    }
}
