using System;
using System.Windows;
using System.Windows.Input;

using Utilities_BSC_dll_x64;
using OpenCV_BSC_dll_x64;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Media;
using System.Windows.Data;
using System.Threading;
using System.Reflection;
using CameraToImage_dll_x64;
using ImageProcessing_BSC_WPF.Modules;
using mUserControl_BSC_dll;
using System.Drawing;
using OpenCV_BSC_dll_x64.Windows;
using CameraToImage_dll_x64.Windows;
using ImageProcessing_BSC_WPF.Modules.MachineLearning;
using System.IO;
using ImageProcessing_BSC_WPF.Modules.OCR;
using ImageProcessing_BSC_WPF.Modules.ZxingDecoder;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;
using mUserControl_BSC_dll.UserControls;
using ImageProcessing_BSC_WPF.Modules.CortexDecoder;
using static ImageProcessing_BSC_WPF.BindManager;
using static ImageProcessing_BSC_WPF.Modules.PTCam;
using static ImageProcessing_BSC_WPF.Modules.MachineLearning.YOLO.YoloSharpCore;
using static ImageProcessing_BSC_WPF.Properties.Settings;
using System.Linq;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.YOLO;
using System.Collections.Generic;

namespace ImageProcessing_BSC_WPF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Local setup
        LoadingScreen loadingScreen = new LoadingScreen();
        public static TrainingWindow tw = new TrainingWindow();
        #endregion Local setup

        public MainWindow()
        {
            //Static MainWindow
            Windows.main = this;
            DataContext = BindMngr;

            loadingScreen.Show();
            InitializeComponent();

            // Setup background worker
            PreviewRoutine.previewSetup();
            ConnectRoutine.connectionSetup();
            ImageResizing.ImageResizingSetup();
            ResNet.CNTK_ResNetSetup();
            ZxingDecoder.DecoderSetup();
            YoloSharpCore.mYolo.TrainModelRoutineSetup();

            OCR.OCRSetup(OCRMode.NUMBERS);

            // Create Directories
            GV.ML_Folders[(int)MLFolders.MLRoot]        = Environment.CurrentDirectory + MLFolders.MLRoot.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_CNTK]       = Environment.CurrentDirectory + MLFolders.ML_CNTK.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_CNTK_model] = Environment.CurrentDirectory + MLFolders.ML_CNTK_model.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_YOLO]       = Environment.CurrentDirectory + MLFolders.ML_YOLO.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_YOLO_backup] = Environment.CurrentDirectory + MLFolders.ML_YOLO_backup.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_YOLO_model] = Environment.CurrentDirectory + MLFolders.ML_YOLO_model.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_YOLO_data]  = Environment.CurrentDirectory + MLFolders.ML_YOLO_data.GetDescription();
            GV.ML_Folders[(int)MLFolders.ML_YOLO_data_img]   = Environment.CurrentDirectory + MLFolders.ML_YOLO_data_img.GetDescription();

            foreach (string str in GV.ML_Folders)
            {
                if (str != null && !Directory.Exists(str))
                {
                    Directory.CreateDirectory(str);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title.Content = "Image processing vers." + Assembly.GetExecutingAssembly().GetName().Version;

            loadProgramSetting();
            applyProgramSetting();

            // Show ML Model types
            ML_cmb_model.ItemsSource = Enum.GetValues(typeof(MLModel)).Cast<MLModel>();
            ML_cmb_model.SelectedIndex = 2;
        }

        private void loadProgramSetting()
        {
            MLCore.MLSelectedLabels = DataSet.LabelSet[ML_cmb_dataset.SelectedIndex];

            GV._camSelected = (camType)Properties.Settings.Default.camSelection;
            GV._camConnectAtStartup = Properties.Settings.Default.camConnect;
            PreviewRoutine._previewFPS = (previewFPS)Properties.Settings.Default.previewFPS;
        }

        private void applyProgramSetting()
        {
            if (Properties.Settings.Default.useCortexDecoder)
            {
                Radio_cortex.IsEnabled = true;
                Radio_cortex.IsChecked = true;
                CortexDecoder.DecoderSetup();
            }
            else
            {
                Radio_cortex.IsEnabled = false;
                Radio_cortex.IsChecked = false;
                Radio_zxing.IsChecked = true;
            }
            Chk_connectCam.IsChecked = GV._camConnectAtStartup;
            selectCam(GV._camSelected);

            Radio_original.IsChecked = true;
            Radio_SURF.IsChecked = true;
            toggleExpander_object(false);

            Chk_isEthernet.IsChecked = Default.isEthernet;
            Slider_contrastSensitivity.Value = Default.contrast_sensitivity;
        }

        private void selectCam(camType index)
        {
            switch (index)
            {
                case camType.WebCam:
                    Radio_webcam.IsChecked = true;
                    Radio_PTcam.IsChecked = false;
                    break;
                case camType.PointGreyCam:
                    Radio_webcam.IsChecked = false;
                    Radio_PTcam.IsChecked = true;
                    break;
            }
        }

        private void saveProgramSetting()
        {
            Properties.Settings.Default.camSelection = (int)GV._camSelected;
            Properties.Settings.Default.camConnect = GV._camConnectAtStartup;
            Properties.Settings.Default.previewFPS = (int)PreviewRoutine._previewFPS;

            Properties.Settings.Default.Save();
        }


        #region<Window operation and Mouse Events>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            saveProgramSetting();
            Application.Current.Shutdown();
        }

        private void Btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        #region Img original Panel

        bool _mouseDown = false;
        Image<Bgr, byte>[] regionImgSet = new Image<Bgr, byte>[3]{new Image<Bgr, byte>(0 , 0), new Image<Bgr, byte>(0, 0), new Image<Bgr, byte>(0, 0)};
        Image<Bgr, byte> finalColorRegionImg;
        private void ibOriginal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = true;
            if (GV.imgOriginal != null)
            {
                PreviewRoutine.StopPreview();
                ibOriginal.Cursor = Cursors.Cross;
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(ibOriginal).X;
                p.Y = (int)e.GetPosition(ibOriginal).Y;

                if ((bool)Radio_Color.IsChecked && (bool)Chk_multiColorPoints.IsChecked)
                {
                    if (GV._remainColorPoints > 0)
                    {
                        Rectangle bond = new Rectangle(
                            p.X - GV._colorRegionSize / 2, p.Y - GV._colorRegionSize / 2, 
                            GV._colorRegionSize, GV._colorRegionSize);

                        GV.imgOriginal.Draw(bond, new Bgr(Color.AliceBlue), 1);

                        Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgOriginal);

                        regionImgSet[GV._remainColorPoints - 1] = GV.imgOriginal_pure.Copy(bond);

                        GV._remainColorPoints--;
                    }
                }
                else
                {
                    ImgCropping.WPF_mouseDown(GV.imgOriginal, ibOriginal, p, GV._zoomFactor);
                }
            }
        }

        private void ibOriginal_MouseMove(object sender, MouseEventArgs e)
        {
            if ((bool)Chk_showRGB.IsChecked && GV.imgOriginal != null)
            {
                Color clr = GV.imgOriginal.ToBitmap().GetPixel((int)e.GetPosition(ibOriginal).X, (int)e.GetPosition(ibOriginal).Y);

                listBox.Items.Clear();
                listBox.Items.Add(string.Format("R:{0}, G:{1}, B:{2}", clr.R, clr.G, clr.B));
            }
            else if (GV.imgOriginal != null)
            {
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(ibOriginal).X;
                p.Y = (int)e.GetPosition(ibOriginal).Y;
                ImgCropping.WPF_mouseMove(ibOriginal, p, GV._zoomFactor);
            }
        }

        private void ibOriginal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = false;
            if (GV.imgOriginal != null)
            {
                if ((bool)Radio_Color.IsChecked && (bool)Chk_multiColorPoints.IsChecked)
                {
                    if (GV._remainColorPoints == 0)
                    {
                        finalColorRegionImg =
                            imageCombine(imageCombine(regionImgSet[0], regionImgSet[1]), regionImgSet[2]);
                        Chk_multiColorPoints.IsChecked = false;
                        Windows.main.ibObject.Source = ImgConverter.ToBitmapSource(finalColorRegionImg);
                    }
                }
                else
                {
                    ibOriginal.Cursor = Cursors.Arrow;
                    ImgCropping.WPF_mouseUp(ibOriginal, GV._zoomFactor);
                }
            }
        }

        private Image<Bgr, Byte> imageCombine(Image<Bgr, Byte> image1, Image<Bgr, Byte> image2)
        {
            int ImageWidth = 0;
            int ImageHeight = 0;

            //get max width
            if (image1.Width > image2.Width)
                ImageWidth = image1.Width;
            else
                ImageWidth = image2.Width;

            //calculate new height
            ImageHeight = image1.Height + image2.Height;

            //declare new image (large image).
            Image<Bgr, Byte> imageResult = new Image<Bgr, Byte>(ImageWidth, ImageHeight);


            imageResult.ROI = new Rectangle(0, 0, image1.Width, image1.Height);
            image1.CopyTo(imageResult);
            imageResult.ROI = new Rectangle(0, image1.Height, image2.Width, image2.Height);
            image2.CopyTo(imageResult);

            imageResult.ROI = Rectangle.Empty;


            return imageResult;
        }

        #endregion Img original Panel

        #endregion</Window operation>

        #region <Menu operation>

        private void File_loadImg_Click(object sender, RoutedEventArgs e)
        {
            Bitmap loadPic = Tools.loadPicture_withDialog();
            if (loadPic != null)
            {
                GV.imgOriginal_save = ImgStiching.createFixRatioBitmap((new Image<Bgr, byte>(loadPic)).Copy(), 4, 3);
                GV.imgOriginal = GV.imgOriginal_save.Copy();
                ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgOriginal);
                GV.IsPictureLoaded = true;
                GF.UpdateImgInfo();
            }
        }
        private void File_loadContour_Click(object sender, RoutedEventArgs e)
        {
            Tools.loadContour();
        }

        private void Setting_openCV_Click(object sender, RoutedEventArgs e)
        {
            GV.mSetting = new Setting();
            GV.mSetting.Show();
        }

        private void Setting_camera_Click(object sender, RoutedEventArgs e)
        {
            if (GV.mCamera == null)
            {
                mMessageBox.Show("Connect a camera first!");
                return;
            }

            if (GV._camSelected == camType.PointGreyCam)
            {
                if (GV.mCamera.m_camCtlDlg.IsVisible())
                {
                    GV.mCamera.m_camCtlDlg.Hide();
                }
                else
                {
                    GV.mCamera.m_camCtlDlg.Show();
                }
            }
            else if (GV._camSelected == camType.WebCam)
            {
                mMessageBox.Show("Currentlly unavailable for webcam");
            }
        }

        private void Setting_preference_Click(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.StopPreview();
            Preference preferenceWindow = new Preference();
            preferenceWindow.preferenceUpdated += preferenceWindow_preferenceUpdated;
            preferenceWindow.Show();
        }

        private void preferenceWindow_preferenceUpdated(object sender, EventArgs e)
        {
            if(GV.IsCameraConnected)
                PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
        }

        private void Menu_conversion_Click(object sender, RoutedEventArgs e)
        {
            if (GV.mCamera != null && GV.mCamera.IsConnected)
            {
                PreviewRoutine.StopPreview();

                GV.mConvert = new Conversion(GV.mCamera, GV.imgWidth, GV.imgHeight);
                GV.mConvert.ShowDialog();
            }
            else
                mMessageBox.Show("Connect a camera first!");
        }


        #endregion  <Menu operation>


        /// <summary>
        /// GUI Operation
        /// </summary>
        #region <GUI operation>
        private void Radio_webcam_Checked(object sender, RoutedEventArgs e)
        {
            if (!Radio_webcam.IsLoaded) return;

            PreviewRoutine.StopPreview();
            GV._camSelected = camType.WebCam;
            if (GV._camConnectAtStartup)                                          //connect cam automatically
            {
                ConnectRoutine.connectWebCam();
                //Chk_connectCam.IsEnabled = false;                              //disable chk box for connecting the camera
            }
            else
            {
                //Chk_connectCam.IsEnabled = true;
                Chk_connectCam.IsChecked = false;
            }

            saveProgramSetting();
        }

        private void Radio_PTcam_Checked(object sender, RoutedEventArgs e)
        {
            if (!Radio_PTcam.IsLoaded) return;

            PreviewRoutine.StopPreview();
            GV._camSelected = camType.PointGreyCam;
            if (GV._camConnectAtStartup)
            {
                ConnectRoutine.connectPointGreyCam();
            }
            else
            {
                Chk_connectCam.IsChecked = false;
            }

            saveProgramSetting(); 
        }

        private void Chk_connectCam_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            switch (GV._camSelected)
            {
                case camType.WebCam:
                    ConnectRoutine.connectWebCam(); break;
                case camType.PointGreyCam:
                    ConnectRoutine.connectPointGreyCam();
                    break;
            }

            Panel_liveViewOptions.IsEnabled = true;
        }

        private void Chk_connectCam_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (PreviewRoutine.IsCapturing)
                PreviewRoutine.StopPreview();
            if (GV.mCamera.IsConnected)                                           //if there is a camera, dispose and reconnect.
            {
                GV.mCamera.disposeCam();
                GV.mCamera.IsConnected = false;
            }

            Panel_liveViewOptions.IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)                    //Preview Button
        {
            if ((bool)Chk_connectCam.IsChecked)
            {
                if (PreviewRoutine.IsCapturing == true)
                {
                    PreviewRoutine.StopPreview();                    //stop capturing 
                    //Chk_connectCam.IsEnabled = false;
                    if (GV.imgOriginal != null)
                    {
                        GV.IsPictureLoaded = true;
                    }
                    PreviewRoutine.IsCapturing = false;
                }
                else
                {
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
                    //Chk_connectCam.IsEnabled = false;
                    PreviewRoutine.IsCapturing = true;
                }
            }
            else
                mMessageBox.Show("Please connect camera first!");
        }

        private void Btn_staticProcess_Click(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.originalImageProcessing();
            PreviewRoutine.processedImageDisplaying();
            PreviewRoutine.GUIUpdates();
        }

        private void Btn_staticReset_Click(object sender, RoutedEventArgs e)
        {
            if (GV.imgOriginal_save != null) GV.imgOriginal = GV.imgOriginal_save.Copy();
            ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgOriginal);
        }


        private void Btn_capture_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewRoutine.IsCapturing && GV.imgOriginal != null)
            {
                if (Combo_saveOption.SelectedItem == Combo_originalImage)
                {
                    ibOriginal.Source = null;
                    GV.CaptureSound.Play();
                    Thread.Sleep(200);
                    Tools.savePicture_withDialog(GV.imgOriginal.ToBitmap());
                }
                else
                {
                    ibOriginal.Source = null;
                    GV.CaptureSound.Play();
                    Thread.Sleep(200);
                    Tools.savePicture_withDialog(GV.imgProcessed.ToBitmap());
                }
            }

        }

        private void Tools_cropView_Checked(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.IsCropViewEnabled = true;
            if (!PreviewRoutine.IsCapturing) PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
        }

        private void Tools_cropView_Unchecked(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.IsCropViewEnabled = false;
        }

        private void Chk_mono_Checked(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.IsMonoViewEnabled = true;
        }

        private void Chk_mono_Unchecked(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.IsMonoViewEnabled = false;
        }

        #region Feature Detection
        private void Radio_original_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._featureType = featureDetectionType.original;
        }

        private void Radio_cannyEdge_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._featureType = featureDetectionType.cannyEdge;
        }

        private void Radio_contour_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._featureType = featureDetectionType.contour;
        }

        private void Radio_line_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._featureType = featureDetectionType.line;
        }

        #endregion Feature Detection

        #region Object Detection
        private void Btn_setObject_Click(object sender, RoutedEventArgs e)
        {
            if (finalColorRegionImg != null)
            {
                GV.object_img = finalColorRegionImg.Copy();
                finalColorRegionImg.Dispose();

                if (GV.mCamera.IsConnected)
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
            }
            else if (ImgCropping.rect.Width * ImgCropping.rect.Height != 0)
            {
                Image<Bgr, byte> Img = GV.imgOriginal;
                GV.object_img = Img.Copy(ImgCropping.rect).Convert<Bgr, byte>(); //new Image<Gray, Byte>(mCrop.cropBitmap(imgOriginal.ToBitmap(), mCrop.rect));
                ibObject.Source = ImgConverter.ToBitmapSource(GV.object_img);

                if (GV.mCamera.IsConnected)
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
            }
        }

        private void Btn_apply_object_Click(object sender, RoutedEventArgs e)
        {
            if (GV.object_img != null && GV.imgOriginal != null)
            {
                //====Processed image==========
                GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, DetectionType.Object, out GV._err);
                ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgProcessed);
            }
            else if (GV.object_img == null)
            {
                GV._err = ErrorCode.No_object_image;
            }

            BindMngr.GMessage.value = GV._err.ToString();
        }

        private void Radio_SURF_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._objectType = objectDetectionType.SURF;
        }

        private void Radio_FFT_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._objectType = objectDetectionType.FFT;
        }

        private void Radio_Color_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._objectType = objectDetectionType.color;
            Chk_multiColorPoints.Visibility = Visibility.Visible;

        }
        private void Radio_Color_Unchecked(object sender, RoutedEventArgs e)
        {
            Chk_multiColorPoints.Visibility = Visibility.Collapsed;
        }

        private void Chk_multiColorPoints_Checked(object sender, RoutedEventArgs e)
        {
            if (PreviewRoutine.IsCapturing)
            {
                PreviewRoutine.StopPreview();
                Thread.Sleep(300);
                if (GV.imgOriginal_pure == null)
                    Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgOriginal_pure);
            }
        }
        private void Chk_multiColorPoints_Unchecked(object sender, RoutedEventArgs e)
        {
            GV._remainColorPoints = 3;
        }

        private void Chk_enableObjectD_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._detectionType = DetectionType.Object;
            toggleExpander_object(true);
            Expander_feature.IsEnabled = false;
        }

        private void Chk_enableObjectD_Unchecked(object sender, RoutedEventArgs e)
        {
            NCVFuns._detectionType = DetectionType.Feature;

            toggleExpander_object(false);
            Expander_feature.IsEnabled = true;
        }

        private void toggleExpander_object(bool bb)
        {
            Btn_setObject.IsEnabled = bb;
            Btn_apply_object.IsEnabled = bb;
            Dock_objectType.IsEnabled = bb;
        }
        #endregion Object Detection

        #region MIS
        private void Chk_findMin_Checked(object sender, RoutedEventArgs e)
        {
            GV._findMinSwitch = (bool)Chk_findMin.IsChecked;
        }

        private void Chk_motionDetect_Checked(object sender, RoutedEventArgs e)
        {
            GV._motionDetectSwitch = (bool)Chk_motionDetect.IsChecked;
        }
        #endregion

        #region Barcode Decoder
        private void Chk_liveDecoding_Checked(object sender, RoutedEventArgs e)
        {
            GV._decodeSwitch = (bool)Chk_liveDecoding.IsChecked;
        }

        private void Btn_decode_Click(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.StopPreview();
            if (GV.mDecoderEngine == DecoderEngine.Zxing)
                ZxingDecoder.StartDecodeRoutine();
            else if (GV.mDecoderEngine == DecoderEngine.Cortex)
            {
                CortexDecoder.Decode(GV.imgOriginal.ToBitmap());
                lbl_barcodeDetectResult.Content = CortexDecoder.ResultString;
            }
        }

        private void Radio_zxing_Checked(object sender, RoutedEventArgs e)
        {
            GV.mDecoderEngine = DecoderEngine.Zxing;
        }

        private void Radio_cortex_Checked(object sender, RoutedEventArgs e)
        {
            GV.mDecoderEngine = DecoderEngine.Cortex;
        }
        #endregion Barcode Decoder

        #region OCR
        private void Chk_OCR_live_Checked(object sender, RoutedEventArgs e)
        {
            GV._OCRSwitch = (bool)Chk_OCR_live.IsChecked;
        }

        private void Btn_ocrOneTime_Click(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.StopPreview();
            OCR.startOCRRoutine();
        }

        private void Menu_testing_Click(object sender, RoutedEventArgs e)
        {
            TestWindow tw = new TestWindow();
            tw.Show();
        }

        private void Btn_setOCRArea_Click(object sender, RoutedEventArgs e)
        {
            if (GV.mCamera != null && GV.mCamera.IsConnected)
            {
                if (PreviewRoutine.IsCapturing)
                {
                    PreviewRoutine.StopPreview();
                    BindMngr.GMessage.value = "Select the area and hit set area again to confirm";
                }
                else if (!PreviewRoutine.IsCapturing && ImgCropping.rect.Width * ImgCropping.rect.Height != 0)
                {
                    OCR.croppedOCRArea = ImgCropping.rect;
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
                    BindMngr.GMessage.value = "Area set! Only do OCR inside the red rectangle!";
                }
            }
            else if(!PreviewRoutine.IsCapturing && ImgCropping.rect.Width * ImgCropping.rect.Height != 0)   //crop static picture
            {
                OCR.croppedOCRArea = ImgCropping.rect;
                BindMngr.GMessage.value = "Area set! Only do OCR inside the red rectangle!";
                Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgOriginal);
                Image<Bgr, byte> bm = GV.imgOriginal.Copy();
                bm.Draw(OCR.croppedOCRArea, new Bgr(Color.Red), 2);
                Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(bm);
            }

        }

        #endregion OCR

        #region Machine Learning
        private void Btn_ML_modelLoad_Click(object sender, RoutedEventArgs e)
        {
            switch (MLCore.MLModelSelected)
            {
                case MLModel.ResNet:
                    ResNet.LoadModel(TB_ML_modelName.Text);
                    break;
                case MLModel.FastRCNN:
                    break;
                case MLModel.Yolo:
                    mYolo.LoadModel(GV.ML_Folders[(int)MLFolders.ML_YOLO_model]);
                    break;
            }
            GB_ML_operation.IsEnabled = true;

        }

        private void Btn_runML_Click(object sender, RoutedEventArgs e)
        {
            Windows.main.listBox.Items.Clear();
            switch (MLCore.MLModelSelected)
            {
                case MLModel.ResNet:
                    ResNet.EvaluationSingleImage(GV.imgOriginal);

                    for (int i = 0; i < ResNet.resultList.Count; i++)
                    {
                        Windows.main.listBox.Items.Add(string.Format("{0}: {1}", MLCore.MLSelectedLabels[i], ResNet.resultList[i]));
                    }
                    BindMngr.GMessage.value = string.Format("This must be a {0}!", ResNet.OutputString, ResNet.OutputProbablility);
                    break;
                case MLModel.FastRCNN:
                    FastRCNN.EvaluateObjectDetectionModel();
                    break;
                case MLModel.Yolo:
                    GV.imgProcessed = new Image<Bgr, byte>(mYolo.Detect(GV.imgOriginal.ToBitmap()));
                    Windows.main.ibOriginal.Source = ImgConverter.ToBitmapSource(GV.imgProcessed);
                    break;
            }
        }

        private void ML_cmb_model_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            GB_ML_operation.IsEnabled = false;
            MLCore.MLModelSelected = (MLModel)ML_cmb_model.SelectedIndex;

            if (MLCore.MLModelSelected == MLModel.Yolo)
            {
                ML_cmb_dataset.Visibility = Visibility.Hidden;
                Panel_ML_LabelJobType.Visibility = Visibility.Collapsed;
                TB_ML_modelName.Text = ModelPath.GetWeightFile(GV.ML_Folders[(int)MLFolders.ML_YOLO_model]);

                Exp_FileDirectory.Visibility = Visibility.Collapsed;
                Exp_ImgResizing.Visibility = Visibility.Collapsed;
                Exp_MeanCalculation.Visibility = Visibility.Collapsed;
            }
            else
            {
                ML_cmb_dataset.Visibility = Visibility.Visible;
                Panel_ML_LabelJobType.Visibility = Visibility.Visible;

                Exp_FileDirectory.Visibility = Visibility.Visible;
                Exp_ImgResizing.Visibility = Visibility.Visible;
                Exp_MeanCalculation.Visibility = Visibility.Visible;
            }
        }

        private void ML_cmb_dataset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            GB_ML_operation.IsEnabled = false;

            MLCore.MLSelectedLabels = DataSet.LabelSet[ML_cmb_dataset.SelectedIndex];
            MLCore.MLTrainedDataSetSelectedIndex = ML_cmb_dataset.SelectedIndex;
        }

        private void TB_ML_modelName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            GB_ML_operation.IsEnabled = false;
        }

        private void Chk_ML_Checked(object sender, RoutedEventArgs e)
        {
            GV._MLSwitch = (bool)Chk_ML.IsChecked;
        }


        private void Btn_generateImgFolder_source_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(BindMngr.ML_CNTK_sourceTrainImgDir.value))
            {
                Directory.CreateDirectory(BindMngr.ML_CNTK_sourceTrainImgDir.value);
                mNotification.Show("Source train image folder is created");
            }
            else
                mNotification.Show("Folder exists");
            if (!Directory.Exists(BindMngr.ML_CNTK_sourceTestImgDir.value))
            {
                Directory.CreateDirectory(BindMngr.ML_CNTK_sourceTestImgDir.value);
                mNotification.Show("Source test image folder is created");
            }
            else
                mNotification.Show("Folder exists");
        }

        private void Btn_generateImgFolder_MLRoot_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(BindMngr.ML_CNTK_trainImgDir.value))
            {
                Directory.CreateDirectory(BindMngr.ML_CNTK_trainImgDir.value);
                mNotification.Show("ML train image folder is created");
            }
            else
                mNotification.Show("Folder exists");
            if (!Directory.Exists(BindMngr.ML_CNTK_testImgDir.value))
            {
                Directory.CreateDirectory(BindMngr.ML_CNTK_testImgDir.value);
                mNotification.Show("ML test image folder is created");
            }
            else
                mNotification.Show("Folder exists");
        }

        private void Btn_resize_Click(object sender, RoutedEventArgs e)
        {
            if (TB_sourceImgDir.Text != "" && TB_resizedImgDir.Text != "" && TB_resizeWidth.Text != "" && TB_resizeHeight.Text != "")
                ImageResizing.ImageBatchResizing(
                    BindMngr.ML_CNTK_sourceImgDir.value, 
                    BindMngr.ML_CNTK_rootDir.value, 
                    Convert.ToInt32(BindMngr.ML_desWidth.value), 
                    Convert.ToInt32(BindMngr.ML_desHeight.value));
            else
                mMessageBox.Show("Empty string");
        }

        private void Btn_openDir_Click(object sender, RoutedEventArgs e)
        {
            BindMngr.ML_CNTK_sourceImgDir.value = GF.OpenDirectoryDialog();
        }

        private void Btn_openDir_ML_root_Click(object sender, RoutedEventArgs e)
        {
            BindMngr.ML_CNTK_rootDir.value = GF.OpenDirectoryDialog();
        }

        private void Btn_calculateMean_Click(object sender, RoutedEventArgs e)
        {
            //string trainFileDir = @"C:\Users\bojun.lin\Downloads\cifar\train";
            //string meanFileDir = @"C:\Users\bojun.lin\Downloads\cifar";//BindMngr.ML_rootDir.value;
            MeanFileGenerator.GenerateMeanFile(TB_meanCalImgDir.Text, BindMngr.ML_CNTK_rootDir.value);
           // MeanFileGenerator.GenerateConstMeanFile(meanFileDir);
        }

        private void Btn_calculateConstMean_Click(object sender, RoutedEventArgs e)
        {
            MeanFileGenerator.GenerateConstMeanFile(BindMngr.ML_CNTK_rootDir.value, ImageColorType.RGB);
        }

        private void Btn_calculateConstMeanMono_Click(object sender, RoutedEventArgs e)
        {
            MeanFileGenerator.GenerateConstMeanFile(BindMngr.ML_CNTK_rootDir.value, ImageColorType.Mono);
        }

        private void Btn_ML_labling_Click(object sender, RoutedEventArgs e)
        {
            if (MLCore.MLModelSelected == MLModel.Yolo)
            {
                ImageLabelTool_Yolo w = new ImageLabelTool_Yolo();
                w.ShowDialog();
            }
            else
            {
                ImageLabelingWindow w = new ImageLabelingWindow((JobType)Cmb_ML_jobType.SelectedIndex, MLCore.MLSelectedLabels);
                w.ShowDialog();
            }
        }

        private void TB_sourceImgDir_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TB_sourceImgDir.Text != "")
                BindMngr.ML_CNTK_sourceImgDir.value = TB_sourceImgDir.Text;
        }

        private void TB_resizedImgDir_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TB_resizedImgDir.Text != "")
                BindMngr.ML_CNTK_rootDir.value = TB_resizedImgDir.Text;
        }

        #endregion Machine Learning

        #endregion <GUI operation>

        private void Chk_checkBoundry_Checked(object sender, RoutedEventArgs e)
        {
            GV._checkBoundry = true;
        }

        private void Chk_checkBoundry_Unchecked(object sender, RoutedEventArgs e)
        {
            GV._checkBoundry = false;
        }

        private void Chk_fitEllipse_Click(object sender, RoutedEventArgs e)
        {
            GV._fitEllipse = (bool)Chk_fitEllipse.IsChecked;
        }

        private void Chk_isEthernet_Click(object sender, RoutedEventArgs e)
        {
            Default.isEthernet = (bool)Chk_isEthernet.IsChecked;
            Default.Save();
        }

        #region Difference Detect
        private void Btn_setReference_Click(object sender, RoutedEventArgs e)
        {
            if (ImgCropping.rect.Width * ImgCropping.rect.Height != 0)
            {
                Image<Bgr, byte> Img = GV.imgOriginal_pure;
                GV.ref_img = Img.Copy(ImgCropping.rect).Convert<Bgr, byte>(); //new Image<Gray, Byte>(mCrop.cropBitmap(imgOriginal.ToBitmap(), mCrop.rect));
                ibReference.Source = ImgConverter.ToBitmapSource(GV.ref_img);

                if (GV.mCamera.IsConnected)
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
            }
        }

        private void Btn_contrastDetect_Click(object sender, RoutedEventArgs e)
        {
            lbl_contrast_result.Content = ContrastDetection.mContrastDetection(GV.imgOriginal, GV.ref_img).ToString();
        }

        #endregion Difference Detect

        private void Slider_contrastSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider_contrastSensitivity.Value = Convert.ToDouble(Slider_contrastSensitivity.Value.ToString("0.00"));
            Default.contrast_sensitivity = Slider_contrastSensitivity.Value;
            Default.Save();
        }


        private void Btn_trainModel_Click(object sender, RoutedEventArgs e)
        {
            switch (MLCore.MLModelSelected)
            {
                case MLModel.ResNet:
                case MLModel.FastRCNN:
                    break;
                case MLModel.Yolo:
                    if (tw.IsActive)
                    {
                        tw.Close();
                        tw = new TrainingWindow();
                        tw.Show();
                    }
                    else
                        tw.Show();

                    mYolo.TrainModel();
                    break;
            }
        }
    }
}
