using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MSR.CNTK.Extensibility.Managed;
using System.Drawing;
using ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning
{
    class FastRCNN
    {
        private static string initialDirectory;
        /// <summary>
        /// This method shows how to evaluate a trained FastR-CNN object detection model
        /// </summary>
        public static void EvaluateObjectDetectionModel()
        {
            initialDirectory = @"C:\local\cntk";
            try
            {
                // This example requires the Fast-RCNN_grocery100 model.
                // The model can be downloaded from <see cref="https://www.cntk.ai/Models/FRCN_Grocery/Fast-RCNN_grocery100.model"/>
                // The model is assumed to be located at: <CNTK>\Examples\Image\PretrainedModels\
                // It further requires the grocery image data set. 
                // Please run 'python install_fastrcnn.py' from <cntkroot>\Examples\Image\Detection\FastRCNN to get the data.
                //string imageDirectory = Path.Combine(initialDirectory, @"\Examples\Image\DataSets\grocery\testImages");
                //string modelDirectory = Path.Combine(initialDirectory, @"\Examples\Image\PretrainedModels");

                string imageDirectory = (initialDirectory + @"\Examples\Image\DataSets\grocery\testImages");
                string modelDirectory = (initialDirectory + @"\Examples\Image\PretrainedModels");
                Environment.CurrentDirectory = initialDirectory;

                List<float> outputs;

                using (var model = new IEvaluateModelManagedF())
                {
                    string modelFilePath = Path.Combine(modelDirectory, "Fast-RCNN_grocery100.model");
                    ThrowIfFileNotExist(modelFilePath,
                        string.Format("Error: The model '{0}' does not exist. Please download the model from https://www.cntk.ai/Models/FRCN_Grocery/Fast-RCNN_grocery100.model " +
                                      "and save it under ..\\..\\Examples\\Image\\PretrainedModels.", modelFilePath));

                    model.CreateNetwork(string.Format("modelPath=\"{0}\"", modelFilePath), deviceId: -1);

                    // Prepare input value in the appropriate structure and size
                    var inDims = model.GetNodeDimensions(NodeGroup.Input);
                    if (inDims.First().Value != 1000 * 1000 * 3)
                    {
                        throw new CNTKRuntimeException(string.Format("The input dimension for {0} is {1} which is not the expected size of {2}.", inDims.First(), inDims.First().Value, 1000 * 1000 * 3), string.Empty);
                    }

                    // Transform the image
                    string imageFileName = Path.Combine(imageDirectory, "WIN_20160803_11_28_42_Pro.jpg");
                    ThrowIfFileNotExist(imageFileName, string.Format("Error: The test image file '{0}' does not exist.", imageFileName));

                    Bitmap bmp = new Bitmap(Bitmap.FromFile(imageFileName));
                    // TODO: preserve aspect ratio while scaling and pad the remaining pixels with (114, 114, 114)
                    var resized = bmp.Resize(1000, 1000, true);
                    var resizedCHW = resized.ParallelExtractCHW();

                    // TODO: generate ROI proposals using an external library, e.g. selective search, 
                    // TODO: project them to the 1000 x 1000 image size and compute (x, y, w, h) relative to the image dimensions.
                    // TODO: Alternative workaround: run script 'A1_GenerateInputROIs.py' from <cntkroot>\Examples\Image\Detection\FastRCNN and read rois from file.

                    // parse rois: groups of 4 floats corresponding to (x, y, w, h) for an ROI
                    string roiCoordinates = "0.219 0.0 0.165 0.29 0.329 0.025 0.07 0.115 0.364 0.0 0.21 0.13 0.484 0.0 0.075 0.06 0.354 0.045 0.055 0.09 0.359 0.075 0.095 0.07 0.434 0.155 0.04 0.085 0.459 0.165 0.145 0.08 0.404 0.12 0.055 0.06 0.714 0.235 0.06 0.12 0.659 0.31 0.065 0.075 0.299 0.16 0.1 0.07 0.449 0.18 0.19 0.15 0.284 0.21 0.135 0.115 0.254 0.205 0.07 0.055 0.234 0.225 0.075 0.095 0.239 0.23 0.07 0.085 0.529 0.235 0.075 0.13 0.229 0.24 0.09 0.085 0.604 0.285 0.12 0.105 0.514 0.335 0.1 0.045 0.519 0.335 0.08 0.045 0.654 0.205 0.08 0.055 0.614 0.215 0.115 0.065 0.609 0.205 0.115 0.075 0.604 0.225 0.115 0.055 0.524 0.23 0.06 0.095 0.219 0.315 0.065 0.075 0.629 0.31 0.095 0.08 0.639 0.325 0.085 0.06 0.219 0.41 0.25 0.11 0.354 0.46 0.185 0.11 0.439 0.515 0.09 0.075 0.359 0.455 0.175 0.125 0.449 0.525 0.08 0.07 0.574 0.46 0.06 0.105 0.579 0.46 0.105 0.1 0.529 0.47 0.15 0.145 0.584 0.475 0.085 0.09 0.354 0.52 0.08 0.06 0.219 0.52 0.115 0.1 0.229 0.53 0.1 0.08 0.229 0.575 0.105 0.045 0.339 0.56 0.085 0.045 0.354 0.535 0.075 0.06 0.299 0.59 0.145 0.05 0.304 0.58 0.12 0.045 0.594 0.555 0.075 0.05 0.534 0.58 0.14 0.06 0.504 0.66 0.07 0.06 0.494 0.73 0.075 0.09 0.504 0.695 0.07 0.095 0.219 0.665 0.075 0.145 0.494 0.755 0.085 0.075 0.704 0.665 0.07 0.21 0.434 0.72 0.055 0.1 0.569 0.695 0.205 0.185 0.219 0.73 0.29 0.13 0.574 0.665 0.08 0.055 0.634 0.665 0.095 0.045 0.499 0.725 0.08 0.135 0.314 0.71 0.155 0.065 0.264 0.72 0.19 0.105 0.264 0.725 0.185 0.095 0.249 0.725 0.12 0.11 0.379 0.77 0.08 0.055 0.509 0.785 0.055 0.06 0.644 0.875 0.13 0.085 0.664 0.875 0.11 0.075 0.329 0.025 0.08 0.115 0.639 0.235 0.135 0.15 0.354 0.46 0.185 0.12 0.354 0.46 0.185 0.135 0.229 0.225 0.08 0.095 0.219 0.72 0.29 0.14 0.569 0.67 0.205 0.21 0.219 0.315 0.1 0.075 0.219 0.23 0.09 0.085 0.219 0.41 0.295 0.11 0.219 0.665 0.27 0.145 0.219 0.225 0.09 0.14 0.294 0.665 0.2 0.05 0.579 0.46 0.105 0.145 0.549 0.46 0.14 0.145 0.219 0.41 0.295 0.125 0.219 0.59 0.11 0.05 0.639 0.235 0.135 0.155 0.629 0.235 0.145 0.155 0.314 0.71 0.155 0.115 0.334 0.56 0.09 0.045 0.264 0.72 0.225 0.1 0.264 0.72 0.225 0.105 0.219 0.71 0.29 0.15 0.249 0.725 0.125 0.11 0.219 0.665 0.27 0.17 0.494 0.73 0.075 0.115 0.494 0.73 0.085 0.115 0.219 0.0 0.14 0.14 0.219 0.07 0.14 0.14 0.219 0.14 0.14 0.14";
                    var rois = roiCoordinates.Split(' ').Select(x => float.Parse(x)).ToList();

                    // inputs are the image itself and the ROI coordinates
                    var inputs = new Dictionary<string, List<float>>() { { inDims.First().Key, resizedCHW }, { inDims.Last().Key, rois } };

                    // We can call the evaluate method and get back the results (predictions per ROI and per class (no softmax applied yet!)...
                    var outDims = model.GetNodeDimensions(NodeGroup.Output);
                    outputs = model.Evaluate(inputs, outDims.First().Key);
                }

                // the object classes used in the grocery example
                var labels = new[] {"__background__",
                   "avocado", "orange", "butter", "champagne", "eggBox", "gerkin", "joghurt", "ketchup",
                   "orangeJuice", "onion", "pepper", "tomato", "water", "milk", "tabasco", "mustard"};
                int numLabels = labels.Length;
                int numRois = outputs.Count / numLabels;

                Console.WriteLine("Only showing predictions for non-background ROIs...");
                int numBackgroundRois = 0;
                for (int i = 0; i < numRois; i++)
                {
                    var outputForRoi = outputs.Skip(i * numLabels).Take(numLabels).ToList();

                    // Retrieve the predicted label as the argmax over all predictions for the current ROI
                    var max = outputForRoi.Select((value, index) => new { Value = value, Index = index })
                        .Aggregate((a, b) => (a.Value > b.Value) ? a : b)
                        .Index;

                    if (max > 0)
                    {
                        Console.WriteLine("Outcome for ROI {0}: {1} \t({2})", i, max, labels[max]);
                    }
                    else
                    {
                        numBackgroundRois++;
                    }
                }

                Console.WriteLine("Number of background ROIs: {0}", numBackgroundRois);
            }
            catch (CNTKException ex)
            {
                OnCNTKException(ex);
            }
            catch (Exception ex)
            {
                OnGeneralException(ex);
            }
        }


        /// <summary>
        /// Checks whether the file exists. If not, write the error message on the console and throw FileNotFoundException.
        /// </summary>
        /// <param name="filePath">The file to check.</param>
        /// <param name="errorMsg">The message to write on console if the file does not exist.</param>
        private static void ThrowIfFileNotExist(string filePath, string errorMsg)
        {
            if (!File.Exists(filePath))
            {
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Console.WriteLine(errorMsg);
                }
                throw new FileNotFoundException(string.Format("File '{0}' not found.", filePath));
            }
        }
        /// <summary>
        /// Handle CNTK exceptions.
        /// </summary>
        /// <param name="ex">The exception to be handled.</param>
        private static void OnCNTKException(CNTKException ex)
        {
            // The pattern "Inner Exception" is used by End2EndTests to catch test failure.
            Console.WriteLine("Error: {0}\nNative CallStack: {1}\n Inner Exception: {2}", ex.Message, ex.NativeCallStack, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            throw ex;
        }

        /// <summary>
        /// Handle general exceptions.
        /// </summary>
        /// <param name="ex">The exception to be handled.</param>
        private static void OnGeneralException(Exception ex)
        {
            // The pattern "Inner Exception" is used by End2EndTests to catch test failure.
            Console.WriteLine("Error: {0}\nCallStack: {1}\n Inner Exception: {2}", ex.Message, ex.StackTrace, ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception");
            throw ex;
        }

    }
}
