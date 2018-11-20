using Emgu.CV;
using Emgu.CV.Structure;
using OpenCV_BSC_dll_x64.FeatureDetection;
using OpenCV_BSC_dll_x64.General;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    class CheckBoundry
    {
        /// <summary>
        /// Check if the object blob found in inputImage is within the boundry
        /// Inside, return true, outside, return false
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="boundry"></param>
        /// <returns></returns>
        public static bool mCheckBoundry(Image<Bgr, byte> inputImage, Rectangle boundry)
        {
            Image<Gray, byte> grayImg = inputImage.Convert<Gray, byte>();
            grayImg = ImageProcessing.colorFilter(grayImg);

            Rectangle[] rect;
            ContourDetection.contourDetection(grayImg.Convert<Bgr, byte>(), out rect);
            return (rectInsideRect(rect[0], boundry));
        }

        private static bool rectInsideRect(Rectangle rect, Rectangle boundry)
        {
            return (rect.Size != new Size(0, 0) &&
                rect.X > boundry.X && rect.Y > boundry.Y &&
                rect.Width < boundry.Width && rect.Height < boundry.Height);

        }

    }
}