using mUserControl_BSC_dll_x64;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities_BSC_dll_x64;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for ImageLabelingWindow.xaml
    /// </summary>
    public partial class ImageLabelingWindow : Window
    {
        #region Local Paras
        int totalImgNum = 0;
        int sImgIndex = 0;                 // The labeling img index inside the imglist
        string[] labels;

        List<int> labelList = new List<int>();
        FileInfo[] sourceImageInfo;
        FileInfo[] resizedImageInfo;
        static string currentImgDir;                                     // Img dir
        static string mapFileUrl_train = BindManager.BindMngr.ML_rootDir.value + "\\train_map.txt";
        static string mapFileUrl_test = BindManager.BindMngr.ML_rootDir.value + "\\test_map.txt";
        string[] mapFileUrlArr = new string[2] { mapFileUrl_train, mapFileUrl_test };

        Style radio_tagStyle;
        /// <summary>
        /// Job type is train or test
        /// </summary>
        JobType jobType;
        string[] sourceImgFolderArr = new string[2] 
        {
            BindManager.BindMngr.ML_sourceTrainImgDir.value,
            BindManager.BindMngr.ML_sourceTestImgDir.value
        };
        string[] mlImgFolderArr = new string[2]
        {
            BindManager.BindMngr.ML_trainImgDir.value,
            BindManager.BindMngr.ML_testImgDir.value
        };

        List<RadioButton> radioBtnList = new List<RadioButton>();
        List<System.Windows.Controls.Image> imgThumbList = new List<System.Windows.Controls.Image>();

        List<int> multiSelectIndex;

        int thumb_w = 80, thumb_h = 80;

        #endregion Local Paras

        public ImageLabelingWindow(JobType jt, string[] _labels)
        {
            InitializeComponent();
            DataContext = BindManager.BindMngr;

            radio_tagStyle = Application.Current.FindResource("Radio_tag") as Style;

            jobType = jt;
            labels = _labels;
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            switch (mMessageBox.ShowYesNoCancel("Save current labeling info?"))
            {
                case mDialogResult.yes:
                    createMap(mapFileUrlArr[(int)jobType]);break;
                case mDialogResult.no:
                    break;
                case mDialogResult.cancel:
                    return;
            }
            radioBtnList.Clear();
            imgThumbList.Clear();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BindManager.BindMngr.GMessage.value = "Start labeling your image";
            /// Adding radio buttons to wrap panel
            loadDataSetLabelRadioButtons();
            /// Show image folder info
            lbl_imgFolder.Content = sourceImgFolderArr[(int)jobType];
            /// Scan images
            currentImgDir = sourceImgFolderArr[(int)jobType];
            totalImgNum = scanImgs(currentImgDir, out sourceImageInfo);
            if (totalImgNum == 0)
            {
                Close(); return;
            }

            /// Load img labels from map file if exists
            loadLabels(mapFileUrlArr[(int)jobType]);
            
            /// Display the first img and the label
            displayImgAndLabel(0);

            /// Loading all images in this folder
            loadImgThumbs();
        }

        private void RadioButton_checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            /// Single select
            if (!(bool)Chk_multiseletect.IsChecked)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if ((bool)radioBtnList[i].IsChecked)
                        labelList[sImgIndex] = i;
                }
            }
            /// Multi-select
            else
            {
                foreach (int m in multiSelectIndex)
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if ((bool)radioBtnList[i].IsChecked)
                            labelList[m] = i;
                    }
                }
            }
        }

        private void Btn_right_Click(object sender, RoutedEventArgs e)
        {
            if (sImgIndex < totalImgNum - 1) sImgIndex++;
            else
                sImgIndex = totalImgNum - 1;
            displayImgAndLabel(sImgIndex);
        }

        private void Btn_left_Click(object sender, RoutedEventArgs e)
        {
            if (sImgIndex > 0) sImgIndex--;
            else
                sImgIndex = 0;
            displayImgAndLabel(sImgIndex);
        }


        private void Btn_resize_Click(object sender, RoutedEventArgs e)
        {
            if (TB_resizeWidth.Text != "" && TB_resizeHeight.Text != "")
                ImageResizing.ImageBatchResizing(
                    sourceImgFolderArr[(int)jobType],
                    mlImgFolderArr[(int)jobType],
                    Convert.ToInt32(BindManager.BindMngr.ML_desWidth.value),
                    Convert.ToInt32(BindManager.BindMngr.ML_desHeight.value));
            else
                mMessageBox.Show("Empty string");
        }

        private void Btn_createMapFile_Click(object sender, RoutedEventArgs e)
        {
            createMap(mapFileUrlArr[(int)jobType]);
        }

        private void List_imgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(bool)Chk_multiseletect.IsChecked)
                displayImgAndLabel(List_imgs.SelectedIndex);
            /// Multi select mode
            else if (List_imgs.SelectedItems.Count > 1)
            {
                multiSelectIndex = (from object obj in List_imgs.SelectedItems select List_imgs.Items.IndexOf(obj)).ToList();
                disabelPreviewAnddeselectTags();
            }
            else if (List_imgs.SelectedItems.Count == 1)
            {
                displayImgAndLabel(List_imgs.SelectedIndex);
            }
        }

        private void disabelPreviewAnddeselectTags()
        {
            Img_viewer.Source = null;
            for (int i = 0; i < labels.Length; i++)
            {
                if ((bool)radioBtnList[i].IsChecked) radioBtnList[i].IsChecked = false;
            }
        }

        private void Chk_multiseletect_Checked(object sender, RoutedEventArgs e)
        {
            List_imgs.SelectionMode = SelectionMode.Multiple;
        }

        private void Chk_multiseletect_Unchecked(object sender, RoutedEventArgs e)
        {
            List_imgs.SelectionMode = SelectionMode.Single;
        }

        #region Functions

        private void loadDataSetLabelRadioButtons()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                radioBtnList.Add(new RadioButton());
                radioBtnList[i].Checked += RadioButton_checked;
                radioBtnList[i].Content = labels[i];
                radioBtnList[i].Style = radio_tagStyle;
                Wrap_radios.Children.Add(radioBtnList[i]);
            }
        }

        private int scanImgs(string _imgDir, out FileInfo[] _fileInfo)
        {
            int total = 0;
            _fileInfo = new DirectoryInfo(_imgDir).GetFiles();
            total = _fileInfo.Length;
            if (total == 0)
            {
                mMessageBox.Show("No image found");
            }
            return total;
        }

        private int scanImgs(string _imgDir)
        {
            int total = 0;
            FileInfo[] fileInfo = new DirectoryInfo(_imgDir).GetFiles();
            total = fileInfo.Length;
            return total;
        }

        private void loadLabels(string mapFileUrl)
        {
            if (!File.Exists(mapFileUrl))
            {
                File.Create(mapFileUrl).Dispose();
                // Initalize
                for (int i = 0; i < totalImgNum; i++)
                {
                    labelList.Add(0);
                }
                using (StreamWriter sw = new StreamWriter(mapFileUrl))
                {
                    for (int i = 0; i < totalImgNum; i++)
                    {
                        sw.WriteLine(string.Format("{0}\t{1}", string.Format(@"{0}\{1}", mlImgFolderArr[(int)jobType], sourceImageInfo[i].Name), labelList[i]));
                    }
                    sw.Dispose();
                }
            }
            else
            {
                using (StreamReader sr = File.OpenText(mapFileUrl))
                {
                    for (int i = 0; i < totalImgNum; i++)
                    {
                        string[] temp;
                        temp = sr.ReadLine().Split('\t');
                        labelList.Add(Convert.ToInt32(temp[1]));
                    }
                    sr.Dispose();
                }
            }
        }

        private void displayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], sourceImageInfo[index].Name)));
            radioBtnList[labelList[index]].IsChecked = true;
        }

        #region Load image thumbs
        BackgroundWorker loadImgRoutine = new BackgroundWorker();
        private struct imgInfo
        {
            public int index;
            public Bitmap image;
        }

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

            imgThumbList[((imgInfo)e.UserState).index].Source = ImgConverter.ToBitmapSource(((imgInfo)e.UserState).image);
            ((imgInfo)e.UserState).image.Dispose();
        }

        private void LoadImg_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < totalImgNum; i++)
            {
                Bitmap bmp = new Bitmap(string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], sourceImageInfo[i].Name));
                Bitmap bmp_resized = CntkBitmapExtensions.Resize(bmp, thumb_w, thumb_h, false);
                //Thread.Sleep(1);
                loadImgRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum), new imgInfo() {index = i, image = bmp_resized });
                bmp.Dispose();
                //bmp_resized.Dispose();
            }
        }
        #endregion Load image thumbs

        private void createMap(string mapfile)
        {
            if (scanImgs(mlImgFolderArr[(int)jobType], out resizedImageInfo) < scanImgs(currentImgDir))
            {
                mNotification.Show("Reszing not done!");
                return;
            }
            using (StreamWriter sw = new StreamWriter(mapfile))
            {
                for (int i = 0; i < totalImgNum; i++)
                {
                    sw.WriteLine(string.Format("{0}\t{1}", string.Format(@"{0}\{1}", mlImgFolderArr[(int)jobType], resizedImageInfo[i].Name), labelList[i]));
                }
                sw.Dispose();
            }
            mNotification.Show("Map file created");
        }
        #endregion

    }
}
