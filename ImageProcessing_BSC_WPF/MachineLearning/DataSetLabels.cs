using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.MachineLearning
{
    public enum DataSet
    {
        CIFAR10,
        CIFAR100
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
    public class DataSetLabels
    {
        public static DataSet MachineLearningTrainedDataSet;
    }
}
