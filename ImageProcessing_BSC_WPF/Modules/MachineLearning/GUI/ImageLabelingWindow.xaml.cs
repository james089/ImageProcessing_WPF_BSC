using mUserControl_BSC_dll_x64;
using System;
using System.Collections.Generic;
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

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for ImageLabelingWindow.xaml
    /// </summary>
    public partial class ImageLabelingWindow : Window
    {
        int totalImgNum = 0;
        int imageIndex = 0;                 // The labeling img index inside the imglist
        
        List<int> imgLabelList = new List<int>();
        FileInfo[] imageInfo;
        static string currentImgDir;                                     // Img dir
        static string mapFileUrl_train = BindManager.BindMngr.ML_rootDir.value + "\\train_map.txt";
        static string mapFileUrl_test = BindManager.BindMngr.ML_rootDir.value + "\\test_map.txt";
        string[] mapFileUrlArr = new string[2] { mapFileUrl_train, mapFileUrl_test };

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

        public ImageLabelingWindow(JobType jt)
        {
            InitializeComponent();
            DataContext = BindManager.BindMngr;

            jobType = jt;
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lbl_imgFolder.Content = sourceImgFolderArr[(int)jobType];
            /// Scan images
            currentImgDir = sourceImgFolderArr[(int)jobType];

            totalImgNum = scanImgs(currentImgDir, out imageInfo);
            if (totalImgNum == 0)
            {
                Close(); return;
            }

            /// Load img labels if exists
            loadLabels(mapFileUrlArr[(int)jobType]);
            
            /// Display the first img and 
            displayImgAndLabel(0);
        }

        private void Btn_right_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex < totalImgNum -1) imageIndex++;
            else
                imageIndex = totalImgNum - 1;
            displayImgAndLabel(imageIndex);
        }

        private void Btn_left_Click(object sender, RoutedEventArgs e)
        {
            if (imageIndex > 0) imageIndex--;
            else
                imageIndex = 0;
            displayImgAndLabel(imageIndex);
        }

        private void TB_currentLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (TB_currentLabel.Text != "")
            {
                int currentLabel = Convert.ToInt32(TB_currentLabel.Text);
                imgLabelList[imageIndex] = currentLabel;
            }  
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



        #region Functions

        private int scanImgs(string _imgDir, out FileInfo[] fileInfo)
        {
            int total = 0;
            fileInfo = new DirectoryInfo(_imgDir).GetFiles();
            total = fileInfo.Length;
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
                    imgLabelList.Add(0);
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
                        imgLabelList.Add(Convert.ToInt32(temp[1]));
                    }
                    sr.Dispose();
                }
            }
        }

        private void displayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", sourceImgFolderArr[(int)jobType], imageInfo[index].Name)));
            TB_currentLabel.Text = imgLabelList[index].ToString();
        }


        private void createMap(string mapfile)
        {
            FileInfo[] resizedFileInfo;
            if (scanImgs(mlImgFolderArr[(int)jobType], out resizedFileInfo) < scanImgs(currentImgDir))
            {
                mNotification.Show("Reszing not done!");
                return;
            }
            using (StreamWriter sw = new StreamWriter(mapfile))
            {
                for (int i = 0; i < totalImgNum; i++)
                {
                    sw.WriteLine(string.Format("{0}\t{1}", string.Format(@"{0}\{1}", mlImgFolderArr[(int)jobType], resizedFileInfo[i].Name), imgLabelList[i]));
                }
                sw.Dispose();
            }
            mNotification.Show("Map file created");
        }
        #endregion

    }
}
