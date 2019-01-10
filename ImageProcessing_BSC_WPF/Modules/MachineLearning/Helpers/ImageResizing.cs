using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK;
using Utilities_BSC_dll_x64;

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
        static bool IsKeepRatio;
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
            ImageBatchResizing(_imgDir, _saveDir, _desWidth, _desHeight, false, false);
        }

        /// <summary>
        /// This will save image with
        /// </summary>
        /// <param name="_imgDir"></param>
        /// <param name="_saveDir"></param>
        /// <param name="_desWidth"></param>
        /// <param name="_desHeight"></param>
        /// <param name="isDeleteOriginal"></param>
        public static void ImageBatchResizing(string _imgDir, string _saveDir, int _desWidth, int _desHeight, bool _isDeleteOriginal, bool _isKeepRatio)
        {
            ImgDir = _imgDir;
            SaveDir = _saveDir;
            DesWidth = _desWidth;
            DesHeight = _desHeight;
            IsDeleteOriginal = _isDeleteOriginal;
            IsKeepRatio = _isKeepRatio;

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
            FileInfo[] ImageInfo = Folder.GetFiles().OrderBy(p => p.Name).ToArray();
            TotalImages = ImageInfo.Length;

            for (int i = 0; i < TotalImages; i++)
            {
                Bitmap bm = new Bitmap(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));
                Bitmap rbm = bm;
                if (IsKeepRatio)
                {
                    Image<Bgr, byte> grayBackground = new Image<Bgr, byte>(DesWidth, DesHeight);
                    grayBackground.SetValue(new Bgr(Color.Gray));   // Set background color

                    if (((double)bm.Width / (double)bm.Height) < ((double)DesWidth / (double)DesHeight))
                    {
                        int rbm_w = bm.Width * (DesHeight / bm.Height);
                        rbm = CntkBitmapExtensions.Resize(bm, rbm_w, DesHeight, true);

                        Image<Bgr, byte> rbm_emgu = new Image<Bgr, byte>(rbm);

                        rbm = ImgStiching.combine(grayBackground, rbm_emgu, new Point((DesWidth - rbm_w) / 2, 0)).ToBitmap();
                    }
                    else
                    {
                        int rbm_h = bm.Height * (DesWidth / bm.Width);
                        rbm = CntkBitmapExtensions.Resize(bm, DesWidth, rbm_h, true);

                        Image<Bgr, byte> rbm_emgu = new Image<Bgr, byte>(rbm);

                        rbm = ImgStiching.combine(grayBackground, rbm_emgu, new Point(0, (DesHeight - rbm_h) / 2)).ToBitmap();
                    }
                }
                else
                    rbm = CntkBitmapExtensions.Resize(bm, DesWidth, DesHeight, true);

                if (IsDeleteOriginal)
                    File.Delete(String.Format(@"{0}\{1}", ImgDir, ImageInfo[i].Name));

                rbm.Save(SaveDir + $"\\res_{i:D5}.jpg");         // This will make it "00000" "00001"...

                bm.Dispose();
                rbm.Dispose();


                ResizingRoutine.ReportProgress(Convert.ToInt32((i + 1) * 100 / TotalImages));
                //Thread.Sleep(1);

                CurrentImageIndex++;
            }
        }

    }
}
