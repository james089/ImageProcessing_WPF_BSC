using Emgu.CV;
using Emgu.CV.Structure;
using OpenCV_BSC_dll;
using OpenCV_BSC_dll.FeatureDetection;
using OpenCV_BSC_dll.ObjectDetection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// Quick standard CV Functions
    /// </summary>
    public class NCVFuns
    {
        public static Image<Bgr, byte> Detection(Image<Bgr, byte> originalImage, DetectionType DT, out ErrorCode Err)
        {
            Err = ErrorCode.Normal;
            Image<Bgr, byte> b = null;
            switch (DT)
            {
                case DetectionType.Feature:
                    switch (GV._featureType)
                    {
                        case featureDetectionType.cannyEdge:
                            b = ContourDetection.cannyEdges(originalImage, false).Convert<Bgr, Byte>(); break;
                        case featureDetectionType.contour:
                            b = ContourDetection.contourDetection(originalImage); break;
                        case featureDetectionType.line:
                            b = LineDetection.lineDetection(originalImage); break;
                    }
                    break;
                case DetectionType.Object:
                    if (GV.imgOriginal != null && GV.object_img != null)
                    {
                        Image<Bgr, byte> outPutImg = GV.imgOriginal;
                        switch (GV._objectType)
                        {
                            case objectDetectionType.FFT:
                                if (!FFT.searchObject_FFT(GV.imgOriginal, GV.object_img, out outPutImg))
                                    Err = ErrorCode.SearchFFT_Fail;
                                b = outPutImg; break;
                            case objectDetectionType.color:
                                if (!ColorDetect.Color_detection(GV.imgOriginal, GV.object_img, out outPutImg))
                                    Err = ErrorCode.SearchColor_Fail;
                                b = outPutImg; break;
                        }
                    }
                    else if (GV.object_img == null)
                    {
                        Err = ErrorCode.No_object_image;
                    }

                    break;
            }
            return b;
        }
    }
}
