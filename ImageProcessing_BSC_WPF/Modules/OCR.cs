using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Emgu.Util;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System.ComponentModel;
using System.IO;
using Utilities_BSC_dll;

namespace ImageProcessing_BSC_WPF.Modules
{
    public enum OCRMode
    {
        NUMBERS,
        COMBINED
    }
    public class OCR
    {
        private static Tesseract _ocr;
        private static double timeSpent;
        public static BackgroundWorker OCRRoutine = new BackgroundWorker();
        public static Rectangle croppedOCRArea;
        public static Image<Bgr, byte> croppedOriginalImg;

        public static void OCRSetup(OCRMode mode)
        {
            string dir = Environment.CurrentDirectory + "\\tessdata";

            switch (mode)
            {
                case OCRMode.NUMBERS:
                    _ocr = new Tesseract(dir, "eng", Tesseract.OcrEngineMode.OEM_TESSERACT_ONLY, "1234567890");
                    break;
                case OCRMode.COMBINED:
                    _ocr = new Tesseract(dir, "eng", Tesseract.OcrEngineMode.OEM_TESSERACT_CUBE_COMBINED);
                    break;
            }
            

            OCRRoutine.DoWork += new DoWorkEventHandler(OCRRoutine_doWork);
            OCRRoutine.ProgressChanged += new ProgressChangedEventHandler(OCRRoutine_ProgressChanged);
            OCRRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OCRRoutine_WorkerCompleted);
            OCRRoutine.WorkerReportsProgress = true;
        }

        public static void startOCRRoutine()
        {
            if (!OCRRoutine.IsBusy)
            {
                GV.mMainWindow.lbl_OCR.Content = "";
                OCRRoutine.RunWorkerAsync();
            }
        }

        private static void OCRRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GV.mMainWindow.ibOriginal.Source = Converter.ToBitmapSource(GV.imgProcessed);
            GV.mMainWindow.lbl_OCR.Content = GV.detectedOCR + " [" + timeSpent.ToString("#") + " ms]";
        }

        private static void OCRRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private static void OCRRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            if (croppedOCRArea.Width * croppedOCRArea.Height != 0) croppedOriginalImg = GV.imgOriginal.Copy(croppedOCRArea);
            else croppedOriginalImg = GV.imgOriginal;

            GV.detectedOCR = OCRDetect(croppedOriginalImg, out GV.imgProcessed);
            timeSpent = (DateTime.Now - startTime).TotalMilliseconds;
        }

        /// <summary>
        /// Processed Image is the one with some rectangles
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="ProcessedImage"></param>
        /// <returns></returns>
        public static string OCRDetect(Image<Bgr, byte> inputImage, out Image<Bgr, byte> ProcessedImage)
        {
            ProcessedImage = inputImage;
            Bgr drawColor = new Bgr(Color.Blue);
            try
            {
                Image<Bgr, Byte> image = inputImage;

                using (Image<Gray, byte> gray = image.Convert<Gray, Byte>())
                {
                    _ocr.Recognize(gray);
                    Tesseract.Charactor[] charactors = _ocr.GetCharactors();
                    foreach (Tesseract.Charactor c in charactors)
                    {
                        image.Draw(c.Region, drawColor, 1);
                    }

                    ProcessedImage = image;

                    //String text = String.Concat( Array.ConvertAll(charactors, delegate(Tesseract.Charactor t) { return t.Text; }) );
                    String text = _ocr.GetText();
                    return text;
                }
            }
            catch (Exception exception)
            {
                return "NULL";
            }
        }

        public static string OCRDetect(Bitmap inputImage)
        {
            try
            {
                Image<Bgr, Byte> image = new Image<Bgr, byte>(inputImage);

                using (Image<Gray, byte> gray = image.Convert<Gray, Byte>())
                {
                    _ocr.Recognize(gray);

                    String text = _ocr.GetText();
                    return text;
                }
            }
            catch (Exception exception)
            {
                return "NULL";
            }
        }


    }
}
