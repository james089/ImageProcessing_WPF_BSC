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

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for ImageLabel_Yolo.xaml
    /// </summary>
    public partial class ImageLabel_Yolo : Window
    {
        enum Steps
        {
            step0_resize, 
            step1_createLabels,

        }
        #region Local Paras
        int totalImgNum = 0;
        int sImgIndex = 0;                 // The labeling img index inside the imglist
        string[] labelSet;
        
        FileInfo[] sourceImageInfo;
        static string currentImgDir;                                     // Img dir

        /// <summary>
        /// For ML model use
        /// </summary>
        static string obj_data_file     = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.data";
        static string obj_names_file    = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\obj.names";
        static string obj_train_file    = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\train.txt";
        static string obj_test_file     = GV.ML_Folders[(int)MLFolders.ML_YOLO_data] + "\\test.txt";

        List<RadioButton> radioBtnList = new List<RadioButton>();
        List<System.Windows.Controls.Image> imgThumbList = new List<System.Windows.Controls.Image>();

        int thumb_w = 120, thumb_h = 120;

        /// <summary>
        /// This is for labbling image
        /// </summary>
        private class imgInfo
        {
            public int index;
            public Bitmap image;
            public List<Rectangle> rectList;

            public imgInfo()
            {
                index = 0;
                image = null;
                rectList = new List<Rectangle>();
            }
        }

        //private class imgMapStruct
        //{
        //    public uint label;
        //    public double dir;
        //}

        //List<imgMapStruct> sourceImgMapList = new List<imgMapStruct>();

        #endregion Local Paras

        public ImageLabel_Yolo()
        {
            InitializeComponent();
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            switch (mMessageBox.ShowYesNoCancel("Save current labeling info?"))
            {
                case mDialogResult.yes:
                    break;
                case mDialogResult.no:
                    break;
                case mDialogResult.cancel:
                    return;
            }
            imgThumbList.Clear();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BindManager.BindMngr.GMessage.value = "Start labeling your image";
            /// Adding radio buttons to wrap panel
            //loadDataSetLabelRadioButtons();
            /// Scan images
            currentImgDir = GV.ML_Folders[(int)MLFolders.ML_YOLO_img];
            totalImgNum = scanImgs(currentImgDir, out sourceImageInfo);
            if (totalImgNum == 0)
            {
                Close(); return;
            }

            /// Show image folder info
            lbl_imgFolder.Content = currentImgDir;
            lbl_trainingFilesFolder.Content = GV.ML_Folders[(int)MLFolders.ML_YOLO_data];

            /// Load existing labels
            loadLabels(currentImgDir);
            
            /// Display the first img and the label
            displayImgAndLabel(0);

            /// Loading all images in this folder
            loadImgThumbs();
        }


        private void RadioButton_checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

        }

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
            displayImgAndLabel(sImgIndex);
        }

        private void disabelPreviewAnddeselectTags()
        {
            Img_viewer.Source = null;
            for (int i = 0; i < labelSet.Length; i++)
            {
                if ((bool)radioBtnList[i].IsChecked) radioBtnList[i].IsChecked = false;
            }
        }

        private void Chk_multiseletect_Checked(object sender, RoutedEventArgs e)
        {
            List_imgs.SelectionMode = SelectionMode.Extended;
        }

        private void Chk_multiseletect_Unchecked(object sender, RoutedEventArgs e)
        {
            List_imgs.SelectionMode = SelectionMode.Single;
        }

        #region Functions

        private void loadDataSetLabelRadioButtons()
        {
            for (int i = 0; i < labelSet.Length; i++)
            {
                radioBtnList.Add(new RadioButton());
                radioBtnList[i].Checked += RadioButton_checked;
                radioBtnList[i].Content = labelSet[i];
                Wrap_radios.Children.Add(radioBtnList[i]);
            }
        }

        private int scanImgs(string _imgDir, out FileInfo[] _fileInfo)
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

        private void loadLabels(string imgFolder)
        {
            if (!File.Exists(imgFolder))
            {
                
            }
            else
            {
                
            }
        }

        private void displayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", currentImgDir, sourceImageInfo[index].Name)));
            //radioBtnList[sourceImgMapList[index].label].IsChecked = true;
        }

        #region Load image thumbs
        BackgroundWorker loadImgRoutine = new BackgroundWorker();

        //Bitmap bmp_resized;
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
                imgThumbList[((imgInfo)e.UserState).index].Source = ImgConverter.ToBitmapSource(((imgInfo)e.UserState).image);
            }
            catch (Exception)
            {;
            }

            ((imgInfo)e.UserState).image.Dispose();
        }

        private void LoadImg_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < totalImgNum; i++)
            {
                Bitmap bmp = new Bitmap(string.Format(@"{0}\{1}", currentImgDir, sourceImageInfo[i].Name));
                Bitmap bmp_resized = CntkBitmapExtensions.Resize(bmp, thumb_w, thumb_h, false);
                //Thread.Sleep(1);
                loadImgRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum), new imgInfo() {index = i, image = bmp_resized });
                bmp.Dispose();
            }
        }
        #endregion Load image thumbs

        #endregion Functions

    }
}
