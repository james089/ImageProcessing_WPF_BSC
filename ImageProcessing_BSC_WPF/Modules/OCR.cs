﻿using System;
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
using OpenCV_BSC_dll;

namespace ImageProcessing_BSC_WPF.Modules
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
            GV.mMainWindow.lbl_OCR.Content = detectedOCRString + " [" + timeSpent.ToString("#") + " ms]";
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

            Image<Bgr, byte> cleanedInputImage = inputImage.Copy();

            //cleanedInputImage = filterNoise(cleanedInputImage, 200);

            try
            {
                using (Image<Gray, byte> gray = cleanedInputImage.Convert<Gray, Byte>())
                {
                    _ocr.Recognize(gray);
                    Tesseract.Charactor[] charactors = _ocr.GetCharactors();
                    foreach (Tesseract.Charactor c in charactors)
                    {
                        if (c.Region.Width * c.Region.Height < 100)
                        {
                            cleanedInputImage.Draw(c.Region, new Bgr(Color.Red), 1);
                        }
                        else
                            cleanedInputImage.Draw(c.Region, drawColor, 1);

                    }

                    ProcessedImage = cleanedInputImage;

                    //String text = String.Concat( Array.ConvertAll(charactors, delegate(Tesseract.Charactor t) { return t.Text; }) );
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
