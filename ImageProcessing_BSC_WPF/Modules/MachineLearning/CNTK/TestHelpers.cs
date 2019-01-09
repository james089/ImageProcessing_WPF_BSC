using CNTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.CNTK
{
    public enum Activation
    {
        None,
        ReLU,
        Sigmoid,
        Tanh
    }
    public class TestHelper
    {
        public static List<int> ValidateModelWithSingleImage(
               string modelFile, MinibatchSource testMinibatchSource,
               int[] imageDim, int numClasses, string featureInputName, string labelInputName, string outputName,
               DeviceDescriptor device, Bitmap inputImage)
        {
            Function model = Function.Load(modelFile, device);
            var imageInput = model.Arguments[0];
            var labelOutput = model.Outputs.Single(o => o.Name == outputName);

            var featureStreamInfo = testMinibatchSource.StreamInfo(featureInputName);
            var labelStreamInfo = testMinibatchSource.StreamInfo(labelInputName);

            int batchSize = 50;
            int miscountTotal = 0, totalCount = 0;

            var minibatchData = testMinibatchSource.GetNextMinibatch((uint)batchSize, device);

            totalCount += (int)minibatchData[featureStreamInfo].numberOfSamples;

            // expected lables are in the minibatch data.
            var labelData = minibatchData[labelStreamInfo].data.GetDenseData<float>(labelOutput);
            var expectedLabels = labelData.Select(l => l.IndexOf(l.Max())).ToList();

            var inputDataMap = new Dictionary<Variable, Value>() {
                    { imageInput, minibatchData[featureStreamInfo].data }
                };

            var outputDataMap = new Dictionary<Variable, Value>() {
                    { labelOutput, null }
                };

            model.Evaluate(inputDataMap, outputDataMap, device);
            var outputData = outputDataMap[labelOutput].GetDenseData<float>(labelOutput);
            var actualLabels = outputData.Select(l => l.IndexOf(l.Max())).ToList();

            //int misMatches = actualLabels.Zip(expectedLabels, (a, b) => a.Equals(b) ? 0 : 1).Sum();
            //miscountTotal += misMatches;
            //Console.WriteLine($"Validating Model: Total Samples = {totalCount}, Misclassify Count = {miscountTotal}");

            //float errorRate = 1.0F * miscountTotal / totalCount;
            //Console.WriteLine($"Model Validation Error = {errorRate}");
            //return errorRate;
            return actualLabels;
        }


        public static float ValidateModelWithMinibatchSource(
               string modelFile, MinibatchSource testMinibatchSource,
               int[] imageDim, int numClasses, string featureInputName, string labelInputName, string outputName,
               DeviceDescriptor device, int maxCount = 1000)
        {
            Function model = Function.Load(modelFile, device);
            var imageInput = model.Arguments[0];
            var labelOutput = model.Outputs.Single(o => o.Name == outputName);

            var featureStreamInfo = testMinibatchSource.StreamInfo(featureInputName);
            var labelStreamInfo = testMinibatchSource.StreamInfo(labelInputName);

            int batchSize = 50;
            int miscountTotal = 0, totalCount = 0;
            while (true)
            {
                var minibatchData = testMinibatchSource.GetNextMinibatch((uint)batchSize, device);
                if (minibatchData == null || minibatchData.Count == 0)
                    break;
                totalCount += (int)minibatchData[featureStreamInfo].numberOfSamples;

                // expected lables are in the minibatch data.
                var labelData = minibatchData[labelStreamInfo].data.GetDenseData<float>(labelOutput);
                var expectedLabels = labelData.Select(l => l.IndexOf(l.Max())).ToList();

                var inputDataMap = new Dictionary<Variable, Value>() {
                    { imageInput, minibatchData[featureStreamInfo].data }
                };

                var outputDataMap = new Dictionary<Variable, Value>() {
                    { labelOutput, null }
                };

                model.Evaluate(inputDataMap, outputDataMap, device);
                var outputData = outputDataMap[labelOutput].GetDenseData<float>(labelOutput);
                var actualLabels = outputData.Select(l => l.IndexOf(l.Max())).ToList();

                int misMatches = actualLabels.Zip(expectedLabels, (a, b) => a.Equals(b) ? 0 : 1).Sum();

                miscountTotal += misMatches;
                Console.WriteLine($"Validating Model: Total Samples = {totalCount}, Misclassify Count = {miscountTotal}");

                if (totalCount > maxCount)
                    break;
            }

            float errorRate = 1.0F * miscountTotal / totalCount;
            Console.WriteLine($"Model Validation Error = {errorRate}");
            return errorRate;
        }

        public static Function Dense(Variable input, int outputDim, DeviceDescriptor device,
            Activation activation = Activation.None, string outputName = "")
        {
            if (input.Shape.Rank != 1)
            {
                // 
                int newDim = input.Shape.Dimensions.Aggregate((d1, d2) => d1 * d2);
                input = CNTKLib.Reshape(input, new int[] { newDim });
            }

            Function fullyConnected = FullyConnectedLinearLayer(input, outputDim, device, outputName);
            switch (activation)
            {
                default:
                case Activation.None:
                    return fullyConnected;
                case Activation.ReLU:
                    return CNTKLib.ReLU(fullyConnected);
                case Activation.Sigmoid:
                    return CNTKLib.Sigmoid(fullyConnected);
                case Activation.Tanh:
                    return CNTKLib.Tanh(fullyConnected);
            }
        }

        public static Function FullyConnectedLinearLayer(Variable input, int outputDim, DeviceDescriptor device,
            string outputName = "")
        {
            System.Diagnostics.Debug.Assert(input.Shape.Rank == 1);
            int inputDim = input.Shape[0];

            int[] s = { outputDim, inputDim };
            var timesParam = new Parameter((NDShape)s, DataType.Float,
                CNTKLib.GlorotUniformInitializer(
                    CNTKLib.DefaultParamInitScale,
                    CNTKLib.SentinelValueForInferParamInitRank,
                    CNTKLib.SentinelValueForInferParamInitRank, 1),
                device, "timesParam");
            var timesFunction = CNTKLib.Times(timesParam, input, "times");

            int[] s2 = { outputDim };
            var plusParam = new Parameter(s2, 0.0f, device, "plusParam");
            return CNTKLib.Plus(plusParam, timesFunction, outputName);
        }

        
        public static bool MiniBatchDataIsSweepEnd(ICollection<MinibatchData> minibatchValues)
        {
            return minibatchValues.Any(a => a.sweepEnd);
        }

        public static void PrintTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();
                Console.WriteLine($"Minibatch: {minibatchIdx} CrossEntropyLoss = {trainLossValue}, EvaluationCriterion = {evaluationValue}");
            }
        }

    }
}
