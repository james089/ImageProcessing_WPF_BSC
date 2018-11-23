using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// Define contrast as white pixels / dark pixels, use 180 as the threshold
    /// </summary>
    class ContrastDetection
    {
        private const double CONTRAST_THRESHOLD = 180;
        private const double DETECTION_THRESHOLD = 0.1;

        /// <summary>
        /// Detect difference, return true means difference found
        /// </summary>
        /// <param name="Img"></param>
        /// <param name="Ref_Img"></param>
        /// <returns></returns>
        public static bool mContrastDetection(Image<Bgr, byte> Img, Image<Bgr, byte> Ref_Img)
        {
            double contrast_ref = calculateContrast(Ref_Img);
            double contrast = calculateContrast(Img);

            return (Math.Abs(contrast - contrast_ref) > DETECTION_THRESHOLD * contrast_ref);
        }

        private static double calculateContrast(Image<Bgr, byte> Img)
        {
            int darkCounter = 0;
            int whiteCounter = 0;
            for (int i = 0; i < Img.Width; i++)
            {
                for (int j = 0; j < Img.Height; j++)
                {
                    if (Img.Data[j, i, 0] > CONTRAST_THRESHOLD)
                        whiteCounter++;
                    else
                        darkCounter++;
                }
            }

            return (double)whiteCounter / (double)darkCounter;
        }
    }
}
