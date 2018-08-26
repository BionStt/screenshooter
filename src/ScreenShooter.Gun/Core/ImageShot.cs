using System;
using System.Drawing;
using System.Drawing.Imaging;
using ScreenShooter.Gun.Extensions;

namespace ScreenShooter.Gun.Core
{
    public class ImageShot : ShotResult
    {
        public ImageShot(Image image,
                         String title,
                         String imageType)
        {
            switch (imageType)
            {
                case "png":
                case ".png":
                {
                    Bytes = image.ToByteArray(ImageFormat.Png);
                    MimeType = "image/png";
                    FileName = $"{title.ToLowerInvariant().ToUrlStandard()}.png";
                    break;
                }
                case "jpg":
                case ".jpg":
                {
                    Bytes = image.ToByteArray(ImageFormat.Jpeg);
                    MimeType = "image/jpeg";
                    FileName = $"{title.ToLowerInvariant().ToUrlStandard()}.jpg";
                    break;
                }
            }
        }
    }
}