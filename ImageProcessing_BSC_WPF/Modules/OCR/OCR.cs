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
using Utilities_BSC_dll_x64;
using OpenCV_BSC_dll_x64;

namespace ImageProcessing_BSC_WPF.Modules.OCR
{
    public enum OCRMode
    {
        NUMBERS,
        COMBINED
    }
    public class OCR
    {
        public static string detectedOCRString;

        private static Tesseract _ocr;
        private static double timeSpent;
        public static BackgroundWorker OCRRoutine = new BackgroundWorker();
        public static Rectangle croppedOCRArea;
        public static Image<Bgr, byte> croppedOriginalImg;

        public static void OCRSetup(OCRMode mode)
        {
            string dir = Environment.CurrentDirectory + @"\Modules\BarcodeDecoder\tessdata";

            switch (mode)
            {
                case OCRMode.NUMBERS:
                    _ocr = new Tesseract(dir, "eng", Tesseract.OcrEngineMode.OEM_TESSERACT_ONLY);
                    _ocr.SetVariable("tessedit_char_whitelist", "1234567890");
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
                Windows.main.lbl_OCR.Content = "";
                OCRRoutine.RunWorkerAsync();
            }
        }

        private static void OCRRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Windows.main.ibOriginal.Source = Converter.ToBitmapSource(GV.imgProcessed);
            Windows.main.lbl_OCR.Content = detectedOCRString + " [" + timeSpent.ToString("#") + " ms]";
        }

        private static void OCRRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private static void OCRRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            if (croppedOCRArea.Width * croppedOCRArea.Height != 0) croppedOriginalImg = GV.imgOriginal.Copy(croppedOCRArea);
            else croppedOriginalImg = GV.imgOriginal;

            detectedOCRString = OCRDetect(croppedOriginalImg, out GV.imgProcessed);
            timeSpent = (DateTime.Now - startTime).TotalMilliseconds;

        }

        /// <summary>
        /// Overload
        /// </summary>
        /// <param name="inputImage"></param>
        /// <returns></returns>
        public static string OCRDetect(Image<Bgr, byte> inputImage)
        {
            Image<Bgr, byte> ProcessedImage = inputImage;
            return OCRDetect(inputImage, out ProcessedImage);
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

            Image<Bgr, byte> localImage = inputImage.Copy();
            
            try
            {
                using (Image<Gray, byte> gray = localImage.Convert<Gray, Byte>())
                {
                    _ocr.Recognize(gray);

                    Tesseract.Charactor[] charactors = _ocr.GetCharactors();

                    /*
                    // This will filter the charactors that are too small. But noise is going to affect the accuracy already
                    List<Tesseract.Charactor> charactorsList = new List<Tesseract.Charactor>();
                    for (int i = 0; i < charactors.Length; i++)
                    {
                        if (charactors[i].Region.Width * charactors[i].Region.Height < 100)   //Too Small
                        {
                            localImage.Draw(charactors[i].Region, new Bgr(Color.Red), 1);
                        }
                        else
                        {
                            localImage.Draw(charactors[i].Region, drawColor, 1);
                            charactorsList.Add(charactors[i]);
                        }
                    }

                    Tesseract.Charactor[] newCharactors = new Tesseract.Charactor[charactorsList.Count];
                    for (int i = 0; i < newCharactors.Length; i++)
                    {
                        newCharactors[i] = charactorsList[i];
                    }
                    
                    */
                    foreach (Tesseract.Charactor c in charactors)
                    {
                        if (c.Region.Width * c.Region.Height < 200)
                        {
                            localImage.Draw(c.Region, new Bgr(Color.Red), 1);
                            //ShapeNDraw.drawString(c.Cost.ToString(), cleanedInputImage, new Point(c.Region.X, c.Region.Y), 0.3, Color.Red);
                        }
                        else
                        {
                            localImage.Draw(c.Region, drawColor, 1);
                            //ShapeNDraw.drawString(c.Cost.ToString(), cleanedInputImage, new Point(c.Region.X, c.Region.Y), 0.3, Color.Red);
                        }
                    }

                    ProcessedImage = localImage;

                    //string text = String.Concat( Array.ConvertAll(newCharactors, delegate(Tesseract.Charactor t) { return t.Text; }) );
                    string text = _ocr.GetText();
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
