using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ScreenShooter.Gun.Core;

namespace ScreenShooter.Web.Requests
{
    public class PdfRequest
    {
        [FromQuery(Name = "url")]
        [Url]
        public String Url { get; set; }

        [FromQuery(Name = "w")]
        public Int32 Width { get; set; }

        [FromQuery(Name = "h")]
        public Int32 Height { get; set; }

        [FromQuery(Name = "f")]
        public String Format { get; set; }
        
        [FromQuery(Name = "g")]
        public Boolean IsGrayscale { get; set; }
        
        [FromQuery(Name = "m")]
        public Boolean TryMobileVersion { get; set; }
        
        public ShotOptions ToShotOptions()
        {
            return new ShotOptions
                   {
                       Uri = new Uri(Url),
                       StepHeight = Height,
                       Width = Width,
                       ImageFormat = Format,
                       IsGrayscale = IsGrayscale,
                       TryMobile = TryMobileVersion
                   };
        }
    }
}