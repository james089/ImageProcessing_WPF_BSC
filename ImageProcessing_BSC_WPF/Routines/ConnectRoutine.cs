﻿using CameraToImage_dll_x64;
using Emgu.CV;
using Emgu.CV.Structure;
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

namespace ImageProcessing_BSC_WPF
{
    class ConnectRoutine
    {
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
            if (GV._cameraConnected) GF.UpdateImgInfo();
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

            GV.mCamera = new CameraConnection();

            camType ct = (camType)e.Argument;
            if (GV.mCamera != null)                                           //if there is a camera, dispose and reconnect.
                GV.mCamera.disposeCam();

            if (!GV.mCamera.connect(ct))
            {
                GV._cameraConnected = false;
                //mMessageBox.Show("No " + ct.ToString() + " found!");
            }
            else
                GV._cameraConnected = true;
            connectRoutine.ReportProgress(100);
        }
    }
}