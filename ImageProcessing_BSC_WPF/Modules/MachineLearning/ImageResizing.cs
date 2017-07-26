using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning
{
    public class ImageResizing
    {
        private static BackgroundWorker ResizingRoutine = new BackgroundWorker();

        static string ImgDir;
        static string SaveDir;
        static int DesWidth;
        static int DesHeight;
        static int TotalImages;
        private static int CurrentImageIndex = 0;


        //public static void ImageBatchResizing(string _imgDir, string _saveDir, int _desWidth, int _desHeight)
        //{

        //    ImgDir = _imgDir;
        //    SaveDir = _saveDir;
        //    DesWidth = _desWidth;
        //    DesHeight = _desHeight;

        //    if (!Directory.Exists(ImgDir)) Directory.CreateDirectory(ImgDir);
        //    if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);

        //    DirectoryInfo Folder = new DirectoryInfo(ImgDir);
        //    FileInfo[] ImageInfo = Folder.GetFiles();
        //    TotalImages = ImageInfo.Length;

        //    for (int i = 0; i < TotalImages; i++)
        //    {
        //        Bitmap bm = new Bitmap(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));
        //        Bitmap rbm = CntkBitmapExtensions.Resize(bm, DesWidth, DesHeight, true);
        //        rbm.Save(SaveDir + string.Format("\\{0:D5}.jpg", i));         // This will make it "00000" "00001"...

        //        bm.Dispose();
        //        rbm.Dispose();

        //        Windows.main.Dispatcher.Invoke(() => {
        //            BindManager.BindMngr.Progress.value = Convert.ToInt32((i + 1) * 100 / TotalImages);
        //            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";
        //        });
        //        Thread.Sleep(100);

        //        CurrentImageIndex++;
        //    }

        //}


        public static void ImageBatchResizing(string _imgDir, string _saveDir, int _desWidth, int _desHeight)
        {
            ResizingRoutine.DoWork += new DoWorkEventHandler(ResizingRoutine_doWork);
            ResizingRoutine.ProgressChanged += new ProgressChangedEventHandler(ResizingRoutine_ProgressChanged);
            ResizingRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ResizingRoutine_WorkerCompleted);
            ResizingRoutine.WorkerReportsProgress = true;
            ResizingRoutine.WorkerSupportsCancellation = true;

            ImgDir = _imgDir;
            SaveDir = _saveDir;
            DesWidth = _desWidth;
            DesHeight = _desHeight;

            if (!Directory.Exists(ImgDir)) Directory.CreateDirectory(ImgDir);
            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);

            //List<Bitmap> imagesList = new List<Bitmap>();


            if (!ResizingRoutine.IsBusy)
                ResizingRoutine.RunWorkerAsync();


        }

        private static void ResizingRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BindManager.BindMngr.GMessage.value = "Resizing complete.";
            BindManager.BindMngr.Progress.value = 100;
            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";

            TotalImages = 0;
            CurrentImageIndex = 0;
        }

        private static void ResizingRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BindManager.BindMngr.Progress.value = e.ProgressPercentage;
            BindManager.BindMngr.ProgressString.value = BindManager.BindMngr.Progress.value + "%";

            BindManager.BindMngr.GMessage.value = string.Format("Resizing {0} images...({1})", 
                TotalImages - CurrentImageIndex,
                BindManager.BindMngr.ProgressString.value);
        }

        private static void ResizingRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo Folder = new DirectoryInfo(ImgDir);
            FileInfo[] ImageInfo = Folder.GetFiles();
            TotalImages = ImageInfo.Length;

            for (int i = 0; i < TotalImages; i++)
            {
                Bitmap bm = new Bitmap(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));
                Bitmap rbm = CntkBitmapExtensions.Resize(bm, DesWidth, DesHeight, true);
                rbm.Save(SaveDir + string.Format("\\{0:D5}.jpg", i));         // This will make it "00000" "00001"...

                bm.Dispose();
                rbm.Dispose();
                ResizingRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / TotalImages));
                Thread.Sleep(1);

                CurrentImageIndex++;
            }
        }

    }
}
