using CNTK;
using Emgu.CV;
using Emgu.CV.Structure;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers;
using mUserControl_BSC_dll.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK
{
  
    class ResNet
    {
        public static bool IsModelLoaded = false;

        public static string OutputString;
        public static double OutputProbablility;
        public static List<double> resultList = new List<double>();


        private static Image<Bgr, byte> _ImgOriginal;
        private static Image<Gray, byte> _ImgOriginal_mono;
        private static DeviceDescriptor device;               //GPU to Use
        private static Function modelFunc;

        private static double timeSpent;
        
        private static BackgroundWorker MLRoutine = new BackgroundWorker();


        public static void CNTK_ResNetSetup()
        {
            device = DeviceDescriptor.GPUDevice(0);

            MLRoutine.DoWork += new DoWorkEventHandler(MLRoutine_doWork);
            MLRoutine.ProgressChanged += new ProgressChangedEventHandler(MLRoutine_ProgressChanged);
            MLRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MLRoutine_WorkerCompleted);
            MLRoutine.WorkerReportsProgress = true;
        }

        public static void startMLRoutine()
        {
            if (!MLRoutine.IsBusy)
            {
                MLRoutine.RunWorkerAsync();
            }
        }

        private static void MLRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private static void MLRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case (int)ErrCode.ModelNotExists:
                    mMessageBox.Show("Model not exists"); break;
                case (int)ErrCode.LoadingError:
                    mMessageBox.Show("Cannot load model"); break;
            }
        }

        private static void MLRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            EvaluationSingleImage(GV.imgOriginal);
        }


        /// <summary>
        /// This is a one time process
        /// </summary>
        /// <param name="device"></param>
        public static void LoadModel(string modelFile)
        {
            // Load the model.
            string modelFilePath = GV.ML_Folders[(int)MLFolders.ML_CNTK_model] + "\\" + modelFile;

            if (!File.Exists(modelFilePath))
            {
                mMessageBox.Show("Model not exists");
                return;
            }

            try
            {
                modelFunc = Function.Load(modelFilePath, device);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
                IsModelLoaded = false;
            }
            IsModelLoaded = true;
        }

        /// <summary>
        /// The example shows
        /// - how to load model.
        /// - how to prepare input data for a single sample.
        /// - how to prepare input and output data map.
        /// - how to evaluate a model.
        /// - how to retrieve evaluation result and retrieve output data in dense format.
        /// </summary>
        /// <param name="device">Specify on which device to run the evaluation.</param>
        public static void EvaluationSingleImage(Image<Bgr, byte> Img)
        {
            if (Img == null) return;

            DateTime startTime = DateTime.Now;
            if (!IsModelLoaded)
            {
                MLRoutine.ReportProgress((int)ErrCode.LoadingError);
                return;
            }
            _ImgOriginal = Img.Copy();
            try
            {
                // Get input variable. The model has only one single input.
                // The same way described above for output variable can be used here to get input variable by name.
                Variable inputVar = modelFunc.Arguments.Single();

                // Get shape data for the input variable
                NDShape inputShape = inputVar.Shape;
                int imageWidth = inputShape[0];
                int imageHeight = inputShape[1];
                int imageChannels = inputShape[2];
                int imageSize = inputShape.TotalSize;

                // The model has only one output.
                // If the model have more than one output, use the following way to get output variable by name.
                // Variable outputVar = modelFunc.Outputs.Where(variable => string.Equals(variable.Name, outputName)).Single();
                Variable outputVar = modelFunc.Output;

                var inputDataMap = new Dictionary<Variable, Value>();
                var outputDataMap = new Dictionary<Variable, Value>();

                Bitmap bmp = _ImgOriginal.CopyBlank().ToBitmap();
                if (_ImgOriginal == null)
                {
                    mNotification.Show("No image");
                    return;
                }
                else
                    bmp = _ImgOriginal.Copy().ToBitmap();

                var resized = bmp.Resize((int)imageWidth, (int)imageHeight, true);
                List<float> resizedImgDataList = new List<float>();

                if (imageChannels == 3)
                {
                    resizedImgDataList = resized.ParallelExtractCHW();
                }
                else if (imageChannels == 1)
                {
                    resized = (new Image<Bgr, byte>(resized)).Convert<Gray, byte>().ToBitmap();
                    resizedImgDataList = resized.ExtractMono();
                }

                // Create input data map
                var inputVal = Value.CreateBatch(inputVar.Shape, resizedImgDataList, device);
                inputDataMap.Add(inputVar, inputVal);

                // Create ouput data map. Using null as Value to indicate using system allocated memory.
                // Alternatively, create a Value object and add it to the data map.
                outputDataMap.Add(outputVar, null);

                // Start evaluation on the device
                modelFunc.Evaluate(inputDataMap, outputDataMap, device);

                // Get evaluate result as dense output
                var outputVal = outputDataMap[outputVar];
                var outputData = outputVal.GetDenseData<float>(outputVar);
                
                timeSpent = (DateTime.Now - startTime).TotalMilliseconds;
                //printOutput(outputVar.Shape.TotalSize, outputData);
                showResult(outputVar.Shape.TotalSize, outputData, OutputProbablility);
            }
            catch (Exception ex)
            {
                //MainWindow.mMainWindow.listBox.Items.Add("Error: {0}\nCallStack: {1}\n Inner Exception: {2}");
                throw ex;
            }
        }

        private static int predictResult<T>(int sampleSize, IList<IList<T>> outputBuffer, out double probability)
        {
            resultList.Clear();
            probability = 0;
            int outputSampleSize = sampleSize;
            // This is for 1 image
            foreach (var seq in outputBuffer)
            {
                foreach (var element in seq)
                {
                    resultList.Add(Convert.ToDouble(element));
                }
            }
            int maxIndex = resultList.IndexOf(resultList.Max());
            probability = resultList.Max();                          //Get the max probablitity value
            return maxIndex;
        }


        private static void showResult<T>(int sampleSize, IList<IList<T>> outputBuffer, double outputValue)
        {
            int predictedIndex = 0;

            predictedIndex = predictResult<T>(sampleSize, outputBuffer, out outputValue);
            OutputString = DataSet.LabelSet[MLCore.MLTrainedDataSetSelectedIndex][predictedIndex];
            OutputProbablility = outputValue;
        }



        
    }
}
