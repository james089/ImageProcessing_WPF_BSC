using CameraToImage_dll_x64;
using Emgu.CV;
using Emgu.CV.Structure;
using OpenCV_BSC_dll_x64;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities_BSC_dll_x64;

namespace ImageProcessing_BSC_WPF
{
    public class GF
    {
        public static void UpdateImgInfo()
        {
            // clear cropping rectangle
            if (ImageCropping.rect != null)
            {
                ImageCropping.rect.Width = 0;
                ImageCropping.rect.Height = 0;
            }
            if (GV.mCamera != null && GV.mCamera.IsConnected)
            {
                switch (GV._camSelected)
                {
                    case camType.WebCam:
                        Image<Bgr, byte> b = GV.mCamera.capture();
                        GV.imgHeight = b.Height;
                        GV.imgWidth = b.Width; break;
                    case camType.PointGreyCam:
                        Image<Bgr, byte> c = GV.mCamera.capture();
                        GV.imgHeight = c.Height;
                        GV.imgWidth = c.Width; break;
                }
            }
            else if (GV._pictureLoaded) // Static picture
            {
                GV.imgHeight = GV.imgOriginal.Height;
                GV.imgWidth = GV.imgOriginal.Width;
            }

            GV._zoomFactor = ImageCropping.zoomFactorCalculator(GV.imgWidth, GV.imgHeight, 4, 3, Windows.main.ibOriginal);
            Windows.main.TB_info_camera.Text = "Image size: (" + GV.imgWidth + "," + GV.imgHeight + ") " +
                                  "PictureBox size: (" + Windows.main.ibOriginal.ActualWidth.ToString("0.#") + "," +
                                  Windows.main.ibOriginal.ActualHeight.ToString("0.#") + ") " +
                                  "Zoom factor: " + GV._zoomFactor.ToString("0.##");
        }

        public static Image<Gray, Byte> ColorFilter(Image<Gray, Byte> img)
        {
            Image<Gray, Byte> thresh = img.ThresholdBinaryInv(new Gray(180), new Gray(255)).Not();

            //thresh._Erode(1);

            return thresh;
        }

        /*
        public static List<Bitmap> GetAllImg(string dir)
        {
            List<Bitmap> imgList = new List<Bitmap>();
            DirectoryInfo Folder = new DirectoryInfo(dir);
            FileInfo[] ImageInfo = Folder.GetFiles();

            for (int i = 0; i < ImageInfo.Length; i++)
            {
                Bitmap bm = new Bitmap(String.Format(@"{0}\{1}", dir, ImageInfo[i].Name));
                imgList.Add(bm);
                bm.Dispose();
            }
            return imgList;
        }
        */
    }
}
