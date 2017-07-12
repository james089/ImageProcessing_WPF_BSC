﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCV_BSC_dll_x64;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.Structure;
using System.ComponentModel;
using CameraToImage_dll_x64;
using System.Threading;
using System.Windows;
using ImageProcessing_BSC_WPF.Modules;
using mUserControl_BSC_dll_x64;
using Utilities_BSC_dll_x64;
using OpenCV_BSC_dll_x64.Windows;
using OpenCV_BSC_dll_x64.General;
using ImageProcessing_BSC_WPF.MachineLearning;

namespace ImageProcessing_BSC_WPF
{
    public enum previewFPS
    {
        LOW = 5,
        MEDIUM = 30,
        HIGH = 60
    }

    public class PreviewRoutine
    {
        public static BackgroundWorker previewRoutine = new BackgroundWorker();
        public static bool IsCropViewEnabled = false;
        public static bool IsCapturing = false;
        public static previewFPS _previewFPS;

        public static void previewSetup()
        {
            previewRoutine.DoWork += new DoWorkEventHandler(previewRoutine_doWork);
            previewRoutine.ProgressChanged += new ProgressChangedEventHandler(previewRoutine_ProgressChanged);
            previewRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(previewRoutine_WorkerCompleted);
            previewRoutine.WorkerReportsProgress = true;
            previewRoutine.WorkerSupportsCancellation = true;
        }
        
        public static void startPreview(previewFPS previewFPS)
        {
            GV.mMainWindow.Btn_PR.Content = "Pause";
            IsCapturing = true;
            GV.mMainWindow.Btn_staticProcess.IsEnabled = false;
            previewRoutine.RunWorkerAsync(previewFPS);
        }

        public static void stopPreview()
        {
            GV.mMainWindow.Btn_PR.Content = "Resume";
            IsCapturing = false;
            GV.mMainWindow.Btn_staticProcess.IsEnabled = true;
            previewRoutine.CancelAsync();
        }


        private static void previewRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsCapturing = false;
        }

        private static void previewRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            GUIUpdates();
        }

        public static void GUIUpdates()
        {
            GV.mMainWindow.TB_info.Text = GV.liveViewMessage;
            //GV.mMainWindow.listBox.Items.Clear();
            //Detect code
            if (GV._decodeSwitch)
            {
                if (BarcodeDecoder.outputStringList[0] != null) GV.mMainWindow.lbl_barcodeDetectResult.Content = BarcodeDecoder.outputStringList[0];
            }

            if (GV._OCRSwitch)
            {
                GV.mMainWindow.lbl_OCR.Content = OCR.detectedOCRString;
                GV.mMainWindow.ibOCR.Source = Converter.ToBitmapSource(GV.OCROutputImg);
            }

            if (GV._MLSwitch)
            {
                if (ResNet.OutputProbablility > 8)
                    GV.liveViewMessage = "This must be a " + ResNet.OutputString + "! {" + ResNet.OutputProbablility.ToString("0.##") + "}";
                else
                    GV.liveViewMessage = "This doesn't look like anything to me... probably a " + ResNet.OutputString + "?";
            }
            // Normal
            GV.mMainWindow.ibOriginal.Source = Converter.ToBitmapSource(GV.imgProcessed);
            // Error reporting
            if (GV._err != ErrorCode.Normal) GV.mMainWindow.TB_info.Text = GV._err.ToString();
        }

        private static void previewRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            IsCapturing = true;
            previewFPS FPS = (previewFPS)e.Argument;
            while (!previewRoutine.CancellationPending)
            {
                if (GV.mCamera == null)
                {
                    mMessageBox.Show("No camera detected!");
                    previewRoutine.CancelAsync();
                    return;
                }

                //==== Display Original image==========
                switch (GV._camSelected)
                {
                    case camType.WebCam:
                        GV.imgOriginal = GV.mCamera.capture(); break;
                    case camType.PointGreyCam:
                        GV.imgOriginal = GV.mCamera.capture(); break;
                }

                originalImageProcessing();

                processedImageDisplaying();
                
                previewRoutine.ReportProgress(0);

                Thread.Sleep(1000 / (int)FPS);
            }
        }

        public static void processedImageDisplaying()
        {
            //====Display Processed image========== 
            if (NCVFuns._featureType != featureDetectionType.original) GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, NCVFuns._detectionType, out GV._err);
            else GV.imgProcessed = GV.imgOriginal.Copy(new Rectangle(new System.Drawing.Point(), GV.imgOriginal.Size));

            if (NCVFuns._detectionType == DetectionType.Object) GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, DetectionType.Object, out GV._err);

            // Checking center
            if (GV._findCenterSwitch)
            {
                CheckCenter.checkCenter();
            }

            // Checking distance
            else if (GV._findMinSwitch)
            {
                FindMinDistance.findMinDistance();
            }

            // Decoding
            else if (GV._decodeSwitch)
            {
                BarcodeDecoder.decoding();
            }

            // OCR detection
            else if (GV._OCRSwitch)
            {
                if (OCR.croppedOCRArea.Width * OCR.croppedOCRArea.Height != 0) OCR.croppedOriginalImg = GV.imgOriginal.Copy(OCR.croppedOCRArea);
                else OCR.croppedOriginalImg = GV.imgOriginal;
                OCR.detectedOCRString = OCR.OCRDetect(OCR.croppedOriginalImg, out GV.OCROutputImg);
            }
            // OCR cropped Area display (Red rectangle)
            if (OCR.croppedOCRArea.Width * OCR.croppedOCRArea.Height != 0)
            {
                GV.imgProcessed.Draw(OCR.croppedOCRArea, new Bgr(Color.Red), 2);
            }

            // Machine Learing
            else if (GV._MLSwitch)
            {
                ResNet.startMLRoutine();

            }
        }

        public static void originalImageProcessing()
        {
            // Inverse color
            if (Setting.IsColorInverseEnabled) GV.imgOriginal = ImageProcessing.colorInvert(GV.imgOriginal);

            // Color filtering
            if (Setting.IsColorFilterEnabled) GV.imgOriginal = ImageProcessing.colorFilter(GV.imgOriginal.Convert<Gray, byte>()).Convert<Bgr, byte>();

            // Cropped View
            if (ImageCropping.rect.Size != new System.Drawing.Size(0, 0) && IsCropViewEnabled)
                GV.imgOriginal = GV.imgOriginal.Copy(ImageCropping.rect);
        }
    }
}
