using ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YoloSharp;
using static ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI.ImageLabelTool_Yolo;

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

        #region Training Model
        private BackgroundWorker trainModelRoutine = new BackgroundWorker();
        public void TrainModelRoutineSetup()
        {
            trainModelRoutine.DoWork += new DoWorkEventHandler(trainModelRoutine_doWork);
            trainModelRoutine.ProgressChanged += new ProgressChangedEventHandler(trainModelRoutine_ProgressChanged);
            trainModelRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(trainModelRoutine_WorkerCompleted);
            trainModelRoutine.WorkerReportsProgress = true;
            trainModelRoutine.WorkerSupportsCancellation = true;
        }

        ProcessStartInfo processInfo;
        Process process;
        public void TrainModel()
        {
            if (!mLabelTool.CheckRequiredFiles()) return;
            
            string temp = mLabelTool.DarknetTrainCmd();

            temp = "/c " + temp; 
            processInfo = new ProcessStartInfo("cmd.exe", temp);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            process = Process.Start(processInfo);

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            Console.WriteLine("output>>" + e.Data);
            //BindManager.BindMngr.CmdWindowString.value += e.Data + "\n";

            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            Console.WriteLine("error>>" + e.Data);
            //BindManager.BindMngr.CmdWindowString.value += e.Data + "\n";
            process.BeginErrorReadLine();

            process.WaitForExit();

            Console.WriteLine();
            process.Close();

            trainModelRoutine.RunWorkerAsync();

        }

        private void trainModelRoutine_doWork(object sender, DoWorkEventArgs e)
        {
           
        }

        private void trainModelRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void trainModelRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        
        #endregion Training Model
    }
}
