using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    class mMath
    {
        public static double Distance3D(byte[] p1, byte[] p2)
        {
            return Math.Sqrt(Math.Pow(p1[0] - p2[0], 2) + Math.Pow(p1[1] - p2[1], 2) + Math.Pow(p1[2] - p2[2], 2));
        }

        public static double Distance2D(byte[] p1, byte[] p2)
        {
            return Math.Sqrt(Math.Pow(p1[0] - p2[0], 2) + Math.Pow(p1[1] - p2[1], 2));
        }
    }
}
