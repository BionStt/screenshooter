using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ScreenShooter.Gun.Core;

namespace ScreenShooter.Web.Requests
{
    public class ImageRequest
    {
        [FromQuery(Name = "url")]
        [Url]
        public String Url { get; set; }

        [FromQuery(Name = "w")]
        public Int32 Width { get; set; }

        [FromQuery(Name = "f")]
        public String Format { get; set; }

        [FromQuery(Name = "m")]
        public Boolean TryMobileVersion { get; set; }

        public ShotOptions ToShotOptions()
        {
            return new ShotOptions
                   {
                       Uri = new Uri(Url),
                       StepHeight = 500,
                       Width = Width,
                       ImageFormat = Format,
                       TryMobile = TryMobileVersion
                   };
        }
    }
}