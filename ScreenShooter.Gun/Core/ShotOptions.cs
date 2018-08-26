using System;

namespace ScreenShooter.Gun.Core
{
    public class ShotOptions
    {
        public ShotOptions()
        {
            OverlaySize = 20;
            Height = 1024;
            Width = 754;
            IsFullPage = true;
            ImageFormat = "png";
        }

        public Uri Uri { get; set; }
        public Boolean TryMobile { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public Int32 OverlaySize { get; set; }
        public Boolean IsFullPage { get; set; }
        public String ImageFormat { get; set; }
        public Boolean IsGrayscale { get; set; }
    }
}