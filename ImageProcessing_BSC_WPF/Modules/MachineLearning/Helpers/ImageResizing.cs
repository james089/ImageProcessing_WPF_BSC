using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers
{
    public class ImageResizing
    {
        private static BackgroundWorker ResizingRoutine = new BackgroundWorker();

        static string ImgDir;
        static string SaveDir;
        static int DesWidth;
        static int DesHeight;
        static int TotalImages;
        static bool IsDeleteOriginal;
        private static int CurrentImageIndex = 0;

        public static void ImageResizingSetup()
        {
            ResizingRoutine.DoWork += new DoWorkEventHandler(ResizingRoutine_doWork);
            ResizingRoutine.ProgressChanged += new ProgressChangedEventHandler(ResizingRoutine_ProgressChanged);
            ResizingRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ResizingRoutine_WorkerCompleted);
            ResizingRoutine.WorkerReportsProgress = true;
            ResizingRoutine.WorkerSupportsCancellation = true;
        }

        public static void ImageBatchResizing(string _imgDir, string _saveDir, int _desWidth, int _desHeight)
        {
            ImageBatchResizing(_imgDir, _saveDir, _desWidth, _desHeight, false);
        }

        /// <summary>
        /// This will save image with
        /// </summary>
        /// <param name="_imgDir"></param>
        /// <param name="_saveDir"></param>
        /// <param name="_desWidth"></param>
        /// <param name="_desHeight"></param>
        /// <param name="isDeleteOriginal"></param>
        public static void ImageBatchResizing(string _imgDir, string _saveDir, int _desWidth, int _desHeight, bool _isDeleteOriginal)
        {
            ImgDir = _imgDir;
            SaveDir = _saveDir;
            DesWidth = _desWidth;
            DesHeight = _desHeight;
            IsDeleteOriginal = _isDeleteOriginal;

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

                if(IsDeleteOriginal)
                    File.Delete(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));

                ResizingRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / TotalImages));
                //Thread.Sleep(1);

                CurrentImageIndex++;
            }
        }

    }
}
