using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using OpenCV_BSC_dll_x64;
using OpenCV_BSC_dll_x64.FeatureDetection;
using OpenCV_BSC_dll_x64.ObjectDetection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF.Modules
{
    /// <summary>
    /// Quick standard CV Functions
    /// </summary>
    public class NCVFuns
    {
        public static DetectionType _detectionType;
        public static featureDetectionType _featureType;
        public static objectDetectionType _objectType;

        public static Image<Bgr, byte> Detection(Image<Bgr, byte> originalImage, DetectionType DT, out ErrorCode Err)
        {
            Err = ErrorCode.Normal;
            Image<Bgr, byte> b = null;
            switch (DT)
            {
                case DetectionType.Feature:
                    switch (_featureType)
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
                        switch (_objectType)
                        {
                            case objectDetectionType.SURF:
                                if (!SURF.SearchObject_SURF(GV.imgOriginal.Convert<Gray, byte>(), GV.object_img.Convert<Gray, byte>(), out outPutImg))
                                    Err = ErrorCode.SearchSURF_Fail;
                                else
                                    BindManager.BindMngr.GMessage.value = "Found using SURF";
                                b = outPutImg; break;
                            case objectDetectionType.FFT:
                                if (!FFT.searchObject_FFT(GV.imgOriginal, GV.object_img, out outPutImg))
                                    Err = ErrorCode.SearchFFT_Fail;
                                else
                                    BindManager.BindMngr.GMessage.value = "Found using FFT";
                                b = outPutImg; break;
                            case objectDetectionType.color:
                                if (!ColorDetection2.Color_detection(GV.imgOriginal, GV.object_img, out outPutImg, Parameters._colorTolerance))
                                    Err = ErrorCode.SearchColor_Fail;
                                else
                                {
                                    //outPutImg = ContourDetection.contourDetection(outPutImg);
                                    PointF[] pts = FindWhitePoints(outPutImg.Convert<Gray, byte>());

                                    MCvBox2D box = SquareFittingWithAngle(pts);
                                    outPutImg.Draw(box, new Bgr(Color.Green), 2);
                                    BindManager.BindMngr.GMessage.value = $"Displaying matching colors. Angle [{box.angle}deg]";
                                }
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


        public static PointF[] FindWhitePoints(Image<Gray, byte> img)
        {
            var points = new List<PointF>();
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (img.Data[j, i, 0] == 255)

                        points.Add(new PointF(i, j));
                }
            }
            return points.ToArray();
        }

        //public static PointF[] ReduceNoise(PointF[] whitePoints, Image<Gray, byte> img)
        //{
        //    for (int i = 0; i < img.Width; i++)
        //    {
        //        for (int j = 0; j < img.Height; j++)
        //        {
        //            if()
        //        }
        //    }
        //}


        /// <summary>
        /// Fit an ellipse to the points collection
        /// </summary>
        /// <param name="points">The points to be fitted</param>
        /// <returns>An ellipse</returns>
        public static Ellipse EllipseLeastSquareFitting(PointF[] points)
        {
            IntPtr seq = Marshal.AllocHGlobal(StructSize.MCvSeq);
            IntPtr block = Marshal.AllocHGlobal(StructSize.MCvSeqBlock);
            GCHandle handle = GCHandle.Alloc(points, GCHandleType.Pinned);
            CvInvoke.cvMakeSeqHeaderForArray(
               CvInvoke.CV_MAKETYPE((int)MAT_DEPTH.CV_32F, 2),
               StructSize.MCvSeq,
               StructSize.PointF,
               handle.AddrOfPinnedObject(),
               points.Length,
               seq,
               block);
            Ellipse e = new Ellipse(CvInvoke.cvFitEllipse2(seq));
            handle.Free();
            Marshal.FreeHGlobal(seq);
            Marshal.FreeHGlobal(block);
            return e;
        }

        public static Rectangle SquareFitting(PointF[] points)
        {
            MCvBox2D box = PointCollection.MinAreaRect(points);
            return box.MinAreaRect();
        }

        public static MCvBox2D SquareFittingWithAngle(PointF[] points)
        {
            MCvBox2D box = PointCollection.MinAreaRect(points);
                return box;
        }

    }
}
