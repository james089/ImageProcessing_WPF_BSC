using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YoloSharp;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.YOLO
{
    class YoloSharpCore
    {
        public static YoloSharpCore mYolo = new YoloSharpCore();

        Bitmap _bitmap = null;
        float _aspectRatio;

        Brush _brush = new SolidBrush(Color.FromArgb(128, 40, 40, 0));

        Yolo _yolo;

        public Tuple<Yolo, float> LoadModel(string modelPath)
        {
            ModelPath model = new ModelPath(modelPath);
            _aspectRatio = model.FixedAspectRatio;
            if (model.Found)
            {
                _yolo = new Yolo(model.ConfigPath, model.WeightsPath, model.NamesPath);
                //string processName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
                //string title = $"{Path.GetFileNameWithoutExtension(model.NamesPath)} - {processName}";
                //this.Text = title;
                //AppendMessage($"{model.ConfigPath},{model.WeightsPath},{model.NamesPath} を読み込みました。\r\n画像を Drag&Drop してください。");
            }
            else
            {
                MessageBox.Show("Missing Required model files", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return Tuple.Create<Yolo, float>(_yolo, _aspectRatio);
        }

        public Bitmap Detect(Bitmap bitmap)
        {
            try
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                }
                using (Bitmap tmp = bitmap)
                {
                    _bitmap = ImageLoader.AddBorder(tmp, _aspectRatio);
                }
                
                
                Stopwatch watch = new Stopwatch();
                watch.Start();
                var result = _yolo.Detect(_bitmap, 0.5f);
                watch.Stop();

                // Draw result
                using (Graphics g = Graphics.FromImage(_bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    float scale = _bitmap.Width / 800f;
                    foreach (var data in result)
                    {
                        Data d = data;
                        Color c = ImageLoader.ConvertHsvToRgb(d.Id * 1.0f / _yolo.ClassNames.Length, 1, 0.8f);

                        Pen pen = new Pen(c, 3f * scale);
                        Font font = new Font(FontFamily.GenericSerif, 20f * scale, System.Drawing.FontStyle.Bold);

                        g.FillRectangle(_brush, d.X, d.Y, d.Width, 35f * scale);
                        g.DrawRectangle(pen, d.X, d.Y, d.Width, d.Height);
                        string status = $"{d.Name} ({d.Confidence * 100:00.0}%)";
                        g.DrawString(status, font, Brushes.White, new PointF(d.X, d.Y + 3f * scale));

                        pen.Dispose();
                        font.Dispose();
                    }
                }
                //// 結果を保存
                //SaveResult(_bitmap, result);
                return _bitmap;
            }
            catch (Exception ex)
            {
                //AppendMessage(ex.Message);
            }
            return bitmap;
        }
    }
}
