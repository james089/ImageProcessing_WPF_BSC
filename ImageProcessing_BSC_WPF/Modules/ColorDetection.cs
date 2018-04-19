using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using Emgu.CV.Util;

namespace ImageProcessing_BSC_WPF.Modules
{
    class ColorDetection
    {
        /// <summary>
        /// This is the algorithm that shows the color detected area as white. It is a test algorithm currently under development
        /// </summary>
        /// <param name="imgOriginal"></param>
        /// <param name="object_img"></param>
        /// <param name="processeImage"></param>
        /// <returns></returns>
        public static bool Color_detection(Image<Bgr, byte> imgOriginal, Image<Bgr, byte> object_img, out Image<Bgr, Byte> _outPutImg, double Tolerance)
        {
            Image<Hsv, byte> object_hsv = object_img.Convert<Hsv, byte>();

            int sum_hu = 0, avg_hue = 0;
            for (int j = 0; j < object_hsv.Height; j++)
            {
                for (int i = 0; i < object_hsv.Width; i++)
                {
                    sum_hu += object_hsv.Data[0, 0, 0];  // Hue values
                }
            }
            avg_hue = sum_hu / (object_img.Height * object_img.Width);

            using (Image<Hsv, byte> hsv = imgOriginal.Convert<Hsv, byte>())
            {
                // 2. Obtain the 3 channels (hue, saturation and value) that compose the HSV image
                Image<Gray, byte>[] channels = hsv.Split();

                try
                {
                    // 3. All points in this range will be white, otherwise is black
                    CvInvoke.cvInRangeS(channels[0], new Gray(avg_hue - Tolerance).MCvScalar, new Gray(avg_hue + Tolerance).MCvScalar, channels[0]);

                    // 4. Display the result
                    _outPutImg = channels[0].Convert<Bgr, byte>();
                    return true;
                }
                catch (Exception e)
                {
                    _outPutImg = null;
                    return false;
                }
                finally
                {
                    channels[1].Dispose();
                    channels[2].Dispose();
                }
            }
        }

    }
}
