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
        static Image<Bgr, byte> img_hist = null;
        static int counter = 0;
        public static bool checkMotion(Image<Bgr, byte> inputImage)
        {

            if (img_hist == null)
            {
                img_hist = inputImage;
                return false;
            }
            else
            {
                if (FFT.searchObject_FFT(inputImage, img_hist))   // means picture doesn't change 
                {
                    counter++;
                    if (counter >= (int)PreviewRoutine._previewFPS * 1)                           // 1s still image               
                    {
                        counter = 0;
                        return false;
                    }
                    return true;
                }
                else
                {
                    img_hist = inputImage;
                    return true;
                }
            }

        }
    }
}
