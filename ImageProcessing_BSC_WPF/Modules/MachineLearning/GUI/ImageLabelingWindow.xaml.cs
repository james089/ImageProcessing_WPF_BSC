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
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;

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
        string[] labelSet;

        //List<int> sourcelabelList = new List<int>();
        FileInfo[] sourceImageInfo;
        //FileInfo[] resizedImageInfo;
        static string currentImgDir;                                     // Img dir

        /// <summary>
        /// For source image labling use
        /// </summary>
        static string sourceMapFile_train = BindManager.BindMngr.ML_CNTK_sourceImgDir.value + "\\local_train_map.txt";
        static string sourceMapFile_test = BindManager.BindMngr.ML_CNTK_sourceImgDir.value + "\\local_test_map.txt";
        string[] sourceMapFileArr = new string[2] { sourceMapFile_train, sourceMapFile_test };

        /// <summary>
        /// For ML model use
        /// </summary>
        static string mapFile_train = BindManager.BindMngr.ML_CNTK_rootDir.value + "\\train_map.txt";
        static string mapFile_test = BindManager.BindMngr.ML_CNTK_rootDir.value + "\\test_map.txt";
        string[] mapFileArr = new string[2] { mapFile_train, mapFile_test };

        Style radio_tagStyle;
        /// <summary>
        /// Job type is train or test
        /// </summary>
        JobType jobType;
        string[] sourceImgFolderArr = new string[2] 
        {
            BindManager.BindMngr.ML_CNTK_sourceTrainImgDir.value,
            BindManager.BindMngr.ML_CNTK_sourceTestImgDir.value
        };
        string[] mlImgFolderArr = new string[2]
        {
            BindManager.BindMngr.ML_CNTK_trainImgDir.value,
            BindManager.BindMngr.ML_CNTK_testImgDir.value
        };

        List<RadioButton> radioBtnList = new List<RadioButton>();
        List<System.Windows.Controls.Image> imgThumbList = new List<System.Windows.Controls.Image>();

        List<int> multiSelectIndex;

        int thumb_w = 80, thumb_h = 80;

        /// <summary>
        /// This is for labbling image
        /// </summary>
        private struct imgInfo
        {
            public int index;
            public Bitmap image;
        }

        private class imgMapStruct
        {
            public string dir;
            public int label;
        }

        List<imgMapStruct> sourceImgMapList = new List<imgMapStruct>();
        List<imgMapStruct> resRandImgMapList = new List<imgMapStruct>();

        #endregion Local Paras

        public ImageLabelingWindow(JobType jt, string[] _labels)
        {
            InitializeComponent();
            DataContext = BindManager.BindMngr;

            radio_tagStyle = Application.Current.FindResource("Radio_tag") as Style;

            jobType = jt;
            labelSet = _labels;

            ResizingRoutine.DoWork += new DoWorkEventHandler(ResizingRoutine_doWork);
            ResizingRoutine.ProgressChanged += new ProgressChangedEventHandler(ResizingRoutine_ProgressChanged);
            ResizingRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ResizingRoutine_WorkerCompleted);
            ResizingRoutine.WorkerReportsProgress = true;
            ResizingRoutine.WorkerSupportsCancellation = true;

        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            switch (mMessageBox.ShowYesNoCancel("Save current labeling info?"))
            {
                case mDialogResult.yes:
                    createSourceMapFile(sourceMapFileArr[(int)jobType]);break;
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
            loadLabels(sourceMapFileArr[(int)jobType]);
            
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
                for (int i = 0; i < labelSet.Length; i++)
                {
                    if ((bool)radioBtnList[i].IsChecked)
                        sourceImgMapList[sImgIndex].label = i;
                }
            }
            /// Multi-select
            else
            {
                foreach (int m in multiSelectIndex)
                {
                    for (int i = 0; i < labelSet.Length; i++)
                    {
                        if ((bool)radioBtnList[i].IsChecked)
                            sourceImgMapList[m].label = i;
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

        private void Btn_createMapFile_Click(object sender, RoutedEventArgs e)
        {
            resize_randomize();
        }

        private void List_imgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(bool)Chk_multiseletect.IsChecked)
            {
                sImgIndex = List_imgs.SelectedIndex;
                displayImgAndLabel(sImgIndex);
            }
            /// Multi select mode
            else if (List_imgs.SelectedItems.Count > 1)
            {
                multiSelectIndex = (from object obj in List_imgs.SelectedItems select List_imgs.Items.IndexOf(obj)).ToList();
                disabelPreviewAnddeselectTags();
            }
            else if (List_imgs.SelectedItems.Count == 1)
            {
                sImgIndex = List_imgs.SelectedIndex;
                displayImgAndLabel(sImgIndex);
            }
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
                //radioBtnList[i].Style = radio_tagStyle;
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

        private void loadLabels(string mapFileUrl)
        {
            sourceImgMapList.Clear();
            if (!File.Exists(mapFileUrl))
            {
                File.Create(mapFileUrl).Dispose();
                // Initalize
                for (int i = 0; i < totalImgNum; i++)
                {
                    imgMapStruct ims = new imgMapStruct() {
                        dir = string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], sourceImageInfo[i].Name),
                        label = 0
                    };
                    sourceImgMapList.Add(ims);
                }
                using (StreamWriter sw = new StreamWriter(mapFileUrl))
                {
                    for (int i = 0; i < totalImgNum; i++)
                    {
                        sw.WriteLine(string.Format("{0}\t{1}", 
                            string.Format(@"{0}\{1}", mlImgFolderArr[(int)jobType], sourceImageInfo[i].Name),
                            sourceImgMapList[i].label));
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

                        imgMapStruct ims = new imgMapStruct()
                        {
                            dir = temp[0],
                            label = Convert.ToInt32(temp[1])
                        };
                        sourceImgMapList.Add(ims);
                    }
                    sr.Dispose();
                }
            }
        }

        private void displayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], sourceImageInfo[index].Name)));
            radioBtnList[sourceImgMapList[index].label].IsChecked = true;
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
                Bitmap bmp = new Bitmap(string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], sourceImageInfo[i].Name));
                Bitmap bmp_resized = CntkBitmapExtensions.Resize(bmp, thumb_w, thumb_h, false);
                //Thread.Sleep(1);
                loadImgRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum), new imgInfo() {index = i, image = bmp_resized });
                bmp.Dispose();
                //bmp_resized.Dispose();
            }
        }
        #endregion Load image thumbs

        //private void resize()
        //{
        //    if (TB_resizeWidth.Text != "" && TB_resizeHeight.Text != "")
        //        ImageResizing.ImageBatchResizing(
        //            sourceImgFolderArr[(int)jobType],
        //            mlImgFolderArr[(int)jobType],
        //            Convert.ToInt32(BindManager.BindMngr.ML_desWidth.value),
        //            Convert.ToInt32(BindManager.BindMngr.ML_desHeight.value));
        //    else
        //        mMessageBox.Show("Empty string");
        //}

        #region Resize and shuffle and create map
        private BackgroundWorker ResizingRoutine = new BackgroundWorker();
        private int CurrentImageIndex = 0;
        private void resize_randomize()
        {
            if (!ResizingRoutine.IsBusy)
                ResizingRoutine.RunWorkerAsync();
        }

        private void ResizingRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            createFinalMapFile(mapFileArr[(int)jobType]);
            generateMeanFile(BindManager.BindMngr.ML_CNTK_rootDir.value);
            BindManager.BindMngr.GMessage.value = "Map file created";
        }

        private void ResizingRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BindManager.BindMngr.Progress.value = e.ProgressPercentage;
            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";

            BindManager.BindMngr.GMessage.value = string.Format("Resize and shuffle {0} images...({1})",
                totalImgNum - CurrentImageIndex,
                BindManager.BindMngr.ProgressString.value);
        }

        private void ResizingRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            resRandImgMapList.Clear();
            var rand = new Random();
            var randomList = sourceImgMapList.OrderBy(x => rand.Next()).ToList();

            for (int i = 0; i < totalImgNum; i++)
            {
                Bitmap bm = null;
                try
                {
                    bm = new Bitmap(randomList[i].dir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                Bitmap rbm = CntkBitmapExtensions.Resize(
                    bm,
                    Convert.ToInt32(BindManager.BindMngr.ML_desWidth.value),
                    Convert.ToInt32(BindManager.BindMngr.ML_desHeight.value),
                    true);
                /// Redo dir, but keep the shuffled label
                imgMapStruct ims = new imgMapStruct()
                {
                    dir = mlImgFolderArr[(int)jobType] + string.Format("\\{0:D5}.jpg", i),
                    label = randomList[i].label
                };

                resRandImgMapList.Add(ims);

                /// This is for creating mean file
                RGBList.Add(CntkBitmapExtensions.ExtractCHW(rbm));

                if (jobType == JobType.train) rbm = addPadding(rbm, 4);

                rbm.Save(resRandImgMapList[i].dir);

                ResizingRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / totalImgNum));
                Thread.Sleep(1);
                bm.Dispose();
                rbm.Dispose();
            }
        }

        List<List<float>> RGBList = new List<List<float>>();

        private void generateMeanFile(string _saveDir)
        {
            float[] AvgRGBArr;
            AvgRGBArr = new float[RGBList[0].Count];                // sum is [1, 3072]

            for (int j = 0; j < RGBList[0].Count; j++)
            {
                for (int i = 0; i < RGBList.Count; i++)
                {
                    AvgRGBArr[j] += RGBList[i][j];
                }
            }
            string[] AvgRGBArrString = new string[RGBList[0].Count];
            // Save to arr
            for (int j = 0; j < RGBList[0].Count; j++)
            {
                AvgRGBArr[j] = (AvgRGBArr[j] / RGBList.Count);           // AvgRGBList is [1, 3072]
                AvgRGBArrString[j] = string.Format("{0:E2}", AvgRGBArr[j]);
            }
            string str = String.Join(" ", AvgRGBArrString);

            // Save to xml
            string file_xml = _saveDir + "\\Custom_mean.xml";
            if (!File.Exists(file_xml)) File.Create(file_xml).Dispose();
            using (StreamWriter sw1 = new StreamWriter(file_xml))
            {
                sw1.WriteLine("<?xml version=\"1.0\" ?>");
                sw1.WriteLine("<opencv_storage>");
                sw1.WriteLine("  <Channel>3</Channel>");
                sw1.WriteLine("  <Row>32</Row>");
                sw1.WriteLine("  <Col>32</Col>");
                sw1.WriteLine("  <MeanImg type_id=\"opencv-matrix\">");
                sw1.WriteLine("    <rows>1</rows>");
                sw1.WriteLine("    <cols>3072</cols>");
                sw1.WriteLine("    <dt>f</dt>");
                sw1.WriteLine("    <data>" + str + "</data>");
                sw1.WriteLine("  </MeanImg>");
                sw1.WriteLine("</opencv_storage>");
            }

            mNotification.Show("Mean file generated!");
            BindManager.BindMngr.GMessage.value = "Mean file generated.";
        }

        private Bitmap addPadding(Bitmap rbm, int pad)
        {
            Bitmap output = new Bitmap(rbm.Width + pad * 2, rbm.Height + pad * 2);
            Graphics g = Graphics.FromImage(output);
            g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(128,128,128)), 0, 0, output.Width, output.Height);

            g.DrawImage(rbm, new PointF(pad, pad));
            return output;
        }
        #endregion  #region Resize and shuffle and create map

        /// <summary>
        /// This is for saving labeling work only
        /// </summary>
        /// <param name="mapfile"></param>
        private void createSourceMapFile(string mapfile)
        {
            using (StreamWriter sw = new StreamWriter(mapfile))
            {
                for (int i = 0; i < totalImgNum; i++)
                {
                    sw.WriteLine(string.Format("{0}\t{1}", sourceImgMapList[i].dir, sourceImgMapList[i].label));
                }
                sw.Dispose();
            }
        }

        /// <summary>
        /// This is create map file for ML model use
        /// </summary>
        /// <param name="mapfile"></param>
        private void createFinalMapFile(string mapfile)
        {
            using (StreamWriter sw = new StreamWriter(mapfile))
            {
                for (int i = 0; i < totalImgNum; i++)
                {
                    sw.WriteLine(string.Format("{0}\t{1}", resRandImgMapList[i].dir, resRandImgMapList[i].label));
                }
                sw.Dispose();
            }
            mNotification.Show("Map file created");
        }
        #endregion Functions

    }
}
