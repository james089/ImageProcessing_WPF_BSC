using ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers;
using mUserControl_BSC_dll;
using mUserControl_BSC_dll.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Utilities_BSC_dll_x64;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;
using System.Threading;
using ImageProcessing_BSC_WPF.GUI.Panels;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for ImageLabelTool_Yolo.xaml
    /// </summary>
    public partial class ImageLabelTool_Yolo : Window
    {
        public static ImageLabelTool_Yolo mLabelTool = new ImageLabelTool_Yolo();
        enum Steps
        {
            //step0_resize, // can be skipped
            step1_createLabels,
            step2_labeling,
        }
        #region Local Paras
        const int thumb_w = 120, thumb_h = 120;
        int res_img_w = 0, res_img_h = 0;
        int totalImgNum = 0;
        /// <summary>
        /// ImageBoxWidth / imgWidth
        /// </summary>
        double zoomFactor_x = 0;
        /// <summary>
        /// ImageBoxHeight / imgHeight
        /// </summary>
        double zoomFactor_y = 0;
        /// <summary>
        ///  The labeling img index inside the imgThumbList
        /// </summary>
        int cImgIndex = 0;

        /// <summary>
        /// train images / total images
        /// </summary>
        const double TRAIN_IMG_RATE = 0.75;

        /// <summary>
        /// The Image that is currently showing
        /// </summary>
        Image<Bgr, byte> currentImage;
        /// <summary>
        /// This is the class index for labeling (0 1 2 3...)
        /// </summary>
        int clabelIndex = 0;

        /// <summary>
        /// Folder contais all src image
        /// </summary>
        string currentImgDir;                                     // Img dir
        /// <summary>
        /// Root folder
        /// </summary>
        string yoloDataDir;

        /// <summary>
        /// For ML model use
        /// </summary>
        string obj_data_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.data";
        string obj_names_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.names";
        string obj_train_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\train.txt";
        string obj_test_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\test.txt";

        string pretrained_weight_file = GV.ML_Folders[(int)MLFolders.ML_YOLO] + "\\darknet53.conv.74";
        string obj_cfg_file = GV.ML_Folders[(int)MLFolders.ML_YOLO] + "\\yolo-obj.cfg";
        string yolo_cfg_file = GV.ML_Folders[(int)MLFolders.ML_YOLO] + "\\yolov3.cfg";
        public string train_cmd_file = GV.ML_Folders[(int)MLFolders.ML_YOLO] + "\\train_obj.cmd";

        List<string> labelSet;
        List<RadioButton> labelSelectorList = new List<RadioButton>();
        List<System.Windows.Controls.Image> imgThumbList = new List<System.Windows.Controls.Image>();
        List<imgInfo> mImgInfoList;


        /// <summary>
        /// This stores every info about an image
        /// </summary>
        private class imgInfo
        {
            /// <summary>
            /// This is filled up when loading image thumbnails
            /// </summary>
            public string imagePath;
            /// <summary>
            /// This contains the location of objects in 1 image
            /// </summary>
            public List<regionInfo> regionInfoList;

            public imgInfo()
            {
                imagePath = "";
                regionInfoList = new List<regionInfo>();
            }
        }

        private class regionInfo
        {
            public int labelIndex;
            public Rectangle rect;
            public double[] location;
            public regionInfo()
            {
                labelIndex = 0;
                rect = new Rectangle();
                location = new double[4];
            }
            public regionInfo(int _label, Rectangle _rect, double[] _loc)
            {
                labelIndex = _label;
                rect = _rect;
                location = _loc;
            }
        }
        //private class imgMapStruct
        //{
        //    public uint label;
        //    public double dir;
        //}

        //List<imgMapStruct> sourceImgMapList = new List<imgMapStruct>();

        #endregion Local Paras

        public ImageLabelTool_Yolo()
        {
            mLabelTool = this;
            InitializeComponent();
            DataContext = BindManager.BindMngr;

            labelSet = new List<string>();
            mImgInfoList = new List<imgInfo>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentImgDir = GV.ML_Folders[(int)MLFolders.ML_YOLO_data_img];
            yoloDataDir = GV.ML_Folders[(int)MLFolders.ML_YOLO_data];

            /// Show image folder info
            lbl_imgFolder.Text = currentImgDir;
            lbl_trainingFilesFolder.Text = GV.ML_Folders[(int)MLFolders.ML_YOLO_data];

            SwitchSteps(Steps.step1_createLabels);
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            switch (mMessageBox.ShowYesNoCancel("Save current labeling info?"))
            {
                case mDialogResult.yes:
                    SaveNamesFile(obj_names_file);
                    SaveDataFile(obj_data_file);
                    SaveTrainAndTestFile();
                    GenerateCfgFile();
                    GenerateCmdFile();
                    break;
                case mDialogResult.no:
                    break;
                case mDialogResult.cancel:
                    return;
            }
            imgThumbList.Clear();
            this.Close();
        }
      
        #region Between Steps
        private void SwitchSteps(Steps mSteps)
        {
            switch (mSteps)
            {
                //case Steps.step0_resize:
                //    Grid_step0.Visibility = Visibility.Visible;
                //    Grid_step1.Visibility = Visibility.Collapsed;
                //    Grid_step2.Visibility = Visibility.Collapsed;

                //    RB_640480_Checked(null, null);
                //    break;
                case Steps.step1_createLabels:
                    BindManager.BindMngr.GMessage.value = "Add your classes";
                    Grid_step0.Visibility = Visibility.Collapsed;
                    Grid_step1.Visibility = Visibility.Visible;
                    Grid_step2.Visibility = Visibility.Collapsed;
                    Btn_close.Visibility = Visibility.Hidden;

                    LoadNames(obj_names_file);

                    LB_classes.Items.Clear();
                    foreach (string str in labelSet)
                    {
                        Label ltb = new Label();
                        ltb.HorizontalContentAlignment = HorizontalAlignment.Left;
                        ltb.Content = str;
                        LB_classes.Items.Add(ltb);
                    }

                    break;
                case Steps.step2_labeling:
                    BindManager.BindMngr.GMessage.value = "Start labeling your image";
                    Grid_step0.Visibility = Visibility.Collapsed;
                    Grid_step1.Visibility = Visibility.Collapsed;
                    Grid_step2.Visibility = Visibility.Visible;
                    Btn_close.Visibility = Visibility.Visible;

                    /// Adding radio buttons to wrap panel
                    LoadDataSetRadioButtons();

                    /// Loading all images in this folder
                    /// Init imgInfoList, load existing label and location from txt file 
                    LoadImgThumbs();

                    break;
            }
        }

        #region =============================== Step0 resize =====================================
        private void RB_Horizontal_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            RB_640480.Content = "640x480";
            RB_320240.Content = "320x240";
            RB_160120.Content = "640x480";
            RB_6448.Content = "64x48";

            if ((bool)RB_640480.IsChecked) RB_640480_Checked(null, null);
            if ((bool)RB_320240.IsChecked) RB_320240_Checked(null, null);
            if ((bool)RB_160120.IsChecked) RB_160120_Checked(null, null);
            if ((bool)RB_6448.IsChecked) RB_6448_Checked(null, null);


        }

        private void RB_Vertical_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            RB_640480.Content = "480x640";
            RB_320240.Content = "240x320";
            RB_160120.Content = "480x640";
            RB_6448.Content = "48x64";

            if ((bool)RB_640480.IsChecked) RB_640480_Checked(null, null);
            if ((bool)RB_320240.IsChecked) RB_320240_Checked(null, null);
            if ((bool)RB_160120.IsChecked) RB_160120_Checked(null, null);
            if ((bool)RB_6448.IsChecked) RB_6448_Checked(null, null);
        }

        private void RB_640480_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if ((bool)RB_Horizontal.IsChecked)
            {
                res_img_w = 640;
                res_img_h = 480;
            }
            else if ((bool)RB_Vertical.IsChecked)
            {
                res_img_w = 480;
                res_img_h = 640;
            }
        }

        private void RB_320240_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if ((bool)RB_Horizontal.IsChecked)
            {
                res_img_w = 320;
                res_img_h = 240;
            }
            else if ((bool)RB_Vertical.IsChecked)
            {
                res_img_w = 240;
                res_img_h = 320;
            }
        }

        private void RB_160120_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if ((bool)RB_Horizontal.IsChecked)
            {
                res_img_w = 160;
                res_img_h = 120;
            }
            else if ((bool)RB_Vertical.IsChecked)
            {
                res_img_w = 120;
                res_img_h = 160;
            }
        }

        private void RB_6448_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if ((bool)RB_Horizontal.IsChecked)
            {
                res_img_w = 64;
                res_img_h = 48;
            }
            else if ((bool)RB_Vertical.IsChecked)
            {
                res_img_w = 48;
                res_img_h = 64;
            }
        }

        private void Btn_next0_Click(object sender, RoutedEventArgs e)
        {
            ImageResizing.ImageBatchResizing(GV.ML_Folders[(int)MLFolders.ML_YOLO_data_img], GV.ML_Folders[(int)MLFolders.ML_YOLO_data_img]
               , res_img_w, res_img_h, (bool)Chk_deleteOriImg.IsChecked, true);


            SwitchSteps(Steps.step1_createLabels);
        }

        private void Btn_skip0_Click(object sender, RoutedEventArgs e)
        {
            SwitchSteps(Steps.step1_createLabels);
        }

        #endregion Step 0 resize 

        #region =============================== Step1 Create Labels ===============================

        private void Btn_addClass_Click(object sender, RoutedEventArgs e)
        {
            Label ltb = new Label();
            ltb.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (TB_className.Text != "")
            {
                ltb.Content = TB_className.Text;
                LB_classes.Items.Add(ltb);
                LB_classes.SelectedIndex = LB_classes.Items.Count - 1;
            }
        }

        private void Btn_deleteClass_Click(object sender, RoutedEventArgs e)
        {
            if(LB_classes.Items != null)
                LB_classes.Items.Remove(LB_classes.SelectedItem);
        }

        private void Btn_back_Click(object sender, RoutedEventArgs e)
        {
            //SwitchSteps(Steps.step0_resize);
        }

        private void Btn_next1_Click(object sender, RoutedEventArgs e)
        {
            labelSet.Clear();
            foreach (Label l in LB_classes.Items)
            {
                labelSet.Add(l.Content.ToString());
            }
            SwitchSteps(Steps.step2_labeling);
        }


        #endregion Step1 Create Labels

        #region ================================= Step2 Labeling =========================================== 
        private void Btn_right_Click(object sender, RoutedEventArgs e)
        {
            if (cImgIndex < totalImgNum - 1) cImgIndex++;
            else
                cImgIndex = totalImgNum - 1;

            List_imgs.SelectedIndex = cImgIndex;
            List_imgs.ScrollIntoView(List_imgs.SelectedItem);
        }

        private void Btn_left_Click(object sender, RoutedEventArgs e)
        {
            if (cImgIndex > 0) cImgIndex--;
            else
                cImgIndex = 0;

            List_imgs.SelectedIndex = cImgIndex;
            List_imgs.ScrollIntoView(List_imgs.SelectedItem);
        }

        private void List_imgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cImgIndex = List_imgs.SelectedIndex;
            DisplayImgAndLabel(cImgIndex);
        }

        private void disabelPreviewAnddeselectTags()
        {
            Img_viewer.Source = null;
            for (int i = 0; i < labelSet.Count; i++)
            {
                if ((bool)labelSelectorList[i].IsChecked) labelSelectorList[i].IsChecked = false;
            }
        }

        #region Load image thumbs
        private struct indexedImg
        {
            public int index;
            public Bitmap image;
        }

        BackgroundWorker loadImgRoutine = new BackgroundWorker();

        /// <summary>
        /// init imgInfoList, load existing label and location from txt file 
        /// </summary>
        private void LoadImgThumbs()
        {
            loadImgRoutine.DoWork += LoadImg_DoWork;
            loadImgRoutine.ProgressChanged += LoadImg_ProgressChanged;
            loadImgRoutine.RunWorkerCompleted += LoadImgRoutine_RunWorkerCompleted;
            loadImgRoutine.WorkerReportsProgress = true;
     
            FileInfo[] _fileInfo = new DirectoryInfo(currentImgDir).GetFiles("*.jpg");
            totalImgNum = _fileInfo.Length;
            if (totalImgNum == 0)
            {
                mMessageBox.Show("No image found");
            }

            /// Create Image Controls and Init imgInfoList, load locations
            for (int i = 0; i < totalImgNum; i++)
            {
                imgThumbList.Add(new System.Windows.Controls.Image());
                imgThumbList[i].Width = thumb_w;
                imgThumbList[i].Height = thumb_h;
                List_imgs.Items.Add(imgThumbList[i]);
                
                mImgInfoList.Add(new imgInfo());
                mImgInfoList[i].imagePath = _fileInfo[i].FullName;

                /// Load txt list
                string txtFile = mImgInfoList[i].imagePath.Replace(".jpg", ".txt");
                LoadLabelAndLocationFromTxt(txtFile, mImgInfoList[i]);
            }
            List_imgs.SelectedIndex = 0;
            Thread.Sleep(20);
            if (!loadImgRoutine.IsBusy) loadImgRoutine.RunWorkerAsync();
        }

        private void LoadImg_DoWork(object sender, DoWorkEventArgs e)
        {
            /// Load Actual Images
            for (int i = 0; i < totalImgNum; i++)
            {
                Bitmap bmp = new Bitmap(mImgInfoList[i].imagePath);
                Bitmap bmp_resized = CntkBitmapExtensions.Resize(bmp, thumb_w, thumb_h, false);
                //Thread.Sleep(1);
                loadImgRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum), new indexedImg() { index = i, image = bmp_resized });
                bmp.Dispose();
            }
        }

        private void LoadImg_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BindManager.BindMngr.Progress.value = e.ProgressPercentage;
            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";

            BindManager.BindMngr.GMessage.value = string.Format("Loading images...({0})", BindManager.BindMngr.ProgressString.value);

            try
            {
                imgThumbList[((indexedImg)e.UserState).index].Source = ImgConverter.ToBitmapSource(((indexedImg)e.UserState).image);
            }
            catch (Exception)
            {
                ;
            }

            ((indexedImg)e.UserState).image.Dispose();
        }

        private void LoadImgRoutine_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BindManager.BindMngr.GMessage.value = "Start labeling your image";
        }
        #endregion Load image thumbs

        #region Select Area
        private Rectangle           cropRect;
        private double              X0, Y0, X1, Y1;
        private bool                selectingArea       = false;
        //private Image<Bgr, byte>    originalImage       = null;
        private Graphics            selectedGraphics    = null;
        private void Img_viewer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentImage != null)
            {
                Img_viewer.Cursor = Cursors.Cross;
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(Img_viewer).X;
                p.Y = (int)e.GetPosition(Img_viewer).Y;

                selectingArea = true;
                X0 = (int)((double)p.X / zoomFactor_x);
                Y0 = (int)((double)p.Y / zoomFactor_y);
            }
        }

        private void Img_viewer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lbl_imgInfo.Content = $"Mouse Location: [{e.GetPosition(Img_viewer).X : 0}, {e.GetPosition(Img_viewer).Y : 0}]";
            if (currentImage != null)
            {
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = (int)e.GetPosition(Img_viewer).X;
                p.Y = (int)e.GetPosition(Img_viewer).Y;
                if (!selectingArea) return;

                if (((double)p.X / zoomFactor_x) != X0)
                    X1 = ((double)p.X / zoomFactor_x);
                if (((double)p.Y / zoomFactor_y) != Y0)
                    Y1 = ((double)p.Y / zoomFactor_y);

                cropRect = ShapeNDraw.MakeRectangle(X0, Y0, X1, Y1);

                Bitmap bmp = currentImage.Copy().ToBitmap();
                selectedGraphics = Graphics.FromImage(bmp);
                selectedGraphics.FillRectangle(new SolidBrush(Color.FromArgb(128, 72, 145, 220)), cropRect);
                Img_viewer.Source = ImgConverter.ToBitmapSource(bmp);
            }
        }

        private void Img_viewer_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentImage != null)
            {
                Img_viewer.Cursor = Cursors.Arrow;
                if (!selectingArea) return;
                selectingArea = false;

                /// Add to region info list
                double[] loc = ConvertRectToLocation(currentImage.ToBitmap(), cropRect);
                mImgInfoList[cImgIndex].regionInfoList.Add(new regionInfo(clabelIndex, cropRect, loc));
                /// Draw 
                DrawRegion(mImgInfoList[cImgIndex], ref currentImage);

                /// Update list
                UpdateListBox(mImgInfoList[cImgIndex]);

                /// Save txt file
                SaveROIToTxtFile(mImgInfoList[cImgIndex]);
                                
                selectedGraphics = null;
            }
        }

        private void DrawRegion(imgInfo imgIn, ref Image<Bgr, byte> image)
        {
            foreach (regionInfo ri in imgIn.regionInfoList)
            {
                Bitmap bmp = image.ToBitmap();
                selectedGraphics = Graphics.FromImage(bmp);
                selectedGraphics.DrawRectangle(new Pen(Color.Red), ri.rect);
                selectedGraphics.DrawString($"{ri.labelIndex}-{labelSet[ri.labelIndex]}"
                    , new Font("Arial", 9), new SolidBrush(Color.Red), ri.rect.X, ri.rect.Y);
                image = new Image<Bgr, byte>(bmp);
            }
            /// Update final image
            Img_viewer.Source = ImgConverter.ToBitmapSource(image.ToBitmap());
        }

        private void UpdateListBox(imgInfo imgIn)
        {
            LB_regionRectangles.Items.Clear();
            LB_outPutRegionValues.Items.Clear();
            foreach (regionInfo r in imgIn.regionInfoList)
            {
                Label l = new Label();
                l.Content = $"Class{r.labelIndex}, ({r.rect.X}, {r.rect.Y}, {r.rect.Width}, {r.rect.Height})";
                l.FontSize = 12;
                LB_regionRectangles.Items.Add(l);
                
                Label l2 = new Label();
                l2.Content = $"Class{r.labelIndex}, ({r.location[0]}, {r.location[1]}, {r.location[2]}, {r.location[3]})";
                l2.FontSize = 12;
                LB_outPutRegionValues.Items.Add(l2);
            }
        }
        #endregion Select Area
        #endregion ================================= Step2 Labeling ===========================================
        #endregion Between Steps

        #region Functions

        private void LoadDataSetRadioButtons()
        {
            for (int i = 0; i < labelSet.Count; i++)
            {
                labelSelectorList.Add(new RadioButton());
                labelSelectorList[i].Checked += RadioButton_checked;
                labelSelectorList[i].Content = labelSet[i];
                Wrap_radios.Children.Add(labelSelectorList[i]);
            }
            labelSelectorList[0].IsChecked = true;
        }

        private void RadioButton_checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            for (int i = 0; i < labelSet.Count; i++)
            {
                if ((bool)labelSelectorList[i].IsChecked)
                    clabelIndex = i;
            }
        }

        private void Btn_deleteLastRegion_Click(object sender, RoutedEventArgs e)
        {
            if (mImgInfoList[cImgIndex].regionInfoList.Count > 0)
            {
                mImgInfoList[cImgIndex].regionInfoList.RemoveAt(mImgInfoList[cImgIndex].regionInfoList.Count - 1);

                /// Draw from blank image
                currentImage = new Image<Bgr, byte>(new Bitmap(mImgInfoList[cImgIndex].imagePath));
                DrawRegion(mImgInfoList[cImgIndex], ref currentImage);

                /// Update list
                UpdateListBox(mImgInfoList[cImgIndex]);

                /// Save txt file
                SaveROIToTxtFile(mImgInfoList[cImgIndex]);
            }
        }

        private void DisplayImgAndLabel(int index)
        {
            Bitmap temp = new Bitmap(mImgInfoList[index].imagePath);
            currentImage = new Image<Bgr, byte>(temp);
            zoomFactor_x = Img_viewer.Width / temp.Width;
            zoomFactor_y = Img_viewer.Height / temp.Height;
            temp.Dispose();

            /// Draw 
            DrawRegion(mImgInfoList[cImgIndex], ref currentImage);

            /// Update list
            UpdateListBox(mImgInfoList[cImgIndex]);
        }

        /// <summary>
        /// Convert a rectangle to relative value that yolo requires
        /// </summary>
        /// <param name="image"></param>
        /// <param name="_rect"></param>
        /// <returns></returns>
        private double[] ConvertRectToLocation(Bitmap image, Rectangle _rect)
        {
            double[] location = new double[4];

            location[0] = ((_rect.X + _rect.Width) / 2) / (double)image.Width;
            location[1] = ((_rect.Y + _rect.Height) / 2) / (double)image.Height;
            location[2] = _rect.Width / (double)image.Width;
            location[3] = _rect.Height / (double)image.Height;

            location[0] = Convert.ToDouble(location[0].ToString("0.00000"));
            location[1] = Convert.ToDouble(location[1].ToString("0.00000"));
            location[2] = Convert.ToDouble(location[2].ToString("0.00000"));
            location[3] = Convert.ToDouble(location[3].ToString("0.00000"));
            return location;
        }
        /// <summary>
        /// Convert yolo location value back to rectangle
        /// </summary>
        /// <param name="image"></param>
        /// <param name="_rect"></param>
        /// <returns></returns>
        private Rectangle ConvertLocationToRect(Bitmap image, double[] loc)
        {
            Rectangle rect = new Rectangle();

            rect.Width = (int)(loc[2] * image.Width);
            rect.Height = (int)(loc[3] * image.Height);
            rect.X = (int)((loc[0] * image.Width) * 2 - rect.Width);
            rect.Y = (int)((loc[1] * image.Height) * 2 - rect.Height);

            return rect;
        }
        private void LoadLabelAndLocationFromTxt(string fileName, imgInfo imgInfo)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            imgInfo.regionInfoList.Clear();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] labelAndLoc = new string[5];
                    labelAndLoc = line.Split(' ');
                    int label = Convert.ToInt32(labelAndLoc[0]);
                    double[] loc = new double[4];
                    loc[0] = Convert.ToDouble(labelAndLoc[1]);
                    loc[1] = Convert.ToDouble(labelAndLoc[2]);
                    loc[2] = Convert.ToDouble(labelAndLoc[3]);
                    loc[3] = Convert.ToDouble(labelAndLoc[4]);
                    Bitmap t = new Bitmap(imgInfo.imagePath);
                    Rectangle r = ConvertLocationToRect(t, loc);
                    imgInfo.regionInfoList.Add(new regionInfo(label, r, loc));
                    t.Dispose();
                }
            }
        }

        private void SaveROIToTxtFile(imgInfo imgInfo)
        {
            string txtFile = mImgInfoList[cImgIndex].imagePath.Replace(".jpg", ".txt");
            if (!File.Exists(txtFile))
            {
                File.Create(txtFile).Dispose();
            }

            using (StreamWriter sw = new StreamWriter(txtFile))
            {
                foreach (regionInfo r in imgInfo.regionInfoList)
                {
                    sw.WriteLine($"{r.labelIndex} {r.location[0]} {r.location[1]} {r.location[2]} {r.location[3]}");
                }
            }

        }

        private string GetFileName(string fileFullName)
        {
            FileInfo f = new FileInfo(fileFullName);
            return f.Name;
        }

        /// <summary>
        /// Replace \ with / in directory string
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        private string ConvertFileName(string fileFullName)
        {
            string str = fileFullName.Replace(@"\", @"/");
            return str;
        }

        private void LoadNames(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            labelSet.Clear();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    labelSet.Add(line);
                }
            }
        }

        private void SaveNamesFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (string str in labelSet)
                {
                    sw.WriteLine(str);
                }
            }

        }

        private void SaveDataFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine($"classes = {labelSet.Count()}");
                sw.WriteLine($"train = {ConvertFileName(obj_train_file)}");
                sw.WriteLine($"valid = {ConvertFileName(obj_test_file)}");
                sw.WriteLine($"names = {ConvertFileName(obj_names_file)}");
                sw.WriteLine($"backup = {ConvertFileName(GV.ML_Folders[(int)MLFolders.ML_YOLO_backup])}/");
            }

        }

        private void SaveTrainAndTestFile()
        {
            /// Split images
            int totalClasses = labelSet.Count;
            int[] eachClassCount = new int[totalClasses];
            List<List<imgInfo>> eachClassImgInfo = new List<List<imgInfo>>();

            List<imgInfo> mImgInfoList_train = new List<imgInfo>();
            List<imgInfo> mImgInfoList_test = new List<imgInfo>();

            for (int i = 0; i < totalClasses; i++)
            {
                eachClassImgInfo.Add(new List<imgInfo>());
            }

            foreach(imgInfo imI in mImgInfoList)
            {
                for (int i = 0; i < totalClasses; i++)
                {
                    if (imI.regionInfoList.Count > 0 && imI.regionInfoList[0].labelIndex == i)
                    {
                        eachClassCount[i]++;
                        eachClassImgInfo[i].Add(imI);
                    }
                }
            }
            for (int i = 0; i < totalClasses; i++)
            {
                for (int j = 0; j < eachClassCount[i]; j++)
                {
                    if (eachClassImgInfo[i][j].regionInfoList[0].labelIndex == i)
                    {
                        if (j < TRAIN_IMG_RATE * eachClassCount[i])
                            mImgInfoList_train.Add(eachClassImgInfo[i][j]);
                        else
                            mImgInfoList_test.Add(eachClassImgInfo[i][j]);
                    }
                }
            }

            SaveFile(obj_train_file, mImgInfoList_train);
            SaveFile(obj_test_file, mImgInfoList_test);
        }

        private void SaveFile(string fileName, List<imgInfo> imgList)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (imgInfo imI in imgList)
                {
                    sw.WriteLine(ConvertFileName(imI.imagePath));
                    //sw.WriteLine($"data/img/{GetFileName(imI.imagePath)}");
                }
            }
        }

        private void GenerateCfgFile()
        {
            if (!File.Exists(yolo_cfg_file))
            {
                mMessageBox.Show($"{GetFileName(yolo_cfg_file)} file doesn't exist");
                return;
            }

            if (File.Exists(obj_cfg_file))
            {
                File.Delete(obj_cfg_file);
            }

            StringBuilder sb = new StringBuilder();
            foreach (string line in File.ReadLines(yolo_cfg_file))
            {
                string result = line;
                if (line == "batch=1")
                {
                    result = line.Replace("batch=1", "batch=64");
                }
                if (line == "subdivisions=1")
                {
                    result = line.Replace("subdivisions=1", "subdivisions=64");
                }
                if (line == "classes=80")
                {
                    result = line.Replace("classes=80", $"classes={labelSet.Count}");
                }
                if (line == "filters=255")
                {
                    result = line.Replace("filters=255", $"filters={(labelSet.Count + 5) * 3}");
                }

                
                sb.AppendLine(result);
            }

            File.AppendAllText(obj_cfg_file, sb.ToString());
        }

        private void GenerateCmdFile()
        {
            if (!CheckRequiredFiles()) return;

            if (!File.Exists(train_cmd_file))
            {
                File.Create(train_cmd_file).Dispose();
            }
            //using (StreamWriter sw = new StreamWriter(train_cmd_file))
            //{
            //    sw.WriteLine($"darknet.exe detector train data/{GetFileName(obj_data_file)} {GetFileName(obj_cfg_file)} {GetFileName(pretrained_weight_file)}");
            //    sw.WriteLine("pause");
            //}

            using (StreamWriter sw = new StreamWriter(train_cmd_file))
            {
                sw.WriteLine(DarknetTrainCmd());
                sw.WriteLine("pause");
            }
        }

        public string DarknetTrainCmd()
        {
            return $"darknet.exe detector train {ConvertFileName(obj_data_file)} {ConvertFileName(obj_cfg_file)} {ConvertFileName(pretrained_weight_file)} -dont_show";
        }


        public bool CheckRequiredFiles()
        {
            StringBuilder sb = new StringBuilder();
            if (!File.Exists(obj_data_file))
            {
                sb.AppendLine(GetFileName(obj_data_file) + " is missing!");
            }
            if (!File.Exists(obj_cfg_file))
            {
                sb.AppendLine(GetFileName(obj_cfg_file) + " is missing!");
            }
            if (!File.Exists(pretrained_weight_file))
            {
                sb.AppendLine(GetFileName(pretrained_weight_file) + " is missing!");
            }

            if (sb.ToString() != "")
            {
                MessageBox.Show(sb.ToString(), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        #endregion Functions

    }
}
