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

    public enum DataSet
    {
        CIFAR10,
        Bag
    }

    public enum CIFAR10
    {
        airplane,
        automobile,
        bird,
        cat,
        deer,
        dog,
        frog,
        horse,
        ship,
        truck
    }

    public enum Bag
    {
        notBag,
        bag
    }

    public class MLCore
    {
        public static DataSet MLTrainedDataSetSelected;
        public static MLModel MLModelSelected;

        public static ErrCode ErrorCode;
    }
}
