using System;

namespace ScreenShooter.Gun.Core
{
    public class ShotOptions
    {
        public ShotOptions()
        {
            OverlaySize = 20;
            StepHeight = 1024;
            Width = 754;
            ImageFormat = "png";
            HideOverlayElementsImmediate = true;
        }

        public Uri Uri { get; set; }
        public Boolean TryMobile { get; set; }
        public Int32 Width { get; set; }
        public Int32 StepHeight { get; set; }
        public Int32 OverlaySize { get; set; }
        public String ImageFormat { get; set; }
        public Boolean IsGrayscale { get; set; }
        public Boolean HideOverlayElementsImmediate { get; set; }
    }
}