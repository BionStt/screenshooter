using System;
using ScreenShooter.Gun.Extensions;

namespace ScreenShooter.Gun.Core
{
    public class PdfShot : ShotResult
    {
        public PdfShot(Byte[] bytes, String title)
        {
            Bytes = bytes;
            MimeType = "application/pdf";
            FileName = $"{title.ToLowerInvariant().ToUrlStandard()}.pdf";
        }
    }
}