using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using mUserControl_BSC_dll;
using System.ComponentModel;
using mUserControl_BSC_dll.UserControls;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers
{
    public class MeanFileGenerator
    {
        static DirectoryInfo Folder;
        static FileInfo[] ImageInfo;
        static string imgDir;
        static string saveDir;

        public static void GenerateConstMeanFile(string _saveDir)
        {
            string[] AvgRGBArrString = new string[3072];
            // Save to arr
            for (int j = 0; j < 3072; j++)
            {
                AvgRGBArrString[j] = string.Format("{0:E2}", 128);
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
        }

        #region Real Mean
        private static BackgroundWorker MeanFileRoutine = new BackgroundWorker();
        public static void GenerateMeanFile(string _imgDir, string _saveDir)
        {
            MeanFileRoutine.DoWork += new DoWorkEventHandler(MeanFileRoutine_doWork);
            MeanFileRoutine.ProgressChanged += new ProgressChangedEventHandler(MeanFileRoutine_ProgressChanged);
            MeanFileRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MeanFileRoutine_WorkerCompleted);
            MeanFileRoutine.WorkerReportsProgress = true;
            MeanFileRoutine.WorkerSupportsCancellation = true;

            imgDir = _imgDir;
            saveDir = _saveDir;

            if (!MeanFileRoutine.IsBusy)
                MeanFileRoutine.RunWorkerAsync();

        }

        private static void MeanFileRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            mNotification.Show("Mean file generated!");
            BindManager.BindMngr.GMessage.value = "Mean file generated.";
        }

        private static void MeanFileRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BindManager.BindMngr.Progress.value = e.ProgressPercentage;
            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";
            BindManager.BindMngr.GMessage.value = "Calculating...";
        }

        private static void MeanFileRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            List<List<float>> RGBList = new List<List<float>>();
            float[] AvgRGBArr;

            Folder = new DirectoryInfo(imgDir);
            ImageInfo = Folder.GetFiles();        //totalImages

            for (int i = 0; i < ImageInfo.Length; i++)
            {
                Bitmap bmp = new Bitmap(String.Format(@"{0}\{1}", imgDir, ImageInfo[i].Name));
                RGBList.Add(CntkBitmapExtensions.ExtractCHW(bmp));
                MeanFileRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / ImageInfo.Length));
            }

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
            string file_xml = saveDir + "\\Custom_mean.xml";
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
        }

        #endregion Real Mean
    }
}
