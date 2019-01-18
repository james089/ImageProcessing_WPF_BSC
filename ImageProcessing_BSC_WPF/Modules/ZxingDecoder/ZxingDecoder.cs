using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Utilities_BSC_dll_x64;
using ZXing;
using ZXing.Common;

namespace ImageProcessing_BSC_WPF.Modules.ZxingDecoder
{
    public enum decodeMode
    {
        SINGLE,
        MULTIPLE
    }
    
    /// <summary>
    /// This is the module to detect QR code\ Barcode\ all codes using Zxing.dll
    /// </summary>
    public class ZxingDecoder
    {
        // Decode region
        public static BarcodeReader reader;
        public static List<string> outputStringList;
        public static List<System.Windows.Point> loc;
        public static Result[] result;
        public static Image<Gray, byte> o_img_rotate;

        static int s = 0;
        static double _angle;
        static Image<Bgr, byte> ImgToDecode;

        public static BackgroundWorker decodeRoutine = new BackgroundWorker();

        public static void DecoderSetup()
        {
            //reader = new BarcodeReaderImage();
            reader = new BarcodeReader(null,
            bitmap => new BitmapLuminanceSource(bitmap),
            luminance => new GlobalHistogramBinarizer(luminance));

            //reader.AutoRotate = true;
            reader.Options.TryHarder = true;
            reader.Options.PossibleFormats = new List<BarcodeFormat>();
            reader.Options.PossibleFormats.Add(BarcodeFormat.CODE_93);
            reader.Options.PossibleFormats.Add(BarcodeFormat.CODE_128);
            reader.Options.PossibleFormats.Add(BarcodeFormat.All_1D);
            reader.Options.PossibleFormats.Add(BarcodeFormat.DATA_MATRIX);
            reader.Options.PossibleFormats.Add(BarcodeFormat.MAXICODE);
            reader.Options.PossibleFormats.Add(BarcodeFormat.AZTEC);
            reader.Options.PossibleFormats.Add(BarcodeFormat.QR_CODE);

            decodeRoutine.DoWork += new DoWorkEventHandler(decodeRoutine_doWork);
            decodeRoutine.ProgressChanged += new ProgressChangedEventHandler(decodeRoutine_ProgressChanged);
            decodeRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(decodeRoutine_WorkerCompleted);
            decodeRoutine.WorkerReportsProgress = true;
        }

        /// <summary>
        /// This is for live decoding
        /// </summary>
        public static void Decode(Bitmap img)
        {
            detectCode(new Image<Gray, byte>(img), decodeMode.SINGLE, out outputStringList, out loc);
        }

        #region Decode Routine

        public static void StartDecodeRoutine()
        {
            if (!decodeRoutine.IsBusy)
            {
                MainWindow.mMainWindow.listBox.Items.Clear();
                decodeRoutine.RunWorkerAsync();
            }
        }

        private static void decodeRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (loc.Count != 0)
            {
                Image<Bgr, Byte> processed = ImgToDecode;
                //drawDecodeResultInImg(processed);  //can't draw on a roated bitmap...

                MainWindow.mMainWindow.ibOriginal.Source = ImgConverter.ToBitmapSource(processed.Convert<Bgr, Byte>().Rotate(_angle, new Bgr(Color.Black), false));
            }
            else
            {
                MainWindow.mMainWindow.listBox.Items.Add("Fail to find any result");
            }
            s = 0;
        }

        private static void decodeRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (s == 0 || s % 5 == 0)                                            //skip the repeated angle (5 rounds of check in each angle)
                MainWindow.mMainWindow.listBox.Items.Add("Checked angle: " + _angle);
            s++;

            MainWindow.mMainWindow.TB_progress.Text = e.ProgressPercentage.ToString() + "%";
            MainWindow.mMainWindow.progressBar.Value = e.ProgressPercentage;

            if (MainWindow.mMainWindow.listBox.Items.Count != 0)
                MainWindow.mMainWindow.listBox.ScrollIntoView(MainWindow.mMainWindow.listBox.Items[MainWindow.mMainWindow.listBox.Items.Count - 1]);
        }

        private static void decodeRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            int _progress = 0;
            ImgToDecode = GV.imgOriginal.Copy();
            // 
            int _rounds = 5;
            double _angleIncrement = 5;
            _angle = 0;
            //Angle -30~30deg check:0, 5, -5, 10, -10 .....
            for (; _angle <= 30f; _angle += _angleIncrement)
            {
                o_img_rotate = ImgToDecode.Rotate(_angle, new Bgr(0, 0, 0), false).Convert<Gray, Byte>();
                for (int i = 0; i < _rounds; i++)
                {
                    detectCode(o_img_rotate, decodeMode.SINGLE, out outputStringList, out loc);
                    _progress += 2;
                    if (_progress > 100)
                        _progress = 100;
                    decodeRoutine.ReportProgress(_progress);
                    if (loc.Count != 0)
                    {
                        _progress = 100;
                        decodeRoutine.ReportProgress(_progress);
                        return;
                    }
                }
                _angle = -_angle;
                if (_angle != 0)
                {
                    o_img_rotate = ImgToDecode.Convert<Bgr, Byte>().Rotate(_angle, new Bgr(0, 0, 0), false).Convert<Gray, Byte>();
                    for (int i = 0; i < _rounds; i++)
                    {
                        detectCode(o_img_rotate, decodeMode.SINGLE, out outputStringList, out loc);
                        _progress += 2;
                        if (_progress > 100)
                            _progress = 100;
                        decodeRoutine.ReportProgress(_progress);
                        if (loc.Count != 0)
                        {
                            _progress = 100;
                            decodeRoutine.ReportProgress(_progress);
                            return;
                        }
                    }
                }
                _angle = -_angle;
            }
            
        }

        #endregion Decode Routine

        private static void detectCode(Image<Gray, Byte> bitmap, decodeMode mode, out List<string> outputStr, out List<System.Windows.Point> location)
        {
            outputStr = new List<string>();
            location = new List<System.Windows.Point>();
            //var bitmap = new Image<Bgr, byte>(img);
            result = null;

            if (mode == decodeMode.SINGLE)
            {
                //Single code
                result = new Result[1];
                if (reader.Decode(bitmap.ToBitmap()) != null)
                    result[0] = reader.Decode(bitmap.ToBitmap());//.Bytes, imgWidth, imgHeight, RGBLuminanceSource.BitmapFormat.Unknown);
                if (result[0] != null)
                {
                    location.Add(new System.Windows.Point(result[0].ResultPoints[0].X, result[0].ResultPoints[0].Y));
                    outputStr.Add("Content: " + result[0].Text + "\n" + result[0].BarcodeFormat.ToString());
                }
                else
                    outputStr.Add("NULL");
            }
            else
            {
                //Multiple codes
                try
                {
                    result = reader.DecodeMultiple(bitmap.ToBitmap());
                }
                catch (Exception)
                {
                    ;
                }
                if (result != null)
                {
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (result[i] != null)
                        {
                            location.Add(new System.Windows.Point(result[i].ResultPoints[0].X, result[i].ResultPoints[0].Y));
                            outputStr.Add("Content: " + result[i].Text + "\n" + result[i].BarcodeFormat.ToString());
                        }
                        else
                            outputStr.Add("NULL");
                    }
                }
            }
        }

        /*
        public static void drawDecodeResultInImg(Image<Bgr, Byte> imgToDraw)
        {
            GV.mGraphics = Graphics.FromImage(imgToDraw.ToBitmap());
            if (result != null && outputStringList.Count != 0 && loc.Count != 0)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    ShapeNDraw.drawString(outputStringList[i], (int)loc[i].X, (int)loc[i].Y + 50, Color.Red, 24, imgToDraw);
                    MainWindow.mMainWindow.listBox.Items.Add(outputStringList[i] + "\n" + loc[i]);
                }
            }
        }
        */

    }
}
