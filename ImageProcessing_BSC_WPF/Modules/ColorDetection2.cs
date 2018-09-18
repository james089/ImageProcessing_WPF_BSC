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
using OpenCV_BSC_dll_x64;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// This is Babak's way, regarding the color as a point and calculate the distance, 
    /// works much better than hsv method
    /// </summary>
    class ColorDetection2
    {
        public static bool Color_detection(Image<Bgr, byte> imgOriginal, Image<Bgr, byte> object_img, out Image<Bgr, Byte> _outPutImg, double Tolerance)
        {
            /// Data property of an image returned by QueryFrame is always null. 
            /// Its image data is stored in unmanaged memory, therfore is not accessible thorough Data property. 
            /// To access the pixel values of a frame, you just have to clone it.

            Image<Bgr, byte> imgOriginal_clone = imgOriginal.Clone();

            byte[] meanValue = CalculateMeanBgr(object_img);
            _outPutImg = imgOriginal.CopyBlank();
            int pixelCounter = 0;

            byte[,,] data = imgOriginal_clone.Data;
            byte[] colorValue = new byte[3];
            for (int i = 0; i < imgOriginal_clone.Width; i++)
            {
                for (int j = 0; j < imgOriginal_clone.Height; j++)
                {
                    colorValue[0] = data[j, i, 0];
                    colorValue[1] = data[j, i, 1];
                    colorValue[2] = data[j, i, 2];

                    if (mMath.Distance3D(colorValue, meanValue) <= Tolerance)
                    {
                        _outPutImg.Data[j, i, 0] = 255;
                        _outPutImg.Data[j, i, 1] = 255;
                        _outPutImg.Data[j, i, 2] = 255;
                        pixelCounter++;
                    }
                    else
                    {
                        _outPutImg.Data[j, i, 0] = 0;
                        _outPutImg.Data[j, i, 1] = 0;
                        _outPutImg.Data[j, i, 2] = 0;
                    }
                }
            }
            return pixelCounter > 10 ? true : false;
        }

        private static byte[] CalculateMeanBgr(Image<Bgr, byte> img)
        {
            int mean_b = 0;
            int mean_g = 0;
            int mean_r = 0;

            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    mean_b += img.Data[j, i, 0];
                    mean_g += img.Data[j, i, 1];
                    mean_r += img.Data[j, i, 2];
                }
            }
            mean_b = mean_b / (img.Width * img.Height);
            mean_g = mean_g / (img.Width * img.Height);
            mean_r = mean_r / (img.Width * img.Height);

            return new byte[3] { (byte)mean_b, (byte)mean_g, (byte)mean_r };
        }
    }
}
