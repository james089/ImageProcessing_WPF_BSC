using mUserControl_BSC_dll.UserControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageProcessing_BSC_WPF.Modules.CortexDecoder.CortexDecoderFunctions;

namespace ImageProcessing_BSC_WPF.Modules.CortexDecoder
{
    public class CortexDecoder
    {
        public static CortexDecoderFunctions mCortexDecoder;
        public static bool IsCortexReady;
        public static string ResultString;
        public static Point ResultCenter;
        public static CortexResult FullResult;
        public static Rectangle BondRec;

        public static void DecoderSetup()
        {
            mCortexDecoder = new CortexDecoderFunctions();
            if (mCortexDecoder.Initialize() <= 0)
            {
                mMessageBox.Show("Could not get handle");
                IsCortexReady = false;
                return;
            }
            IsCortexReady = true;
        }

        public static void Decode(Bitmap bmp)
        {
            if (bmp == null) return;
            if (!IsCortexReady) return;

            try
            {
                mCortexDecoder.Decode(bmp);
            }
            catch (Exception)
            {
                ResultString = "-Decode Error-";
                return;
            }
            FullResult = mCortexDecoder.GetResult();
            ResultString = FullResult.decodeData;
            ResultString = (ResultString == null || ResultString == "") ? ResultString = "NULL" : ResultString;
            ResultCenter = FullResult.center;
            BondRec = new Rectangle(FullResult.corner0.X, FullResult.corner0.Y, 
                FullResult.corner1.X - FullResult.corner0.X, FullResult.corner2.Y - FullResult.corner0.Y);
        }

    }
}
