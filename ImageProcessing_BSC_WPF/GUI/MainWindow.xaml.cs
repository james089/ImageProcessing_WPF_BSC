using System;
using System.Windows;
using System.Windows.Input;

using Utilities_BSC_dll_x64;
using OpenCV_BSC_dll_x64;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Media;
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
using mUserControl_BSC_dll.UserControls;
using ImageProcessing_BSC_WPF.Modules.CortexDecoder;

namespace ImageProcessing_BSC_WPF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Local setup
        LoadingScreen loadingScreen = new LoadingScreen();
        #endregion Local setup

        public MainWindow()
        {
            loadingScreen.Show();
            InitializeComponent();

            PreviewRoutine.previewSetup();
            ConnectRoutine.connectionSetup();
            ResNet.MLSetup();

            ZxingDecoder.DecoderSetup();
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

            OCR.OCRSetup(OCRMode.NUMBERS);

            //Static MainWindow

            Windows.main = this;

            //DataContext = Windows.main;                         // This is neccessary
            //GMessage = new BindString();

            DataContext = BindManager.BindMngr;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title.Content = "Image processing vers." + Assembly.GetExecutingAssembly().GetName().Version;

            loadProgramSetting();
            applyProgramSetting();
            //Chk_connectCam.IsChecked = true;
        }

        private void loadProgramSetting()
        {
            MLCore.MLSelectedLabels = DataSet.labelSet[ML_cmb_dataset.SelectedIndex];

            GV._camSelected = (camType)Properties.Settings.Default.camSelection;
            GV._camConnectAtStartup = Properties.Settings.Default.camConnect;
            PreviewRoutine._previewFPS = (previewFPS)Properties.Settings.Default.previewFPS;
        }

        private void applyProgramSetting()
        {
            Chk_connectCam.IsChecked = GV._camConnectAtStartup;
            //Chk_connectCam.IsEnabled = !GV._camConnectAtStartup;
            selectCam(GV._camSelected);

            Radio_original.IsChecked = true;
            Radio_FFT.IsChecked = true;
            toggleExpander_object(false);
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
            Properties.Settings.Default.previewFPS = (int) PreviewRoutine._previewFPS;

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
        private void ibOriginal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = true;
            if ((bool)Chk_findCenter.IsChecked)
            {
            }
            else if (GV.imgOriginal != null)
            {
                PreviewRoutine.stopPreview();
                ibOriginal.Cursor = Cursors.Cross;
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(ibOriginal).X;
                p.Y = (int)e.GetPosition(ibOriginal).Y;
                ImgCropping.WPF_mouseDown(GV.imgOriginal, ibOriginal, p, GV._zoomFactor);
            }
        }

        private void ibOriginal_MouseMove(object sender, MouseEventArgs e)
        {
            if ((bool)Chk_findCenter.IsChecked)
            {
                if (!_mouseDown)
                {
                    int yOffset = 1;
                    if (e.GetPosition(ibOriginal).Y >= CheckCenter.centerChkBarPos_y - yOffset && e.GetPosition(ibOriginal).Y <= CheckCenter.centerChkBarPos_y + yOffset)
                    {
                        ibOriginal.Cursor = Cursors.No;
                    }
                    else
                        ibOriginal.Cursor = Cursors.Arrow;
                }
                else
                {
                    CheckCenter.centerChkBarPos_y = (int)e.GetPosition(ibOriginal).Y;
                }

            }
            else if ((bool)Chk_showRGB.IsChecked && GV.imgOriginal != null)
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
                ibOriginal.Cursor = Cursors.Arrow;
                ImgCropping.WPF_mouseUp(ibOriginal, GV._zoomFactor);
            }
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
                GV._pictureLoaded = true;
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
            PreviewRoutine.stopPreview();
            Preference preferenceWindow = new Preference();
            preferenceWindow.preferenceUpdated += preferenceWindow_preferenceUpdated;
            preferenceWindow.Show();
        }

        private void preferenceWindow_preferenceUpdated(object sender, EventArgs e)
        {
            PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
        }

        private void Menu_conversion_Click(object sender, RoutedEventArgs e)
        {
            if (GV.mCamera != null && GV.mCamera.IsConnected)
            {
                PreviewRoutine.stopPreview();

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

            PreviewRoutine.stopPreview();
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

            PreviewRoutine.stopPreview();
            GV._camSelected = camType.PointGreyCam;
            if (GV._camConnectAtStartup)
            {
                ConnectRoutine.connectPointGreyCam();
                //Chk_connectCam.IsEnabled = false;                              //disable chk box for connecting the camera
            }
            else
            {
                //Chk_connectCam.IsEnabled = true;
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
                    ConnectRoutine.connectPointGreyCam(); break;
            }

            Panel_liveViewOptions.IsEnabled = true;
        }

        private void Chk_connectCam_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (PreviewRoutine.IsCapturing)
                PreviewRoutine.stopPreview();
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
                    PreviewRoutine.stopPreview();                    //stop capturing 
                    //Chk_connectCam.IsEnabled = false;
                    if (GV.imgOriginal != null)
                    {
                        GV._pictureLoaded = true;
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

        private void Btn_setObject_Click(object sender, RoutedEventArgs e)
        {
            if (ImgCropping.rect.Width * ImgCropping.rect.Height != 0)
            {
                Image<Bgr, byte> Img = GV.imgOriginal;
                GV.object_img = Img.Copy(ImgCropping.rect).Convert<Bgr, Byte>(); //new Image<Gray, Byte>(mCrop.cropBitmap(imgOriginal.ToBitmap(), mCrop.rect));
                ibObject.Source = ImgConverter.ToBitmapSource(GV.object_img);
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

            BindManager.BindMngr.GMessage.value = GV._err.ToString();
        }

        private void Radio_FFT_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._objectType = objectDetectionType.FFT;
        }

        private void Radio_Color_Checked(object sender, RoutedEventArgs e)
        {
            NCVFuns._objectType = objectDetectionType.color;
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

        #region MIS
        private void Chk_findMin_Checked(object sender, RoutedEventArgs e)
        {
            GV._findMinSwitch = (bool)Chk_findMin.IsChecked;
        }

        private void Chk_findCenter_Checked(object sender, RoutedEventArgs e)
        {
            GV._findCenterSwitch = (bool)Chk_findCenter.IsChecked;
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
            PreviewRoutine.stopPreview();
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
            PreviewRoutine.stopPreview();
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
                    PreviewRoutine.stopPreview();
                    BindManager.BindMngr.GMessage.value = "Select the area and hit set area again to confirm";
                }
                else if (!PreviewRoutine.IsCapturing && ImgCropping.rect.Width * ImgCropping.rect.Height != 0)
                {
                    OCR.croppedOCRArea = ImgCropping.rect;
                    PreviewRoutine.startPreview(PreviewRoutine._previewFPS);
                    BindManager.BindMngr.GMessage.value = "Area set! Only do OCR inside the red rectangle!";
                }
            }
            else if(!PreviewRoutine.IsCapturing && ImgCropping.rect.Width * ImgCropping.rect.Height != 0)   //crop static picture
            {
                OCR.croppedOCRArea = ImgCropping.rect;
                BindManager.BindMngr.GMessage.value = "Area set! Only do OCR inside the red rectangle!";
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
                    BindManager.BindMngr.GMessage.value = string.Format("This must be a {0}!", ResNet.OutputString, ResNet.OutputProbablility);
                    break;
                case MLModel.FastRCNN:
                    FastRCNN.EvaluateObjectDetectionModel();
                    break;
            }
        }

        private void ML_cmb_model_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            GB_ML_operation.IsEnabled = false;
            MLCore.MLModelSelected = (MLModel)ML_cmb_model.SelectedIndex;
        }
        private void ML_cmb_dataset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            GB_ML_operation.IsEnabled = false;
            MLCore.MLSelectedLabels = DataSet.labelSet[ML_cmb_dataset.SelectedIndex];
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
            if (!Directory.Exists(BindManager.BindMngr.ML_sourceTrainImgDir.value))
            {
                Directory.CreateDirectory(BindManager.BindMngr.ML_sourceTrainImgDir.value);
                mNotification.Show("Source train image folder is created");
            }
            else
                mNotification.Show("Folder exists");
            if (!Directory.Exists(BindManager.BindMngr.ML_sourceTestImgDir.value))
            {
                Directory.CreateDirectory(BindManager.BindMngr.ML_sourceTestImgDir.value);
                mNotification.Show("Source test image folder is created");
            }
            else
                mNotification.Show("Folder exists");
        }

        private void Btn_generateImgFolder_MLRoot_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(BindManager.BindMngr.ML_trainImgDir.value))
            {
                Directory.CreateDirectory(BindManager.BindMngr.ML_trainImgDir.value);
                mNotification.Show("ML train image folder is created");
            }
            else
                mNotification.Show("Folder exists");
            if (!Directory.Exists(BindManager.BindMngr.ML_testImgDir.value))
            {
                Directory.CreateDirectory(BindManager.BindMngr.ML_testImgDir.value);
                mNotification.Show("ML test image folder is created");
            }
            else
                mNotification.Show("Folder exists");
        }

        private void Btn_resize_Click(object sender, RoutedEventArgs e)
        {
            if (TB_sourceImgDir.Text != "" && TB_resizedImgDir.Text != "" && TB_resizeWidth.Text != "" && TB_resizeHeight.Text != "")
                ImageResizing.ImageBatchResizing(
                    BindManager.BindMngr.ML_sourceImgDir.value, 
                    BindManager.BindMngr.ML_rootDir.value, 
                    Convert.ToInt32(BindManager.BindMngr.ML_desWidth.value), 
                    Convert.ToInt32(BindManager.BindMngr.ML_desHeight.value));
            else
                mMessageBox.Show("Empty string");
        }

        private void Btn_openDir_Click(object sender, RoutedEventArgs e)
        {
            BindManager.BindMngr.ML_sourceImgDir.value = GF.OpenDirectoryDialog();
        }

        private void Btn_openDir_ML_root_Click(object sender, RoutedEventArgs e)
        {
            BindManager.BindMngr.ML_rootDir.value = GF.OpenDirectoryDialog();
        }

        private void Btn_calculateMean_Click(object sender, RoutedEventArgs e)
        {
            //string trainFileDir = @"C:\Users\bojun.lin\Downloads\cifar\train";
            //string meanFileDir = @"C:\Users\bojun.lin\Downloads\cifar";//BindManager.BindMngr.ML_rootDir.value;
            MeanFileGenerator.GenerateMeanFile(TB_meanCalImgDir.Text, BindManager.BindMngr.ML_rootDir.value);
           // MeanFileGenerator.GenerateConstMeanFile(meanFileDir);
        }

        private void Btn_calculateConstMean_Click(object sender, RoutedEventArgs e)
        {
            MeanFileGenerator.GenerateConstMeanFile(BindManager.BindMngr.ML_rootDir.value, ImageColorType.RGB);
        }

        private void Btn_calculateConstMeanMono_Click(object sender, RoutedEventArgs e)
        {
            MeanFileGenerator.GenerateConstMeanFile(BindManager.BindMngr.ML_rootDir.value, ImageColorType.Mono);
        }

        private void Btn_ML_labling_Click(object sender, RoutedEventArgs e)
        {
            ImageLabelingWindow w = new ImageLabelingWindow((JobType)Cmb_ML_jobType.SelectedIndex, MLCore.MLSelectedLabels);
            w.ShowDialog();
        }

        private void TB_sourceImgDir_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TB_sourceImgDir.Text != "")
                BindManager.BindMngr.ML_sourceImgDir.value = TB_sourceImgDir.Text;
        }

        private void TB_resizedImgDir_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TB_resizedImgDir.Text != "")
                BindManager.BindMngr.ML_rootDir.value = TB_resizedImgDir.Text;
        }

        #endregion Machine Learning

        #endregion <GUI operation>

    }
}
