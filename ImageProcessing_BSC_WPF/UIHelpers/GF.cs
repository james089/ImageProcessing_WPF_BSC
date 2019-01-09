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
using static ImageProcessing_BSC_WPF.Modules.PTCam;
using static ImageProcessing_BSC_WPF.Properties.Settings;

namespace ImageProcessing_BSC_WPF
{
    public class GF
    {
        public static void UpdateImgInfo()
        {
            // clear cropping rectangle
            if (ImgCropping.rect != null)
            {
                ImgCropping.rect.Width = 0;
                ImgCropping.rect.Height = 0;
            }
            if (GV.mCamera != null && GV.mCamera.IsConnected)
            {
                switch (GV._camSelected)
                {
                    case camType.WebCam:
                        Image<Bgr, byte> b = GV.mCamera.capture();
                        if (b == null) return;
                        GV.imgHeight = b.Height;
                        GV.imgWidth = b.Width; break;
                    case camType.PointGreyCam:
                        Image<Bgr, byte> c = GV.mCamera.capture();
                        if (c == null) return;
                        GV.imgHeight = c.Height;
                        GV.imgWidth = c.Width; break;
                }
            }
            else if (Default.isEthernet && mPTCam.mCameras[0].IsConnected())
            {
                Image<Bgr, byte> c = mPTCam.capture(mPTCam.mCameras[0], 0);
                if (c == null) return;
                GV.imgHeight = c.Height;
                GV.imgWidth = c.Width; 
            }
            else if (GV.IsPictureLoaded) // Static picture
            {
                GV.imgHeight = GV.imgOriginal.Height;
                GV.imgWidth = GV.imgOriginal.Width;
            }

            GV._zoomFactor = ImgCropping.zoomFactorCalculator(GV.imgWidth, GV.imgHeight, 4, 3, Windows.main.ibOriginal);
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

        public static string OpenDirectoryDialog()
        {
            // Create an instance of the open file dialog box.
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            // Process input if the user clicked OK.
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return "";
        }
    }
}
