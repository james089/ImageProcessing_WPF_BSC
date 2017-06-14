using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

using Utilities_BSC_dll;
using OpenCV_BSC_dll;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Windows.Threading;
using System.Media;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using CameraToImage_dll;
using ZXing.Common;
using ZXing;
using ZXing.QrCode;
using ImageProcessing_BSC_WPF.Modules;
using mUserControl_BSC_dll;
using System.Drawing;

namespace ImageProcessing_BSC_WPF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Local setup

        //public PictureBox ibOriginal = new PictureBox() {Width = 640, Height = 480 };
        //public PictureBox ibObjectImage = new PictureBox();

        SoundPlayer sound = new SoundPlayer(System.Environment.CurrentDirectory + @"\Resources\camera-shutter-click-03.wav");

        LoadingScreen loadingScreen = new LoadingScreen();
        #endregion Local setup

        public MainWindow()
        {
            InitializeComponent();
            loadingScreen.Show();
            //Static MainWindow
            GV.mMainWindow = this;

            PreviewRoutine.previewSetup();
            ConnectRoutine.connectionSetup();

            BarcodeDecoder.decoderSetup();
            OCR.OCRSetup(OCRMode.NUMBERS);

            loadProgramSetting();
            applyProgramSetting();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title.Content = "Image processing vers." + Assembly.GetExecutingAssembly().GetName().Version;

            loadingScreen.Close();
        }

        private void loadProgramSetting()
        {
            GV._camSelected = (camType)Properties.Settings.Default.camSelection;
            GV._camConnectAtStartup = Properties.Settings.Default.camConnect;
            GV._previewFPS = (previewFPS)Properties.Settings.Default.previewFPS;
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
            Properties.Settings.Default.previewFPS = (int) GV._previewFPS;

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
            this.Close();
        }

        private void Btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 
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
                ImageCropping.WPF_mouseDown(GV.imgOriginal, ibOriginal, p, 0.5);
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
            else if (GV.imgOriginal != null)
            {
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(ibOriginal).X;
                p.Y = (int)e.GetPosition(ibOriginal).Y;
                ImageCropping.WPF_mouseMove(ibOriginal, p, 0.5);
            }
        }

        private void ibOriginal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = false;
            if (GV.imgOriginal != null)
            {
                ibOriginal.Cursor = Cursors.Arrow;
                ImageCropping.WPF_mouseUp(ibOriginal, 0.5);
            }
        }   
        
        #endregion Img original Panel

        #endregion</Window operation>

        #region <Menu operation>

        private void File_loadImg_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr,byte> loadPic = new Image<Bgr, byte>(Tools.loadPicture_withDialog());
            if (loadPic != null)
            {
                GV.imgOriginal = new Image<Bgr, byte>( ImageStiching.createFixRatioBitmap(loadPic.ToBitmap(), 4, 3));
                //ibOriginal.Image = GV.imgOriginal;
                GV._pictureLoaded = true;
                GF.updateImgInfo();
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
            PreviewRoutine.startPreview(GV._previewFPS);
        }


        private void Menu_conversion_Click(object sender, RoutedEventArgs e)
        {
            GV.mConvert = new Conversion(GV.mCamera, GV.imgWidth, GV.imgHeight);
            GV.mConvert.ShowDialog();
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

        private void Button_Click(object sender, RoutedEventArgs e)                    //Preview Button
        {
            if ((bool)Chk_connectCam.IsChecked)
            {
                if (GV._capturing == true)
                {
                    PreviewRoutine.stopPreview();                    //stop capturing 
                    //Chk_connectCam.IsEnabled = false;
                    if (GV.imgOriginal != null)
                    {
                        GV._pictureLoaded = true;
                    }
                    GV._capturing = false;
                }
                else
                {
                    PreviewRoutine.startPreview(GV._previewFPS);
                    //Chk_connectCam.IsEnabled = false;
                    GV._capturing = true;
                }
            }
            else
                mMessageBox.Show("Please connect camera first!");
        }

        private void Chk_connectCam_Checked(object sender, RoutedEventArgs e)
        {
            switch (GV._camSelected)
            {
                case camType.WebCam:
                    ConnectRoutine.connectWebCam();break;
                case camType.PointGreyCam:
                    ConnectRoutine.connectPointGreyCam(); break;
            }
        }

        private void Chk_connectCam_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GV._capturing)
                PreviewRoutine.stopPreview();
            if (GV.mCamera.IsConnected)                                           //if there is a camera, dispose and reconnect.
            {
                GV.mCamera.disposeCam();
                mPopText.popText("Disconnected", 1.5);
            }
        }

        private void Radio_original_Checked(object sender, RoutedEventArgs e)
        {
            GV._featureType = featureDetectionType.original;
        }

        private void Radio_cannyEdge_Checked(object sender, RoutedEventArgs e)
        {
            GV._featureType = featureDetectionType.cannyEdge;
        }

        private void Radio_contour_Checked(object sender, RoutedEventArgs e)
        {
            GV._featureType = featureDetectionType.contour;
        }

        private void Radio_line_Checked(object sender, RoutedEventArgs e)
        {
            GV._featureType = featureDetectionType.line;
        }

        private void Radio_invert_Checked(object sender, RoutedEventArgs e)
        {
            GV._colorInverse = (bool)Radio_invert.IsChecked;
        }


        private void Btn_apply_Click(object sender, RoutedEventArgs e)
        {
            if (GV._pictureLoaded && GV.imgOriginal != null)
            {
                //====Processed image==========
                GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, DetectionType.Feature, out GV._err);
                ibOriginal.Source = Converter.ToBitmapSource(GV.imgProcessed);
            }
        }

        private void Btn_capture_Click(object sender, RoutedEventArgs e)
        {
            if (GV._capturing && GV.imgOriginal != null)
            {
                if (Combo_saveOption.SelectedItem == Combo_originalImage)
                {
                    ibOriginal.Source = null;
                    sound.Play();
                    Thread.Sleep(200);
                    Tools.savePicture_withDialog(GV.imgOriginal.ToBitmap());
                }
                else
                {
                    ibOriginal.Source = null;
                    sound.Play();
                    Thread.Sleep(200);
                    Tools.savePicture_withDialog(GV.imgProcessed.ToBitmap());
                }
            }

        }

        private void Btn_setObject_Click(object sender, RoutedEventArgs e)
        {
            if (ImageCropping.rect.Width * ImageCropping.rect.Height != 0)
            {
                Image<Bgr, byte> Img = GV.imgOriginal;
                GV.object_img = Img.Copy(ImageCropping.rect).Convert<Bgr, Byte>(); //new Image<Gray, Byte>(mCrop.cropBitmap(imgOriginal.ToBitmap(), mCrop.rect));
                ibObject.Source = Converter.ToBitmapSource(GV.object_img);
                PreviewRoutine.startPreview(GV._previewFPS);
            }
        }

        private void Btn_apply_object_Click(object sender, RoutedEventArgs e)
        {
            if (GV.object_img != null && GV.imgOriginal != null)
            {
                //====Processed image==========
                GV.imgProcessed = NCVFuns.Detection(GV.imgOriginal, DetectionType.Object, out GV._err);
                ibOriginal.Source = Converter.ToBitmapSource(GV.imgProcessed);
            }
            else if (GV.object_img == null)
            {
                GV._err = ErrorCode.No_object_image;
            }

            TB_info.Text = GV._err.ToString();
        }

        private void Radio_FFT_Checked(object sender, RoutedEventArgs e)
        {
            GV._objectType = objectDetectionType.FFT;
        }

        private void Radio_Color_Checked(object sender, RoutedEventArgs e)
        {
            GV._objectType = objectDetectionType.color;
        }

        private void Chk_enableObjectD_Checked(object sender, RoutedEventArgs e)
        {
            GV._detectionType = DetectionType.Object;
            toggleExpander_object(true);
            Expander_feature.IsEnabled = false;
        }

        private void Chk_enableObjectD_Unchecked(object sender, RoutedEventArgs e)
        {
            GV._detectionType = DetectionType.Feature;

            toggleExpander_object(false);
            Expander_feature.IsEnabled = true;
        }

        private void toggleExpander_object(bool bb)
        {
            Btn_setObject.IsEnabled = bb;
            Btn_apply_object.IsEnabled = bb;
            Dock_objectType.IsEnabled = bb;
        }


        private void Chk_findMin_Checked(object sender, RoutedEventArgs e)
        {
            GV._findMinSwitch = (bool)Chk_findMin.IsChecked;
        }

        private void Chk_findCenter_Checked(object sender, RoutedEventArgs e)
        {
            GV._findCenterSwitch = (bool)Chk_findCenter.IsChecked;
        }

        private void Chk_liveDecoding_Checked(object sender, RoutedEventArgs e)
        {
            GV._decodeSwitch = (bool)Chk_liveDecoding.IsChecked;
        }

        private void Btn_decode_Click(object sender, RoutedEventArgs e)
        {
            PreviewRoutine.stopPreview();
            BarcodeDecoder.startDecodeRoutine();
        }

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
                if (GV._capturing)
                {
                    PreviewRoutine.stopPreview();
                    GV.mMainWindow.TB_info.Text = "Select the area and hit set area again to confirm";
                }
                else if (!GV._capturing && ImageCropping.rect.Width * ImageCropping.rect.Height != 0)
                {
                    OCR.croppedOCRArea = ImageCropping.rect;
                    PreviewRoutine.startPreview(GV._previewFPS);
                    GV.mMainWindow.TB_info.Text = "Area set! Only do OCR inside the red rectangle!";
                }
            }
            else if(!GV._capturing && ImageCropping.rect.Width * ImageCropping.rect.Height != 0)   //crop static picture
            {
                OCR.croppedOCRArea = ImageCropping.rect;
                GV.mMainWindow.TB_info.Text = "Area set! Only do OCR inside the red rectangle!";
                GV.mMainWindow.ibOriginal.Source = Converter.ToBitmapSource(GV.imgOriginal);
                Image<Bgr, byte> bm = GV.imgOriginal.Copy();
                bm.Draw(OCR.croppedOCRArea, new Bgr(Color.Red), 2);
                GV.mMainWindow.ibOriginal.Source = Converter.ToBitmapSource(bm);
            }

        }



        #endregion <GUI operation>

        /// <summary>
        /// Functions region
        /// </summary>
        /// 



    }
}
