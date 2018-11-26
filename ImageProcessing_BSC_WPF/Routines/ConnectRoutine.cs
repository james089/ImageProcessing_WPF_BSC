using CameraToImage_dll_x64;
using Emgu.CV;
using Emgu.CV.Structure;
using FlyCapture2Managed;
using mUserControl_BSC_dll;
using mUserControl_BSC_dll.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Utilities_BSC_dll_x64;
using static ImageProcessing_BSC_WPF.Modules.PTCam;
using static ImageProcessing_BSC_WPF.Properties.Settings;

namespace ImageProcessing_BSC_WPF
{
    class ConnectRoutine
    {
        static camType ct;
        public static BackgroundWorker connectRoutine = new BackgroundWorker();
        public static void connectionSetup()
        {
            connectRoutine.DoWork += new DoWorkEventHandler(connectRoutine_doWork);
            connectRoutine.ProgressChanged += new ProgressChangedEventHandler(connectRoutine_ProgressChanged);
            connectRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(connectRoutine_WorkerCompleted);
            connectRoutine.WorkerReportsProgress = true;
            connectRoutine.WorkerSupportsCancellation = true;
        }

        public static void connectWebCam()
        {
            connectRoutine.RunWorkerAsync(camType.WebCam);
        }

        public static void connectPointGreyCam()
        {
            connectRoutine.RunWorkerAsync(camType.PointGreyCam);
        }


        private static void connectRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!Default.isEthernet && GV.mCamera.IsConnected) GF.UpdateImgInfo();
            else if(Default.isEthernet && mPTCam.mCameras[0].IsConnected()) GF.UpdateImgInfo();
        }

        private static void connectRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Image<Bgr, byte> connecting = new Image<Bgr, byte>(new Bitmap((int)Windows.main.ibOriginal.Width, (int)Windows.main.ibOriginal.Height));
            connecting.SetValue(new Bgr(Color.Gray));   // Set background color
            ShapeNDraw.drawString("Connecting", connecting, new System.Drawing.Point(connecting.Width / 3 - 10, connecting.Height / 2 - 10), 1, Color.White);

            Image<Bgr, byte> connected = new Image<Bgr, byte>(new Bitmap((int)Windows.main.ibOriginal.Width, (int)Windows.main.ibOriginal.Height));
            connected.SetValue(new Bgr(Color.Black));   // Set background color
            ShapeNDraw.drawString("Connected", connected, new System.Drawing.Point(connected.Width / 3 - 10, connected.Height / 2 - 10), 1, Color.White);


            if (e.ProgressPercentage == 0)
            {
                Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(connecting);
                Windows.main.Btn_PR.IsEnabled = false;
            }


            if (e.ProgressPercentage == 100)
            {
                Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(connected);
                //mNotification.Show("Connected");
                Windows.main.Btn_PR.IsEnabled = true;
                PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
            }
        }

        private static void connectRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            connectRoutine.ReportProgress(0);

            ct = (camType)e.Argument;

            if (ct == camType.PointGreyCam && Default.isEthernet)
            {
                if (mPTCam.mCameras[0] != null)
                    mPTCam.mCameras[0].Disconnect();
                
                if (mPTCam.CamConnection(mPTCam.CamSerialList))
                {
                    mPTCam.SetModeAndStartCapture(mPTCam.mCameras[0], Mode.Mode1);
                    connectRoutine.ReportProgress(100);
                }
            }
            else
            {
                GV.mCamera = new CameraConnection();
                if (GV.mCamera != null)                                           //if there is a camera, dispose and reconnect.
                    GV.mCamera.disposeCam();
                if (!GV.mCamera.connect(ct))
                {
                    GV._cameraConnected = false;
                    //mMessageBox.Show("No " + ct.ToString() + " found!");
                }
                else
                {
                    GV._cameraConnected = true;
                    connectRoutine.ReportProgress(100);
                }
            }

        }
    }
}
