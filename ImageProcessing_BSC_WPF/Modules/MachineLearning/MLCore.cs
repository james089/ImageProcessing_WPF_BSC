using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning
{
    public enum JobType
    {
        train,
        test
    }

    public enum ErrCode
    {
        ModelNotExists,
        LoadingError
    }

    public enum MLModel
    {
        ResNet,
        FastRCNN
    }

    public struct DataSet
    {
        static string[] CIFAR10 = new string[] { "airplane", "automobile", "bird", "cat", "deer", "dog", "frog", "horse", "ship", "truck" };
        static string[] Custom1 = new string[] { "crown", "bag", "cat" };

        public static List<string[]> labelSet = new List<string[]>() { CIFAR10, Custom1 };
    }

    public class MLCore
    {
        public static string[] MLSelectedLabels;
        public static int MLTrainedDataSetSelectedIndex;
        public static MLModel MLModelSelected;

        public static ErrCode ErrorCode;
    }
}
