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

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for ImageLabelTool_Yolo.xaml
    /// </summary>
    public partial class ImageLabelTool_Yolo : Window
    {
        enum Steps
        {
            step0_resize,
            step1_createLabels,
            step2_labeling,
        }
        #region Local Paras
        const int thumb_w = 120, thumb_h = 120;
        int res_img_w = 0, res_img_h = 0;
        int totalImgNum = 0;
        /// <summary>
        ///  The labeling img index inside the imgThumbList
        /// </summary>
        int sImgIndex = 0;       

        FileInfo[] srcImgFile_Info;
        FileInfo[] ImgLabelROI_Info;
        static string currentImgDir;                                     // Img dir
        static string yoloDataDir;

        /// <summary>
        /// For ML model use
        /// </summary>
        static string obj_data_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.data";
        static string obj_names_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.names";
        static string obj_train_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\train.txt";
        static string obj_test_file = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\test.txt";

        List<string> labelSet = new List<string>();
        List<RadioButton> labelSelectorList = new List<RadioButton>();
        List<System.Windows.Controls.Image> imgThumbList = new List<System.Windows.Controls.Image>();
        List<imgInfo> mImgInfoList = new List<imgInfo>();

        /// <summary>
        /// This stores every info about an image
        /// </summary>
        private class imgInfo
        {
            public int index;
            public string imagePath;
            public List<regionInfo> regionInfoList;

            public imgInfo()
            {
                index = 0;
                imagePath = "";
                regionInfoList = new List<regionInfo>();
            }
        }

        private class regionInfo
        {
            public int label;
            public Rectangle rect;
            //public double[] location;
            public regionInfo()
            {
                label = 0;
                rect = new Rectangle();
                //location = new double[4] {0,0,0,0};
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
            InitializeComponent();
            DataContext = BindManager.BindMngr;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentImgDir = GV.ML_Folders[(int)MLFolders.ML_YOLO_img];
            yoloDataDir = GV.ML_Folders[(int)MLFolders.ML_YOLO_data];

            SwitchSteps(Steps.step0_resize);
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            switch (mMessageBox.ShowYesNoCancel("Save current labeling info?"))
            {
                case mDialogResult.yes:
                    SaveNamesFile(obj_names_file);
                    SaveDataFile(obj_data_file);
                    SaveTrainFile(obj_train_file);
                    SaveTestFile(obj_test_file);
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
                case Steps.step0_resize:
                    Grid_step0.Visibility = Visibility.Visible;
                    Grid_step1.Visibility = Visibility.Collapsed;
                    Grid_step2.Visibility = Visibility.Collapsed;

                    RB_640480_Checked(null, null);
                    break;
                case Steps.step1_createLabels:
                    BindManager.BindMngr.GMessage.value = "Add your classes";
                    Grid_step0.Visibility = Visibility.Collapsed;
                    Grid_step1.Visibility = Visibility.Visible;
                    Grid_step2.Visibility = Visibility.Collapsed;

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

                    /// Adding radio buttons to wrap panel
                    loadDataSetLabelRadioButtons();

                    /// Scan images
                    totalImgNum = ScanImgs(currentImgDir, out srcImgFile_Info);
                    if (totalImgNum == 0)
                    {
                        Close(); return;
                    }

                    /// Show image folder info
                    lbl_imgFolder.Text = currentImgDir;
                    lbl_trainingFilesFolder.Text = GV.ML_Folders[(int)MLFolders.ML_YOLO_data];

                    /// Load existing labels
                    LoadLabelAndRegions(currentImgDir);

                    /// Loading all images in this folder
                    loadImgThumbs();

                    /// Display the first img and the label
                    DisplayImgAndLabel(0);
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
            ImageResizing.ImageBatchResizing(GV.ML_Folders[(int)MLFolders.ML_YOLO_img], GV.ML_Folders[(int)MLFolders.ML_YOLO_img]
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
            SwitchSteps(Steps.step0_resize);
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
            if (sImgIndex < totalImgNum - 1) sImgIndex++;
            else
                sImgIndex = totalImgNum - 1;

            List_imgs.SelectedIndex = sImgIndex;
            List_imgs.ScrollIntoView(List_imgs.SelectedItem);
        }

        private void Btn_left_Click(object sender, RoutedEventArgs e)
        {
            if (sImgIndex > 0) sImgIndex--;
            else
                sImgIndex = 0;

            List_imgs.SelectedIndex = sImgIndex;
            List_imgs.ScrollIntoView(List_imgs.SelectedItem);
        }

        private void List_imgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sImgIndex = List_imgs.SelectedIndex;
            DisplayImgAndLabel(sImgIndex);
        }

        private void disabelPreviewAnddeselectTags()
        {
            Img_viewer.Source = null;
            for (int i = 0; i < labelSet.Count; i++)
            {
                if ((bool)labelSelectorList[i].IsChecked) labelSelectorList[i].IsChecked = false;
            }
        }

        private void Btn_editLabels_Click(object sender, RoutedEventArgs e)
        {
           
        }

        #endregion ================================= Step2 Labeling ===========================================
        #endregion Between Steps

        #region Load image thumbs

        private struct indexedImg
        {
            public int index;
            public Bitmap image;
        }

        BackgroundWorker loadImgRoutine = new BackgroundWorker();
        private void loadImgThumbs()
        {
            loadImgRoutine.DoWork += LoadImg_DoWork;
            loadImgRoutine.ProgressChanged += LoadImg_ProgressChanged;
            loadImgRoutine.RunWorkerCompleted += LoadImgRoutine_RunWorkerCompleted;
            loadImgRoutine.WorkerReportsProgress = true;

            for (int i = 0; i < totalImgNum; i++)
            {
                imgThumbList.Add(new System.Windows.Controls.Image());
                imgThumbList[i].Width = thumb_w;
                imgThumbList[i].Height = thumb_h;
                List_imgs.Items.Add(imgThumbList[i]);
            }
            List_imgs.SelectedIndex = 0;
            Thread.Sleep(20);
            if (!loadImgRoutine.IsBusy) loadImgRoutine.RunWorkerAsync();
        }

        private void LoadImgRoutine_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BindManager.BindMngr.GMessage.value = "Start labeling your image";
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

        private void LoadImg_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < totalImgNum; i++)
            {
                Bitmap bmp = new Bitmap(string.Format(@"{0}\{1}", currentImgDir, srcImgFile_Info[i].Name));
                Bitmap bmp_resized = CntkBitmapExtensions.Resize(bmp, thumb_w, thumb_h, false);
                //Thread.Sleep(1);
                loadImgRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum), new indexedImg() { index = i, image = bmp_resized });
                bmp.Dispose();
            }
        }
        #endregion Load image thumbs
        #region Functions

        private void loadDataSetLabelRadioButtons()
        {
            for (int i = 0; i < labelSet.Count; i++)
            {
                labelSelectorList.Add(new RadioButton());
                labelSelectorList[i].Checked += RadioButton_checked;
                labelSelectorList[i].Content = labelSet[i];
                Wrap_radios.Children.Add(labelSelectorList[i]);
            }
        }

        private void RadioButton_checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            //for (int i = 0; i < labelSet.Length; i++)
            //{
            //    if ((bool)labelSelectorList[i].IsChecked)
            //        mImgInfoList[sImgIndex].regionInfoList[0].label = i;
            //}
        }

        private int ScanImgs(string _imgDir, out FileInfo[] _fileInfo)
        {
            int total = 0;
            _fileInfo = new DirectoryInfo(_imgDir).GetFiles("*.jpg");
            total = _fileInfo.Length;
            if (total == 0)
            {
                mMessageBox.Show("No image found");
            }
            return total;
        }

        private void LoadLabelAndRegions(string _imgDir)
        {
            if (!Directory.Exists(_imgDir))
            {
                mMessageBox.Show("Folder not exits");
                return;
            }
            else
            {

            }
        }

        private void DisplayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", currentImgDir, srcImgFile_Info[index].Name)));
            //radioBtnList[sourceImgMapList[index].label].IsChecked = true;
        }

        private double[] OutputRegionInfo(Bitmap image, Rectangle _rect)
        {
            double[] location = new double[4];

            location[0] = ((_rect.X + _rect.Width) / 2) / (double)image.Width;
            location[1] = ((_rect.Y + _rect.Height) / 2) / (double)image.Height;
            location[2] = _rect.Width / (double)image.Width;
            location[3] = _rect.Height / (double)image.Height;

            return location;
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

        private void SaveDataFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine($"classes = {labelSet.Count()}");
                sw.WriteLine($"train =  data/{new FileInfo(obj_train_file).Name}");
                sw.WriteLine($"valid = data/{new FileInfo(obj_test_file).Name}");
                sw.WriteLine($"names = data/{new FileInfo(obj_names_file).Name}");
                sw.WriteLine($"backup = backup/");
            }

        }

        private void SaveTrainFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                //sw.WriteLine($"classes = {labelSet.Count()}");
            }

        }
        private void SaveTestFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                //sw.WriteLine($"classes = {labelSet.Count()}");
            }

        }
        #endregion Functions

    }
}
