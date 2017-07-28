using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.Helpers
{
    public class MeanFileGenerator
    {
        static DirectoryInfo Folder;
        static FileInfo[] ImageInfo;
        static List<Bitmap> BitmapList = new List<Bitmap>();
        static float[] AvgRGBArr;

        public static void GenerateMeanFile(string _imgDir, string _saveDir)
        {
            List<List<float>> RGBList = new List<List<float>>();
            Folder = new DirectoryInfo(_imgDir);
            ImageInfo = Folder.GetFiles();        //totalImages

            for (int i = 0; i < ImageInfo.Length; i++)
            {
                Bitmap bmp = new Bitmap(String.Format(@"{0}\{1}", _imgDir, ImageInfo[i].Name));
                BitmapList.Add(bmp);
                RGBList.Add(CntkBitmapExtensions.ExtractCHW(bmp));
            }

            AvgRGBArr = new float[RGBList[0].Count];                // sum is [1, 3072]
            
            for (int j = 0; j < RGBList[0].Count; j++)
            {
                for (int i = 0; i < RGBList.Count; i++)
                {
                    AvgRGBArr[j] += RGBList[i][j];
                }
            }
            string[] AvgRGBArrString = new string[RGBList[0].Count];
            // Save to arr
            for (int j = 0; j < RGBList[0].Count; j++)
            {
                AvgRGBArr[j] = (AvgRGBArr[j] / RGBList.Count);           // AvgRGBList is [1, 3072]
                AvgRGBArrString[j] = string.Format("{0:E2}", AvgRGBArr[j]);
            }
            string str = String.Join(" ", AvgRGBArrString);
            /*
            // Save to txt file
            string file = _saveDir + "\\meanfile.txt";
            if (!File.Exists(file)) File.Create(file).Dispose();
            StreamWriter sw = new StreamWriter(file);
            sw.Write(str);
            sw.Dispose();
            */
            // Save to xml
            string file_xml = _saveDir + "\\CIFAR-10_mean.xml";
            if (!File.Exists(file_xml)) File.Create(file_xml).Dispose();
            using (StreamWriter sw1 = new StreamWriter(file_xml))
            {
                sw1.WriteLine("<?xml version=\"1.0\" ?>");
                sw1.WriteLine("<opencv_storage>");
                sw1.WriteLine("  <Channel>3</Channel>");
                sw1.WriteLine("  <Row>32</Row>");
                sw1.WriteLine("  <Col>32</Col>");
                sw1.WriteLine("  <MeanImg type_id=\"opencv-matrix\">");
                sw1.WriteLine("    <rows>1</rows>");
                sw1.WriteLine("    <cols>3072</cols>");
                sw1.WriteLine("    <dt>f</dt>");
                sw1.WriteLine("    <data>" + str + "</data>");
                sw1.WriteLine("  </MeanImg>");
                sw1.WriteLine("</opencv_storage>");
            }
        }
    }
}
