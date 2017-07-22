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
            StringManager.StrMngr.GMessage.value = "Resizing complete.";
            Windows.main.progressBar.Value = 100;
            Windows.main.TB_progress.Text = "100%";

            TotalImages = 0;
            CurrentImageIndex = 0;
        }

        private static void ResizingRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StringManager.StrMngr.GMessage.value = string.Format("Resizing {0} images...", TotalImages - CurrentImageIndex);
            Windows.main.progressBar.Value = e.ProgressPercentage;
            Windows.main.TB_progress.Text = e.ProgressPercentage + "%";
        }

        private static void ResizingRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo Folder = new DirectoryInfo(ImgDir);
            FileInfo[] ImageInfo = Folder.GetFiles();
            TotalImages = ImageInfo.Length;

            for (int i = 0; i < TotalImages; i++)
            {
                //imagesList.Add(new Bitmap(String.Format(@"{0}\{1}", imgDir, ImageInfo[i].Name)));
                Bitmap bm = new Bitmap(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));
                Bitmap rbm = CntkBitmapExtensions.Resize(bm, DesWidth, DesHeight, true);
                //rbm.Save(saveDir + "\\" + ImageInfo[i].Name);               // Keep original name
                rbm.Save(SaveDir + string.Format("\\{0:D5}.jpg", i));         // This will make it "00000" "00001"...

                bm.Dispose();
                rbm.Dispose();
                ResizingRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / TotalImages));
                Thread.Sleep(100);

                CurrentImageIndex++;
            }
        }
    }
}
