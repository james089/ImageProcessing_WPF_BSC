using System;
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
using mUserControl_BSC_dll;
using Utilities_BSC_dll_x64;
using OpenCV_BSC_dll_x64.Windows;
using OpenCV_BSC_dll_x64.General;
using ImageProcessing_BSC_WPF.Modules.MachineLearning;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;
using ImageProcessing_BSC_WPF.Modules.ZxingDecoder;
using ImageProcessing_BSC_WPF.Modules.OCR;
using ImageProcessing_BSC_WPF.Modules;
using mUserControl_BSC_dll.UserControls;
using ImageProcessing_BSC_WPF.Modules.CortexDecoder;
using static ImageProcessing_BSC_WPF.Modules.MachineLearning.YOLO.YoloSharpCore;
using static ImageProcessing_BSC_WPF.Modules.PTCam;
using static ImageProcessing_BSC_WPF.Properties.Settings;

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
        public static bool IsMonoViewEnabled = false;
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
            if (!GV.IsCameraConnected)
            {
                mMessageBox.Show("No camera connected");
                return;
            }
            MainWindow.mMainWindow.Btn_PR.Content = "Pause";
            IsCapturing = true;
            MainWindow.mMainWindow.Panel_staticImageOperation.IsEnabled = false;
            if(!previewRoutine.IsBusy)
                previewRoutine.RunWorkerAsync(previewFPS);
        }

        public static void StopPreview()
        {
            MainWindow.mMainWindow.Btn_PR.Content = "Resume";
            IsCapturing = false;
            MainWindow.mMainWindow.Panel_staticImageOperation.IsEnabled = true;
            previewRoutine.CancelAsync();
        }


        private static void previewRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsCapturing = false;
            mNotification.Show("Live view stopped");
        }

        private static void previewRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (GV.imgOriginal == null) return;
            GUIUpdates();
        }

        public static void GUIUpdates()
        {
            //MainWindow.mMainWindow.TB_info.Text = GV.liveViewMessage;
            MainWindow.mMainWindow.listBox.Items.Clear();
            MainWindow.mMainWindow.lbl_barcodeDetectResult.Content = "";

            //Detect code
            if (GV._decodeSwitch)
            {
                if (GV.mDecoderEngine == DecoderEngine.Zxing)
                {
                    if (ZxingDecoder.outputStringList != null && ZxingDecoder.outputStringList.Count != 0)
                        MainWindow.mMainWindow.lbl_barcodeDetectResult.Content = ZxingDecoder.outputStringList[0];
                }
                else if (GV.mDecoderEngine == DecoderEngine.Cortex)
                {
                    MainWindow.mMainWindow.lbl_barcodeDetectResult.Content = CortexDecoder.ResultString;
                    MainWindow.mMainWindow.listBox.Items.Add(CortexDecoder.FullResult.corner0);
                    MainWindow.mMainWindow.listBox.Items.Add(CortexDecoder.FullResult.corner1);
                    MainWindow.mMainWindow.listBox.Items.Add(CortexDecoder.FullResult.corner2);
                    MainWindow.mMainWindow.listBox.Items.Add(CortexDecoder.FullResult.corner3);
                    MainWindow.mMainWindow.listBox.Items.Add(CortexDecoder.BondRec);

                    GV.imgProcessed.Draw(CortexDecoder.BondRec, new Bgr(0, 0, 255), 3);
                }
            }

            if (GV._OCRSwitch)
            {
                MainWindow.mMainWindow.lbl_OCR.Content = OCR.detectedOCRString;
                MainWindow.mMainWindow.ibOCR.Source = ImgConverter.ToBitmapSource(GV.OCROutputImg);
            }

            if (GV._MLSwitch)
            {
                switch (MLCore.MLModelSelected)
                {
                    case MLModel.ResNet:
                        for (int i = 0; i < ResNet.resultList.Count; i++)
                        {
                            MainWindow.mMainWindow.listBox.Items.Add(string.Format("{0}: {1}", MLCore.MLSelectedLabels[i], ResNet.resultList[i]));
                        }
                        if (ResNet.OutputProbablility > 0)
                            BindManager.BindMngr.GMessage.value = string.Format("This must be a {0}!", ResNet.OutputString, ResNet.OutputProbablility);
                        else
                            BindManager.BindMngr.GMessage.value = "This doesn't look like anything to me... probably a " + ResNet.OutputString + "?";
                        break;
                    case MLModel.Yolo:

                        break;
                }
            }

            // Motion Detection
            if (GV._motionDetectSwitch)
            {
                if (MotionDetection.checkMotion(GV.imgOriginal))
                {
                    GV.CaptureSound.Play();
                    StopPreview();
                    if (mMessageBox.showNotification("Motion Detected") == mDialogResult.yes)
                        startPreview(_previewFPS);
                }

            }


            // Normal
            if (GV.imgProcessed == null) GV.imgProcessed = GV.imgOriginal;
            MainWindow.mMainWindow.ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgProcessed);


            // Error reporting
            if (GV._err != ErrorCode.Normal) BindManager.BindMngr.GMessage.value = GV._err.ToString();
        }

        private static void previewRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            IsCapturing = true;
            previewFPS FPS = (previewFPS)e.Argument;
            while (!previewRoutine.CancellationPending)
            {
                if (GV.mCamera == null)
                {
                    previewRoutine.CancelAsync();
                    return;
                }

                //==== Display Original image==========
                switch (GV._camSelected)
                {
                    case camType.WebCam:
                        GV.imgOriginal = GV.mCamera.capture(); break;
                    case camType.PointGreyCam:
                        if (Default.isEthernet)
                            GV.imgOriginal = mPTCam.capture(mPTCam.mCameras[0], 0);
                        else
                            GV.imgOriginal = GV.mCamera.capture(); break;
                }

                if(GV.imgOriginal != null)
                    GV.imgOriginal_pure = GV.imgOriginal.Copy();

                originalImageProcessing();

                processedImageDisplaying();
                
                previewRoutine.ReportProgress(0);

                Thread.Sleep(1000 / (int)FPS);
            }
        }


        public static void processedImageDisplaying()
        {
            //====Display Processed image========== 
            if (NCVFuns._featureType != featureDetectionType.original)
                GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, NCVFuns._detectionType, out GV._err);
            else if(GV.imgOriginal != null)
                GV.imgProcessed = GV.imgOriginal.Copy(new Rectangle(new System.Drawing.Point(), GV.imgOriginal.Size));

            if (NCVFuns._detectionType == DetectionType.Object) GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, DetectionType.Object, out GV._err);

            #region == MIS ==
            // Checking distance
            if (GV._findMinSwitch)
            {
                FindMinDistance.findMinDistance();
            }

            // Find Boundry
            else if (GV._checkBoundry && ImgCropping.rect.Size != new Size(0, 0))
            {
                if (CheckBoundry.mCheckBoundry(GV.imgOriginal, ImgCropping.rect))
                {
                    GV.imgProcessed.Draw(ImgCropping.rect, new Bgr(Color.Green), 2);
                }
                else
                    GV.imgProcessed.Draw(ImgCropping.rect, new Bgr(Color.Red), 2);
            }
            else if (GV._fitEllipse)
            {
                Rectangle box = NCVFuns.SquareFitting(NCVFuns.FindWhitePoints(GV.imgProcessed.Convert<Gray, byte>()));
                GV.imgProcessed.Draw(box, new Bgr(Color.Blue), 2);
            }

            #endregion == MIS ==

            // Decoding
            else if (GV._decodeSwitch)
            {
                if (GV.mDecoderEngine == DecoderEngine.Zxing)
                    ZxingDecoder.Decode(GV.imgOriginal.ToBitmap());
                else if (GV.mDecoderEngine == DecoderEngine.Cortex)
                {
                    CortexDecoder.Decode(GV.imgOriginal.ToBitmap());
                }
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
                switch (MLCore.MLModelSelected)
                {
                    case MLModel.ResNet:
                        ResNet.startMLRoutine();
                        break;
                    case MLModel.Yolo:
                        GV.imgProcessed = new Image<Bgr, byte>(mYolo.Detect(GV.imgOriginal.ToBitmap()));
                        break;
                }
            }
        }

        public static void originalImageProcessing()
        {
            // Inverse color
            if (Setting.IsColorInverseEnabled) GV.imgOriginal = ImageProcessing.colorInvert(GV.imgOriginal);

            // Color filtering
            if (Setting.IsColorFilterEnabled) GV.imgOriginal = ImageProcessing.colorFilter(GV.imgOriginal.Convert<Gray, byte>()).Convert<Bgr, byte>();

            // Cropped View
            if (ImgCropping.rect.Size != new System.Drawing.Size(0, 0) && IsCropViewEnabled)
                GV.imgOriginal = GV.imgOriginal.Copy(ImgCropping.rect);

            // Black and white View
            if (IsMonoViewEnabled)
                GV.imgOriginal = GV.imgOriginal.Convert<Gray, byte>().Convert<Bgr, byte>();
        }
    }
}
