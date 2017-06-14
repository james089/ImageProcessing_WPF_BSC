using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// This is used to check the center of a bright object
    /// </summary>
    public class CheckCenter
    {
        public static int centerChkBarPos_y = 100;              
        private static Rectangle centerChkBarRegion;
        private static Image<Bgr, byte> originalImage;

        public static void checkCenter()
        {
            originalImage = GV.imgOriginal;
            centerChkBarRegion = new Rectangle() { X = 0, Y = (int)(centerChkBarPos_y / GV._zoomFactor), Width = originalImage.Width, Height = 2 };
            originalImage.Draw(centerChkBarRegion, new Bgr(0, 255, 0), 2);            //draw the green bar
            GV.imgOriginal = originalImage;
        }
    }
}
