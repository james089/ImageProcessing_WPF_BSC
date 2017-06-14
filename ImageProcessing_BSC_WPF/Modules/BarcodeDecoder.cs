using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Utilities_BSC_dll;
using ZXing;
using ZXing.Common;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// A barcode reader which accepts an Image instance from EmguCV
    /// </summary>
    internal class BarcodeReaderImage : BarcodeReaderGeneric<Image<Emgu.CV.Structure.Bgr, byte>>, IBarcodeReaderImage
    {
        private static readonly Func<Image<Emgu.CV.Structure.Bgr, byte>, LuminanceSource> defaultCreateLuminanceSource =
           (image) => new ImageLuminanceSource(image);

        public BarcodeReaderImage()
           : base(null, defaultCreateLuminanceSource, null)
        {
        }
    }
    /// <summary>
    /// The interface for a barcode reader which accepts an Image instance from EmguCV
    /// </summary>
    internal interface IBarcodeReaderImage : IBarcodeReaderGeneric<Image<Emgu.CV.Structure.Bgr, byte>>
    {
    }
    /// <summary>
    /// A luminance source class which consumes a Image from EmguCV and calculates the luminance values based on the bytes of the image
    /// </summary>
    internal class ImageLuminanceSource : BaseLuminanceSource
    {
        public ImageLuminanceSource(Image<Emgu.CV.Structure.Bgr, byte> image)
           : base(image.Size.Width, image.Size.Height)
        {
            var bytes = image.Bytes;
            for (int indexB = 0, indexL = 0; indexB < bytes.Length; indexB += 3, indexL++)
            {
                var b = bytes[indexB];
                var g = bytes[indexB + 1];
                var r = bytes[indexB + 2];
                // Calculate luminance cheaply, favoring green.
                luminances[indexL] = (byte)((r + g + g + b) >> 2);
            }
        }

        protected ImageLuminanceSource(byte[] luminances, int width, int height)
           : base(luminances, width, height)
        {
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
        {
            return new ImageLuminanceSource(newLuminances, width, height);
        }
    }
    /// <summary>
    /// This is the module to detect QR code\ Barcode\ all codes using Zxing.dll
    /// </summary>
    public class BarcodeDecoder
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

        public static void decoderSetup()
        {
            //reader = new BarcodeReaderImage();
            reader = new BarcodeReader(null,
            bitmap => new BitmapLuminanceSource(bitmap),
            luminance => new GlobalHistogramBinarizer(luminance));

            //reader.AutoRotate = true;
            reader.Options.TryHarder = true;
            reader.Options.PossibleFormats = new List<BarcodeFormat>();
            reader.Options.PossibleFormats.Add(BarcodeFormat.CODE_93);

            decodeRoutine.DoWork += new DoWorkEventHandler(decodeRoutine_doWork);
            decodeRoutine.ProgressChanged += new ProgressChangedEventHandler(decodeRoutine_ProgressChanged);
            decodeRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(decodeRoutine_WorkerCompleted);
            decodeRoutine.WorkerReportsProgress = true;
        }

        public static void decoding()
        {
            detectCode(GV.imgOriginal.Convert<Gray, byte>(), decodeMode.SINGLE, out outputStringList, out loc);
        }

        #region Decode Routine

        public static void startDecodeRoutine()
        {
            if (!decodeRoutine.IsBusy)
            {
                GV.mMainWindow.listBox.Items.Clear();
                decodeRoutine.RunWorkerAsync();
            }
        }

        private static void decodeRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (loc.Count != 0)
            {
                Image<Bgr, Byte> processed = ImgToDecode;
                //drawDecodeResultInImg(processed);  //can't draw on a roated bitmap...

                GV.mMainWindow.ibOriginal.Source = Converter.ToBitmapSource(processed.Convert<Bgr, Byte>().Rotate(_angle, new Bgr(Color.Black), false));
            }
            else
            {
                GV.mMainWindow.listBox.Items.Add("Fail to find any result");
            }
            s = 0;
        }

        private static void decodeRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (s == 0 || s % 5 == 0)                                            //skip the repeated angle (5 rounds of check in each angle)
                GV.mMainWindow.listBox.Items.Add("Checked angle: " + _angle);
            s++;

            GV.mMainWindow.TB_progress.Text = e.ProgressPercentage.ToString() + "%";
            GV.mMainWindow.progressBar_decode.Value = e.ProgressPercentage;

            if (GV.mMainWindow.listBox.Items.Count != 0)
                GV.mMainWindow.listBox.ScrollIntoView(GV.mMainWindow.listBox.Items[GV.mMainWindow.listBox.Items.Count - 1]);
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
            /*
            //Angle -5~-30 check
            for (_angle = -_angleIncrement; _angle >= -30f; _angle -= _angleIncrement)
            {
                o_img_rotate = ImgToDecode.Convert<Bgr, Byte>().Rotate(_angle, new Bgr(0, 0, 0), false).Convert<Gray, Byte>();
                for (int i = 0; i < _rounds; i++)
                {
                    detectCode(o_img_rotate.ToBitmap(), decodeMode.single, out output, out loc);
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
            */
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
                    GV.mMainWindow.listBox.Items.Add(outputStringList[i] + "\n" + loc[i]);
                }
            }
        }
        */

    }
}
