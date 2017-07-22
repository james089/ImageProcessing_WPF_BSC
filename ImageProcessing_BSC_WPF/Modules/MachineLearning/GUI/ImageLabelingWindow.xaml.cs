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
        int imageIndex = 0;

        List<BitmapImage> imgList = new List<BitmapImage>();
        List<int> imgLabelList = new List<int>();
        FileInfo[] imageInfo;
        string dir;

        public ImageLabelingWindow()
        {
            InitializeComponent();
            DataContext = StringManager.StrMngr;
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dir = StringManager.StrMngr.ML_resizedImgDir.value;
            imageInfo = new DirectoryInfo(dir).GetFiles();
            totalImgNum = imageInfo.Length;
            if (totalImgNum == 0)
            {
                mMessageBox.Show("No image found");
                Close();
                return;
            }
            // Initalize
            for (int i = 0; i < totalImgNum; i++)
            {
                imgLabelList.Add(0);
            }

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

        private void displayImgAndLabel(int index)
        {
            Img_viewer.Source = new BitmapImage(new Uri(string.Format(@"{0}\{1}", dir, imageInfo[index].Name)));
            TB_currentLabel.Text = imgLabelList[index].ToString();
        }

        private void Btn_createMapFile_Click(object sender, RoutedEventArgs e)
        {
            string mapfile = StringManager.StrMngr.ML_rootDir.value + "\\train_map.txt";
            if (!File.Exists(mapfile)) File.Create(mapfile).Dispose();
            using (StreamWriter sw = new StreamWriter(mapfile))
            {
                for (int i = 0; i < totalImgNum; i++)
                {
                    sw.WriteLine(string.Format("{0}\t{1}", string.Format(@"{0}\{1}", dir, imageInfo[i].Name), imgLabelList[i]));
                }
                sw.Dispose();
            }
        }
    }
}
