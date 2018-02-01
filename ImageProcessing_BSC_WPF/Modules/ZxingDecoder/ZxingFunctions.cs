using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace ImageProcessing_BSC_WPF.Modules.ZxingDecoder
{
    /// <summary>
    /// A barcode reader which accepts an Image instance from EmguCV
    /// </summary>
    internal class BarcodeReaderImage : BarcodeReaderGeneric<Image<Emgu.CV.Structure.Bgr, byte>>, IBarcodeReaderImage
    {
        private static readonly Func<Image<Emgu.CV.Structure.Bgr, byte>, LuminanceSource> defaultCreateLuminanceSource =
           (image) => new ImageLuminanceSource(image);

        public BarcodeReaderImage()
           : base(null, defaultCreateLuminanceSource, null)
        {
        }
    }
    /// <summary>
    /// The interface for a barcode reader which accepts an Image instance from EmguCV
    /// </summary>
    internal interface IBarcodeReaderImage : IBarcodeReaderGeneric<Image<Emgu.CV.Structure.Bgr, byte>>
    {
    }
    /// <summary>
    /// A luminance source class which consumes a Image from EmguCV and calculates the luminance values based on the bytes of the image
    /// </summary>
    internal class ImageLuminanceSource : BaseLuminanceSource
    {
        public ImageLuminanceSource(Image<Emgu.CV.Structure.Bgr, byte> image)
           : base(image.Size.Width, image.Size.Height)
        {
            var bytes = image.Bytes;
            for (int indexB = 0, indexL = 0; indexB < bytes.Length; indexB += 3, indexL++)
            {
                var b = bytes[indexB];
                var g = bytes[indexB + 1];
                var r = bytes[indexB + 2];
                // Calculate luminance cheaply, favoring green.
                luminances[indexL] = (byte)((r + g + g + b) >> 2);
            }
        }

        protected ImageLuminanceSource(byte[] luminances, int width, int height)
           : base(luminances, width, height)
        {
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
        {
            return new ImageLuminanceSource(newLuminances, width, height);
        }
    }
}
