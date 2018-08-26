using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenShooter.Gun.Extensions
{
    internal static class ImageExtension
    {
        internal static MemoryStream ToMemoryStream(this Image image, ImageFormat format)
        {
            var stream = new MemoryStream();
            image.Save(stream, format);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        internal static MemoryStream ToMemoryStream(this Image image)
        {
            return image.ToMemoryStream(ImageFormat.Bmp);
        }
        
        internal static Byte[] ToByteArray(this Image image, ImageFormat format)
        {
            return image.ToMemoryStream(format)
                        .ToArray();
        }
    }
}